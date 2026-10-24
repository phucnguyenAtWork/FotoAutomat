namespace FotoAutomat.Core;

public sealed record RecipeParameter(string Name, double Min, double Max);

/// <summary>Fixed numeric layout of the global, LUT-bakeable part of a ColorRecipe, used by the Match Look parameter
/// predictor (dataset labels and ONNX output). Unit form maps every parameter to [-1, 1] (tanh-range style, as in
/// Hu et al. "Exposure"); raw form is the recipe value. Spatial tools, layers, LUTs and Monochrome are not encoded.</summary>
public static class RecipeVector
{
    private static readonly double[] Knots = [.25, .5, .75];
    private static readonly int[] CurveChannels = [0, 2, 3, 4]; // RGB, Red, Green, Blue (index 1, luma, stays identity)
    private static readonly string[] Bands = ["Red", "Orange", "Yellow", "Green", "Aqua", "Blue", "Purple", "Magenta"];
    private static readonly string[] Wheels = ["Shadows", "Midtones", "Highlights"];

    public static IReadOnlyList<RecipeParameter> Parameters { get; } = Build();
    public static int Length => Parameters.Count;

    private static List<RecipeParameter> Build()
    {
        var p = new List<RecipeParameter>
        {
            new("Exposure", -1.5, 1.5), new("Warmth", -60, 60), new("Tint", -40, 40),
            new("Contrast", -50, 50), new("Highlights", -60, 60), new("Shadows", -60, 60), new("Whites", -50, 50), new("Blacks", -50, 50),
            new("Saturation", -60, 60), new("Vibrance", -50, 50)
        };
        foreach (var channel in new[] { "RGB", "Red", "Green", "Blue" }) foreach (var knot in Knots) p.Add(new($"Curve.{channel}.{knot:0.00}", -.15, .15));
        foreach (var band in Bands) { p.Add(new($"HSL.{band}.Hue", -30, 30)); p.Add(new($"HSL.{band}.Saturation", -60, 60)); p.Add(new($"HSL.{band}.Lightness", -40, 40)); }
        foreach (var wheel in Wheels) { p.Add(new($"Balance.{wheel}.X", -40, 40)); p.Add(new($"Balance.{wheel}.Y", -40, 40)); }
        return p;
    }

    /// <summary>Raw parameter values of a recipe, clamped to the encoded ranges.</summary>
    public static double[] Encode(ColorRecipe r)
    {
        var v = new List<double> { r.Exposure, r.Warmth, r.Tint, r.Contrast, r.Highlights, r.Shadows, r.Whites, r.Blacks, r.Saturation, r.Vibrance };
        foreach (int c in CurveChannels) foreach (var x in Knots) v.Add((r.Curves is { } curves ? curves[c].Evaluate(x) : x) - x);
        for (int b = 0; b < 8; b++) { var h = r.Hsl?[b] ?? new(); v.Add(h.Hue); v.Add(h.Saturation); v.Add(h.Lightness); }
        for (int w = 1; w <= 3; w++)
        {
            var g = r.Balance?[w] ?? new(); double a = g.Hue * Math.PI / 180;
            v.Add(g.Amount * Math.Cos(a)); v.Add(g.Amount * Math.Sin(a));
        }
        return v.Select((x, i) => Math.Clamp(x, Parameters[i].Min, Parameters[i].Max)).ToArray();
    }

    /// <summary>Builds a valid recipe from raw values (clamped; curve knots kept monotone).</summary>
    public static ColorRecipe Decode(IReadOnlyList<double> raw, string name = "Matched look")
    {
        if (raw.Count != Length) throw new ArgumentException($"Recipe vector needs {Length} values.");
        double V(int i) => double.IsFinite(raw[i]) ? Math.Clamp(raw[i], Parameters[i].Min, Parameters[i].Max) : 0;
        var curves = Enumerable.Range(0, 5).Select(_ => ToneCurve.Identity).ToArray(); int k = 10;
        foreach (int c in CurveChannels)
        {
            var points = new List<CurvePoint> { new(0, 0) }; double previous = 0;
            foreach (var x in Knots) { double y = Math.Clamp(x + V(k++), previous, 1); points.Add(new(x, y)); previous = y; }
            points.Add(new(1, 1)); curves[c] = new(points.ToArray());
        }
        var hsl = Enumerable.Range(0, 8).Select(_ => new HslAdjustment(V(k++), V(k++), V(k++))).ToArray();
        var balance = new ColorGrade[4]; balance[0] = new();
        for (int w = 1; w <= 3; w++)
        {
            double x = V(k++), y = V(k++), amount = Math.Min(100, Math.Sqrt(x * x + y * y));
            balance[w] = new(amount < 1e-9 ? 0 : (Math.Atan2(y, x) * 180 / Math.PI + 360) % 360, amount);
        }
        var recipe = new ColorRecipe(name, Exposure: V(0), Warmth: V(1), Tint: V(2), Contrast: V(3), Highlights: V(4), Shadows: V(5), Whites: V(6), Blacks: V(7), Saturation: V(8))
        { Vibrance = V(9), Curves = curves, Hsl = hsl, Balance = balance };
        recipe.Validate(); return recipe;
    }

    public static double[] ToUnit(IReadOnlyList<double> raw) => raw.Select((x, i) => (x - (Parameters[i].Max + Parameters[i].Min) / 2) / ((Parameters[i].Max - Parameters[i].Min) / 2)).ToArray();
    public static double[] FromUnit(IReadOnlyList<double> unit) => unit.Select((u, i) => (Parameters[i].Max + Parameters[i].Min) / 2 + Math.Clamp(u, -1, 1) * (Parameters[i].Max - Parameters[i].Min) / 2).ToArray();
}
