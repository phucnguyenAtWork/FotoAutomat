using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Tests;

public sealed class BoothCameraPipelineTests
{
    [Theory]
    [InlineData(CameraBrand.Canon)]
    [InlineData(CameraBrand.Fujifilm)]
    [InlineData(CameraBrand.Nikon)]
    public void Missing_vendor_is_not_a_simulator(CameraBrand brand)
    {
        var registry = new CameraDriverRegistry([]);
        Assert.Throws<InvalidOperationException>(() => registry.Require(brand));
    }

    [Fact]
    public async Task Intent_precedes_shutter_and_receipt_follows_download()
    {
        var root = NewRoot(); var device = new FakeCamera();
        await using var pipeline = new BoothCameraPipeline(device, root);
        await pipeline.ConnectAsync();
        var session = Guid.NewGuid(); var shot = Guid.NewGuid();
        var file = await pipeline.CaptureAsync(session, shot);
        Assert.True(device.IntentWasPresent);
        Assert.True(File.Exists(Path.ChangeExtension(file, ".received.json")));
        Assert.Equal(new[] { "connect", "live-start", "live-stop", "capture", "live-start" }, device.Calls);
        Assert.Equal(1, device.Shutters);
        await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.CaptureAsync(session, shot));
        Assert.Equal(1, device.Shutters);
    }

    [Fact]
    public async Task Interrupted_shot_is_not_repeated_after_reopen()
    {
        var root = NewRoot(); var session = Guid.NewGuid(); var shot = Guid.NewGuid();
        var device = new FakeCamera { FailDownload = true };
        await using (var pipeline = new BoothCameraPipeline(device, root))
        {
            await pipeline.ConnectAsync();
            await Assert.ThrowsAsync<IOException>(() => pipeline.CaptureAsync(session, shot));
            Assert.False(pipeline.IsReady);
        }
        var reopened = new FakeCamera();
        await using var next = new BoothCameraPipeline(reopened, root);
        await next.ConnectAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => next.CaptureAsync(session, shot));
        Assert.Equal(0, reopened.Shutters);
        Assert.Empty(Directory.GetFiles(root, "*.received.json", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Truncated_download_is_not_acknowledged()
    {
        var root = NewRoot(); var device = new FakeCamera { Truncate = true };
        await using var pipeline = new BoothCameraPipeline(device, root);
        await pipeline.ConnectAsync();
        await Assert.ThrowsAsync<InvalidDataException>(() => pipeline.CaptureAsync(Guid.NewGuid(), Guid.NewGuid()));
        Assert.False(pipeline.IsReady);
        Assert.Empty(Directory.GetFiles(root, "*.received.json", SearchOption.AllDirectories));
    }

    private static string NewRoot() => Path.Combine(Path.GetTempPath(), "FotoAutomat-camera-tests", Guid.NewGuid().ToString("N"));
    private sealed class FakeCamera : ICameraConnection
    {
        public CameraDevice Device => new("fake", CameraBrand.Simulator, "test double", true, true);
        public List<string> Calls { get; } = [];
        public bool FailDownload { get; init; }
        public bool Truncate { get; init; }
        public bool IntentWasPresent { get; private set; }
        public int Shutters { get; private set; }
        public Task ConnectAsync(CancellationToken token) { Calls.Add("connect"); return Task.CompletedTask; }
        public Task StartLiveViewAsync(CancellationToken token) { Calls.Add("live-start"); return Task.CompletedTask; }
        public Task StopLiveViewAsync(CancellationToken token) { Calls.Add("live-stop"); return Task.CompletedTask; }
        public Task<CameraFrame?> ReadLiveViewAsync(CancellationToken token) => Task.FromResult<CameraFrame?>(null);
        public Task CaptureAsync(CameraShot shot, CancellationToken token)
        {
            Calls.Add("capture"); Shutters++;
            IntentWasPresent = File.Exists(Path.ChangeExtension(shot.DestinationPath, ".intent.json"));
            if (FailDownload) throw new IOException("Disconnected after shutter");
            // Marker-only fixture tests transfer acknowledgement, not image decoding.
            File.WriteAllBytes(shot.DestinationPath, Truncate ? [0xff, 0xd8, 0, 0] : [0xff, 0xd8, 0xff, 0xd9]);
            return Task.CompletedTask;
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
