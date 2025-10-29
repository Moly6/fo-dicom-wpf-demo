using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace DicomFilmPrinter
{
    public partial class MainWindow : Window
    {
        private List<BitmapSource> _loadedImages = new List<BitmapSource>();
        private int _currentImageIndex = 0;
        private bool _isSingleViewMode = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 打开单个或多个DICOM文件
        /// </summary>
        private void BtnOpenFiles_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "DICOM 文件 (*.dcm)|*.dcm|所有文件 (*.*)|*.*",
                Multiselect = true,
                Title = "选择 DICOM 文件",
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            TxtStatus.Text = "加载中...";
            try
            {
                _loadedImages = Application.Current.Dispatcher.Invoke(
                    () =>
                        DicomService.LoadDicomFilesAsBitmapSources(
                            openFileDialog.FileNames.OrderBy(f => f).ToList()
                        )
                );
                ImagesPanel.ItemsSource = _loadedImages;
                TxtStatus.Text = $"已加载 {_loadedImages.Count} 张图像";

                // 重置到第一张图像
                _currentImageIndex = 0;
                if (_isSingleViewMode)
                {
                    UpdateSingleView();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "加载 DICOM 文件时出错: " + ex.Message,
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                TxtStatus.Text = "加载失败";
            }
        }

        /// <summary>
        /// 打开文件夹，加载文件夹中的所有DICOM文件
        /// </summary>
        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "选择包含 DICOM 文件的文件夹",
            };
            var result = folderDialog.ShowDialog();
            if (result != CommonFileDialogResult.Ok)
                return;

            string folderPath = folderDialog.FileName;

            TxtStatus.Text = "加载中...";
            try
            {
                _loadedImages = Application.Current.Dispatcher.Invoke(
                    () => DicomService.LoadDicomFolderAsBitmapSources(folderPath)
                );
                ImagesPanel.ItemsSource = _loadedImages;
                TxtStatus.Text =
                    $"已从 {Path.GetFileName(folderPath)} 加载 {_loadedImages.Count} 张图像";

                // 重置到第一张图像
                _currentImageIndex = 0;
                if (_isSingleViewMode)
                {
                    UpdateSingleView();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "加载 DICOM 文件时出错: " + ex.Message,
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                TxtStatus.Text = "加载失败";
            }
        }

        /// <summary>
        /// 切换视图模式（网格视图 / 单张连续查看）
        /// </summary>
        private void BtnToggleView_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedImages.Count == 0)
            {
                MessageBox.Show(
                    "未加载图像，请先打开文件或文件夹",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            _isSingleViewMode = !_isSingleViewMode;

            if (_isSingleViewMode)
            {
                // 切换到单张查看模式
                GridViewPanel.Visibility = Visibility.Collapsed;
                SingleViewPanel.Visibility = Visibility.Visible;
                BtnToggleView.Content = "切换到网格视图";

                // 初始化单张查看
                _currentImageIndex = 0;
                UpdateSingleView();
            }
            else
            {
                // 切换到网格视图模式
                SingleViewPanel.Visibility = Visibility.Collapsed;
                GridViewPanel.Visibility = Visibility.Visible;
                BtnToggleView.Content = "切换到单张查看";
            }
        }

        /// <summary>
        /// 更新单张查看模式的显示
        /// </summary>
        private void UpdateSingleView()
        {
            if (_loadedImages.Count == 0)
                return;

            // 确保索引在有效范围内
            _currentImageIndex = Math.Max(0, Math.Min(_currentImageIndex, _loadedImages.Count - 1));

            // 应用淡入动画
            var fadeInStoryboard = (Storyboard)this.Resources["FadeInAnimation"];
            //SingleImage.BeginStoryboard(fadeInStoryboard);

            // 更新图像
            SingleImage.Source = _loadedImages[_currentImageIndex];

            // 更新信息和控件状态
            TxtImageInfo.Text = $"{_currentImageIndex + 1} / {_loadedImages.Count}";
            ImageSlider.Maximum = _loadedImages.Count;
            ImageSlider.Value = _currentImageIndex + 1;

            // 更新按钮状态
            BtnPrevious.IsEnabled = _currentImageIndex > 0;
            BtnNext.IsEnabled = _currentImageIndex < _loadedImages.Count - 1;
        }

        /// <summary>
        /// 上一张按钮点击事件
        /// </summary>
        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_currentImageIndex > 0)
            {
                _currentImageIndex--;
                UpdateSingleView();
            }
        }

        /// <summary>
        /// 下一张按钮点击事件
        /// </summary>
        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentImageIndex < _loadedImages.Count - 1)
            {
                _currentImageIndex++;
                UpdateSingleView();
            }
        }

        /// <summary>
        /// 滑块值变化事件
        /// </summary>
        private void ImageSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e
        )
        {
            if (!_isSingleViewMode || _loadedImages.Count == 0)
                return;

            int newIndex = (int)e.NewValue - 1;
            if (newIndex != _currentImageIndex)
            {
                _currentImageIndex = newIndex;
                UpdateSingleView();
            }
        }

        /// <summary>
        /// 鼠标滚轮事件（在单张查看模式下切换图像）
        /// </summary>
        private void SingleView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                // 向上滚动，显示上一张
                if (_currentImageIndex > 0)
                {
                    _currentImageIndex--;
                    UpdateSingleView();
                }
            }
            else if (e.Delta < 0)
            {
                // 向下滚动，显示下一张
                if (_currentImageIndex < _loadedImages.Count - 1)
                {
                    _currentImageIndex++;
                    UpdateSingleView();
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// 键盘事件处理（左右箭头切换图像）
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_isSingleViewMode || _loadedImages.Count == 0)
                return;

            switch (e.Key)
            {
                case Key.Left:
                case Key.Up:
                    // 上一张
                    if (_currentImageIndex > 0)
                    {
                        _currentImageIndex--;
                        UpdateSingleView();
                    }
                    e.Handled = true;
                    break;

                case Key.Right:
                case Key.Down:
                    // 下一张
                    if (_currentImageIndex < _loadedImages.Count - 1)
                    {
                        _currentImageIndex++;
                        UpdateSingleView();
                    }
                    e.Handled = true;
                    break;

                case Key.Home:
                    // 第一张
                    _currentImageIndex = 0;
                    UpdateSingleView();
                    e.Handled = true;
                    break;

                case Key.End:
                    // 最后一张
                    _currentImageIndex = _loadedImages.Count - 1;
                    UpdateSingleView();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// 打印当前页面（直接打印窗口的可视元素）
        /// </summary>
        private void BtnPrintCurrentPage_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedImages.Count == 0)
            {
                MessageBox.Show(
                    "未加载图像，请先打开文件或文件夹",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            try
            {
                PrintService.PrintCurrentPageVisual(this, "DICOM 胶片打印 - 当前页面");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "打印失败: " + ex.Message,
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 打印自定义文档（创建FlowDocument进行打印）
        /// </summary>
        private void BtnPrintCustomDoc_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedImages.Count == 0)
            {
                MessageBox.Show(
                    "未加载图像，请先打开文件或文件夹",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            try
            {
                PrintService.PrintCustomDocument(
                    this,
                    "DICOM 胶片打印 - 自定义文档",
                    _loadedImages
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "打印失败: " + ex.Message,
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}
