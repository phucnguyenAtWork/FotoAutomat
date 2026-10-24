namespace FotoAutomat.Core;

/// <summary>What a Color Editor range actually sees and touches, rebuilt from the same pipeline the preview uses.
/// Eyedropper samples and "View selected color range" read from here instead of the final preview, which already
/// includes later stages (ranges after this one, Clarity, glow, vignette, grain, layers).</summary>
public static class ColorEditorStage
{
    /// <summary>Ranges of a recipe in the order the processor applies them: enabled Advanced, then enabled Skin.</summary>
    private static (ColorRangeAdjustment Range, bool Skin, int Index)[] Ordered(ColorRecipe recipe)
    {
        var advanced = (recipe.AdvancedColors ?? []).Select((r, i) => (r, false, i));
        var skin = (recipe.SkinColors ?? (recipe.SkinColor is { } one ? [one] : [])).Select((r, i) => (r, true, i));
        return [.. advanced.Concat(skin).Where(x => x.r.Enabled)];
    }

    /// <summary>Enabled ranges applied before the given one. An index past the end means "a range appended to that list".</summary>
    public static ColorRangeAdjustment[] PrecedingRanges(ColorRecipe recipe, bool skin, int index) =>
        [.. Ordered(recipe).Where(x => skin ? !x.Skin || x.Index < index : !x.Skin && x.Index < index).Select(x => x.Range)];

    /// <summary>BGRA8 pixels the selected range of <paramref name="look"/> (or of its layer) receives. Input is never modified.</summary>
    public static byte[] RangeInput(byte[] source, ColorRecipe look, int layer, bool skin, int index, int width, int height, CancellationToken ct = default)
    {
        var recipe = RecipeOf(look, layer);
        // A layer recipe runs on the base result with the layers above it already blended in.
        var start = layer < 0 ? source : ColorProcessor.Apply(source, look with { Layers = [.. look.Layers!.Take(layer)] }, width, height, ct);
        var pixels = ColorProcessor.BeforeColorEditor(start, recipe, width, height, ct);
        var preceding = PrecedingRanges(recipe, skin, index);
        if (preceding.Length == 0) return pixels;
        return AdjustToolProcessor.Apply(pixels, recipe with { AdvancedColors = preceding, SkinColors = [], SkinColor = null, Monochrome = false, BlackWhite = null }, ct);
    }

    /// <summary>Share (0–1) of the range's effect at each pixel: its weight on the colour it receives, times the layer mask and opacity.</summary>
    public static float[] Coverage(byte[] rangeInput, ColorRangeAdjustment range, ColorRecipe look, int layer, int width, int height, CancellationToken ct = default)
    {
        if ((long)width * height * 4 != rangeInput.Length) throw new ArgumentException("Kích thước ảnh không hợp lệ.");
        var coverage = new float[width * height];
        for (int i = 0; i < coverage.Length; i++)
        {
            if (i % 16384 == 0) ct.ThrowIfCancellationRequested();
            var (h, s, _) = AdjustToolProcessor.ToHsl(rangeInput[i * 4 + 2] / 255d, rangeInput[i * 4 + 1] / 255d, rangeInput[i * 4] / 255d);
            coverage[i] = (float)range.Weight(h, s);
        }
        if (layer >= 0 && look.Layers![layer] is { } adjustment)
        {
            var mask = adjustment.Enabled ? adjustment.Mask.Rasterize(width, height, ct) : new float[coverage.Length];
            for (int i = 0; i < coverage.Length; i++) coverage[i] *= (float)(mask[i] * adjustment.Opacity);
        }
        return coverage;
    }

    private static ColorRecipe RecipeOf(ColorRecipe look, int layer) =>
        layer < 0 ? look : look.Layers is { } layers && layer < layers.Length ? layers[layer].Recipe : throw new ArgumentOutOfRangeException(nameof(layer));
}
