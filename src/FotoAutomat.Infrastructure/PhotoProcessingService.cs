using System.Diagnostics;
using System.Text.Json;
using FotoAutomat.Core;

namespace FotoAutomat.Infrastructure;

public enum PhotoJobKind { Print, Full }
public enum PhotoJobState { Queued, Running, Done, Failed, Cancelled }
public sealed record PhotoJob(Guid Id, Guid SessionId, Guid? ShotId, PhotoJobKind Kind, PhotoJobState State, string? Output, string? Error, int Attempts, long? DurationMs, long? WorkingSetMb);
/// <summary>Ảnh của một ô: vị trí ô trong danh sách ô hiển thị của khung, shot và file gốc.</summary>
public sealed record PrintSlot(int SlotIndex, Guid ShotId, string OriginalPath);
public sealed record PrintOutput(string FramePath, string SheetPath, string LayoutPath);

/// <summary>
/// Xử lý ảnh sau chụp cho phiên đã trả tiền. Đường in chạy ngay (khách đang chờ), ở mật độ in của ô; ảnh full độ phân giải
/// cho QR chạy nền từng ảnh một, ưu tiên thấp và tạm dừng khi có job in. Luôn xử lý lại từ ảnh gốc, ghi qua file tạm rồi đổi tên,
/// không bao giờ sửa ảnh gốc. Mặt nhận diện một lần cho mỗi shot và dùng chung cho cả hai đường.
/// </summary>
public sealed class PhotoProcessingService : IDisposable
{
    public const int MaxAttempts = 3;
    private const int DetectionLongEdge = 1920;
    private readonly IImageCodec codec;
    private readonly IFrameComposer composer;
    private readonly IFaceAnalyzer? analyzer;
    private readonly string root;
    private readonly SqliteCustomerSessionStore sessions;
    private readonly SemaphoreSlim signal = new(0);
    private readonly ManualResetEventSlim notPrinting = new(true);
    private readonly CancellationTokenSource lifetime = new();
    private readonly object start = new();
    private Thread? worker;
    private int printing;

    public PhotoProcessingService(IImageCodec codec, IFrameComposer composer, IFaceAnalyzer? analyzer, string? directory = null)
    {
        this.codec = codec; this.composer = composer; this.analyzer = analyzer;
        root = directory ?? BoothDatabase.DefaultRoot;
        sessions = new SqliteCustomerSessionStore(root);
    }

    /// <summary>File khung (PNG), tờ in (PNG, đã ghép đôi nếu 2 × 6) và bản sao khung trong thư mục phiên/print.</summary>
    public async Task<PrintOutput> RenderPrintAsync(Guid sessionId, ShootingMode mode, PrintLayout frame, IReadOnlyList<PrintSlot> slots, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (slots.Count == 0) throw new ArgumentException("Chưa có ảnh trong khung.");
        var job = Insert(sessionId, null, PhotoJobKind.Print, PhotoJobState.Running);
        Interlocked.Increment(ref printing); notPrinting.Reset();
        var clock = Stopwatch.StartNew();
        try
        {
            var output = await Task.Run(() =>
            {
                var visible = frame.Slots.Where(s => s.IsVisible).ToArray();
                string folder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(slots[0].OriginalPath))!, "print");
                Directory.CreateDirectory(folder);
                string layoutPath = Path.Combine(folder, "frame.fotolayout");
                new PrintLayoutStore(root).Save(layoutPath, frame);
                var photos = new Dictionary<int, SlotImage>();
                for (int i = 0; i < slots.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var slot = slots[i];
                    if (slot.SlotIndex < 0 || slot.SlotIndex >= visible.Length || visible[slot.SlotIndex].Kind != LayoutElementKind.Photo) throw new ArgumentException($"Ô {slot.SlotIndex} không phải ô ảnh.");
                    var target = SlotTarget.Of(visible[slot.SlotIndex], frame.Dpi);
                    var faces = mode.Features.Retouch && mode.Retouch != new RetouchSettings() ? Faces(slot.ShotId, slot.OriginalPath, ct) : [];
                    var (w, h) = codec.Probe(slot.OriginalPath);
                    var (pixels, dw, dh) = codec.Decode(slot.OriginalPath, PhotoPipeline.PrintScale(w, h, target));
                    var rendered = PhotoPipeline.Render(pixels, dw, dh, mode, faces, RefineProcessor.SeedOf(slot.ShotId), ct);
                    photos[slot.SlotIndex] = new(PhotoPipeline.CropToSlot(rendered, dw, dh, target, ct), target.Width, target.Height);
                    progress?.Report((i + 1d) / (slots.Count + 1));
                }
                string framePath = Path.Combine(folder, "frame.png"), sheetPath = Path.Combine(folder, "sheet.png");
                composer.Compose(frame.ResolvePrintData(DateTimeOffset.Now), photos, framePath + ".tmp", sheetPath + ".tmp");
                File.Move(framePath + ".tmp", framePath, true); File.Move(sheetPath + ".tmp", sheetPath, true);
                progress?.Report(1);
                return new PrintOutput(framePath, sheetPath, layoutPath);
            }, ct);
            Finish(job, PhotoJobState.Done, output.SheetPath, null, clock.ElapsedMilliseconds);
            return output;
        }
        catch (Exception error)
        {
            Finish(job, error is OperationCanceledException ? PhotoJobState.Cancelled : PhotoJobState.Failed, null, error.Message, clock.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            if (Interlocked.Decrement(ref printing) == 0) notPrinting.Set();
        }
    }

    /// <summary>Ô ảnh của khách → shot: số ảnh (1, 2, …) là vị trí trong danh sách ảnh đã giữ; shot tìm theo đường dẫn file gốc
    /// (shot bị xóa khi chụp lại vẫn có file nhưng không còn trong danh sách ảnh đã giữ).</summary>
    public static PrintSlot[] MapSlots(IEnumerable<(int SlotIndex, int Photo)> slots, IReadOnlyList<string> keptPaths, IReadOnlyList<SessionShot> shots) =>
        [.. slots.Select(slot =>
        {
            if (slot.Photo < 1 || slot.Photo > keptPaths.Count) throw new ArgumentException($"Không có ảnh {slot.Photo}.");
            string path = keptPaths[slot.Photo - 1];
            var shot = shots.FirstOrDefault(x => x.State != ShotState.Discarded && string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Không tìm thấy shot của ảnh {slot.Photo}.");
            return new PrintSlot(slot.SlotIndex, shot.Id, path);
        })];

    /// <summary>Xếp hàng ảnh full độ phân giải cho mọi shot đã nhận và không bị xóa của phiên (QR chứa toàn bộ ảnh).</summary>
    public int EnqueueFull(Guid sessionId)
    {
        var existing = Jobs(sessionId).Where(j => j.Kind == PhotoJobKind.Full && j.State != PhotoJobState.Failed).Select(j => j.ShotId).ToHashSet();
        int added = 0;
        foreach (var shot in sessions.Shots(sessionId).Where(s => s.Path is not null && s.State != ShotState.Discarded && !existing.Contains(s.Id)))
        {
            Insert(sessionId, shot.Id, PhotoJobKind.Full, PhotoJobState.Queued); added++;
        }
        if (added > 0) Wake();
        return added;
    }

    /// <summary>Khởi động: job in dang dở bị hủy (khách đã đi, phiên chuyển NeedsReview); job full dang dở chạy lại.</summary>
    public int Recover()
    {
        using var connection = BoothDatabase.Open(root);
        var now = BoothDatabase.Time(DateTimeOffset.UtcNow);
        BoothDatabase.Execute(connection, null, "UPDATE photo_jobs SET state = 'Cancelled', error = 'interrupted', updated_at = $now WHERE kind = 'Print' AND state IN ('Queued', 'Running')", ("$now", now));
        int requeued = BoothDatabase.Execute(connection, null, "UPDATE photo_jobs SET state = 'Queued', updated_at = $now WHERE kind = 'Full' AND state = 'Running'", ("$now", now));
        using var count = BoothDatabase.Command(connection, null, "SELECT COUNT(*) FROM photo_jobs WHERE kind = 'Full' AND state = 'Queued'");
        if (Convert.ToInt64(count.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0) Wake();
        return requeued;
    }

    public IReadOnlyList<PhotoJob> Jobs(Guid sessionId)
    {
        using var connection = BoothDatabase.Open(root);
        using var command = BoothDatabase.Command(connection, null,
            "SELECT id, session_id, shot_id, kind, state, output, error, attempts, duration_ms, working_set_mb FROM photo_jobs WHERE session_id = $id ORDER BY created_at", ("$id", sessionId.ToString()));
        using var r = command.ExecuteReader();
        var jobs = new List<PhotoJob>();
        while (r.Read())
            jobs.Add(new(Guid.Parse(r.GetString(0)), Guid.Parse(r.GetString(1)), r.IsDBNull(2) ? null : Guid.Parse(r.GetString(2)), Enum.Parse<PhotoJobKind>(r.GetString(3)), Enum.Parse<PhotoJobState>(r.GetString(4)),
                r.IsDBNull(5) ? null : r.GetString(5), r.IsDBNull(6) ? null : r.GetString(6), r.GetInt32(7), r.IsDBNull(8) ? null : r.GetInt64(8), r.IsDBNull(9) ? null : r.GetInt64(9)));
        return jobs;
    }

    /// <summary>Chờ hàng đợi full trống (dùng cho kiểm thử và khi đóng app có chủ đích).</summary>
    public async Task WhenIdleAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            using (var connection = BoothDatabase.Open(root))
            using (var command = BoothDatabase.Command(connection, null, "SELECT COUNT(*) FROM photo_jobs WHERE kind = 'Full' AND state IN ('Queued', 'Running')"))
                if (Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) == 0) return;
            await Task.Delay(50);
        }
        throw new TimeoutException("Hàng đợi ảnh full chưa xong.");
    }

    // Mặt theo tọa độ chuẩn hóa của khung đã xoay EXIF, nhận diện trên bản thu nhỏ; lưu lại để đường in và đường full dùng chung.
    private IReadOnlyList<DetectedFace> Faces(Guid shotId, string path, CancellationToken ct)
    {
        if (analyzer is null) return [];
        using (var connection = BoothDatabase.Open(root))
        using (var command = BoothDatabase.Command(connection, null, "SELECT faces FROM shot_faces WHERE shot_id = $id", ("$id", shotId.ToString())))
            if (command.ExecuteScalar() is string json) return JsonSerializer.Deserialize<DetectedFace[]>(json) ?? [];
        var (w, h) = codec.Probe(path);
        var (pixels, dw, dh) = codec.Decode(path, Math.Min(1, DetectionLongEdge / (double)Math.Max(w, h)));
        var faces = analyzer.DetectAsync(pixels, dw, dh, ct).GetAwaiter().GetResult();
        using (var connection = BoothDatabase.Open(root))
            BoothDatabase.Execute(connection, null, "INSERT OR REPLACE INTO shot_faces (shot_id, faces, detector, at) VALUES ($id, $faces, $detector, $at)",
                ("$id", shotId.ToString()), ("$faces", JsonSerializer.Serialize(faces)), ("$detector", analyzer.Name), ("$at", BoothDatabase.Time(DateTimeOffset.UtcNow)));
        return faces;
    }

    private void Wake()
    {
        lock (start)
        {
            if (worker is null)
            {
                worker = new Thread(Work) { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = "FotoAutomat full-resolution photos" };
                worker.Start();
            }
        }
        signal.Release();
    }

    private void Work()
    {
        var ct = lifetime.Token;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                signal.Wait(TimeSpan.FromSeconds(30), ct);
                while (!ct.IsCancellationRequested && Next() is { } job)
                {
                    notPrinting.Wait(ct);
                    RunFull(job, ct);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private PhotoJob? Next()
    {
        using var connection = BoothDatabase.Open(root);
        using var command = BoothDatabase.Command(connection, null, "SELECT session_id FROM photo_jobs WHERE kind = 'Full' AND state = 'Queued' ORDER BY created_at LIMIT 1");
        return command.ExecuteScalar() is string session ? Jobs(Guid.Parse(session)).FirstOrDefault(j => j.Kind == PhotoJobKind.Full && j.State == PhotoJobState.Queued) : null;
    }

    private void RunFull(PhotoJob job, CancellationToken ct)
    {
        var clock = Stopwatch.StartNew();
        Update(job.Id, PhotoJobState.Running, job.Attempts + 1);
        try
        {
            var shot = sessions.Shots(job.SessionId).Single(s => s.Id == job.ShotId);
            var mode = PhotoPipeline.ModeOf(sessions.Get(job.SessionId).Snapshot);
            string path = shot.Path ?? throw new InvalidOperationException("Shot chưa có ảnh gốc.");
            var faces = mode.Features.Retouch && mode.Retouch != new RetouchSettings() ? Faces(shot.Id, path, ct) : [];
            var (pixels, w, h) = codec.Decode(path, 1);
            var rendered = PhotoPipeline.Render(pixels, w, h, mode, faces, RefineProcessor.SeedOf(shot.Id), ct);
            string folder = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, "final");
            Directory.CreateDirectory(folder);
            string output = Path.Combine(folder, shot.Id.ToString("N") + ".jpg");
            codec.SaveJpeg(rendered, w, h, output + ".tmp");
            File.Move(output + ".tmp", output, true);
            Finish(job with { Attempts = job.Attempts + 1 }, PhotoJobState.Done, output, null, clock.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) { Update(job.Id, PhotoJobState.Queued, job.Attempts + 1); throw; }
        catch (Exception error)
        {
            Debug.WriteLine(error);
            Finish(job with { Attempts = job.Attempts + 1 }, job.Attempts + 1 >= MaxAttempts ? PhotoJobState.Failed : PhotoJobState.Queued, null, error.Message, clock.ElapsedMilliseconds);
        }
        finally
        {
            // Mỗi ảnh full tạo nhiều mảng hàng trăm MB; trả bộ nhớ ngay để không dồn lên giữa các phiên khách.
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }
    }

    private PhotoJob Insert(Guid sessionId, Guid? shotId, PhotoJobKind kind, PhotoJobState state)
    {
        var job = new PhotoJob(Guid.NewGuid(), sessionId, shotId, kind, state, null, null, kind == PhotoJobKind.Print ? 1 : 0, null, null);
        var now = BoothDatabase.Time(DateTimeOffset.UtcNow);
        using var connection = BoothDatabase.Open(root);
        BoothDatabase.Execute(connection, null,
            "INSERT INTO photo_jobs (id, session_id, shot_id, kind, state, attempts, created_at, updated_at) VALUES ($id, $session, $shot, $kind, $state, $attempts, $now, $now)",
            ("$id", job.Id.ToString()), ("$session", sessionId.ToString()), ("$shot", shotId?.ToString()), ("$kind", kind.ToString()), ("$state", state.ToString()), ("$attempts", job.Attempts), ("$now", now));
        return job;
    }

    private void Update(Guid id, PhotoJobState state, int attempts)
    {
        using var connection = BoothDatabase.Open(root);
        BoothDatabase.Execute(connection, null, "UPDATE photo_jobs SET state = $state, attempts = $attempts, updated_at = $now WHERE id = $id",
            ("$state", state.ToString()), ("$attempts", attempts), ("$now", BoothDatabase.Time(DateTimeOffset.UtcNow)), ("$id", id.ToString()));
    }

    // Ghi kết quả kèm thời gian và bộ nhớ tiến trình (số liệu cho bước đo trên NUC).
    private void Finish(PhotoJob job, PhotoJobState state, string? output, string? error, long durationMs)
    {
        long workingSet = Environment.WorkingSet / (1024 * 1024);
        using var connection = BoothDatabase.Open(root);
        BoothDatabase.Execute(connection, null,
            "UPDATE photo_jobs SET state = $state, attempts = $attempts, output = $output, error = $error, duration_ms = $duration, working_set_mb = $memory, updated_at = $now WHERE id = $id",
            ("$state", state.ToString()), ("$attempts", job.Attempts), ("$output", output), ("$error", error), ("$duration", durationMs), ("$memory", workingSet),
            ("$now", BoothDatabase.Time(DateTimeOffset.UtcNow)), ("$id", job.Id.ToString()));
    }

    public void Dispose()
    {
        lifetime.Cancel();
        worker?.Join(TimeSpan.FromSeconds(5));
        lifetime.Dispose(); signal.Dispose(); notPrinting.Dispose();
    }
}
