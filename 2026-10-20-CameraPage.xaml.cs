using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace FotoAutomat.Admin.Camera;

public partial class CameraPage : UserControl
{
    private Task cleanup = Task.CompletedTask;
    public CameraPage()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
        Loaded += async (_, _) =>
        {
            await cleanup;
            if (IsLoaded && DataContext is CameraPageViewModel vm) await vm.InitializeAsync();
        };
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CameraPageViewModel vm) { cleanup = vm.DeactivateAsync(); await cleanup; }
    }

    // OpenFolderDialog cần .NET 8 trở lên.
    private void BrowseSdk_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Chọn thư mục SDK" };
        if (dialog.ShowDialog() == true && DataContext is CameraPageViewModel vm)
            vm.SdkPath = dialog.FolderName;
    }

    // GIẢ ĐỊNH: "Mở ảnh…" mở một file ảnh bất kỳ bằng trình xem mặc định của Windows.
    private void OpenImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh",
            Filter = "Ảnh|*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.cr2;*.cr3|Tất cả|*.*"
        };
        if (dialog.ShowDialog() == true)
            Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
    }
}

