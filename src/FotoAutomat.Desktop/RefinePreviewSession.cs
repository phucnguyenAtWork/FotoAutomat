using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
namespace FotoAutomat.Desktop;
public sealed class RefinePreviewSession
{
    public static RefinePreviewSession Shared { get; } = new(ColorPreviewSession.Shared);
    private readonly ColorPreviewSession colors;
    private readonly RefineWorkspaceStore store;
    private readonly List<RefineWorkspace> undo = [];
    private readonly Stack<RefineWorkspace> redo = new();
    private BitmapSource? source;
    private ColorRecipe? recipe;
    private RetouchWorkspace? retouch;
    private CancellationTokenSource? rendering;
    private int revision;
    private bool useColor;
    public RefineWorkspace Workspace { get; private set; } = new(null, new());
    public BitmapSource? Preview { get; private set; }
    public BitmapSource? Before { get; private set; }
    public bool Busy { get; private set; }
    public string Status { get; private set; } = "Đổi ảnh để bắt đầu";
    public bool UseColorPreset { get => useColor; set { if (useColor == value) return; useColor = value; Render(); } }
    public event Action? Changed;
    public RefinePreviewSession(ColorPreviewSession colors, string? path = null)
    {
        this.colors = colors; store = new(path);
        try { Workspace = store.Load(); } catch (Exception e) { Status = e.Message; }
        colors.Changed += SourceChanged; SourceChanged();
    }
    private void SourceChanged()
    {
        var next = colors.Published.Looks[colors.Published.DefaultIndex]; var nextRetouch = colors.SavedRetouch;
        bool changed = source != colors.Original;
        if (!changed && recipe == next && retouch == nextRetouch) return;
        source = colors.Original; recipe = next; retouch = nextRetouch;
        if (changed)
        {
            if (!string.Equals(Workspace.SamplePath, colors.Workspace.SamplePath, StringComparison.OrdinalIgnoreCase)) Workspace = new(colors.Workspace.SamplePath, new());
            undo.Clear(); redo.Clear(); Status = "Refine · Các hiệu ứng mặc định ở mức trung tính";
        }
        Render();
    }
    public void Update(RefineSettings settings)
    {
        settings.Validate(); if (settings == Workspace.Settings) return;
        undo.Add(Workspace); if (undo.Count > 100) undo.RemoveAt(0); redo.Clear(); Workspace = new(colors.Workspace.SamplePath, settings);
        Status = "Chưa lưu Refine"; Render();
    }
    public void Undo() { if (undo.Count == 0) return; redo.Push(Workspace); Workspace = undo[^1]; undo.RemoveAt(undo.Count - 1); Status = "Đã hoàn tác · Chưa lưu"; Render(); }
    public void Redo() { if (redo.Count == 0) return; undo.Add(Workspace); Workspace = redo.Pop(); Status = "Đã làm lại · Chưa lưu"; Render(); }
    public void Save() { store.Save(Workspace); colors.PublishRefine(Workspace); Status = "Đã lưu Refine cho ảnh này"; Changed?.Invoke(); }
    private async void Render()
    {
        rendering?.Cancel(); rendering?.Dispose(); rendering = new(); var token = rendering.Token; int current = ++revision;
        if (source is null) { Preview = Before = null; Busy = false; Changed?.Invoke(); return; }
        var image = source; var settings = Workspace.Settings; var color = useColor ? recipe! : new ColorRecipe();
        var faces = string.Equals(retouch?.SamplePath, colors.Workspace.SamplePath, StringComparison.OrdinalIgnoreCase) ? retouch?.Faces ?? [] : [];
        var pixels = new byte[image.PixelWidth * image.PixelHeight * 4]; image.CopyPixels(pixels, image.PixelWidth * 4, 0); Busy = true; Changed?.Invoke();
        try
        {
            await Task.Delay(90, token);
            var result = await Task.Run(() =>
            {
                BitmapSource Bitmap(byte[] p) { var b = BitmapSource.Create(image.PixelWidth, image.PixelHeight, 96, 96, PixelFormats.Bgra32, null, p, image.PixelWidth * 4); b.Freeze(); return b; }
                var baseline = ColorProcessor.Apply(RetouchProcessor.Apply(pixels, image.PixelWidth, image.PixelHeight, faces, token), color, image.PixelWidth, image.PixelHeight, token);
                return (before: Bitmap(baseline), after: Bitmap(RefineProcessor.Apply(baseline, image.PixelWidth, image.PixelHeight, settings, token)));
            }, token);
            if (current != revision) return; Before = result.before; Preview = result.after; Busy = false; Changed?.Invoke();
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { if (current != revision) return; Busy = false; Status = e.Message; Changed?.Invoke(); }
    }
}
