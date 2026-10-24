using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;
public sealed class AdvancedColorTests
{
    private static byte[] Ramp() => [20, 20, 20, 128, 90, 90, 90, 255, 160, 160, 160, 255, 230, 230, 230, 255];
    [Fact]
    public void Neutral_advanced_groups_preserve_pixels_and_invalid_shapes_are_rejected()
    {
        var recipe = new ColorRecipe { Levels = Enumerable.Range(0, 4).Select(_ => new LevelsAdjustment()).ToArray(), Curves = Enumerable.Range(0, 5).Select(_ => ToneCurve.Identity).ToArray(), Hsl = Enumerable.Range(0, 8).Select(_ => new HslAdjustment()).ToArray(), Balance = Enumerable.Range(0, 4).Select(_ => new ColorGrade()).ToArray() };
        Assert.Equal(Ramp(), ColorProcessor.Apply(Ramp(), recipe, 4, 1));
        Assert.Throws<ArgumentException>(() => ColorProcessor.Apply(Ramp(), recipe with { Curves = [ToneCurve.Identity] }));
        Assert.Throws<ArgumentException>(() => new LevelsAdjustment(200, 100).Validate());
        Assert.Throws<ArgumentException>(() => new ToneCurve([new(0, 0), new(.7, .5), new(.5, .5), new(1, 1)]).Validate());
    }
    [Fact]
    public void Levels_and_curves_change_only_requested_channel()
    {
        var levels = Enumerable.Range(0, 4).Select(_ => new LevelsAdjustment()).ToArray(); levels[1] = new(Gamma: 2);
        var result = ColorProcessor.Apply(Ramp(), new() { Levels = levels });
        Assert.True(result[6] > Ramp()[6]); Assert.Equal(Ramp()[4], result[4]);
        var curves = Enumerable.Range(0, 5).Select(_ => ToneCurve.Identity).ToArray(); curves[4] = new([new(0, 0), new(.5, .8), new(1, 1)]);
        result = ColorProcessor.Apply(Ramp(), new() { Curves = curves });
        Assert.True(result[4] > Ramp()[4]); Assert.Equal(Ramp()[6], result[6]); Assert.Equal(128, result[3]);
    }
    [Fact]
    public void Hsl_selects_red_and_balance_grades_by_luminance()
    {
        byte[] colors = [0, 0, 255, 255, 255, 0, 0, 255];
        var hsl = Enumerable.Range(0, 8).Select(_ => new HslAdjustment()).ToArray(); hsl[0] = new(Hue: 100);
        var result = ColorProcessor.Apply(colors, new() { Hsl = hsl });
        Assert.True(result[1] > 0); Assert.Equal(colors[4..], result[4..]);
        var balance = Enumerable.Range(0, 4).Select(_ => new ColorGrade()).ToArray(); balance[1] = new(0, 100);
        result = ColorProcessor.Apply(Ramp(), new() { Balance = balance });
        Assert.True(result[2] - result[0] > result[14] - result[12]);
    }
    [Fact]
    public void Clarity_changes_edges_dehaze_changes_veil_and_neither_changes_alpha()
    {
        byte[] image = Enumerable.Range(0, 49).SelectMany(i => new byte[] { (byte)(i == 24 ? 160 : 80), (byte)(i == 24 ? 160 : 80), (byte)(i == 24 ? 160 : 80), 200 }).ToArray();
        var sharp = ColorProcessor.Apply(image, new() { Clarity = 60, Structure = 20 }, 7, 7);
        Assert.True(sharp[24 * 4] > image[24 * 4]);
        var haze = ColorProcessor.Apply(image, new() { Dehaze = 70 }, 7, 7);
        Assert.True(haze[0] < image[0]); Assert.Equal(200, haze[3]); Assert.Equal(80, image[0]);
    }
    [Fact]
    public void Mask_paint_erase_inversion_and_layer_opacity_limit_adjustments_to_region()
    {
        var mask = new PhotoMask(PhotoMaskKind.Brush, [new(.25, .5, .2)], Feather: .2);
        var values = mask.Rasterize(20, 10); Assert.True(values[5 * 20 + 5] > .5); Assert.Equal(0, values[5 * 20 + 18]);
        var erased = mask with { Dabs = [new(.25, .5, .2), new(.25, .5, .2, Erase: true)] };
        Assert.True(erased.Rasterize(20, 10)[5 * 20 + 5] < values[5 * 20 + 5]);
        Assert.Equal(1, (mask with { Inverted = true }).Rasterize(20, 10)[5 * 20 + 18]);
        byte[] image = Enumerable.Range(0, 200).SelectMany(_ => new byte[] { 80, 80, 80, 123 }).ToArray();
        var recipe = new ColorRecipe { Layers = [new("Sáng vùng", new(Exposure: 1), mask, .5)] };
        var result = ColorProcessor.Apply(image, recipe, 20, 10);
        Assert.True(result[(5 * 20 + 5) * 4] > 80); Assert.Equal(80, result[(5 * 20 + 18) * 4]); Assert.Equal(123, result[3]);
        Assert.Equal(image, ColorProcessor.Apply(image, recipe with { Layers = [recipe.Layers[0] with { Enabled = false }] }, 20, 10));
    }
    [Fact]
    public void Radial_and_gradient_masks_have_soft_edges_and_follow_normalized_geometry()
    {
        var radial = new PhotoMask(PhotoMaskKind.Radial, Radius: .3, Feather: .5);
        var small = radial.Rasterize(20, 20); var large = radial.Rasterize(40, 40);
        Assert.True(small[10 * 20 + 10] > .9); Assert.Equal(0, small[0]);
        Assert.True(large[20 * 40 + 20] > .9); Assert.Equal(0, large[0]);
        var gradient = new PhotoMask(PhotoMaskKind.Linear, Radius: .5, Feather: 1);
        var horizontal = gradient.Rasterize(20, 20); var vertical = (gradient with { Angle = 90 }).Rasterize(20, 20);
        Assert.True(horizontal[10 * 20 + 18] > horizontal[10 * 20 + 1]);
        Assert.True(vertical[18 * 20 + 10] > vertical[1 * 20 + 10]);
        var hard = (gradient with { Feather = .1 }).Rasterize(20, 20);
        Assert.True(hard[10 * 20 + 12] > horizontal[10 * 20 + 12]);
    }
    [Fact]
    public void Full_recipe_roundtrips_with_masks_and_cancellation_is_observed()
    {
        var recipe = new ColorRecipe { Levels = Enumerable.Range(0, 4).Select(_ => new LevelsAdjustment()).ToArray(), Layers = [new("Mask", new(Exposure: .5), new(PhotoMaskKind.Radial))] };
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new ColorWorkspaceStore(path); store.Save(new(null, [recipe, new(), new()])); var loaded = store.Load();
            Assert.Equal(ColorProcessor.Apply(Ramp(), recipe, 4, 1), ColorProcessor.Apply(Ramp(), loaded.Looks[0], 4, 1));
            using var canceled = new CancellationTokenSource(); canceled.Cancel();
            Assert.Throws<OperationCanceledException>(() => ColorProcessor.Apply(Ramp(), recipe, 4, 1, canceled.Token));
        }
        finally { File.Delete(path); }
    }
}
