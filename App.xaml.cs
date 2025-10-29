using System.Configuration;
using System.Data;
using System.Windows;
using FellowOakDicom;
using FellowOakDicom.Imaging;

namespace DicomFilmPrinter
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var dicomSetupBuilder = new DicomSetupBuilder().RegisterServices(
                (service) => service.AddFellowOakDicom().AddImageManager<ImageSharpImageManager>()
            );
            dicomSetupBuilder.Build();
        }
    }
}
