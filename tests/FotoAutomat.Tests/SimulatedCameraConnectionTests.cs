using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;

public class SimulatedCameraConnectionTests
{
    private static string Folder() => Path.Combine(Path.GetTempPath(), "FotoAutomat-tests", Guid.NewGuid().ToString());
    private static string Jpeg(string folder)
    {
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "sample.jpg");
        File.WriteAllBytes(path, [0xFF, 0xD8, 1, 2, 3, 0xFF, 0xD9]);
        return path;
    }
    [Fact]
    public async Task Booth_pipeline_streams_live_view_and_receives_a_capture_from_the_simulated_camera()
    {
        string folder = Folder();
        await using var pipeline = new BoothCameraPipeline(new SimulatedCameraConnection(Jpeg(folder)), Path.Combine(folder, "sessions"));
        await pipeline.ConnectAsync();
        Assert.True(pipeline.IsReady);
        Assert.Equal(CameraBrand.Simulator, pipeline.Device.Brand);
        Assert.NotNull(await pipeline.ReadLiveViewAsync());
        string path = await pipeline.CaptureAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.True(File.Exists(path));
        Assert.NotNull(await pipeline.ReadLiveViewAsync());
    }
    [Fact]
    public async Task Missing_sample_fails_the_connection()
    {
        string folder = Folder();
        await using var pipeline = new BoothCameraPipeline(new SimulatedCameraConnection(Path.Combine(folder, "missing.jpg")), folder);
        await Assert.ThrowsAsync<FileNotFoundException>(() => pipeline.ConnectAsync());
        Assert.False(pipeline.IsReady);
    }
}
