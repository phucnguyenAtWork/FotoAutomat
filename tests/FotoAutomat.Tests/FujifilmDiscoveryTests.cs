using FotoAutomat.Infrastructure;

namespace FotoAutomat.Tests;

public sealed class FujifilmDiscoveryTests
{
    [Fact]
    public void Discovery_skips_invalid_paths_and_resolves_nested_download_without_loading_native_code()
    {
        var root = Path.Combine(Path.GetTempPath(), "FotoAutomat-discovery", Guid.NewGuid().ToString("N"));
        var x64 = Path.Combine(root, "SDK13410", "REDISTRIBUTABLES", "Windows", "64bit");
        Directory.CreateDirectory(x64);
        File.WriteAllText(Path.Combine(x64, "XAPI.dll"), "not executable");
        Assert.Null(FujifilmSdkDiscovery.FindInCandidates([null, "", root]));
        File.WriteAllText(Path.Combine(x64, "XSDK.DAT"), "fixture");
        Assert.Equal(x64, FujifilmSdkDiscovery.FindInCandidates([root + "-missing", root]));
    }
}
