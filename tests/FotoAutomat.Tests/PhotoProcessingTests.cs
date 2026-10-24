using System.Collections.Concurrent;
using System.Text.Json;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;

public sealed class PhotoProcessingTests
{
    // Ảnh giả: gradient theo kích thước (đã xoay EXIF), giải mã theo tỉ lệ; lưu JPEG ghi kích thước ra file.
    private sealed class FakeCodec(int width = 600, int height = 400) : IImageCodec
    {
        public readonly ConcurrentBag<(string Path, double Scale)> Decodes = [];
        public readonly ConcurrentDictionary<string, (int W, int H)> Saved = new();
        public HashSet<string> Broken { get; } = [];
        public (int Width, int Height) Probe(string path) => Broken.Contains(path) ? throw new IOException("hỏng") : (width, height);
        public (byte[] Pixels, int Width, int Height) Decode(string path, double scale)
        {
            if (Broken.Contains(path)) throw new IOException("hỏng");
            Decodes.Add((path, scale));
            int w = Math.Max(1, (int)Math.Round(width * scale)), h = Math.Max(1, (int)Math.Round(height * scale));
            return (Gradient(w, h), w, h);
        }
        public void SaveJpeg(byte[] bgra, int w, int h, string path, int quality = 92) { File.WriteAllText(path, $"{w}x{h}"); Saved[path] = (w, h); }
    }
    private sealed class FakeComposer : IFrameComposer
    {
        public IReadOnlyDictionary<int, SlotImage>? Photos;
        public PrintLayout? Layout;
        public void Compose(PrintLayout layout, IReadOnlyDictionary<int, SlotImage> photos, string framePath, string sheetPath)
        { Layout = layout; Photos = photos; File.WriteAllText(framePath, "frame"); File.WriteAllText(sheetPath, "sheet"); }
    }
    private sealed class FakeAnalyzer : IFaceAnalyzer
    {
        public int Calls;
        public string Name => "Fake";
        public Task<IReadOnlyList<DetectedFace>> DetectAsync(byte[] bgra, int width, int height, CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref Calls); return Task.FromResult<IReadOnlyList<DetectedFace>>([new(.3, .2, .4, .6)]); }
    }

    private static byte[] Gradient(int w, int h) => [.. Enumerable.Range(0, w * h).SelectMany(i => new byte[] { (byte)(120 + i % w * 60 / w), (byte)(150 + i / w * 40 / h), 200, 255 })];
    private static PrintLayout Frame() => new("Khung", false, [new(1, 20, 20, 160, 240), new(2, 220, 20, 160, 240), new(1, 10, 560, 100, 30, Kind: LayoutElementKind.SessionDate)]);

    private static (string Root, string Sessions, Guid SessionId, Guid[] Shots, SqliteCustomerSessionStore Store) Session(ModeFeatures features, int shots = 2)
    {
        string root = Path.Combine(Path.GetTempPath(), "FotoAutomat-tests", Guid.NewGuid().ToString());
        var store = new SqliteCustomerSessionStore(root);
        var snapshot = new SessionSnapshot("Khổ lớn", 2, shots, 0, "p", "Mẫu", JsonSerializer.Serialize(new ColorRecipe(Exposure: .2)), null, "{}",
            new RetouchSettings(EvenSkin: 40), new RefineSettings(SharpenAmount: 50), null, features);
        var session = store.StartPaid(snapshot, new SessionPayment(120000, 0, 120000, "cash"));
        string folder = Path.Combine(root, "Sessions", session.Id.ToString("N"));
        Directory.CreateDirectory(folder);
        var ids = new Guid[shots];
        for (int i = 0; i < shots; i++)
        {
            ids[i] = Guid.NewGuid(); store.RecordShotRequested(session.Id, ids[i], i + 1);
            store.RecordShotReceived(ids[i], Path.Combine(folder, ids[i].ToString("N") + ".jpg"));
        }
        return (root, folder, session.Id, ids, store);
    }

    [Fact]
    public async Task Print_renders_each_slot_at_print_density_and_detects_faces_once_per_shot()
    {
        var (root, folder, id, shots, store) = Session(ModeFeatures.All);
        var codec = new FakeCodec(); var composer = new FakeComposer(); var analyzer = new FakeAnalyzer();
        using var service = new PhotoProcessingService(codec, composer, analyzer, root);
        string path = store.Shots(id)[0].Path!;
        var reports = new List<double>();
        var output = await service.RenderPrintAsync(id, PhotoPipeline.ModeOf(store.Get(id).Snapshot), Frame(), [new(0, shots[0], path), new(1, shots[0], path)], new Progress<double>(reports.Add));

        Assert.Equal(1, analyzer.Calls);
        Assert.Equal((480, 720), (composer.Photos![0].Width, composer.Photos[0].Height)); // 160 × 240 đơn vị ở 300 DPI
        // Khung 600 × 400 cắt giữa theo tỉ lệ 2:3 rộng 266,7 px, cần 480 px nên không phóng to: tỉ lệ 1.
        Assert.Contains(codec.Decodes, d => d.Scale == 1);
        Assert.Equal(LayoutElementKind.Text, composer.Layout!.Slots[2].Kind); // ngày in đã điền
        Assert.True(File.Exists(output.SheetPath) && File.Exists(output.FramePath) && File.Exists(output.LayoutPath));
        Assert.StartsWith(folder, output.SheetPath);
        var job = Assert.Single(service.Jobs(id));
        Assert.Equal((PhotoJobKind.Print, PhotoJobState.Done), (job.Kind, job.State));
        Assert.NotNull(job.DurationMs);
    }

    [Fact]
    public async Task Without_retouch_no_face_detection_runs()
    {
        var (root, _, id, shots, store) = Session(ModeFeatures.Default);
        var analyzer = new FakeAnalyzer();
        using var service = new PhotoProcessingService(new FakeCodec(), new FakeComposer(), analyzer, root);
        await service.RenderPrintAsync(id, PhotoPipeline.ModeOf(store.Get(id).Snapshot), Frame(), [new(0, shots[0], store.Shots(id)[0].Path!)]);
        Assert.Equal(0, analyzer.Calls);
    }

    [Fact]
    public async Task Full_resolution_jobs_cover_kept_shots_in_background_and_skip_discarded()
    {
        var (root, folder, id, shots, store) = Session(ModeFeatures.All, 3);
        store.MarkShot(shots[1], ShotState.Discarded);
        var codec = new FakeCodec();
        using var service = new PhotoProcessingService(codec, new FakeComposer(), new FakeAnalyzer(), root);
        Assert.Equal(2, service.EnqueueFull(id));
        Assert.Equal(0, service.EnqueueFull(id));
        await service.WhenIdleAsync(TimeSpan.FromSeconds(20));
        var jobs = service.Jobs(id);
        Assert.All(jobs, j => Assert.Equal(PhotoJobState.Done, j.State));
        Assert.Equal(new HashSet<Guid?> { shots[0], shots[2] }, jobs.Select(j => j.ShotId).ToHashSet());
        Assert.All(jobs, j => Assert.Equal((600, 400), codec.Saved[j.Output! + ".tmp"]));
        Assert.True(File.Exists(Path.Combine(folder, "final", shots[0].ToString("N") + ".jpg")));
        Assert.False(File.Exists(Path.Combine(folder, "final", shots[1].ToString("N") + ".jpg")));
    }

    [Fact]
    public async Task Broken_original_fails_after_retries_and_print_failure_is_recorded()
    {
        var (root, _, id, shots, store) = Session(ModeFeatures.Default, 1);
        var codec = new FakeCodec(); string path = store.Shots(id)[0].Path!; codec.Broken.Add(path);
        using var service = new PhotoProcessingService(codec, new FakeComposer(), null, root);
        await Assert.ThrowsAsync<IOException>(() => service.RenderPrintAsync(id, PhotoPipeline.ModeOf(store.Get(id).Snapshot), Frame(), [new(0, shots[0], path)]));
        service.EnqueueFull(id);
        await service.WhenIdleAsync(TimeSpan.FromSeconds(20));
        var jobs = service.Jobs(id);
        Assert.Equal(PhotoJobState.Failed, jobs.Single(j => j.Kind == PhotoJobKind.Print).State);
        var full = jobs.Single(j => j.Kind == PhotoJobKind.Full);
        Assert.Equal((PhotoJobState.Failed, PhotoProcessingService.MaxAttempts), (full.State, full.Attempts));
        Assert.Equal("hỏng", full.Error);
    }

    [Fact]
    public async Task Restart_cancels_unfinished_print_and_resumes_full_jobs()
    {
        var (root, _, id, shots, _) = Session(ModeFeatures.Default, 1);
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(root, "booth.db")};Pooling=False"))
        {
            connection.Open();
            using var insert = connection.CreateCommand();
            string now = DateTimeOffset.UtcNow.ToString("O");
            insert.CommandText = $"INSERT INTO photo_jobs (id, session_id, shot_id, kind, state, attempts, created_at, updated_at) VALUES ('{Guid.NewGuid()}', '{id}', NULL, 'Print', 'Running', 1, '{now}', '{now}'), ('{Guid.NewGuid()}', '{id}', '{shots[0]}', 'Full', 'Running', 1, '{now}', '{now}')";
            insert.ExecuteNonQuery();
        }
        using var service = new PhotoProcessingService(new FakeCodec(), new FakeComposer(), null, root);
        Assert.Equal(1, service.Recover());
        await service.WhenIdleAsync(TimeSpan.FromSeconds(20));
        var jobs = service.Jobs(id);
        Assert.Equal(PhotoJobState.Cancelled, jobs.Single(j => j.Kind == PhotoJobKind.Print).State);
        Assert.Equal(PhotoJobState.Done, jobs.Single(j => j.Kind == PhotoJobKind.Full).State);
    }
}

public sealed class PrintWiringTests
{
    [Fact]
    public void Slots_map_to_kept_shots_after_a_retake()
    {
        var session = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var first = new SessionShot(Guid.NewGuid(), session, 1, "a.jpg", ShotState.Kept, now);
        var retaken = new SessionShot(Guid.NewGuid(), session, 2, "b.jpg", ShotState.Discarded, now);
        var second = new SessionShot(Guid.NewGuid(), session, 3, "c.jpg", ShotState.Kept, now);
        var slots = PhotoProcessingService.MapSlots([(0, 2), (2, 1), (3, 2)], ["a.jpg", "C.JPG"], [first, retaken, second]);
        Assert.Equal([second.Id, first.Id, second.Id], slots.Select(s => s.ShotId));
        Assert.Equal([0, 2, 3], slots.Select(s => s.SlotIndex));
        Assert.Throws<ArgumentException>(() => PhotoProcessingService.MapSlots([(0, 3)], ["a.jpg", "c.jpg"], [first, second]));
        Assert.Throws<InvalidOperationException>(() => PhotoProcessingService.MapSlots([(0, 1)], ["b.jpg"], [retaken]));
    }

    private static CustomerPreviewFlow AtPrinting()
    {
        var flow = CustomerPreviewFlowTests.AtCapture();
        for (int i = 0; i < 420 && flow.Screen == CustomerScreen.Capture; i++) flow.Tick();
        flow.Next(); flow.Next();
        for (int i = 0; i < flow.Slots.Length; i++) flow.AssignSlot(i);
        flow.Next();
        Assert.Equal(CustomerScreen.Printing, flow.Screen);
        return flow;
    }

    [Fact]
    public void Real_print_progress_replaces_the_simulation_and_errors_block_finishing()
    {
        var flow = AtPrinting();
        flow.ReportPrintProgress(30); flow.Tick(); flow.Tick();
        Assert.Equal(30, flow.PrintProgress);
        flow.ReportPrintProgress(100); Assert.True(flow.CanFinishPrint);
        flow.FailPrint("lỗi"); Assert.False(flow.CanFinishPrint); Assert.True(flow.HasPrintError);
        flow.Restart();
        Assert.Equal(("", 0), (flow.PrintError, flow.PrintProgress));
        flow.ReportPrintProgress(50); flow.FailPrint("x");
        Assert.Equal(("", 0), (flow.PrintError, flow.PrintProgress)); // ngoài màn In thì bỏ qua
    }

    [Fact]
    public void Without_real_print_the_simulation_still_finishes()
    {
        var flow = AtPrinting();
        for (int i = 0; i < 5; i++) flow.Tick();
        Assert.True(flow.CanFinishPrint);
    }
}

public sealed class PhotoPipelineTests
{
    private static byte[] Gradient(int w, int h) => [.. Enumerable.Range(0, w * h).SelectMany(i => new byte[] { (byte)(60 + i % w * 140 / w), (byte)(90 + i / w * 120 / h), 180, 255 })];

    [Fact]
    public void Center_crop_and_scale_match_uniform_to_fill()
    {
        Assert.Equal((100d, 0d, 400d, 600d), PhotoPipeline.CenterCrop(600, 600, 2d / 3));
        Assert.Equal((0d, 150d, 600d, 300d), PhotoPipeline.CenterCrop(600, 600, 2));
        Assert.Equal(.25, PhotoPipeline.PrintScale(6000, 4000, new(667, 1000)), 3);
        Assert.Equal(1, PhotoPipeline.PrintScale(600, 400, new(4000, 6000)));
    }

    [Fact]
    public void Print_path_matches_the_full_path_downscaled_and_cropped()
    {
        // Vignette và mist tính theo cả khung: xử lý ở mật độ in rồi cắt phải ra gần như ảnh full đã thu nhỏ và cắt.
        var mode = new ShootingMode(new ColorRecipe(Contrast: 20) { Vignette = -60, BlurGlow = new BlurGlowSettings { MistAmount = 40 } }, new(), new(), ModeFeatures.All, []);
        int w = 600, h = 400; var slot = new SlotTarget(100, 150);
        var full = PhotoPipeline.Render(Gradient(w, h), w, h, mode, [], 1);
        var expected = PhotoPipeline.CropToSlot(full, w, h, slot);
        double s = PhotoPipeline.PrintScale(w, h, slot);
        int sw = (int)Math.Round(w * s), sh = (int)Math.Round(h * s);
        var small = PhotoPipeline.CropToSlot(Gradient(w, h), w, h, new SlotTarget(sw, sh)); // cùng tỉ lệ khung: chỉ thu nhỏ
        var print = PhotoPipeline.CropToSlot(PhotoPipeline.Render(small, sw, sh, mode, [], 1), sw, sh, slot);
        double mad = expected.Zip(print).Where((_, i) => i % 4 != 3).Average(p => Math.Abs(p.First - p.Second));
        Assert.True(mad < 3, $"mean abs diff {mad:0.00}");
    }

    [Fact]
    public void Mode_is_rebuilt_from_the_session_snapshot()
    {
        var snapshot = new SessionSnapshot("Khổ", 2, 4, 0, "p", "n", JsonSerializer.Serialize(new ColorRecipe(Exposure: .4)), null, "{}", new RetouchSettings(EvenSkin: 10), null, null, ModeFeatures.All);
        var mode = PhotoPipeline.ModeOf(snapshot);
        Assert.Equal(.4, mode.Color.Exposure); Assert.Equal(10, mode.Retouch.EvenSkin); Assert.Equal(new RefineSettings(), mode.Refine); Assert.Equal(ModeFeatures.All, mode.Features);
    }
}
