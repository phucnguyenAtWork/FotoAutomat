namespace FotoAutomat.Core;

public enum CameraBrand { Simulator, Canon, Fujifilm, Nikon }
public sealed record CameraDevice(string Id, CameraBrand Brand, string Model, bool LiveView, bool RemoteCapture);
/// <summary>Owned encoded JPEG frame. Drivers must serialize native SDK calls on their required thread.</summary>
public sealed record CameraFrame(byte[] Jpeg, DateTimeOffset Timestamp);
public sealed record CameraShot(Guid SessionId, Guid ShotId, string DestinationPath);

/// <summary>Vendor implementations belong in Infrastructure, not Core. No automatic shutter retries.</summary>
public interface ICameraConnection : IAsyncDisposable
{
    CameraDevice Device { get; }
    Task ConnectAsync(CancellationToken cancellationToken);
    Task StartLiveViewAsync(CancellationToken cancellationToken);
    Task<CameraFrame?> ReadLiveViewAsync(CancellationToken cancellationToken);
    Task StopLiveViewAsync(CancellationToken cancellationToken);
    /// <summary>Return only after the complete original JPEG is closed at DestinationPath.
    /// Correlate download callbacks with ShotId; never return a stale preview or an earlier capture.</summary>
    Task CaptureAsync(CameraShot shot, CancellationToken cancellationToken);
}

public interface ICameraDriver
{
    CameraBrand Brand { get; }
    Task<IReadOnlyList<CameraDevice>> DiscoverAsync(CancellationToken cancellationToken);
    ICameraConnection Open(CameraDevice device);
}
