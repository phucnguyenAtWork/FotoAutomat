using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

/// <summary>
/// Đo xử lý ảnh trên máy booth (--benchmark-processing ảnh.jpg [--frame khung.fotolayout] [--out thư mục]).
/// Từ một ảnh chân dung tạo ảnh đúng độ phân giải camera có 1, 4, 8 mặt (ghép lưới), chạy chế độ chỉ màu và đầy đủ:
/// từng bước ở cỡ ô in và ảnh full, cùng job in trọn vẹn (thời gian khách chờ ở màn In, chưa tính máy in).
/// Chạy trên thư mục tạm, không đụng dữ liệu booth. Đóng giao diện khách trước khi đo.
/// </summary>
public static class ProcessingBenchmark
{
    public sealed record Stage(string Name, double Ms, long WorkingSetMb);
    public sealed record Run(int Faces, string Mode, int FacesFound, string Evidence, Stage[] Print, Stage[] Full, double PrintJobMs, double FullJobMs, double GcPauseMs, long PeakWorkingSetMb);
    public sealed record Report(DateTimeOffset At, string Machine, string Cpu, int LogicalCores, long MemoryMb, string Photo, int Width, int Height, string Frame, string Detector, Run[] Runs);

    public static async Task<string> RunAsync(string photo, string? framePath, string? outDirectory)
    {
        string work = Path.Combine(Path.GetTempPath(), "FotoAutomat-benchmark-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var codec = new WpfImageCodec();
            var analyzer = SampleFaceDetection.CustomerAnalyzer();
            var frame = LoadFrame(framePath);
            var (width, height) = codec.Probe(photo);
            var runs = new List<Run>();
            foreach (int faces in new[] { 1, 4, 8 })
            {
                string source = faces == 1 ? photo : Composite(codec, photo, faces, width, height, Path.Combine(work, $"group-{faces}.jpg"));
                foreach (var (name, mode) in Modes())
                    runs.Add(await Measure(codec, analyzer, frame, source, faces, name, mode, work));
            }
            var report = new Report(DateTimeOffset.Now, Environment.MachineName, Cpu(), Environment.ProcessorCount, GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1048576,
                Path.GetFileName(photo), width, height, frame.Name, analyzer.Name, [.. runs]);
            string folder = outDirectory ?? Path.Combine(new GeneralSettingsStore().Load().DataDirectory, "Benchmarks");
            Directory.CreateDirectory(folder);
            string stem = Path.Combine(folder, $"benchmark-{Environment.MachineName}-{DateTime.Now:yyyyMMdd-HHmm}");
            File.WriteAllText(stem + ".json", JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(stem + ".md", Markdown(report));
            return stem + ".md";
        }
        finally
        {
            try { Directory.Delete(work, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    private static IEnumerable<(string Name, ShootingMode Mode)> Modes()
    {
        var look = new ColorRecipe(Exposure: .1, Contrast: 12, Saturation: 5) { Vignette = -20, BlurGlow = new BlurGlowSettings { MistAmount = 35, GlowAmount = 25 } };
        var retouch = new RetouchSettings(Blemishes: 50, DarkCircles: 40, EvenSkin: 60, Contouring: 20, TeethBrightness: 40, LeftIris: 15, RightIris: 15);
        var refine = new RefineSettings(SharpenAmount: 60, LuminanceNoise: 20, GrainImpact: 20);
        yield return ("Chỉ màu", ShootingMode.Compose(look, retouch, refine, ModeFeatures.Default));
        yield return ("Đầy đủ", ShootingMode.Compose(look, retouch, refine, ModeFeatures.All));
    }

    private static async Task<Run> Measure(IImageCodec codec, IFaceAnalyzer analyzer, PrintLayout frame, string source, int faceCount, string name, ShootingMode mode, string work)
    {
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        var pause = GC.GetTotalPauseDuration();
        long peak = 0;
        Stage Timed(string label, Action action)
        {
            var clock = Stopwatch.StartNew(); action(); double ms = clock.Elapsed.TotalMilliseconds;
            long memory = Environment.WorkingSet / 1048576; peak = Math.Max(peak, memory);
            return new(label, Math.Round(ms, 1), memory);
        }
        var (w, h) = codec.Probe(source);
        IReadOnlyList<DetectedFace> faces = [];
        bool retouch = mode.Features.Retouch && mode.Retouch != new RetouchSettings();
        var print = new List<Stage>();
        if (retouch)
            print.Add(Timed("Nhận diện mặt (1920 px)", () =>
            {
                var (pixels, dw, dh) = codec.Decode(source, Math.Min(1, 1920d / Math.Max(w, h)));
                faces = analyzer.DetectAsync(pixels, dw, dh).GetAwaiter().GetResult();
            }));
        var slot = SlotTarget.Of(frame.Slots.Where(s => s.IsVisible && s.Kind == LayoutElementKind.Photo).OrderByDescending(s => s.Width * s.Height).First(), frame.Dpi);
        print.AddRange(Pipeline(codec, source, PhotoPipeline.PrintScale(w, h, slot), mode, faces, Timed, slot, null));
        var full = Pipeline(codec, source, 1, mode, faces, Timed, null, Path.Combine(work, "full.jpg"));

        // Job in trọn vẹn: phiên tạm, mọi ô dùng ảnh này, ghép khung và tờ in như màn In.
        string root = Path.Combine(work, $"db-{faceCount}-{name.GetHashCode():x}");
        var store = new SqliteCustomerSessionStore(root);
        var session = store.StartPaid(new SessionSnapshot("Đo", 2, 1, 0, "bench", name, JsonSerializer.Serialize(mode.Color), null, "{}", mode.Retouch, mode.Refine, null, mode.Features),
            new SessionPayment(0, 0, 0, "cash"));
        string folder = Path.Combine(root, "Sessions", session.Id.ToString("N")); Directory.CreateDirectory(folder);
        var shot = Guid.NewGuid(); string original = Path.Combine(folder, shot.ToString("N") + ".jpg"); File.Copy(source, original);
        store.RecordShotRequested(session.Id, shot, 1); store.RecordShotReceived(shot, original); store.MarkShot(shot, ShotState.Kept);
        using var service = new PhotoProcessingService(codec, new WpfFrameComposer(new PrinterConfiguration()), analyzer, root);
        var slots = frame.Slots.Where(s => s.IsVisible).Select((s, i) => (s, i)).Where(x => x.s.Kind == LayoutElementKind.Photo).Select(x => new PrintSlot(x.i, shot, original)).ToArray();
        var clock = Stopwatch.StartNew();
        await service.RenderPrintAsync(session.Id, mode, frame, slots);
        double printJob = clock.Elapsed.TotalMilliseconds;
        service.EnqueueFull(session.Id);
        clock.Restart(); await service.WhenIdleAsync(TimeSpan.FromMinutes(10));
        double fullJob = clock.Elapsed.TotalMilliseconds;
        peak = Math.Max(peak, Environment.WorkingSet / 1048576);
        var found = faces.Count == 0 ? "—" : string.Join(" ", faces.GroupBy(f => f.Evidence).Select(g => $"{g.Key}×{g.Count()}"));
        return new(faceCount, name, faces.Count, found, [.. print], [.. full], Math.Round(printJob), Math.Round(fullJob), Math.Round((GC.GetTotalPauseDuration() - pause).TotalMilliseconds), peak);
    }

    // Cùng các bước của PhotoPipeline, đo riêng từng bước.
    private static Stage[] Pipeline(IImageCodec codec, string source, double scale, ShootingMode mode, IReadOnlyList<DetectedFace> faces, Func<string, Action, Stage> timed, SlotTarget? slot, string? save)
    {
        byte[] pixels = []; int w = 0, h = 0; var stages = new List<Stage>();
        stages.Add(timed($"Giải mã (×{scale:0.###})", () => (pixels, w, h) = codec.Decode(source, scale)));
        stages[^1] = stages[^1] with { Name = stages[^1].Name + $" {w}×{h}" };
        if (mode.Retouch != new RetouchSettings() && faces.Count > 0) stages.Add(timed("Retouch", () => pixels = RetouchProcessor.Apply(pixels, w, h, faces, mode.Retouch)));
        stages.Add(timed(mode.Color.BlurGlow is { IsActive: true } ? "Màu + Blur & Glow" : "Màu", () => pixels = ColorProcessor.Apply(pixels, mode.Color, w, h)));
        if (mode.Refine != new RefineSettings())
        {
            stages.Add(timed("Refine Clean", () => pixels = RefineProcessor.Clean(pixels, w, h, mode.Refine)));
            stages.Add(timed("Refine Finish", () => pixels = RefineProcessor.Finish(pixels, w, h, mode.Refine, 1)));
        }
        if (slot is not null) stages.Add(timed($"Cắt vào ô {slot.Width}×{slot.Height}", () => PhotoPipeline.CropToSlot(pixels, w, h, slot)));
        if (save is not null) stages.Add(timed("Ghi JPEG", () => codec.SaveJpeg(pixels, w, h, save)));
        return [.. stages];
    }

    // Ảnh nhóm giả lập: lưới bản thu nhỏ của ảnh chân dung, cùng kích thước ảnh camera.
    private static string Composite(IImageCodec codec, string photo, int faces, int width, int height, string path)
    {
        int columns = faces == 4 ? 2 : 4, rows = faces / columns;
        double scale = Math.Min(1d / columns, 1d / rows);
        var (cell, cw, ch) = codec.Decode(photo, scale);
        var tile = BitmapSource.Create(cw, ch, 96, 96, PixelFormats.Bgra32, null, cell, cw * 4);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new System.Windows.Rect(0, 0, width, height));
            for (int r = 0; r < rows; r++) for (int c = 0; c < columns; c++)
                dc.DrawImage(tile, new System.Windows.Rect(c * width / (double)columns + (width / (double)columns - cw) / 2, r * height / (double)rows + (height / (double)rows - ch) / 2, cw, ch));
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
        var pixels = new byte[width * height * 4]; new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0).CopyPixels(pixels, width * 4, 0);
        codec.SaveJpeg(pixels, width, height, path, 95);
        return path;
    }

    private static PrintLayout LoadFrame(string? path)
    {
        if (path is not null) return LegacyStripMigration.Convert(new PrintLayoutStore().Load(path));
        try { if (new PrintLayoutStore().LoadPublished().FirstOrDefault() is { } published) return LegacyStripMigration.Convert(published); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidDataException) { }
        return new PrintLayout("Khung đo 2 × 6 · 3 ô", false, [new(1, 10, 40, 180, 160), new(2, 10, 220, 180, 160), new(3, 10, 400, 180, 160)], SingleStrip: true);
    }

    private static string Cpu()
    {
        try { return Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", null) as string ?? "?"; }
        catch (Exception error) when (error is System.Security.SecurityException or IOException) { return "?"; }
    }

    private static string Markdown(Report r)
    {
        var c = CultureInfo.InvariantCulture; var s = new StringBuilder();
        s.AppendLine($"# Đo xử lý ảnh · {r.Machine} · {r.At:dd/MM/yyyy HH:mm}");
        s.AppendLine();
        s.AppendLine($"- CPU: {r.Cpu.Trim()} · {r.LogicalCores} luồng · RAM {r.MemoryMb / 1024.0:0.#} GB");
        s.AppendLine($"- Ảnh: {r.Photo} {r.Width}×{r.Height} ({r.Width * (long)r.Height / 1e6:0.#} MP) · khung: {r.Frame} · nhận diện: {r.Detector}");
        s.AppendLine("- Ảnh 4 và 8 mặt là lưới bản thu nhỏ của ảnh chân dung, cùng độ phân giải camera. Chế độ đầy đủ: Retouch, Refine (sharpen, giảm nhiễu, grain), Blur & Glow (Mist, Glow).");
        s.AppendLine();
        s.AppendLine("## Thời gian khách chờ ở màn In (chưa tính máy in) và ảnh full chạy nền");
        s.AppendLine();
        s.AppendLine("| Mặt | Chế độ | Tìm thấy | Job in (khách chờ) | Ảnh full (nền) | GC dừng | RAM đỉnh |");
        s.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var run in r.Runs)
            s.AppendLine(string.Create(c, $"| {run.Faces} | {run.Mode} | {run.Evidence} | {run.PrintJobMs / 1000:0.0} s | {run.FullJobMs / 1000:0.0} s | {run.GcPauseMs:0} ms | {run.PeakWorkingSetMb} MB |"));
        foreach (var run in r.Runs)
        {
            s.AppendLine();
            s.AppendLine($"### {run.Faces} mặt · {run.Mode}");
            s.AppendLine();
            s.AppendLine("| Đường | Bước | ms | RAM sau bước |");
            s.AppendLine("|---|---|---|---|");
            foreach (var st in run.Print) s.AppendLine(string.Create(c, $"| In (1 ô) | {st.Name} | {st.Ms:0} | {st.WorkingSetMb} MB |"));
            foreach (var st in run.Full) s.AppendLine(string.Create(c, $"| Full | {st.Name} | {st.Ms:0} | {st.WorkingSetMb} MB |"));
        }
        return s.ToString();
    }
}
