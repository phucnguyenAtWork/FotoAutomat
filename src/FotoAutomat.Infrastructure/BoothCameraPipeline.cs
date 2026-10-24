using System.Text.Json;
using FotoAutomat.Core;

namespace FotoAutomat.Infrastructure;

/// <summary>One owner per connection. Preview polling and capture never overlap SDK operations.
/// An uncertain operation faults the session; reconnect is explicit, never a shutter retry.</summary>
public sealed class BoothCameraPipeline(ICameraConnection camera, string storageRoot) : IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly HashSet<Guid> attemptedShots = [];
    private bool connected, faulted, disposed;
    public CameraDevice Device => camera.Device;
    public bool IsReady => connected && !faulted && !disposed;
    public string? Error { get; private set; }
    public async Task ConnectAsync(CancellationToken token = default)
    {
        await gate.WaitAsync(token);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (connected || faulted) throw new InvalidOperationException("Create a new connection to reconnect.");
            if (!Device.LiveView || !Device.RemoteCapture) throw new NotSupportedException("Live view and remote capture are required for booth mode.");
            await camera.ConnectAsync(token);
            await camera.StartLiveViewAsync(token);
            connected = true;
        }
        catch (Exception e) { Fault(e); throw; }
        finally { gate.Release(); }
    }
    public async Task<CameraFrame?> ReadLiveViewAsync(CancellationToken token = default)
    {
        // Drop a preview request while capture/download is busy. Never queue stale frames.
        if (!await gate.WaitAsync(0, token)) return null;
        try { RequireReady(); return await camera.ReadLiveViewAsync(token); }
        catch (Exception e) { Fault(e); throw; }
        finally { gate.Release(); }
    }
    public async Task<string> CaptureAsync(Guid sessionId, Guid shotId, CancellationToken token = default)
    {
        if (sessionId == Guid.Empty || shotId == Guid.Empty) throw new ArgumentException("Session and shot IDs are required.");
        await gate.WaitAsync(token);
        try
        {
            RequireReady();
            var directory = Path.Combine(Path.GetFullPath(storageRoot), sessionId.ToString("N"));
            Directory.CreateDirectory(directory);
            var stem = Path.Combine(directory, shotId.ToString("N"));
            if (attemptedShots.Contains(shotId) || File.Exists(stem + ".intent.json"))
                throw new InvalidOperationException("This shot was already requested. Reconcile its receipt before taking another shot.");
            // CreateNew + disk flush before the first possible device side effect.
            WriteNew(stem + ".intent.json", new { sessionId, shotId, state = "Requested", at = DateTimeOffset.UtcNow });
            attemptedShots.Add(shotId);
            var shot = new CameraShot(sessionId, shotId, stem + ".jpg");
            await camera.StopLiveViewAsync(token);
            await camera.CaptureAsync(shot, token);
            using (var original = new FileStream(shot.DestinationPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (original.Length < 4 || original.ReadByte() != 0xff || original.ReadByte() != 0xd8)
                    throw new InvalidDataException("The camera did not deliver a JPEG original.");
                original.Seek(-2, SeekOrigin.End);
                if (original.ReadByte() != 0xff || original.ReadByte() != 0xd9)
                    throw new InvalidDataException("Incomplete JPEG transfer.");
            }
            WriteNew(stem + ".received.json", new { sessionId, shotId, path = shot.DestinationPath, at = DateTimeOffset.UtcNow });
            await camera.StartLiveViewAsync(token);
            return shot.DestinationPath;
        }
        catch (Exception e) { Fault(e); throw; }
        finally { gate.Release(); }
    }
    private static void WriteNew(string path, object data)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        JsonSerializer.Serialize(stream, data); stream.Flush(flushToDisk: true);
    }
    private void RequireReady() { if (!IsReady) throw new InvalidOperationException("Camera is not ready. " + Error); }
    private void Fault(Exception e) { faulted = true; Error = e.Message; }
    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync();
        try { if (disposed) return; disposed = true; connected = false; await camera.DisposeAsync(); }
        finally { gate.Release(); }
    }
}
