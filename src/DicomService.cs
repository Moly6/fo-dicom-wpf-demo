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
        /// ���ļ��м�������DICOM�ļ���ת��ΪBitmapSource�б�
        /// </summary>
        /// <param name="folderPath">�ļ���·��</param>
        /// <returns>BitmapSourceͼ���б�</returns>
        public static List<BitmapSource> LoadDicomFolderAsBitmapSources(string folderPath)
        {
            var dicomFiles = Directory.GetFiles(folderPath, "*.dcm").ToList();
            return LoadDicomFilesAsBitmapSources(dicomFiles);
        }

        /// <summary>
        /// ��ָ����DICOM�ļ��б���ز�ת��ΪBitmapSource�б�
        /// </summary>
        /// <param name="filePaths">DICOM�ļ�·���б�</param>
        /// <returns>BitmapSourceͼ���б�</returns>
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
                    // ������Ⱦʧ�ܵ��ļ����������������ļ�
                    Console.WriteLine($"�����ļ� {Path.GetFileName(filePath)} ʧ��: {ex.Message}");
                }
            }

            return imageList;
        }

        /// <summary>
        /// ��ImageSharp��Image����ת��ΪWPF��BitmapSource
        /// </summary>
        /// <param name="sharpImage">ImageSharpͼ�����</param>
        /// <returns>WPF BitmapSource����</returns>
        public static BitmapSource ConvertToBitmapSource(Image<Bgra32> sharpImage)
        {
            // ȷ������ͼ��Ϊnull
            if (sharpImage == null)
                throw new ArgumentNullException(nameof(sharpImage));

            // �� Image<Bgra32> ת��Ϊ MemoryStream
            using (var memoryStream = new MemoryStream())
            {
                sharpImage.SaveAsBmp(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                // ����һ�� BitmapImage ������Դ����Ϊ MemoryStream
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
