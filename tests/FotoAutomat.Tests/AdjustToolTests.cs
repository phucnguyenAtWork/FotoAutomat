using System.Text.Json;
using FotoAutomat.Core;

namespace FotoAutomat.Tests;

public sealed class AdjustToolTests
{
    private static readonly byte[] Colors = [30, 60, 140, 128, 140, 60, 30, 255, 100, 100, 100, 60];

    [Fact]
    public void Shot_and_default_custom_preserve_pixels_and_custom_changes_balance()
    {
        foreach (var mode in Enum.GetValues<WhiteBalanceMode>())
            Assert.Equal(Colors, ColorProcessor.Apply(Colors, new() { WhiteBalance = new(mode) }));
        var warm = ColorProcessor.Apply(Colors, new() { WhiteBalance = new(WhiteBalanceMode.Custom, 8000, 10) });
        Assert.True(warm[2] > Colors[2]); Assert.True(warm[0] < Colors[0]); Assert.Equal(Colors[3], warm[3]);
        Assert.Equal(Colors, ColorProcessor.Apply(Colors, new(Warmth: 30, Tint: 20) { WhiteBalance = new(WhiteBalanceMode.Shot, 8000, 10) }));
        foreach (double k in new[] { 800d, 140000 }) new ColorRecipe { WhiteBalance = new(WhiteBalanceMode.Custom, k) }.Validate();
        Assert.Throws<ArgumentException>(() => new WhiteBalanceSettings(Kelvin: 140001).Validate());
        Assert.Throws<ArgumentException>(() => new WhiteBalanceSettings(Tint: 51).Validate());
    }
    [Fact]
    public void Advanced_range_preserves_unselected_blue_and_neutral_range_is_identity()
    {
        var range = new ColorRangeAdjustment(CenterHue: 16, Width: 25, Hue: 25, Saturation: 200);
        var result = ColorProcessor.Apply(Colors, new() { AdvancedColors = [range] });
        Assert.NotEqual(Colors[1], result[1]); Assert.Equal(Colors[4..8], result[4..8]);
        Assert.Equal(Colors, ColorProcessor.Apply(Colors, new() { AdvancedColors = [range with { Enabled = false }] }));
        Assert.Equal(Colors, ColorProcessor.Apply(Colors, new() { AdvancedColors = [new()], SkinColor = new() }));
        Assert.Throws<ArgumentException>(() => (range with { Smoothness = 0 }).Validate());
        Assert.Throws<ArgumentException>(() => (range with { Saturation = 201 }).Validate());
    }
    [Fact]
    public void Skin_uniformity_reduces_hue_difference_only_in_selected_range()
    {
        var first = AdjustToolProcessor.FromHsl(15, .5, .6); var second = AdjustToolProcessor.FromHsl(35, .5, .6);
        byte[] pixels = [B(first.B), B(first.G), B(first.R), 255, B(second.B), B(second.G), B(second.R), 255];
        var result = ColorProcessor.Apply(pixels, new() { SkinColor = new(CenterHue: 25, Width: 30, UniformHue: 100) });
        double h1 = AdjustToolProcessor.ToHsl(result[2] / 255d, result[1] / 255d, result[0] / 255d).H;
        double h2 = AdjustToolProcessor.ToHsl(result[6] / 255d, result[5] / 255d, result[4] / 255d).H;
        Assert.True(Math.Abs(AdjustToolProcessor.HueDelta(h1, h2)) < 2);
    }
    [Fact]
    public void Black_white_sensitivity_uses_colour_before_desaturation_and_is_gated()
    {
        byte[] colors = [0, 0, 180, 255, 180, 0, 0, 255];
        var settings = new BlackWhiteSettings { Sensitivity = [100, 0, 0, 0, 0, 0] };
        Assert.Equal(colors, ColorProcessor.Apply(colors, new() { BlackWhite = settings }));
        var baseline = ColorProcessor.Apply(colors, new(Monochrome: true) { BlackWhite = new() });
        var adjusted = ColorProcessor.Apply(colors, new(Monochrome: true) { BlackWhite = settings });
        Assert.True(adjusted[0] > baseline[0]); Assert.Equal(baseline[4..], adjusted[4..]);
        Assert.Equal(adjusted[0], adjusted[1]); Assert.Equal(adjusted[1], adjusted[2]);
        var split = ColorProcessor.Apply(colors, new(Monochrome: true) { BlackWhite = settings with { ShadowHue = 240, ShadowSaturation = 100 } });
        Assert.True(split[4] > split[6]);
    }
    [Fact]
    public void Vignette_preserves_center_changes_edges_and_is_excluded_from_lut_and_live()
    {
        byte[] image = Enumerable.Range(0, 153).SelectMany(_ => new byte[] { 100, 100, 100, 123 }).ToArray();
        var r = new ColorRecipe { VignetteEv = -2 };
        var result = ColorProcessor.Apply(image, r, 17, 9);
        Assert.Equal(100, result[(4 * 17 + 8) * 4]); Assert.True(result[0] < 100); Assert.Equal(123, result[3]);
        Assert.Equal(image, ColorProcessor.ApplyLiveView(image, r, 17, 9));
        Assert.Contains("Vignetting", RecipeLutBaker.Bake(r, 3).Excluded);
        var circle = ColorProcessor.Apply(image, r with { VignetteMethod = VignetteMethod.CircularOnCrop }, 17, 9);
        Assert.True(circle[(4 * 17) * 4] < result[(4 * 17) * 4]);
    }
    [Fact]
    public void Full_new_recipe_roundtrips_and_methods_have_real_effects()
    {
        byte[] image = Enumerable.Range(0, 49).SelectMany(i => new byte[] { (byte)(i == 24 ? 200 : 30), (byte)(i == 24 ? 200 : 30), (byte)(i == 24 ? 200 : 30), 222 }).ToArray();
        var recipe = new ColorRecipe { WhiteBalance = new(WhiteBalanceMode.Custom, 5100, 3), AdvancedColors = [new(Hue: 10)], SkinColor = new(UniformHue: 10), BlackWhite = new(), Clarity = 60, ClarityMethod = ClarityMethod.Punch, Dehaze = 30, DehazeShadowMode = ShadowToneMode.Manual, DehazeShadowHue = 240, DehazeShadowSaturation = 30, VignetteEv = -1 };
        var restored = JsonSerializer.Deserialize<ColorRecipe>(JsonSerializer.Serialize(recipe))!;
        Assert.Equal(ColorProcessor.Apply(image, recipe, 7, 7), ColorProcessor.Apply(image, restored, 7, 7));
        var results = Enum.GetValues<ClarityMethod>().Select(method => Convert.ToHexString(ColorProcessor.Apply(image, new() { Clarity = 60, ClarityMethod = method }, 7, 7))).Distinct().Count();
        Assert.Equal(4, results);
        Assert.NotEqual(ColorProcessor.Apply(image, recipe, 7, 7), ColorProcessor.Apply(image, recipe with { DehazeShadowMode = ShadowToneMode.Auto }, 7, 7));
        Assert.Equal(Colors, ColorProcessor.Apply(Colors, JsonSerializer.Deserialize<ColorRecipe>("{\"Name\":\"old\"}")!));
    }
    private static byte B(double value) => (byte)Math.Round(value * 255);
}
