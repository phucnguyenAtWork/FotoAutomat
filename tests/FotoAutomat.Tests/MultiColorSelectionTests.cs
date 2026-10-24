using System.Text.Json;
using FotoAutomat.Core;

namespace FotoAutomat.Tests;

public sealed class MultiColorSelectionTests
{
    [Fact]
    public void Multiple_skin_ranges_roundtrip_apply_and_preserve_legacy()
    {
        byte[] pixels = [0, 0, 180, 255, 180, 0, 0, 255, 0, 180, 0, 255];
        var red = new ColorRangeAdjustment(CenterHue: 0, Width: 10, Lightness: 30);
        var blue = new ColorRangeAdjustment(CenterHue: 240, Width: 10, Lightness: -30);
        var recipe = new ColorRecipe { SkinColors = [red, blue] };
        var copy = JsonSerializer.Deserialize<ColorRecipe>(JsonSerializer.Serialize(recipe))!;
        copy.Validate(); Assert.Equal(recipe.SkinColors, copy.SkinColors);
        var result = ColorProcessor.Apply(pixels, copy);
        Assert.NotEqual(pixels[2], result[2]); Assert.NotEqual(pixels[4], result[4]); Assert.Equal(pixels[8..], result[8..]);
        Assert.Equal(ColorProcessor.Apply(pixels, new() { SkinColor = red }), ColorProcessor.Apply(pixels, new() { SkinColors = [red] }));
        Assert.Throws<ArgumentException>(() => new ColorRecipe { SkinColors = Enumerable.Repeat(red, 33).ToArray() }.Validate());
    }

    [Fact]
    public void Match_only_selected_component_is_fitted()
    {
        var pixels = Enumerable.Range(0, 256).SelectMany(i => new byte[] { (byte)i, (byte)i, (byte)i, 255 }).ToArray();
        var target = ColorProcessor.Apply(pixels, new(Contrast: 40));
        var options = new MatchOptions(Samples: 256, Passes: 2, Normalize: false) { Components = MatchComponents.Contrast };
        var result = PairedMatchEngine.Fit([new(pixels, target, 16, 16)], options);
        Assert.NotEqual(0, result.Recipe.Contrast);
        Assert.Equal(0, result.Recipe.Saturation); Assert.Equal(0, result.Recipe.Highlights); Assert.Equal(0, result.Recipe.Shadows);
        foreach (var curve in result.Recipe.Curves!) Assert.Equal(.5, curve.Evaluate(.5), 6);
        foreach (var grade in result.Recipe.Balance!) Assert.Equal(0, grade.Amount);
        foreach (var band in result.Recipe.Hsl!) Assert.Equal(new HslAdjustment(), band);
        Assert.Throws<ArgumentException>(() => (options with { Components = MatchComponents.None }).Validate());
    }
}
