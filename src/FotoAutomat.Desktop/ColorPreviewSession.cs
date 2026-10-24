using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

public sealed class ColorPreviewSession
{
    public static ColorPreviewSession Shared { get; } = new();
    private readonly ColorWorkspaceStore store;
    private RetouchWorkspace savedRetouch = new(null, []);
    private readonly RetouchWorkspaceStore retouchStore;
    private RefineWorkspace savedRefine = new(null, new());
    private readonly RefineWorkspaceStore refineStore;
    internal RetouchWorkspace SavedRetouch => savedRetouch;
    internal RefineWorkspace SavedRefine => savedRefine;
    private (byte[] Source, FaceRetouch[] Faces, byte[] Result)? retouchedInput;

    /// <summary>BGRA input of ColorProcessor for the full previews: the sample after Retouch. Null when no sample is loaded.</summary>
    internal (byte[] Pixels, int Width, int Height)? ColorInput()
    {
        if (pixels is not { } source) return null;
        var faces = string.Equals(savedRetouch.SamplePath, Workspace.SamplePath, StringComparison.OrdinalIgnoreCase) ? savedRetouch.Faces : [];
        if (faces.Length == 0) return (source, width, height);
        if (retouchedInput is not { } cached || !ReferenceEquals(cached.Source, source) || !ReferenceEquals(cached.Faces, faces))
            retouchedInput = cached = (source, faces, RetouchProcessor.Apply(source, width, height, faces, CancellationToken.None));
        return (cached.Result, width, height);
    }
    private byte[]? pixels;
    private int width, height, revision;
    private CancellationTokenSource? rendering;
    private Dictionary<ColorRecipe, BitmapSource> renderCache = new(ReferenceEqualityComparer.Instance);
    // Ảnh khách thấy (thẻ chế độ, live view mô phỏng) theo tính năng của chế độ, nên cache kèm tính năng.
    private Dictionary<ColorRecipe, (ModeFeatures Features, BitmapSource Image)> modeRenderCache = new(ReferenceEqualityComparer.Instance), liveRenderCache = new(ReferenceEqualityComparer.Instance);
    /// <summary>Tính năng chế độ đã đổi trong quản trị: render lại ảnh khách thấy.</summary>
    public void ModesChanged() => RequestRender();
    private static Dictionary<string, ModeFeatures> LoadModeFeatures()
    {
        try { return new ModeCardStore().Load().ToDictionary(p => p.Key, p => p.Value.EffectiveFeatures); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) { return []; }
    }
    private object? cacheContext, liveCacheSource;
    private readonly List<ColorWorkspace> undo = [];
    private readonly Stack<ColorWorkspace> redo = new();
    private int lastEditIndex = -1;
    private long lastEditTicks;
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;
    public ColorWorkspace Workspace { get; private set; } = new(null, [new(), new("Ấm", Warmth: 25), new("Trắng đen", Monochrome: true)]);
    public ColorWorkspace Published { get; private set; } = new(null, [new(), new("Ấm", Warmth: 25), new("Trắng đen", Monochrome: true)]);
    public BitmapSource?[] PublishedPreviews { get; private set; } = new BitmapSource?[3];
    public BitmapSource?[] LivePreviews { get; private set; } = new BitmapSource?[3];
    public BitmapSource? Original { get; private set; }
    public ImageExposureMetadata ExposureMetadata { get; private set; } = new();
    public BitmapSource?[] Previews { get; private set; } = new BitmapSource?[3];
    public string Status { get; private set; } = "Chọn ảnh mẫu";
    public bool Busy { get; private set; }
    public event Action? Changed;
    public ColorPreviewSession(string? workspacePath = null)
    {
        store = new(workspacePath);
        retouchStore = new(workspacePath is null ? null : workspacePath + ".retouch.json");
        refineStore = new(workspacePath is null ? null : workspacePath + ".refine.json");
        try { savedRefine = refineStore.Load(); } catch (Exception e) { Status = e.Message; }
        try { savedRetouch = retouchStore.Load(); } catch (Exception e) { Status = e.Message; }
        try
        {
            var loaded = store.Load();
            if (loaded.Looks.Length is < 1 or > 100 || loaded.DefaultIndex < 0 || loaded.DefaultIndex >= loaded.Looks.Length) throw new InvalidDataException("Bộ màu không hợp lệ.");
            foreach (var recipe in loaded.Looks) recipe.Validate();
            Workspace = Published = loaded;
            if (Workspace.SamplePath is { } path && File.Exists(path)) LoadImage(path);
        }
        catch (Exception e) { Status = e.Message; }
    }
    public void LoadImage(string path) => LoadMatchImage(MatchImageReader.Read(path));
    public void LoadMatchImage(MatchImage image)
    {
        var converted = image.Bitmap;
        width = converted.PixelWidth; height = converted.PixelHeight;
        pixels = image.Pixels();
        ExposureMetadata = ImageExposureMetadata.Read(image.Path);
        Original = converted; Workspace = Workspace with { SamplePath = Path.GetFullPath(image.Path) }; RequestRender();
        undo.Clear(); redo.Clear();
    }
    public void Update(int index, ColorRecipe recipe)
    {
        recipe.Validate(); if (Workspace.Looks[index] == recipe) return;
        // A continuous edit of the same preset (slider or wheel drag: updates < 600 ms apart) is one undo step.
        long now = Environment.TickCount64; bool continuing = index == lastEditIndex && now - lastEditTicks < 600 && undo.Count > 0;
        lastEditIndex = index; lastEditTicks = now;
        if (!continuing) { undo.Add(Workspace); if (undo.Count > 100) undo.RemoveAt(0); }
        redo.Clear();
        var looks = Workspace.Looks.ToArray(); looks[index] = recipe; Workspace = Workspace with { Looks = looks }; RequestRender();
    }
    public void Undo() { if (!CanUndo) return; lastEditIndex = -1; redo.Push(Workspace); Workspace = undo[^1]; undo.RemoveAt(undo.Count - 1); RequestRender(); }
    public void RemovePreset(int index)
    {
        string id = Workspace.Looks[index].PresetId;
        var draft = Workspace.RemovePreset(id);
        var published = Published.RemovePreset(id);
        // Persist only the published removal; unrelated drafts must stay unpublished.
        if (published != Published) store.Save(published);
        Workspace = draft; Published = published;
        undo.Clear(); redo.Clear();
        Previews = new BitmapSource?[draft.Looks.Length];
        PublishedPreviews = new BitmapSource?[published.Looks.Length];
        LivePreviews = new BitmapSource?[published.Looks.Length];
        RequestRender();
    }
    public void Redo() { if (!CanRedo) return; lastEditIndex = -1; undo.Add(Workspace); Workspace = redo.Pop(); RequestRender(); }
    public void Save(int? selectedIndex = null)
    {
        var saved = Workspace;
        if (selectedIndex is { } index)
        {
            var recipe = Workspace.Looks[index]; var looks = Published.Looks.ToList(); int existing = looks.FindIndex(r => r.PresetId == recipe.PresetId);
            if (existing < 0) looks.Add(recipe); else looks[existing] = recipe;
            saved = new(Workspace.SamplePath, looks.ToArray()) { DefaultIndex = Published.DefaultIndex };
        }
        store.Save(saved); Published = saved; RequestRender();
    }
    public int AddPreset(ColorRecipe recipe)
    {
        recipe.Validate(); if (Workspace.Looks.Length >= 100) throw new ArgumentException("Tối đa 100 preset.");
        lastEditIndex = -1; undo.Add(Workspace); redo.Clear(); Workspace = Workspace with { Looks = Workspace.Looks.Append(recipe).ToArray() }; RequestRender(); return Workspace.Looks.Length - 1;
    }
    public void SetDefault(int index) { Save(index); SetPublishedDefault(Array.FindIndex(Published.Looks, r => r.PresetId == Workspace.Looks[index].PresetId)); }
    public void SetPublishedDefault(int index) { var saved = Published with { DefaultIndex = index }; store.Save(saved); Published = saved; Workspace = Workspace with { DefaultIndex = Array.FindIndex(Workspace.Looks, r => r.PresetId == Published.Looks[index].PresetId) }; Changed?.Invoke(); }
    public void PublishRetouch(RetouchWorkspace value) { retouchStore.Save(value); savedRetouch = value; RequestRender(); }
    public void PublishRefine(RefineWorkspace value) { refineStore.Save(value); savedRefine = value; RequestRender(); }
    private async void RequestRender()
    {
        rendering?.Cancel(); rendering?.Dispose(); rendering = new CancellationTokenSource(); var token = rendering.Token;
        int current = ++revision;
        if (pixels is null) { Changed?.Invoke(); return; }
        var source = pixels; var looks = Workspace.Looks.ToArray(); var published = Published.Looks.ToArray(); var faces = string.Equals(savedRetouch.SamplePath, Workspace.SamplePath, StringComparison.OrdinalIgnoreCase) ? savedRetouch.Faces : []; int w = width, h = height;
        var refine = string.Equals(savedRefine.SamplePath, Workspace.SamplePath, StringComparison.OrdinalIgnoreCase) ? savedRefine.Settings : new RefineSettings();
        var modeFeatures = LoadModeFeatures();
        ModeFeatures FeaturesOf(ColorRecipe r) => modeFeatures.GetValueOrDefault(r.PresetId, ModeFeatures.Default);
        Busy = true; Status = "Đang cập nhật màu…"; Changed?.Invoke();
        await Task.Delay(90);
        if (current != revision) return;
        try
        {
            BitmapSource Render(ColorRecipe recipe, byte[] input, bool final = true)
            {
                var data = ColorProcessor.Apply(input, recipe, w, h, token);
                if (final) data = RefineProcessor.Apply(data, w, h, ShootingMode.Compose(recipe, null, refine).Refine with { DustSpots = refine.DustSpots }, token);
                var image = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, data, w * 4);
                image.Freeze(); return image;
            }
            // Recipes are immutable records and an edit replaces only the edited one, so unchanged recipes keep their
            // instance: render only recipes not in the cache (dragging one slider re-renders one image, not every preset).
            var context = (source, faces, refine);
            var fullCache = Equals(cacheContext, context) ? renderCache : new Dictionary<ColorRecipe, BitmapSource>(ReferenceEqualityComparer.Instance);
            var modeCache = Equals(cacheContext, context) ? modeRenderCache : new Dictionary<ColorRecipe, (ModeFeatures, BitmapSource)>(ReferenceEqualityComparer.Instance);
            var liveCache = ReferenceEquals(liveCacheSource, source) ? liveRenderCache : new Dictionary<ColorRecipe, (ModeFeatures, BitmapSource)>(ReferenceEqualityComparer.Instance);
            var rendered = await Task.Run(() =>
            {
                byte[]? retouched = null;
                var nextFull = new Dictionary<ColorRecipe, BitmapSource>(ReferenceEqualityComparer.Instance);
                var nextMode = new Dictionary<ColorRecipe, (ModeFeatures Features, BitmapSource Image)>(ReferenceEqualityComparer.Instance);
                var nextLive = new Dictionary<ColorRecipe, (ModeFeatures Features, BitmapSource Image)>(ReferenceEqualityComparer.Instance);
                BitmapSource Cached(Dictionary<ColorRecipe, (ModeFeatures Features, BitmapSource Image)> next, Dictionary<ColorRecipe, (ModeFeatures, BitmapSource)> previous, ColorRecipe r, Func<ModeFeatures, BitmapSource> render)
                {
                    var f = FeaturesOf(r);
                    if (next.TryGetValue(r, out var hit) && hit.Features == f) return hit.Image;
                    if (previous.TryGetValue(r, out var old) && old.Item1 == f) { next[r] = old; return old.Item2; }
                    var image = render(f); next[r] = (f, image); return image;
                }
                BitmapSource Full(ColorRecipe r)
                {
                    if (nextFull.TryGetValue(r, out var hit) || fullCache.TryGetValue(r, out hit)) return nextFull[r] = hit;
                    retouched ??= faces.Length == 0 ? source : RetouchProcessor.Apply(source, w, h, faces, token);
                    return nextFull[r] = Render(r, retouched);
                }
                // Màn khách: chỉ các tính năng chế độ bật. Retouch dùng mặt của ảnh mẫu, điểm bụi của ảnh mẫu chỉ áp cho ảnh mẫu.
                BitmapSource Mode(ColorRecipe r) => Cached(nextMode, modeCache, r, f =>
                {
                    var mode = ShootingMode.Compose(r, null, refine, f);
                    retouched ??= faces.Length == 0 ? source : RetouchProcessor.Apply(source, w, h, faces, token);
                    var data = ColorProcessor.Apply(f.Retouch ? retouched : source, mode.Color, w, h, token);
                    data = RefineProcessor.Apply(data, w, h, f.Refine ? mode.Refine with { DustSpots = refine.DustSpots } : mode.Refine, token);
                    var image = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, data, w * 4);
                    image.Freeze(); return image;
                });
                BitmapSource Live(ColorRecipe r) => Cached(nextLive, liveCache, r, f => Render(ColorProcessor.ForLiveView(ShootingMode.Compose(r, null, null, f).Color), source, false));
                var full = looks.Select(Full).ToArray(); var saved = published.Select(Mode).ToArray(); var live = published.Select(Live).ToArray();
                return (full, saved, live, nextFull, nextMode, nextLive);
            });
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (current != revision) return;
                renderCache = rendered.nextFull; modeRenderCache = rendered.nextMode; cacheContext = context; liveRenderCache = rendered.nextLive; liveCacheSource = source;
                Previews = rendered.full; PublishedPreviews = rendered.saved; LivePreviews = rendered.live; Busy = false; Status = Path.GetFileName(Workspace.SamplePath) ?? "Ảnh mẫu"; Changed?.Invoke();
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            await Application.Current.Dispatcher.InvokeAsync(() => { if (current != revision) return; Busy = false; Status = e.Message; Changed?.Invoke(); });
        }
    }
}

