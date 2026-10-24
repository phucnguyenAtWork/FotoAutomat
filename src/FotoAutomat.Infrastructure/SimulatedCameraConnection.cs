using FotoAutomat.Core;

namespace FotoAutomat.Infrastructure;

/// <summary>Camera connection for development and tests: live view and captures come from one JPEG on disk.
/// It never touches USB or a vendor SDK, so the booth flow can run without hardware.</summary>
public sealed class SimulatedCameraConnection(string jpegPath) : ICameraConnection
{
    private bool connected, live;
    public CameraDevice Device { get; } = new("simulator", CameraBrand.Simulator, "Máy ảnh mô phỏng", LiveView: true, RemoteCapture: true);

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(jpegPath)) throw new FileNotFoundException("Không tìm thấy ảnh mẫu cho máy ảnh mô phỏng.", jpegPath);
        connected = true;
        return Task.CompletedTask;
    }

    public Task StartLiveViewAsync(CancellationToken cancellationToken) { RequireConnected(); live = true; return Task.CompletedTask; }

    public Task StopLiveViewAsync(CancellationToken cancellationToken) { live = false; return Task.CompletedTask; }

    public async Task<CameraFrame?> ReadLiveViewAsync(CancellationToken cancellationToken)
    {
        RequireConnected();
        return live ? new CameraFrame(await File.ReadAllBytesAsync(jpegPath, cancellationToken), DateTimeOffset.UtcNow) : null;
    }

    public Task CaptureAsync(CameraShot shot, CancellationToken cancellationToken)
    {
        RequireConnected();
        cancellationToken.ThrowIfCancellationRequested();
        File.Copy(jpegPath, shot.DestinationPath, overwrite: false);
        return Task.CompletedTask;
    }

    private void RequireConnected() { if (!connected) throw new InvalidOperationException("Máy ảnh mô phỏng chưa kết nối."); }

    public ValueTask DisposeAsync() { connected = live = false; return ValueTask.CompletedTask; }
}
