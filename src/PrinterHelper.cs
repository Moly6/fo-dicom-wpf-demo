using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DicomFilmPrinter;

/// <summary>
/// 打印机辅助类，用于检查打印机状态和管理打印任务
/// </summary>
public class PrinterHelper
{
    /// <summary>
    /// 检查打印机状态（使用 LocalPrintServer 方式）
    /// </summary>
    /// <param name="printerName">打印机名称</param>
    /// <exception cref="Exception">打印机状态异常时抛出</exception>
    public static void CheckPrintStatus(string printerName)
    {
        LocalPrintServer printServer = new LocalPrintServer();
        PrintQueue printQueue = printServer.GetPrintQueue(printerName);
        var statusMessage = printQueue switch
        {
            _ when printQueue.IsOutOfPaper => "打印机缺纸，请更换纸张或插入纸盒",
            _ when printQueue.IsPrinting => "打印机正在打印，请等待...",
            _ when printQueue.IsPaperJammed => "打印机卡纸，请检查纸张",
            _ when printQueue.IsTonerLow => "打印机缺墨，请更换墨盒或插入墨盒",
            _ when printQueue.HasPaperProblem => "打印机纸张有问题，请检查纸张",
            _ when printQueue.IsDoorOpened => "打印机门打开，请检查门锁",
            _ when printQueue.IsInError => "打印机处于错误状态，请检查打印机设置",
            _ when printQueue.IsNotAvailable => "打印机不可用，请检查打印机设置",
            _ when printQueue.IsOffline => "打印机离线，请检查网络连接或打印机设置",
            _ when printQueue.IsOutOfMemory => "打印机内存不足，请检查打印机设置",
            _ when printQueue.IsOutputBinFull => "打印机输出纸盒已满，请检查打印机设置",
            _ when printQueue.NeedUserIntervention => "打印机需要用户操作，请检查打印机设置",
            _ when printQueue.IsPaused => "打印机已暂停，请重新启动打印机",
            _ when printQueue.IsProcessing => "打印机正在打印，请等待打印完成",
            _ when printQueue.IsWaiting => "打印机正在等待，请等待打印机准备就绪",
            _ when printQueue.IsInitializing => "打印机正在初始化，请等待打印机准备就绪",
            _ when printQueue.IsWarmingUp => "打印机正在加热，请等待打印机准备就绪",
            _ when printQueue.IsBusy => "打印机繁忙，请等待打印机准备就绪",
            _ => string.Empty,
        };
        if (!string.IsNullOrWhiteSpace(statusMessage))
            throw new Exception(statusMessage);
    }

    /// <summary>
    /// 检查打印机状态是否可用（使用 Windows API 方式）
    /// </summary>
    /// <param name="printerName">打印机名称</param>
    /// <exception cref="Exception">打印机状态异常时抛出</exception>
    public static void CheckPrintStatusWin(string printerName)
    {
        var statusCode = GetPrinterStatusCodeInt(printerName);
        if (!CheckIsEnable(statusCode, printerName))
        {
            throw new Exception(GetPrinterStatusMessage(statusCode));
        }
    }

    /// <summary>
    /// 检查打印机是否正在打印
    /// </summary>
    /// <param name="printerName">打印机名称</param>
    /// <returns>如果正在打印返回 true，否则返回 false</returns>
    public static bool IsPrinting(string printerName)
    {
        var statusCode = GetPrinterStatusCodeInt(printerName);
        if (statusCode == 0x00000400 || statusCode == 0x00004000)
        {
            return true;
        }
        return false;
    }

    #region Windows API 获取打印机状态

    /// <summary>
    /// 打开指定的打印机
    /// </summary>
    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool OpenPrinter(
        string pPrinterName,
        out IntPtr hPrinter,
        IntPtr pDefault
    );

    /// <summary>
    /// 关闭打印机句柄
    /// </summary>
    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    /// <summary>
    /// 获取打印机信息
    /// </summary>
    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool GetPrinter(
        IntPtr hPrinter,
        int dwLevel,
        IntPtr pPrinter,
        int cbBuf,
        out int pcbNeeded
    );

    /// <summary>
    /// 枚举打印任务
    /// </summary>
    [DllImport("winspool.drv", CharSet = CharSet.Auto)]
    public static extern int EnumJobs(
        IntPtr hPrinter,
        int FirstJob,
        int NoJobs,
        int Level,
        IntPtr pInfo,
        int cdBuf,
        out int pcbNeeded,
        out int pcReturned
    );

    /// <summary>
    /// 打印机信息结构体（PRINTER_INFO_2）
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct PRINTER_INFO_2
    {
        public string pServerName; // 服务器名称
        public string pPrinterName; // 打印机名称
        public string pShareName; // 共享名称
        public string pPortName; // 端口名称
        public string pDriverName; // 驱动名称
        public string pComment; // 注释
        public string pLocation; // 位置
        public IntPtr pDevMode; // 设备模式
        public string pSepFile; // 分隔文件
        public string pPrintProcessor; // 打印处理器
        public string pDatatype; // 数据类型
        public string pParameters; // 参数
        public IntPtr pSecurityDescriptor; // 安全描述符
        public uint Attributes; // 属性
        public uint Priority; // 优先级
        public uint DefaultPriority; // 默认优先级
        public uint StartTime; // 开始时间
        public uint UntilTime; // 结束时间
        public uint Status; // 状态
        public uint cJobs; // 作业数量
        public uint AveragePPM; // 平均每分钟打印页数
    }

    /// <summary>
    /// 获取打印机的状态编码
    /// </summary>
    /// <param name="printerName">打印机名称</param>
    /// <returns>状态编码</returns>
    public static int GetPrinterStatusCodeInt(string printerName)
    {
        int statusCode = 0;
        IntPtr hPrinter;

        if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
        {
            int cbNeeded = 0;
            bool result = GetPrinter(hPrinter, 2, IntPtr.Zero, 0, out cbNeeded);
            if (cbNeeded > 0)
            {
                IntPtr pAddr = Marshal.AllocHGlobal((int)cbNeeded);
                result = GetPrinter(hPrinter, 2, pAddr, cbNeeded, out cbNeeded);
                if (result)
                {
                    PRINTER_INFO_2 printerInfo = new PRINTER_INFO_2();
                    printerInfo = (PRINTER_INFO_2)
                        Marshal.PtrToStructure(pAddr, typeof(PRINTER_INFO_2));
                    statusCode = System.Convert.ToInt32(printerInfo.Status);
                }
                Marshal.FreeHGlobal(pAddr);
            }
            ClosePrinter(hPrinter);
        }

        return statusCode;
    }

    /// <summary>
    /// 检查打印机是否可用
    /// </summary>
    /// <param name="statusCodeValue">状态编码值</param>
    /// <param name="printerName">打印机名称</param>
    /// <returns>如果可用返回 true，否则返回 false</returns>
    public static bool CheckIsEnable(int statusCodeValue, string printerName)
    {
        // 如果没有任务在队列中，打印机可用
        if (PrinterHelper.GetTaskNumber(printerName) == 0)
        {
            return true;
        }

        // 检查特定的可用状态码
        if (
            statusCodeValue == 0x0008000 // 初始化中
            || statusCodeValue == 0x00000400 // 正在打印
            || statusCodeValue == 0x00004000
        ) // 正在处理
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取打印机的状态信息描述
    /// </summary>
    /// <param name="statusCodeValue">状态编码值</param>
    /// <returns>状态信息字符串</returns>
    public static string GetPrinterStatusMessage(int intStatusCodeValue)
    {
        string strRet = string.Empty;
        switch (intStatusCodeValue)
        {
            case 0:
                strRet = "准备就绪（Ready）";
                break;
            case 0x00000200:
                strRet = "忙(Busy）";
                break;
            case 0x00400000:
                strRet = "被打开（Printer Door Open）";
                break;
            case 0x00000002:
                strRet = "错误(Printer Error）";
                break;
            case 0x0008000:
                strRet = "初始化(Initializing）";
                break;
            case 0x00000100:
                strRet = "正在输入,输出（I/O Active）";
                break;
            case 0x00000020:
                strRet = "手工送纸（Manual Feed）";
                break;
            case 0x00040000:
                strRet = "无墨粉（No Toner）";
                break;
            case 0x00001000:
                strRet = "不可用（Not Available）";
                break;
            case 0x00000080:
                strRet = "脱机（Off Line）";
                break;
            case 0x00200000:
                strRet = "内存溢出（Out of Memory）";
                break;
            case 0x00000800:
                strRet = "输出口已满（Output Bin Full）";
                break;
            case 0x00080000:
                strRet = "当前页无法打印（Page Punt）";
                break;
            case 0x00000008:
                strRet = "塞纸（Paper Jam）";
                break;
            case 0x00000010:
                strRet = "打印纸用完（Paper Out）";
                break;
            case 0x00000040:
                strRet = "纸张问题（Page Problem）";
                break;
            case 0x00000001:
                strRet = "暂停（Paused）";
                break;
            case 0x00000004:
                strRet = "正在删除（Pending Deletion）";
                break;
            case 0x00000400:
                strRet = "正在打印（Printing）";
                break;
            case 0x00004000:
                strRet = "正在处理（Processing）";
                break;
            case 0x00020000:
                strRet = "墨粉不足（Toner Low）";
                break;
            case 0x00100000:
                strRet = "需要用户干预（User Intervention）";
                break;
            case 0x20000000:
                strRet = "等待（Waiting）";
                break;
            case 0x00010000:
                strRet = "热机中（Warming Up）";
                break;
            default:
                strRet = "未知状态（Unknown Status）";
                break;
        }
        return strRet;
    }

    /// <summary>
    /// 获取打印机正在处理的任务数量
    /// </summary>
    /// <param name="printerName">打印机名称</param>
    /// <returns>任务数量</returns>
    public static int GetTaskNumber(string printerName)
    {
        IntPtr handle;
        int firstJob = 0;
        int maxJobs = 127; // 假设打印机队列最多有 128 个任务 (0..127)
        int bytesNeeded;
        int jobsReturned = -1;

        OpenPrinter(printerName, out handle, IntPtr.Zero);

        // 获取所需字节数
        EnumJobs(handle, firstJob, maxJobs, 1, IntPtr.Zero, 0, out bytesNeeded, out jobsReturned);

        // 分配非托管内存
        IntPtr pData = Marshal.AllocHGlobal(bytesNeeded);

        // 获取任务结构
        EnumJobs(
            handle,
            firstJob,
            maxJobs,
            1,
            pData,
            bytesNeeded,
            out bytesNeeded,
            out jobsReturned
        );

        return jobsReturned;
    }

    #endregion
}
