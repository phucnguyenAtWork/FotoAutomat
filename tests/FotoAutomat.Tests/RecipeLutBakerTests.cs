using System.Globalization;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
using Xunit.Abstractions;

namespace FotoAutomat.Tests;

public sealed class RecipeLutBakerTests(ITestOutputHelper output)
{
    static ColorRecipe Look() => new("Look", Exposure: .25, Contrast: 18, Warmth: 15, Saturation: -10, Tint: -6, Highlights: -20, Shadows: 15, Blacks: 8)
    {
        Curves = [new([new(0, .04), new(.5, .52), new(1, .97)]), ToneCurve.Identity, ToneCurve.Identity, ToneCurve.Identity, new([new(0, .03), new(1, 1)])],
        Hsl = [new(5, -10, 0), new(0, 8, 4), new(), new(-8, -20, 0), new(), new(10, 12, -5), new(), new()],
        Balance = [new(), new(190, 30, 0), new(), new(35, 25, 0)]
    };

    static byte[] Colors(int count, int seed)
    {
        var random = new Random(seed); var p = new byte[count * 4];
        for (int i = 0; i < p.Length; i += 4) { p[i] = (byte)random.Next(256); p[i + 1] = (byte)random.Next(256); p[i + 2] = (byte)random.Next(256); p[i + 3] = 255; }
        return p;
    }

    static (double Mean, double Max) Error(ColorRecipe recipe, CubeLut lut, byte[] colors)
    {
        var expected = ColorProcessor.Apply(colors, recipe); double sum = 0, max = 0;
        for (int i = 0; i < colors.Length; i += 4)
        {
            var c = lut.Evaluate(colors[i + 2] / 255d, colors[i + 1] / 255d, colors[i] / 255d);
            foreach (var (got, want) in new[] { (c.R, expected[i + 2]), (c.G, expected[i + 1]), (c.B, expected[i]) })
            { double e = Math.Abs(Math.Round(Math.Clamp(got, 0, 1) * 255) - want); sum += e; max = Math.Max(max, e); }
        }
        return (sum / (colors.Length / 4 * 3), max);
    }

    [Fact]
    public void Identity_recipe_bakes_to_identity_within_byte_rounding()
    {
        var lut = RecipeLutBaker.Bake(new ColorRecipe(), 33).Lut;
        var (mean, max) = Error(new ColorRecipe(), lut, Colors(20000, 1));
        Assert.True(mean < .5 && max <= 1, $"{mean} {max}");
    }

    [Fact]
    public void Baked_lut_reproduces_recipe_render_and_larger_grid_is_closer()
    {
        var colors = Colors(40000, 2);
        var e33 = Error(Look(), RecipeLutBaker.Bake(Look(), 33).Lut, colors);
        var e65 = Error(Look(), RecipeLutBaker.Bake(Look(), 65).Lut, colors);
        output.WriteLine($"33: mean {e33.Mean:0.000} max {e33.Max}; 65: mean {e65.Mean:0.000} max {e65.Max}");
        // Measured 14/10/2026: 33³ mean 0.40 max 6; 65³ mean 0.27 max 3 (/255).
        Assert.True(e33.Mean < .6 && e33.Max <= 8, $"33: {e33}");
        Assert.True(e65.Mean <= e33.Mean && e65.Max <= 5, $"65: {e65}");
    }

    [Fact]
    public void Spatial_tools_and_masked_layers_are_reported_and_full_layers_are_kept()
    {
        var layerLook = new ColorRecipe("L", Saturation: 40);
        var recipe = new ColorRecipe() with
        {
            Clarity = 20,
            Vignette = -15,
            Grain = 10,
            Layers = [new("Match Look", layerLook, new(), .6), new("Brush", layerLook, new(PhotoMaskKind.Radial))]
        };
        var result = RecipeLutBaker.Bake(recipe, 17);
        Assert.Equal(["Clarity", "Vignette", "Grain", "Layer 'Brush'"], result.Excluded);
        var withLayer = RecipeLutBaker.Bake(new ColorRecipe() with { Layers = [new("Match Look", layerLook, new(), .6)] }, 17).Lut;
        Assert.Equal(withLayer.Values, result.Lut.Values);
        Assert.NotEqual(RecipeLutBaker.Bake(new ColorRecipe(), 17).Lut.Values, result.Lut.Values);
        Assert.Throws<ArgumentException>(() => RecipeLutBaker.Bake(new ColorRecipe(), 1));
    }

    [Fact]
    public void Cube_file_round_trips_through_importer_with_red_fastest_in_any_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("vi-VN");
            var lut = RecipeLutBaker.Bake(Look(), 9).Lut;
            var text = CubeLutFile.Format(lut, "My \"Look\"");
            Assert.DoesNotContain(',', text);
            Assert.StartsWith("TITLE \"My Look\"\n", text);
            var imported = LightroomPresetImporter.Cube(text, "x").Recipe.Lut!;
            Assert.Equal(9, imported.Size);
            Assert.All(lut.Values.Zip(imported.Values), p => Assert.Equal(p.First, p.Second, 6));
            // Second table row is the next red node with green and blue at zero.
            var identity = RecipeLutBaker.Bake(new ColorRecipe(), 9).Lut;
            Assert.Equal(32 / 255d, identity.Values[3], 6); Assert.Equal(0, identity.Values[4]); Assert.Equal(0, identity.Values[5]);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
