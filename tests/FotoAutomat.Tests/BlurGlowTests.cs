using System.Text.Json;
using FotoAutomat.Core;

namespace FotoAutomat.Tests;

public sealed class BlurGlowTests
{
    private static byte[] Image(int size, byte background = 20)
    {
        var image = new byte[size * size * 4];
        for (int i = 0; i < image.Length; i += 4)
        { image[i] = image[i + 1] = image[i + 2] = background; image[i + 3] = 255; }
        for (int y = size / 3; y < size * 2 / 3; y++)
            for (int x = size / 3; x < size * 2 / 3; x++)
            { int i = (y * size + x) * 4; image[i] = image[i + 1] = image[i + 2] = 255; }
        return image;
    }

    [Fact]
    public void Default_is_identity_and_settings_roundtrip()
    {
        var source = Image(60);
        Assert.Equal(source, ColorProcessor.Apply(source, new() { BlurGlow = new() }, 60, 60));
        var recipe = new ColorRecipe { BlurGlow = new() { MistAmount = 25, GlowAmount = 40, GaussianMix = 20, Warmth = -20 } };
        Assert.Equal(recipe.BlurGlow, JsonSerializer.Deserialize<ColorRecipe>(JsonSerializer.Serialize(recipe))!.BlurGlow);
        Assert.Throws<ArgumentException>(() => new BlurGlowSettings { GlowAmount = double.NaN }.Validate());
        Assert.Throws<ArgumentException>(() => new BlurGlowSettings { GaussianRadius = 101 }.Validate());
    }

    [Fact]
    public void Glow_spreads_highlights_without_blurring_source_or_mutating_alpha()
    {
        var source = Image(120); var copy = source.ToArray();
        int edge = (60 * 120 + 39) * 4;
        var recipe = new ColorRecipe { BlurGlow = new() { GlowAmount = 100, GlowRadius = 100 } };
        var output = ColorProcessor.Apply(source, recipe, 120, 120);
        Assert.True(output[edge] > source[edge]);
        Assert.Equal(copy, source);
        for (int i = 3; i < source.Length; i += 4) Assert.Equal(source[i], output[i]);
        Assert.Equal(source, ColorProcessor.Apply(source, recipe with { BlurGlow = recipe.BlurGlow with { Threshold = 100 } }, 120, 120));
    }

    [Fact]
    public void Gaussian_softens_edges_and_transparent_colours_do_not_bleed()
    {
        var source = Image(120);
        var recipe = new ColorRecipe { BlurGlow = new() { GaussianMix = 100, GaussianRadius = 100 } };
        var output = ColorProcessor.Apply(source, recipe, 120, 120);
        Assert.True(output[(60 * 120 + 40) * 4] < 255);
        Assert.True(output[(60 * 120 + 39) * 4] > 20);
        var a = Image(120, 0); var b = a.ToArray();
        for (int y = 0; y < 120; y++) for (int x = 0; x < 40; x++)
        { int i = (y * 120 + x) * 4; a[i + 3] = b[i + 3] = 0; b[i + 2] = 255; }
        var first = ColorProcessor.Apply(a, recipe, 120, 120); var second = ColorProcessor.Apply(b, recipe, 120, 120);
        for (int i = 0; i < a.Length; i += 4)
            if (a[i + 3] != 0) Assert.Equal(first[i..(i + 4)], second[i..(i + 4)]);
    }

    [Fact]
    public void Mist_protects_blacks_and_live_view_and_cube_exclude_spatial_effects()
    {
        var source = Image(120, 0);
        var recipe = new ColorRecipe { BlurGlow = new() { MistAmount = 100, Haze = 100, PreserveBlacks = 100 } };
        var protectedImage = ColorProcessor.Apply(source, recipe, 120, 120);
        var lifted = ColorProcessor.Apply(source, recipe with { BlurGlow = recipe.BlurGlow with { PreserveBlacks = 0 } }, 120, 120);
        Assert.Equal(0, protectedImage[0]); Assert.True(lifted[0] > 0);
        Assert.Equal(source, ColorProcessor.ApplyLiveView(source, recipe, 120, 120));
        var bake = RecipeLutBaker.Bake(recipe, 3);
        Assert.Contains("Blur & Glow", bake.Excluded);
        Assert.Equal(RecipeLutBaker.Bake(new(), 3).Lut.Evaluate(.5, .5, .5), bake.Lut.Evaluate(.5, .5, .5));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Assert.Throws<OperationCanceledException>(() => ColorProcessor.Apply(source, recipe, 120, 120, cancel.Token));
    }
}
