using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DicomFilmPrinter
{
    public static class DicomService
    {
        /// <summary>
        /// 从文件夹加载所有DICOM文件并转换为BitmapSource列表
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <returns>BitmapSource图像列表</returns>
        public static List<BitmapSource> LoadDicomFolderAsBitmapSources(string folderPath)
        {
            var dicomFiles = Directory.GetFiles(folderPath, "*.dcm").ToList();
            return LoadDicomFilesAsBitmapSources(dicomFiles);
        }

        /// <summary>
        /// 从指定的DICOM文件列表加载并转换为BitmapSource列表
        /// </summary>
        /// <param name="filePaths">DICOM文件路径列表</param>
        /// <returns>BitmapSource图像列表</returns>
        public static List<BitmapSource> LoadDicomFilesAsBitmapSources(List<string> filePaths)
        {
            var imageList = new List<BitmapSource>();
            
            foreach (var filePath in filePaths)
            {
                try
                {
                    var dicomImage = new DicomImage(filePath);
                    var sharpImage = dicomImage.RenderImage().AsSharpImage();
                    var bitmapSource = ConvertToBitmapSource(sharpImage);
                    imageList.Add(bitmapSource);
                }
                catch (Exception ex)
                {
                    // 跳过渲染失败的文件，继续处理其他文件
                    Console.WriteLine($"加载文件 {Path.GetFileName(filePath)} 失败: {ex.Message}");
                }
            }

            return imageList;
        }

        /// <summary>
        /// 将ImageSharp的Image对象转换为WPF的BitmapSource
        /// </summary>
        /// <param name="sharpImage">ImageSharp图像对象</param>
        /// <returns>WPF BitmapSource对象</returns>
        public static BitmapSource ConvertToBitmapSource(Image<Bgra32> sharpImage)
        {
            // 确保输入图像不为null
            if (sharpImage == null)
                throw new ArgumentNullException(nameof(sharpImage));

            // 将 Image<Bgra32> 转换为 MemoryStream
            using (var memoryStream = new MemoryStream())
            {
                sharpImage.SaveAsBmp(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                // 创建一个 BitmapImage 并将其源设置为 MemoryStream
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
    }
}
