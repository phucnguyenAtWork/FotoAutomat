using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;
public sealed class ColorProcessorTests
{
    [Fact]
    public void Tonal_controls_target_dark_and_light_regions_and_tint_changes_green()
    {
        byte[] ramp = [40, 40, 40, 255, 210, 210, 210, 255];
        var shadows = ColorProcessor.Apply(ramp, new(Shadows: 50));
        var highlights = ColorProcessor.Apply(ramp, new(Highlights: -50));
        Assert.True(shadows[0] > ramp[0]);
        Assert.Equal(ramp[4], shadows[4]);
        Assert.True(highlights[4] < ramp[4]);
        Assert.Equal(ramp[0], highlights[0]);
        var tint = ColorProcessor.Apply(ramp, new(Tint: 50));
        Assert.True(tint[1] < tint[2]);
        Assert.Throws<ArgumentException>(() => ColorProcessor.Apply(ramp, new(Whites: 101)));
    }
    [Fact]
    public void Neutral_preserves_pixels_and_alpha_without_mutating_source()
    {
        byte[] source = [12, 80, 190, 230, 0, 255, 255, 0];
        var result = ColorProcessor.Apply(source, new());
        Assert.Equal(source, result); Assert.NotSame(source, result);
    }
    [Fact]
    public void Exposure_warmth_and_monochrome_change_color_and_keep_alpha()
    {
        byte[] source = [100, 100, 100, 255, 20, 80, 180, 128];
        var bright = ColorProcessor.Apply(source, new(Exposure: 1));
        Assert.True(bright[0] > source[0]);
        var warm = ColorProcessor.Apply(source, new(Warmth: 50));
        Assert.True(warm[2] > warm[0]);
        var mono = ColorProcessor.Apply(source, new(Monochrome: true));
        Assert.Equal(mono[4], mono[5]); Assert.Equal(mono[5], mono[6]); Assert.Equal(128, mono[7]);
        Assert.Equal(100, source[0]);
        Assert.Throws<ArgumentException>(() => ColorProcessor.Apply(source, new(Exposure: double.NaN)));
    }
    [Fact]
    public void Workspace_roundtrips_all_looks_and_invalid_save_preserves_previous()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new ColorWorkspaceStore(path); var value = store.Load(); store.Save(value);
            Assert.Equal(value.Looks, store.Load().Looks);
            Assert.Throws<ArgumentException>(() => store.Save(value with { Looks = [] }));
            Assert.Equal(value.Looks, store.Load().Looks);
        }
        finally { File.Delete(path); }
    }
}
