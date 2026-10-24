namespace FotoAutomat.Core;

public sealed record LutBakeResult(CubeLut Lut, string[] Excluded);

/// <summary>Bakes the per-colour part of a recipe into a 3D LUT through the production ColorProcessor.
/// Spatial tools depend on pixel position, cannot be represented by a LUT and are reported as excluded.</summary>
public static class RecipeLutBaker
{
    public static LutBakeResult Bake(ColorRecipe recipe, int size = 33, CancellationToken cancellationToken = default)
    {
        recipe.Validate();
        if (size is < 2 or > 65) throw new ArgumentException("LUT size must be 2–65.");
        var excluded = new List<string>();
        var global = GlobalOnly(recipe, "", excluded);
        int count = size * size * size; var grid = new byte[count * 4];
        // Red changes fastest, matching CubeLut and the .cube format.
        for (int b = 0, i = 0; b < size; b++) for (int g = 0; g < size; g++) for (int r = 0; r < size; r++, i++)
        {
            grid[i * 4 + 2] = Node(r, size); grid[i * 4 + 1] = Node(g, size); grid[i * 4] = Node(b, size); grid[i * 4 + 3] = 255;
        }
        var output = ColorProcessor.Apply(grid, global, size * size, size, cancellationToken);
        var values = new double[count * 3];
        for (int i = 0; i < count; i++) { values[i * 3] = output[i * 4 + 2] / 255d; values[i * 3 + 1] = output[i * 4 + 1] / 255d; values[i * 3 + 2] = output[i * 4] / 255d; }
        var lut = new CubeLut(size, values, [0, 0, 0], [1, 1, 1]); lut.Validate();
        return new(lut, excluded.Distinct().ToArray());
    }

    // ColorProcessor works on 8-bit input, so grid nodes are evaluated at the nearest byte value.
    private static byte Node(int k, int size) => (byte)Math.Round(k * 255d / (size - 1));

    private static ColorRecipe GlobalOnly(ColorRecipe r, string scope, List<string> excluded)
    {
        foreach (var (name, value) in new[] { ("Clarity", r.Clarity), ("Structure", r.Structure), ("Dehaze", r.Dehaze), ("Vignette", r.Vignette), ("Grain", r.Grain), ("Sharpening", r.Sharpening) })
            if (value != 0) excluded.Add(scope + name);
        if ((r.VignetteEv ?? 0) != 0) excluded.Add(scope + "Vignetting");
        if (r.BlurGlow is { IsActive: true }) excluded.Add(scope + "Blur & Glow");
        var layers = new List<PhotoAdjustmentLayer>();
        foreach (var layer in r.Layers ?? [])
        {
            // A Full mask is uniform (inverted Full is uniformly zero), so the layer is still a per-colour operation.
            if (layer.Mask.Kind != PhotoMaskKind.Full) { excluded.Add($"Layer '{layer.Name}'"); continue; }
            layers.Add(layer with { Recipe = GlobalOnly(layer.Recipe, $"Layer '{layer.Name}': ", excluded) });
        }
        return r with { BlurGlow = null, Clarity = 0, Structure = 0, Dehaze = 0, Vignette = 0, VignetteEv = null, Grain = 0, Sharpening = 0, Layers = r.Layers is null ? null : layers.ToArray() };
    }
}
