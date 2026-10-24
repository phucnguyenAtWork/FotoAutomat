using FotoAutomat.Core;
namespace FotoAutomat.Tests;

public class ColorEditorStageTests
{
    // Two pixels: saturated red and saturated blue (BGRA).
    private static byte[] RedAndBlue() => [30, 30, 220, 255, 220, 30, 30, 255];
    private static (byte R, byte G, byte B) Pixel(byte[] bgra, int i) => (bgra[i * 4 + 2], bgra[i * 4 + 1], bgra[i * 4]);
    private static ColorRangeAdjustment RedRange(double saturation = -100) => new(CenterHue: 0, CenterSaturation: .76, Width: 20, SaturationRadius: .3, Saturation: saturation);

    [Fact]
    public void Coverage_marks_exactly_the_pixels_the_range_changes()
    {
        var recipe = new ColorRecipe { AdvancedColors = [RedRange()] };
        var source = RedAndBlue();
        var input = ColorEditorStage.RangeInput(source, recipe, -1, false, 0, 2, 1);
        var coverage = ColorEditorStage.Coverage(input, recipe.AdvancedColors![0], recipe, -1, 2, 1);
        var output = ColorProcessor.Apply(source, recipe, 2, 1);
        Assert.True(coverage[0] > .9f);
        Assert.Equal(0f, coverage[1]);
        Assert.NotEqual(Pixel(input, 0), Pixel(output, 0));
        Assert.Equal(Pixel(input, 1), Pixel(output, 1));
    }

    [Fact]
    public void Range_input_ignores_stages_after_the_color_editor()
    {
        // Vignette and a strong second range change the final colour; the first range must still see the original red.
        var recipe = new ColorRecipe { AdvancedColors = [RedRange(saturation: 0) with { Hue = 30 }, RedRange()], Vignette = -80 };
        var source = RedAndBlue();
        var final = ColorProcessor.Apply(source, recipe, 2, 1);
        var firstInput = ColorEditorStage.RangeInput(source, recipe, -1, false, 0, 2, 1);
        Assert.Equal(Pixel(source, 0), Pixel(firstInput, 0));
        Assert.NotEqual(Pixel(final, 0), Pixel(firstInput, 0));
        Assert.Equal(RedAndBlue(), source);
    }

    [Fact]
    public void Later_ranges_see_the_output_of_earlier_ranges_and_skin_comes_after_advanced()
    {
        var shift = RedRange(saturation: 0) with { Hue = 30 };
        var recipe = new ColorRecipe { AdvancedColors = [shift, RedRange()], SkinColors = [RedRange()] };
        var source = RedAndBlue();
        var second = ColorEditorStage.RangeInput(source, recipe, -1, false, 1, 2, 1);
        Assert.NotEqual(Pixel(source, 0), Pixel(second, 0));
        Assert.Equal(2, ColorEditorStage.PrecedingRanges(recipe, true, 0).Length);
        Assert.Single(ColorEditorStage.PrecedingRanges(recipe, false, 1));
        Assert.Equal(2, ColorEditorStage.PrecedingRanges(recipe, false, 5).Length);
        var disabled = recipe with { AdvancedColors = [shift with { Enabled = false }, RedRange()] };
        Assert.Empty(ColorEditorStage.PrecedingRanges(disabled, false, 1));
    }

    [Fact]
    public void Layer_range_runs_on_the_base_result_and_coverage_follows_the_layer_mask()
    {
        var layerRecipe = new ColorRecipe { AdvancedColors = [RedRange()] };
        var look = new ColorRecipe(Exposure: .5) { Layers = [new PhotoAdjustmentLayer("L", layerRecipe, new PhotoMask(), Opacity: .5)] };
        var source = RedAndBlue();
        var input = ColorEditorStage.RangeInput(source, look, 0, false, 0, 2, 1);
        var baseOnly = ColorProcessor.Apply(source, look with { Layers = [] }, 2, 1);
        Assert.Equal(Pixel(baseOnly, 0), Pixel(input, 0));
        var coverage = ColorEditorStage.Coverage(input, layerRecipe.AdvancedColors![0], look, 0, 2, 1);
        Assert.InRange(coverage[0], .4f, .5f);
        Assert.Equal(0f, coverage[1]);
    }

    [Fact]
    public void Splitting_the_pipeline_does_not_change_rendered_output()
    {
        var recipe = new ColorRecipe(Exposure: .3, Contrast: 20, Saturation: 10) { AdvancedColors = [RedRange(-50)], Vignette = -30 };
        var source = RedAndBlue();
        var full = ColorProcessor.Apply(source, recipe, 2, 1);
        Assert.Equal(full, ColorProcessor.Apply(RedAndBlue(), recipe, 2, 1));
        Assert.Equal(RedAndBlue(), source);
    }
}
