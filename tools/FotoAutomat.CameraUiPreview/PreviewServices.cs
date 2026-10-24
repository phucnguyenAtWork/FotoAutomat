using System.Windows;
using System.Windows.Media;
using FotoAutomat.Admin.Camera;
namespace FotoAutomat.CameraUiPreview;
internal sealed class PreviewStore : ICameraConfigStore
{
    private CameraConfig config = new("Fujifilm", "X-H2 · Demo", "", new Dictionary<string, string>());
    public CameraConfig Load() => config;
    public void Save(CameraConfig value) => config = value;
}
internal sealed class PreviewCamera : ICameraService
{
    public IReadOnlyList<string> Brands => ["Fujifilm", "Canon", "Nikon"];
    public event Action<ImageSource>? LiveViewFrame;
    private readonly Dictionary<string, CameraSettingChoice> settings = new()
    {
        ["mode"] = new(["M", "A", "S", "P"], "M"), ["iso"] = new(["125", "200", "400", "800"], "200"),
        ["shutter"] = new(["1/60", "1/100", "1/125", "1/250"], "1/125"), ["aperture"] = new(["f/2.8", "f/4", "f/5.6", "f/8"], "f/4"),
        ["wb"] = new(["Auto", "Daylight", "Kelvin"], "Kelvin"), ["kelvin"] = new(["4000 K", "4800 K", "5200 K", "5600 K"], "4800 K")
    };
    public async Task<IReadOnlyList<string>> DetectAsync(string brand) { await Task.Delay(200); return [brand == "Fujifilm" ? "X-H2 · Demo" : brand + " · Demo"]; }
    public Task ConnectAsync(string brand, string model, string sdkPath) => Task.Delay(200);
    public Task DisconnectAsync() => Task.CompletedTask;
    public Task StartLiveViewAsync() { LiveViewFrame?.Invoke(Frame()); return Task.CompletedTask; }
    public Task StopLiveViewAsync() => Task.CompletedTask;
    public Task<IReadOnlyDictionary<string, CameraSettingChoice>> ReadSettingsAsync() => Task.FromResult<IReadOnlyDictionary<string, CameraSettingChoice>>(new Dictionary<string, CameraSettingChoice>(settings));
    public Task ApplySettingsAsync(IReadOnlyDictionary<string, string> values)
    {
        foreach (var (key, value) in values)
            if (!settings.TryGetValue(key, out var choice) || !choice.Options.Contains(value)) throw new ArgumentException("Giá trị không hợp lệ.");
        foreach (var (key, value) in values) settings[key] = settings[key] with { Current = value };
        return Task.CompletedTask;
    }
    public Task<ImageSource> CaptureTestAsync() => Task.FromResult(Frame());
    private static ImageSource Frame()
    {
        var drawing = new DrawingGroup();
        using (var dc = drawing.Open())
        {
            dc.DrawRectangle(new LinearGradientBrush(Color.FromRgb(40, 65, 75), Color.FromRgb(170, 190, 180), 45), null, new Rect(0, 0, 640, 480));
            dc.DrawRectangle(null, new Pen(Brushes.White, 2), new Rect(24, 24, 592, 432));
            dc.DrawEllipse(Brushes.LightGray, null, new Point(320, 180), 58, 68);
            dc.DrawRoundedRectangle(Brushes.SlateGray, null, new Rect(210, 260, 220, 220), 60, 60);
        }
        var image = new DrawingImage(drawing); image.Freeze(); return image;
    }
}

