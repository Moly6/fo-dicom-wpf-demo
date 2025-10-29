# DICOM 胶片查看Demo

一个基于 WPF 和 Fellow Oak DICOM (fo-dicom) 库的 DICOM 医疗影像查看和打印的Demo。
- ` 网格布局 `
![image-网格](images\网格.png)
- ` 单张 `
![image-单张](images\单张.png)

## ✨ 功能特性

- 📁 **多种加载方式**
  - 支持单个或多个 DICOM 文件加载
  - 支持文件夹批量加载
- 🖼️ **双视图模式**
  - **网格视图**：3列网格布局，一次查看多张图像
  - **单张连续查看**：医疗影像序列连续浏览模式
- 🎮 **多种切换方式**（单张查看模式）
  - 鼠标滚轮：向上/向下滚动切换
  - 键盘方向键：左/右箭头或上/下箭头
  - 快捷键：Home（第一张）、End（最后一张）
  - 可视化控件：按钮、进度滑块
- 🖨️ **双打印模式**
  - **当前页面打印**：直接打印窗口可视内容
  - **自定义文档打印**：创建 FlowDocument 进行专业打印

## 🚀 快速开始

- 使用的是 .NET 9.0 

### 安装依赖

项目使用以下 NuGet 包：

```xml
<PackageReference Include="fo-dicom" Version="5.2.4" />
<PackageReference Include="fo-dicom.Imaging.ImageSharp" Version="5.2.4" />
<PackageReference Include="Microsoft-WindowsAPICodePack-Shell" Version="1.1.5" />
```

在项目目录下运行：

```bash
dotnet restore
```

### 构建运行

```bash
dotnet build
dotnet run
```

## 📚 使用 fo-dicom 库的关键步骤

### 1. 初始化 DICOM 设置（必需）

在 `App.xaml.cs` 的 `OnStartup` 方法中初始化 fo-dicom：

```csharp
using FellowOakDicom;
using FellowOakDicom.Imaging;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 重要：必须在使用 fo-dicom 之前进行初始化
        var dicomSetupBuilder = new DicomSetupBuilder().RegisterServices(
            (service) => service.AddFellowOakDicom().AddImageManager<ImageSharpImageManager>()
        );
        dicomSetupBuilder.Build();
    }
}
```

> **注意**：如果不添加这段初始化代码，在加载和渲染 DICOM 文件时会抛出异常！

### 2. 加载 DICOM 文件

```csharp
using FellowOakDicom;
using FellowOakDicom.Imaging;

// 从文件加载 DICOM 图像
var dicomImage = new DicomImage(filePath);
```

### 3. 渲染为图像

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

// 渲染 DICOM 图像为 ImageSharp 格式
var sharpImage = dicomImage.RenderImage().AsSharpImage();
```

### 4. 转换为 WPF BitmapSource

```csharp
public static BitmapSource ConvertToBitmapSource(Image<Bgra32> sharpImage)
{
    if (sharpImage == null)
        throw new ArgumentNullException(nameof(sharpImage));

    using (var memoryStream = new MemoryStream())
    {
        // 保存为 BMP 格式到内存流
        sharpImage.SaveAsBmp(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        // 创建 WPF BitmapImage
        BitmapImage bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memoryStream;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        
        return bitmapImage;
    }
}
```

### 5. 完整的加载流程

参考 `DicomService.cs` 中的实现：

```csharp
public static List<BitmapSource> LoadDicomFilesAsBitmapSources(List<string> filePaths)
{
    var imageList = new List<BitmapSource>();
    
    foreach (var filePath in filePaths)
    {
        try
        {
            // 1. 加载 DICOM 文件
            var dicomImage = new DicomImage(filePath);
            
            // 2. 渲染为 ImageSharp 图像
            var sharpImage = dicomImage.RenderImage().AsSharpImage();
            
            // 3. 转换为 WPF BitmapSource
            var bitmapSource = ConvertToBitmapSource(sharpImage);
            
            // 4. 添加到列表
            imageList.Add(bitmapSource);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载文件 {Path.GetFileName(filePath)} 失败: {ex.Message}");
        }
    }

    return imageList;
}
```

## 🏗️ 项目结构

```
DicomFilmPrinter/
├── Resources                   # 资源文件
├── App.xaml                    # WPF 应用程序定义
├── App.xaml.cs                 # 应用程序启动逻辑（包含 fo-dicom 初始化）
├── MainWindow.xaml             # 主窗口 UI 定义
├── MainWindow.xaml.cs          # 主窗口逻辑（视图切换、图像浏览）
├── DicomService.cs             # DICOM 文件加载和转换服务
├── PrintService.cs             # 打印服务（FlowDocument 打印）
├── PrinterHelper.cs            # 打印机状态检查辅助类
└── DicomFilmPrinter.csproj     # 项目文件
```

## 🎯 核心代码说明

### DicomService.cs
负责 DICOM 文件的加载、渲染和格式转换：
- `LoadDicomFolderAsBitmapSources()` - 从文件夹加载
- `LoadDicomFilesAsBitmapSources()` - 从文件列表加载
- `ConvertToBitmapSource()` - ImageSharp 转 WPF BitmapSource

### MainWindow.xaml.cs
主窗口功能实现：
- 文件/文件夹打开
- 视图模式切换（网格/单张）
- 图像浏览控制（滚轮、键盘、滑块）

### PrintService.cs
打印功能实现：
- `PrintCustomDocument()` - 自定义文档打印
- `PrintCurrentPageVisual()` - 当前页面打印
- `CreateDicomFlowDocument()` - 创建打印文档



## 📖 fo-dicom 库参考资源

- **官方文档**: https://github.com/fo-dicom/fo-dicom
- **NuGet 包**: https://www.nuget.org/packages/fo-dicom
- **ImageSharp 扩展**: https://www.nuget.org/packages/fo-dicom.Imaging.ImageSharp

