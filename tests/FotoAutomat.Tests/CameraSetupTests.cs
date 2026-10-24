using FotoAutomat.Infrastructure;

namespace FotoAutomat.Tests;

public sealed class CameraSetupTests
{
    [Fact]
    public void Operator_camera_preferences_survive_reopen()
    {
        var path = Path.Combine(Path.GetTempPath(), "FotoAutomat-camera-tests", Guid.NewGuid().ToString("N"), "setup.json");
        var store = new CameraSetupStore(path);
        Assert.False(store.Exists);
        var expected = new CameraSetup { Brand = "Nikon", Model = "Operator target", MirrorLiveView = false, Rotation = 90, PreviewWidth = 960, SdkDirectory = "sdk-folder", ParameterValues = new() { ["iso"] = "400", ["wb"] = "Kelvin" } };
        store.Save(expected);
        Assert.Equivalent(expected, new CameraSetupStore(path).Load());
        Assert.Throws<ArgumentException>(() => store.Save(expected with { Rotation = 45 }));
        Assert.Equivalent(expected, store.Load());
    }
}

