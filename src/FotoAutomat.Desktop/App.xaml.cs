using System.Threading;
using System.Windows;

namespace FotoAutomat.Desktop;

public partial class App : System.Windows.Application
{
    private Mutex? instanceMutex;
    private bool ownsMutex;

    /// <summary>Debug builds or --dev: shows engine versions and research options (Match Look reference type). Production hides them.</summary>
    public static bool DeveloperMode { get; private set; }
    public static bool UiPreview { get; private set; }

    private async void RunBenchmark(string[] args, int index)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        string? Option(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        try
        {
            string report = await ProcessingBenchmark.RunAsync(System.IO.Path.GetFullPath(args[index + 1]), Option("--frame"), Option("--out"));
            if (!args.Contains("--no-dialog")) MessageBox.Show($"Đã đo xong. Báo cáo:\n{report}", "FotoAutomat · Đo xử lý ảnh");
        }
        catch (Exception error)
        {
            if (Option("--out") is { } folder) { System.IO.Directory.CreateDirectory(folder); System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "benchmark-error.txt"), error.ToString()); }
            if (!args.Contains("--no-dialog")) MessageBox.Show($"Không đo được: {error.Message}", "FotoAutomat · Đo xử lý ảnh");
        }
        Shutdown();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        UiPreview = e.Args.Contains("--ui-preview");
#if DEBUG
        DeveloperMode = true;
#else
        DeveloperMode = e.Args.Contains("--dev");
#endif
        // Phát triển: --simulated-camera ảnh.jpg dùng ảnh đó làm live view và ảnh chụp thay cho máy ảnh thật.
        int simulatedCamera = Array.IndexOf(e.Args, "--simulated-camera");
        if (simulatedCamera >= 0 && simulatedCamera + 1 < e.Args.Length) CustomerCamera.SimulatedSource = System.IO.Path.GetFullPath(e.Args[simulatedCamera + 1]);
        instanceMutex = new Mutex(true, @"Local\FotoAutomat.Simulator", out ownsMutex);
        if (!ownsMutex)
        {
            MessageBox.Show("FotoAutomat đang chạy.", "FotoAutomat");
            Shutdown();
            return;
        }
        // Đo xử lý ảnh trên máy booth: FotoAutomat.Desktop.exe --benchmark-processing ảnh.jpg [--frame khung.fotolayout] [--out thư mục] [--no-dialog].
        int benchmark = Array.IndexOf(e.Args, "--benchmark-processing");
        if (benchmark >= 0 && benchmark + 1 < e.Args.Length)
        {
            RunBenchmark(e.Args, benchmark);
            return;
        }
        // Phiên khách còn mở từ lần chạy trước (máy tắt, app đóng giữa chừng) chuyển "Cần kiểm tra"; không tự làm lại gì.
        try { new FotoAutomat.Infrastructure.SqliteCustomerSessionStore().RecoverInterrupted(); }
        catch (Exception error) { System.Diagnostics.Debug.WriteLine(error); }
        try
        {
            Window window = e.Args.Contains("--admin") || e.Args.Contains("--camera-settings") || e.Args.Contains("--adjust-settings") || UiPreview ? new LayoutWindow()
                : e.Args.Contains("--workflow-simulator") ? new SimulatorWindow() : !new FotoAutomat.Infrastructure.GeneralSettingsStore().HasPin ? new LayoutWindow() : new MainWindow();
            if (window is LayoutWindow admin && !new FotoAutomat.Infrastructure.GeneralSettingsStore().HasPin) ((FotoAutomat.Desktop.ViewModels.LayoutEditorViewModel)admin.DataContext).Section = "Quyền quản trị";
            int renderArgument = Array.IndexOf(e.Args, "--render-preview");
            if (renderArgument >= 0 && renderArgument + 1 < e.Args.Length)
            {
                window.ContentRendered += (_, _) =>
                {
                    try
                    {
                        var content = (FrameworkElement)window.Content;
                        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)content.ActualWidth, (int)content.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        bitmap.Render(content);
                        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                        using var output = System.IO.File.Create(e.Args[renderArgument + 1]);
                        encoder.Save(output);
                        Shutdown();
                    }
                    catch (Exception error)
                    {
                        System.Console.Error.WriteLine(error.Message);
                        Shutdown(1);
                    }
                };
            }
            if (UiPreview && window is LayoutWindow preview)
            {
                preview.Title = "FotoAutomat · UI mới · Camera MÔ PHỎNG";
                ((FotoAutomat.Desktop.ViewModels.LayoutEditorViewModel)preview.DataContext).Section = "Máy ảnh";
            }
            if (!UiPreview && e.Args.Contains("--camera-settings") && window is LayoutWindow cameraAdmin)
            {
                cameraAdmin.Title = "FotoAutomat · Máy ảnh USB";
                ((FotoAutomat.Desktop.ViewModels.LayoutEditorViewModel)cameraAdmin.DataContext).Section = "Máy ảnh";
            }
            if (e.Args.Contains("--adjust-settings") && window is LayoutWindow adjustAdmin)
                ((FotoAutomat.Desktop.ViewModels.LayoutEditorViewModel)adjustAdmin.DataContext).Section = "Bộ lọc màu";
            window.Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Không khởi động được FotoAutomat: {exception.Message}", "FotoAutomat");
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (ownsMutex) instanceMutex?.ReleaseMutex();
        instanceMutex?.Dispose();
        base.OnExit(e);
    }
}

