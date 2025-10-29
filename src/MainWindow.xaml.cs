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
        /// �򿪵�������DICOM�ļ�
        /// </summary>
        private void BtnOpenFiles_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "DICOM �ļ� (*.dcm)|*.dcm|�����ļ� (*.*)|*.*",
                Multiselect = true,
                Title = "ѡ�� DICOM �ļ�",
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            TxtStatus.Text = "������...";
            try
            {
                _loadedImages = Application.Current.Dispatcher.Invoke(
                    () =>
                        DicomService.LoadDicomFilesAsBitmapSources(
                            openFileDialog.FileNames.OrderBy(f => f).ToList()
                        )
                );
                ImagesPanel.ItemsSource = _loadedImages;
                TxtStatus.Text = $"�Ѽ��� {_loadedImages.Count} ��ͼ��";

                // ���õ���һ��ͼ��
                _currentImageIndex = 0;
                if (_isSingleViewMode)
                {
                    UpdateSingleView();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "���� DICOM �ļ�ʱ����: " + ex.Message,
                    "����",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                TxtStatus.Text = "����ʧ��";
            }
        }

        /// <summary>
        /// ���ļ��У������ļ����е�����DICOM�ļ�
        /// </summary>
        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "ѡ����� DICOM �ļ����ļ���",
            };
            var result = folderDialog.ShowDialog();
            if (result != CommonFileDialogResult.Ok)
                return;

            string folderPath = folderDialog.FileName;

            TxtStatus.Text = "������...";
            try
            {
                _loadedImages = Application.Current.Dispatcher.Invoke(
                    () => DicomService.LoadDicomFolderAsBitmapSources(folderPath)
                );
                ImagesPanel.ItemsSource = _loadedImages;
                TxtStatus.Text =
                    $"�Ѵ� {Path.GetFileName(folderPath)} ���� {_loadedImages.Count} ��ͼ��";

                // ���õ���һ��ͼ��
                _currentImageIndex = 0;
                if (_isSingleViewMode)
                {
                    UpdateSingleView();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "���� DICOM �ļ�ʱ����: " + ex.Message,
                    "����",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                TxtStatus.Text = "����ʧ��";
            }
        }

        /// <summary>
        /// �л���ͼģʽ��������ͼ / ���������鿴��
        /// </summary>
        private void BtnToggleView_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedImages.Count == 0)
            {
                MessageBox.Show(
                    "δ����ͼ�����ȴ��ļ����ļ���",
                    "��ʾ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            _isSingleViewMode = !_isSingleViewMode;

            if (_isSingleViewMode)
            {
                // �л������Ų鿴ģʽ
                GridViewPanel.Visibility = Visibility.Collapsed;
                SingleViewPanel.Visibility = Visibility.Visible;
                BtnToggleView.Content = "�л���������ͼ";

                // ��ʼ�����Ų鿴
                _currentImageIndex = 0;
                UpdateSingleView();
            }
            else
            {
                // �л���������ͼģʽ
                SingleViewPanel.Visibility = Visibility.Collapsed;
                GridViewPanel.Visibility = Visibility.Visible;
                BtnToggleView.Content = "�л������Ų鿴";
            }
        }

        /// <summary>
        /// ���µ��Ų鿴ģʽ����ʾ
        /// </summary>
        private void UpdateSingleView()
        {
            if (_loadedImages.Count == 0)
                return;

            // ȷ����������Ч��Χ��
            _currentImageIndex = Math.Max(0, Math.Min(_currentImageIndex, _loadedImages.Count - 1));

            // Ӧ�õ��붯��
            var fadeInStoryboard = (Storyboard)this.Resources["FadeInAnimation"];
            //SingleImage.BeginStoryboard(fadeInStoryboard);

            // ����ͼ��
            SingleImage.Source = _loadedImages[_currentImageIndex];

            // ������Ϣ�Ϳؼ�״̬
            TxtImageInfo.Text = $"{_currentImageIndex + 1} / {_loadedImages.Count}";
            ImageSlider.Maximum = _loadedImages.Count;
            ImageSlider.Value = _currentImageIndex + 1;

            // ���°�ť״̬
            BtnPrevious.IsEnabled = _currentImageIndex > 0;
            BtnNext.IsEnabled = _currentImageIndex < _loadedImages.Count - 1;
        }

        /// <summary>
        /// ��һ�Ű�ť����¼�
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
        /// ��һ�Ű�ť����¼�
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
        /// ����ֵ�仯�¼�
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
        /// �������¼����ڵ��Ų鿴ģʽ���л�ͼ��
        /// </summary>
        private void SingleView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                // ���Ϲ�������ʾ��һ��
                if (_currentImageIndex > 0)
                {
                    _currentImageIndex--;
                    UpdateSingleView();
                }
            }
            else if (e.Delta < 0)
            {
                // ���¹�������ʾ��һ��
                if (_currentImageIndex < _loadedImages.Count - 1)
                {
                    _currentImageIndex++;
                    UpdateSingleView();
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// �����¼��������Ҽ�ͷ�л�ͼ��
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_isSingleViewMode || _loadedImages.Count == 0)
                return;

            switch (e.Key)
            {
                case Key.Left:
                case Key.Up:
                    // ��һ��
                    if (_currentImageIndex > 0)
                    {
                        _currentImageIndex--;
                        UpdateSingleView();
                    }
                    e.Handled = true;
                    break;

                case Key.Right:
                case Key.Down:
                    // ��һ��
                    if (_currentImageIndex < _loadedImages.Count - 1)
                    {
                        _currentImageIndex++;
                        UpdateSingleView();
                    }
                    e.Handled = true;
                    break;

                case Key.Home:
                    // ��һ��
                    _currentImageIndex = 0;
                    UpdateSingleView();
                    e.Handled = true;
                    break;

                case Key.End:
                    // ���һ��
                    _currentImageIndex = _loadedImages.Count - 1;
                    UpdateSingleView();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// ��ӡ��ǰҳ�棨ֱ�Ӵ�ӡ���ڵĿ���Ԫ�أ�
        /// </summary>
        private void BtnPrintCurrentPage_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedImages.Count == 0)
            {
                MessageBox.Show(
                    "δ����ͼ�����ȴ��ļ����ļ���",
                    "��ʾ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            try
            {
                PrintService.PrintCurrentPageVisual(this, "DICOM ��Ƭ��ӡ - ��ǰҳ��");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "��ӡʧ��: " + ex.Message,
                    "����",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// ��ӡ�Զ����ĵ�������FlowDocument���д�ӡ��
        /// </summary>
        private void BtnPrintCustomDoc_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedImages.Count == 0)
            {
                MessageBox.Show(
                    "δ����ͼ�����ȴ��ļ����ļ���",
                    "��ʾ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            try
            {
                PrintService.PrintCustomDocument(
                    this,
                    "DICOM ��Ƭ��ӡ - �Զ����ĵ�",
                    _loadedImages
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "��ӡʧ��: " + ex.Message,
                    "����",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}
