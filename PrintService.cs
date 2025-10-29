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
    /// ��ӡ�Զ����ĵ���ʹ��FlowDocument��
    /// </summary>
    /// <param name="ownerWindow">������</param>
    /// <param name="jobName">��ӡ��ҵ����</param>
    /// <param name="images">Ҫ��ӡ��ͼ���б�</param>
    public static void PrintCustomDocument(Window ownerWindow, string jobName, List<BitmapSource> images)
    {
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
            return;
        
        // ����FlowDocument�ĵ�
        var flowDocument = CreateDicomFlowDocument(images);
        
        // ִ�д�ӡ
        ExecutePrint(flowDocument, printDialog, jobName);
        
        MessageBox.Show(ownerWindow, $"�ѷ��ʹ�ӡ��ҵ����ӡ��: {printDialog.PrintQueue.FullName}", "��ӡ", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// ��DICOMͼ���б�ת��ΪFlowDocument
    /// </summary>
    /// <param name="images">ͼ���б�</param>
    /// <returns>FlowDocument�ĵ�����</returns>
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
            flowDocument.Blocks.Add(new Paragraph(new Run("\n"))); // ��ͼ��֮����ӿհ�
        }
        
        return flowDocument;
    }

    /// <summary>
    /// ִ��FlowDocument��ӡ
    /// </summary>
    /// <param name="flowDocument">Ҫ��ӡ���ĵ�</param>
    /// <param name="printDialog">��ӡ�Ի���</param>
    /// <param name="jobDescription">��ӡ��ҵ����</param>
    static void ExecutePrint(FlowDocument flowDocument, PrintDialog printDialog, string jobDescription)
    {
        LocalPrintServer printServer = new LocalPrintServer();
        PrintQueue printQueue = printServer.GetPrintQueue(printDialog.PrintQueue.FullName);
        
        // ����ӡ��״̬
        PrinterHelper.CheckPrintStatus(printDialog.PrintQueue.FullName);
        PrinterHelper.CheckPrintStatusWin(printDialog.PrintQueue.FullName);

        printDialog.PrintQueue = printQueue;
        
        // ����ֽ�Ŵ�СΪ A4
        PageMediaSize pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
        printDialog.PrintTicket.PageMediaSize = pageMediaSize;
        // printDialog.PrintTicket.Duplexing = Duplexing.TwoSidedLongEdge; // ˫���ӡ����ѡ��
        
        // ִ�д�ӡ
        printDialog.PrintDocument(((IDocumentPaginatorSource)flowDocument).DocumentPaginator, jobDescription);
    }

    /// <summary>
    /// ��ӡ��ǰҳ��Ŀ���Ԫ�أ�ֱ�Ӵ�ӡ���ڣ�
    /// </summary>
    /// <param name="ownerWindow">Ҫ��ӡ�Ĵ���</param>
    /// <param name="jobName">��ӡ��ҵ����</param>
    public static void PrintCurrentPageVisual(Window ownerWindow, string jobName)
    {
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
            return;
        
        // ��ӡ�������ڵĿ���Ԫ��
        // ע�⣺���ַ�ʽ��ֱ�Ӵ�ӡ���ڵĵ�ǰ��ʾ���ݣ������߿򡢰�ť��UIԪ��
        printDialog.PrintVisual(ownerWindow, jobName);
        
        MessageBox.Show(ownerWindow, $"�ѷ��ʹ�ӡ��ҵ����ӡ��: {printDialog.PrintQueue.FullName}", "��ӡ", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
