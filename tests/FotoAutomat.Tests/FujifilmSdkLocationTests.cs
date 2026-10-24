using FotoAutomat.Infrastructure;

namespace FotoAutomat.Tests;

public sealed class FujifilmSdkLocationTests
{
    [Fact]
    public void Missing_sdk_is_rejected_before_worker_or_native_load()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Assert.Throws<DirectoryNotFoundException>(() => new FujifilmCameraConnection(path));
    }
    [Fact]
    public void Chooses_64bit_distribution_and_requires_data_file()
    {
        var root = Path.Combine(Path.GetTempPath(), "FotoAutomat-fuji-tests", Guid.NewGuid().ToString("N"));
        var x64 = Path.Combine(root, "REDISTRIBUTABLES", "Windows", "64bit");
        var x86 = Path.Combine(root, "REDISTRIBUTABLES", "Windows", "32bit");
        Directory.CreateDirectory(x64); Directory.CreateDirectory(x86);
        File.WriteAllText(Path.Combine(x64, "XAPI.dll"), "path fixture, not executable");
        File.WriteAllText(Path.Combine(x86, "XAPI.dll"), "path fixture, not executable");
        File.WriteAllText(Path.Combine(x86, "XSDK.DAT"), "fixture");
        Assert.Throws<DirectoryNotFoundException>(() => FujifilmCameraConnection.ResolveDirectory(root));
        File.WriteAllText(Path.Combine(x64, "XSDK.DAT"), "fixture");
        Assert.Equal(x64, FujifilmCameraConnection.ResolveDirectory(root));
    }
}
