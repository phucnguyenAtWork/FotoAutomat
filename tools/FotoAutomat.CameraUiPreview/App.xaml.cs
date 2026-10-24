using System.Windows;
using System.Windows.Media;
using FotoAutomat.Admin.Camera;

namespace FotoAutomat.CameraUiPreview;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var vm = new CameraPageViewModel(new PreviewCamera(), new PreviewStore());
        var page = new CameraPage { DataContext = vm };
        var window = new Window { Title = "FotoAutomat · Camera UI Preview · MÔ PHỎNG", Width = 1280, Height = 850, MinWidth = 1000, MinHeight = 700, Content = page };
        vm.GuideRequested += () => MessageBox.Show(window, "Bản thử giao diện. Camera, ảnh và lưu cấu hình đều mô phỏng trong bộ nhớ; không điều khiển thiết bị thật.", "UI Preview");
        window.Show();
    }
}

