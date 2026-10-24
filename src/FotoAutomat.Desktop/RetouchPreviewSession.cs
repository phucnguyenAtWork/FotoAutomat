using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
namespace FotoAutomat.Desktop;

public sealed class RetouchPreviewSession
{
    public static RetouchPreviewSession Shared { get; } = new(ColorPreviewSession.Shared);
    private readonly ColorPreviewSession colors;
    private readonly RetouchWorkspaceStore store;
    private readonly List<RetouchWorkspace> undo = [];
    private readonly Stack<RetouchWorkspace> redo = new();
    private BitmapSource? source;
    private ColorRecipe? recipe;
    private CancellationTokenSource? rendering;
    private int revision;
    private RetouchWorkspace? stroke;
    private bool useColorPreset;
    public bool UseColorPreset
    {
        get => useColorPreset;
        set { if (useColorPreset == value) return; useColorPreset = value; RequestRender(); }
    }
    public string ColorPresetName => recipe?.Name ?? "";
    public RetouchWorkspace Workspace { get; private set; } = new(null, []);
    public BitmapSource? Before { get; private set; }
    public BitmapSource? Preview { get; private set; }
    public string Status { get; private set; } = "Đổi ảnh để bắt đầu";
    public bool Busy { get; private set; }
    public bool Detecting { get; private set; }
    public event Action? Changed;
    public RetouchPreviewSession(ColorPreviewSession colors, string? path = null)
    {
        this.colors = colors; store = new(path);
        try { Workspace = store.Load(); } catch (Exception e) { Status = e.Message; }
        colors.Changed += SourceChanged; SourceChanged();
    }
    private void SourceChanged()
    {
        var next = colors.Published.Looks[colors.Published.DefaultIndex]; bool newSource = source != colors.Original;
        if (!newSource && recipe == next) return;
        source = colors.Original; recipe = next;
        if (newSource) { if (!string.Equals(Workspace.SamplePath, colors.Workspace.SamplePath, StringComparison.OrdinalIgnoreCase)) Workspace = new(colors.Workspace.SamplePath, []); undo.Clear(); redo.Clear(); Status = source is null ? "Đổi ảnh để bắt đầu" : Path.GetFileName(colors.Workspace.SamplePath) ?? "Ảnh mẫu"; }
        RequestRender();
    }
    public void Update(FaceRetouch[] faces, bool history = true)
    {
        if (faces.Length > 20) throw new ArgumentException("Maximum 20 faces."); foreach (var face in faces) face.Validate();
        if (history) { undo.Add(Workspace); if (undo.Count > 100) undo.RemoveAt(0); redo.Clear(); }
        Workspace = new(colors.Workspace.SamplePath, faces); Status = "Chưa lưu Retouch · Lưu để áp vào ảnh sau chụp"; RequestRender();
    }
    public void BeginStroke() => stroke = Workspace;
    public void EndStroke() { if (stroke is { } start && start != Workspace) { undo.Add(start); if (undo.Count > 100) undo.RemoveAt(0); redo.Clear(); } stroke = null; }
    public void Undo() { if (undo.Count == 0) return; redo.Push(Workspace); Workspace = undo[^1]; undo.RemoveAt(undo.Count - 1); Status = "Đã hoàn tác · Chưa lưu Retouch"; RequestRender(); }
    public void Redo() { if (redo.Count == 0) return; undo.Add(Workspace); Workspace = redo.Pop(); Status = "Đã làm lại · Chưa lưu Retouch"; RequestRender(); }
    public void Save() { store.Save(Workspace); colors.PublishRetouch(Workspace); Status = "Đã lưu Retouch cho ảnh này"; Changed?.Invoke(); }
    public async Task DetectAsync()
    {
        if (source is null) throw new InvalidOperationException("Chọn ảnh trước khi nhận diện khuôn mặt.");
        if (Detecting) return; var input = source; var pixels = Pixels(input); Detecting = true; Status = "Đang nhận diện khuôn mặt…"; Changed?.Invoke();
        try
        {
            var (faces, detector) = await SampleFaceDetection.DetectAsync(pixels, input.PixelWidth, input.PixelHeight);
            if (source != input) return;
            // Detection only adds non-overlapping candidates; never drops manually corrected faces/settings.
            var combined = Workspace.Faces.ToList(); foreach (var face in faces) if (combined.Count < 20 && !combined.Any(f => Overlaps(f, face))) combined.Add(face);
            Update(combined.ToArray()); Status = faces.Length == 0 ? "Không tìm thấy mặt. Dùng Add face để chọn thủ công." : $"Đã tìm thấy {faces.Length} mặt ({detector}) · Căn lại mắt/miệng nếu cần";
        }
        finally { Detecting = false; Changed?.Invoke(); }
    }
    private static bool Overlaps(FaceRetouch a, FaceRetouch b)
    {
        double overlap = Math.Max(0, Math.Min(a.X + a.Width, b.X + b.Width) - Math.Max(a.X, b.X)) * Math.Max(0, Math.Min(a.Y + a.Height, b.Y + b.Height) - Math.Max(a.Y, b.Y));
        return overlap / Math.Max(1e-9, Math.Min(a.Width * a.Height, b.Width * b.Height)) > .5;
    }
    private static byte[] Pixels(BitmapSource bitmap) { var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0); return pixels; }
    private async void RequestRender()
    {
        rendering?.Cancel(); rendering?.Dispose(); rendering = new(); var token = rendering.Token; int current = ++revision;
        if (source is null || recipe is null) { Preview = Before = null; Busy = false; Changed?.Invoke(); return; }
        var image = source; var color = useColorPreset ? recipe : new ColorRecipe(); var faces = Workspace.Faces.ToArray(); var pixels = Pixels(image); Busy = true; Changed?.Invoke();
        try
        {
            await Task.Delay(90, token);
            var result = await Task.Run(() =>
            {
                BitmapSource Bitmap(byte[] data) { var bitmap = BitmapSource.Create(image.PixelWidth, image.PixelHeight, 96, 96, PixelFormats.Bgra32, null, data, image.PixelWidth * 4); bitmap.Freeze(); return bitmap; }
                var before = Bitmap(ColorProcessor.Apply(pixels, color, image.PixelWidth, image.PixelHeight, token));
                var retouched = RetouchProcessor.Apply(pixels, image.PixelWidth, image.PixelHeight, faces, token);
                return (before, after: Bitmap(ColorProcessor.Apply(retouched, color, image.PixelWidth, image.PixelHeight, token)));
            }, token);
            await Application.Current.Dispatcher.InvokeAsync(() => { if (current != revision) return; Before = result.before; Preview = result.after; Busy = false; Changed?.Invoke(); });
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { await Application.Current.Dispatcher.InvokeAsync(() => { if (current != revision) return; Busy = false; Status = e.Message; Changed?.Invoke(); }); }
    }
}
