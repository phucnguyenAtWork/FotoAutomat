using FotoAutomat.Infrastructure;

namespace FotoAutomat.Tests;

public class FujifilmSettingsTests
{
    [Theory]
    [InlineData("AEMode", "ShutterSpeed")]
    [InlineData("WBMode", "WBColorTemp")]
    public void Dependent_edits_are_rejected_before_device_write(string mode, string dependent)
    {
        Assert.Throws<ArgumentException>(() => FujifilmCameraConnection.ValidateSettingChanges(new Dictionary<string, int> { [mode] = 1, [dependent] = 100 }));
        FujifilmCameraConnection.ValidateSettingChanges(new Dictionary<string, int> { [mode] = 1 });
    }
    [Fact]
    public void Unknown_setting_cannot_become_a_native_export_name()
    {
        Assert.Throws<ArgumentException>(() => FujifilmCameraConnection.ValidateSettingChanges(new Dictionary<string, int> { ["Release"] = 1 }));
    }
    [Fact]
    public void Camera_value_units_are_preserved_for_display()
    {
        Assert.Equal("1/60 s", FujifilmCameraConnection.FormatSetting("ShutterSpeed", 15625));
        Assert.Equal("Kelvin", FujifilmCameraConnection.FormatSetting("WBMode", 0x8007));
        Assert.Equal("4800 K", FujifilmCameraConnection.FormatSetting("WBColorTemp", 4800));
        Assert.Equal("SDK 123456", FujifilmCameraConnection.FormatSetting("ShutterSpeed", 123456));
    }
}
