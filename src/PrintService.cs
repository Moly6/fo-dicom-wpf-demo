using System.IO;
using System.Printing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

namespace DicomFilmPrinter;

public static class PrintService
{
    /// <summary>
    /// 打印自定义文档（使用FlowDocument）
    /// </summary>
    /// <param name="ownerWindow">父窗口</param>
    /// <param name="jobName">打印作业名称</param>
    /// <param name="images">要打印的图像列表</param>
    public static void PrintCustomDocument(Window ownerWindow, string jobName, List<BitmapSource> images)
    {
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
            return;
        
        // 创建FlowDocument文档
        var flowDocument = CreateDicomFlowDocument(images);
        
        // 执行打印
        ExecutePrint(flowDocument, printDialog, jobName);
        
        MessageBox.Show(ownerWindow, $"已发送打印作业到打印机: {printDialog.PrintQueue.FullName}", "打印", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 将DICOM图像列表转换为FlowDocument
    /// </summary>
    /// <param name="images">图像列表</param>
    /// <returns>FlowDocument文档对象</returns>
    public static FlowDocument CreateDicomFlowDocument(List<BitmapSource> images)
    {
        var flowDocument = new FlowDocument { };
        
        foreach (var imageSource in images)
        {
            var image = new Image
            {
                Source = imageSource,
                Width = imageSource.Width,
                Height = imageSource.Height,
                Stretch = Stretch.Uniform,
            };
            
            var blockContainer = new BlockUIContainer(image) { };
            flowDocument.Blocks.Add(blockContainer);
            flowDocument.Blocks.Add(new Paragraph(new Run("\n"))); // 在图像之间添加空白
        }
        
        return flowDocument;
    }

    /// <summary>
    /// 执行FlowDocument打印
    /// </summary>
    /// <param name="flowDocument">要打印的文档</param>
    /// <param name="printDialog">打印对话框</param>
    /// <param name="jobDescription">打印作业描述</param>
    static void ExecutePrint(FlowDocument flowDocument, PrintDialog printDialog, string jobDescription)
    {
        LocalPrintServer printServer = new LocalPrintServer();
        PrintQueue printQueue = printServer.GetPrintQueue(printDialog.PrintQueue.FullName);
        
        // 检查打印机状态
        PrinterHelper.CheckPrintStatus(printDialog.PrintQueue.FullName);
        PrinterHelper.CheckPrintStatusWin(printDialog.PrintQueue.FullName);

        printDialog.PrintQueue = printQueue;
        
        // 设置纸张大小为 A4
        PageMediaSize pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
        printDialog.PrintTicket.PageMediaSize = pageMediaSize;
        // printDialog.PrintTicket.Duplexing = Duplexing.TwoSidedLongEdge; // 双面打印（可选）
        
        // 执行打印
        printDialog.PrintDocument(((IDocumentPaginatorSource)flowDocument).DocumentPaginator, jobDescription);
    }

    /// <summary>
    /// 打印当前页面的可视元素（直接打印窗口）
    /// </summary>
    /// <param name="ownerWindow">要打印的窗口</param>
    /// <param name="jobName">打印作业名称</param>
    public static void PrintCurrentPageVisual(Window ownerWindow, string jobName)
    {
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
            return;
        
        // 打印整个窗口的可视元素
        // 注意：这种方式会直接打印窗口的当前显示内容，包括边框、按钮等UI元素
        printDialog.PrintVisual(ownerWindow, jobName);
        
        MessageBox.Show(ownerWindow, $"已发送打印作业到打印机: {printDialog.PrintQueue.FullName}", "打印", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
