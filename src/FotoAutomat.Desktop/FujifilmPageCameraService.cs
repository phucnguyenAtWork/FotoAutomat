using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Admin.Camera;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

/// <summary>Bridges the admin page to the serialized SDK and durable capture pipeline.</summary>
public sealed class FujifilmPageCameraService : ICameraService, ICameraConnectionStatus
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly System.Windows.Threading.DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private readonly string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "CameraTests");
    private readonly Guid session = Guid.NewGuid();
    private FujifilmCameraConnection? camera;
    private BoothCameraPipeline? pipeline;
    private bool polling;
    private static readonly Dictionary<string, string> Keys = new()
    {
        ["mode"] = "AEMode",
        ["iso"] = "Sensitivity",
        ["shutter"] = "ShutterSpeed",
        ["aperture"] = "Aperture",
        ["wb"] = "WBMode",
        ["kelvin"] = "WBColorTemp"
    };
    public FujifilmPageCameraService() => timer.Tick += OnFrame;
    public IReadOnlyList<string> Brands => ["Fujifilm", "Canon", "Nikon"];
    public string ConnectedModel => pipeline?.Device.Model ?? "";
    public event Action<ImageSource>? LiveViewFrame;
    public event Action<string>? ConnectionFailed;

    // This is a connection target, not a claim that a physical device was detected.
    // XSDK_Detect and model identification run inside ConnectAsync.
    public Task<IReadOnlyList<string>> DetectAsync(string brand)
    {
        if (brand != "Fujifilm") throw new NotSupportedException("Chưa có adapter " + brand + ".");
        return Task.FromResult<IReadOnlyList<string>>(["Fujifilm USB · tự nhận model"]);
    }
    public async Task ConnectAsync(string brand, string model, string sdkPath)
    {
        if (brand != "Fujifilm") throw new NotSupportedException("Chưa có adapter " + brand + ".");
        await gate.WaitAsync();
        try
        {
            if (pipeline is not null) throw new InvalidOperationException("Hãy ngắt kết nối trước.");
            camera = new FujifilmCameraConnection(sdkPath);
            pipeline = new BoothCameraPipeline(camera, root);
            try { await pipeline.ConnectAsync(); }
            catch { await CloseCoreAsync(); throw; }
            active = this;
        }
        finally { gate.Release(); }
    }
    // Chỉ một bên được giữ USB. Giao diện khách gọi ReleaseActiveAsync trước khi tự kết nối.
    private static FujifilmPageCameraService? active;

    public static async Task ReleaseActiveAsync()
    {
        if (active is not { } service) return;
        await service.DisconnectAsync();
        service.ConnectionFailed?.Invoke("Đã ngắt máy ảnh để giao diện khách sử dụng.");
    }

    private async Task CloseCoreAsync()
    {
        if (active == this) active = null;
        timer.Stop();
        try { if (pipeline is not null) await pipeline.DisposeAsync(); }
        finally { pipeline = null; camera = null; }
    }
    public async Task DisconnectAsync()
    {
        timer.Stop();
        await gate.WaitAsync();
        try { await CloseCoreAsync(); }
        finally { gate.Release(); }
    }
    private void RequireReady()
    {
        if (pipeline?.IsReady != true || camera is null) throw new InvalidOperationException("Camera chưa sẵn sàng. Hãy kết nối lại.");
    }
    public async Task StartLiveViewAsync()
    {
        await gate.WaitAsync();
        try { RequireReady(); await camera!.StartLiveViewAsync(default); timer.Start(); }
        finally { gate.Release(); }
    }
    public async Task StopLiveViewAsync()
    {
        timer.Stop();
        await gate.WaitAsync();
        try { if (camera is not null) await camera.StopLiveViewAsync(default); }
        finally { gate.Release(); }
    }
    private async void OnFrame(object? sender, EventArgs args)
    {
        if (polling || !await gate.WaitAsync(0)) return;
        polling = true;
        string? failure = null;
        try
        {
            if (!timer.IsEnabled) return;
            RequireReady();
            var frame = await pipeline!.ReadLiveViewAsync();
            if (frame is not null && timer.IsEnabled) LiveViewFrame?.Invoke(Decode(frame.Jpeg));
        }
        catch (Exception error)
        {
            failure = error.Message;
            try { await CloseCoreAsync(); } catch (Exception cleanup) { System.Diagnostics.Debug.WriteLine(cleanup); }
        }
        finally { polling = false; gate.Release(); }
        if (failure is not null) ConnectionFailed?.Invoke(failure);
    }
    private static ImageSource Decode(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.DecodePixelWidth = 1280;
        image.StreamSource = stream; image.EndInit(); image.Freeze(); return image;
    }
    public async Task<IReadOnlyDictionary<string, CameraSettingChoice>> ReadSettingsAsync()
    {
        await gate.WaitAsync();
        try
        {
            RequireReady();
            var settings = await camera!.ReadSettingsAsync(default);
            return Keys.ToDictionary(pair => pair.Key, pair =>
            {
                var setting = settings.Single(s => s.Key == pair.Value);
                return new CameraSettingChoice(setting.Error is null ? setting.Options.Select(o => o.Label).ToArray() : [],
                    setting.Current is int current ? FujifilmCameraConnection.FormatSetting(setting.Key, current) : "");
            });
        }
        finally { gate.Release(); }
    }
    public async Task ApplySettingsAsync(IReadOnlyDictionary<string, string> values)
    {
        await gate.WaitAsync();
        try
        {
            RequireReady();
            var settings = await camera!.ReadSettingsAsync(default);
            var changes = new Dictionary<string, int>();
            foreach (var (key, label) in values)
            {
                if (!Keys.TryGetValue(key, out var native)) throw new ArgumentException("Thông số không hỗ trợ.");
                var setting = settings.Single(s => s.Key == native);
                var matches = setting.Options.Where(o => o.Label == label).ToArray();
                if (setting.Error is not null || matches.Length != 1) throw new InvalidOperationException("Thông số đã thay đổi; hãy đọc lại từ máy.");
                if (matches[0].Value != setting.Current) changes.Add(native, matches[0].Value);
            }
            if (changes.Count > 0) await camera.ApplySettingsAsync(changes, Path.Combine(root, "Settings"), default);
        }
        finally { gate.Release(); }
    }
    public async Task<ImageSource> CaptureTestAsync()
    {
        timer.Stop();
        await gate.WaitAsync();
        try
        {
            RequireReady();
            var path = await pipeline!.CaptureAsync(session, Guid.NewGuid());
            return Decode(await File.ReadAllBytesAsync(path));
        }
        catch
        {
            try { await CloseCoreAsync(); } catch (Exception cleanup) { System.Diagnostics.Debug.WriteLine(cleanup); }
            ConnectionFailed?.Invoke("Chụp chưa hoàn tất. Kiểm tra camera và kết nối lại; không tự chụp lại.");
            throw;
        }
        finally { gate.Release(); }
    }
}

public sealed class CameraPageConfigStore : ICameraConfigStore
{
    private readonly CameraSetupStore store = new();
    public CameraConfig Load()
    {
        var saved = store.Load();
        var sdk = saved.SdkDirectory;
        if (string.IsNullOrWhiteSpace(sdk)) sdk = FujifilmSdkDiscovery.FindInstalled() ?? "";
        return new CameraConfig(store.Exists ? saved.Brand : "Fujifilm", saved.Model, sdk, saved.ParameterValues);
    }
    public void Save(CameraConfig config)
    {
        var sdk = config.Brand == "Fujifilm" ? FujifilmCameraConnection.ResolveDirectory(config.SdkPath) : config.SdkPath;
        store.Save(store.Load() with
        {
            Brand = config.Brand,
            Model = config.Model,
            SdkDirectory = sdk,
            ParameterValues = config.Values.ToDictionary(p => p.Key, p => p.Value)
        });
    }
}
