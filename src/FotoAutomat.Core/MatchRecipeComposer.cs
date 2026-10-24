namespace FotoAutomat.Core;
public static class MatchRecipeComposer
{
    /// <summary>Impact scales the look; exposure/WB normalization stays full strength when retained.</summary>
    public static ColorRecipe Compose(ColorRecipe recipe, double impact, bool keepNormalize = true, bool asLayer = false)
    {
        recipe.Validate(); if (!double.IsFinite(impact) || impact < 0 || impact > 100 || recipe.Layers?.Length > 0) throw new ArgumentException("Match Impact must be 0–100; input recipe must have no layers.");
        double amount = impact / 100;
        var normalize = new ColorRecipe(recipe.Name, Exposure: keepNormalize ? recipe.Exposure : 0, Warmth: keepNormalize ? recipe.Warmth : 0, Tint: keepNormalize ? recipe.Tint : 0);
        if (asLayer) return normalize with { Layers = [new("Match Look", recipe with { Exposure = 0, Warmth = 0, Tint = 0 }, new(), amount)] };
        return recipe with
        {
            Exposure = normalize.Exposure, Warmth = normalize.Warmth, Tint = normalize.Tint,
            Contrast = recipe.Contrast * amount, Brightness = recipe.Brightness * amount,
            Highlights = recipe.Highlights * amount, Shadows = recipe.Shadows * amount, Whites = recipe.Whites * amount, Blacks = recipe.Blacks * amount,
            Saturation = recipe.Saturation * amount,
            Curves = recipe.Curves?.Select(c => new ToneCurve(c.Points.Select(p => p with { Y = p.X + (p.Y - p.X) * amount }).ToArray())).ToArray(),
            Hsl = recipe.Hsl?.Select(h => new HslAdjustment(h.Hue * amount, h.Saturation * amount, h.Lightness * amount)).ToArray(),
            Balance = recipe.Balance?.Select(g => g with { Amount = g.Amount * amount, Lightness = g.Lightness * amount }).ToArray()
        };
    }
}
