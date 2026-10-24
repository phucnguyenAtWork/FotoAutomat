using FotoAutomat.Core;
namespace FotoAutomat.Tests;
public sealed class MatchControlsTests
{
    [Fact]
    public void Impact_zero_keeps_normalization_and_full_impact_reproduces_the_recipe()
    {
        var recipe = new ColorRecipe(Exposure: .3, Warmth: 10, Tint: -5, Contrast: 20, Saturation: -15, Highlights: -10) { Curves = Enumerable.Range(0, 5).Select(_ => new ToneCurve([new(0, 0), new(.5, .6), new(1, 1)])).ToArray() };
        byte[] pixels = [70, 110, 160, 255, 150, 140, 130, 255];
        Assert.Equal(ColorProcessor.Apply(pixels, new(Exposure: .3, Warmth: 10, Tint: -5)), ColorProcessor.Apply(pixels, MatchRecipeComposer.Compose(recipe, 0)));
        Assert.Equal(pixels, ColorProcessor.Apply(pixels, MatchRecipeComposer.Compose(recipe, 0, keepNormalize: false)));
        Assert.Equal(ColorProcessor.Apply(pixels, recipe), ColorProcessor.Apply(pixels, MatchRecipeComposer.Compose(recipe, 100)));
        var layer = MatchRecipeComposer.Compose(recipe, 35, asLayer: true); Assert.Equal(.35, layer.Layers![0].Opacity); Assert.Equal(.3, layer.Exposure); Assert.Equal(0, layer.Layers[0].Recipe.Exposure); layer.Validate();
        Assert.Equal(ColorProcessor.Apply(pixels, new(Exposure: .3, Warmth: 10, Tint: -5)), ColorProcessor.Apply(pixels, MatchRecipeComposer.Compose(recipe, 0, asLayer: true)));
    }
    [Fact]
    public void Color_tone_can_fit_without_HSL_and_Normalize_exposure_can_be_disabled()
    {
        var p = new byte[1024 * 4]; for (int i = 0; i < 1024; i++) { p[i * 4] = (byte)(30 + i % 180); p[i * 4 + 1] = (byte)(40 + i % 180); p[i * 4 + 2] = (byte)(50 + i % 180); p[i * 4 + 3] = 255; }
        var target = ColorProcessor.Apply(p, new(Exposure: .5, Warmth: 20));
        var onlyTone = ReferenceMatchEngine.Fit(new(p, 32, 32), new(target, 16, 64), new(Passes: 2, Normalize: false, Tone: false, Color: false) { ColorTone = true });
        Assert.Equal(0, onlyTone.Recipe.Exposure); Assert.Equal(0, onlyTone.Recipe.Saturation); Assert.All(onlyTone.Recipe.Hsl!, h => Assert.Equal(new HslAdjustment(), h)); Assert.Equal(0, onlyTone.Recipe.Contrast);
        var onlyWb = ReferenceMatchEngine.Fit(new(p, 32, 32), new(target, 16, 64), new(Passes: 2, Tone: false, Color: false) { NormalizeExposure = false, ColorTone = false }); Assert.Equal(0, onlyWb.Recipe.Exposure);
        Assert.Throws<ArgumentException>(() => new MatchOptions(Normalize: true, Tone: false, Color: false) { NormalizeExposure = false, NormalizeWhiteBalance = false, ColorTone = false }.Validate());
        Assert.Throws<ArgumentException>(() => MatchRecipeComposer.Compose(new(), double.NaN));
    }
}
