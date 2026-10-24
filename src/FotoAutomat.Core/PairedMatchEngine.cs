using System.Diagnostics;
namespace FotoAutomat.Core;

public sealed record MatchPair(byte[] Source, byte[] Reference, int Width, int Height);
public sealed record MatchOptions(int Samples = 2048, int Passes = 6, bool Normalize = true, bool Tone = true, bool Color = true)
{
    public bool NormalizeExposure { get; init; } = true;
    public bool NormalizeWhiteBalance { get; init; } = true;
    public bool? ColorTone { get; init; }
    public MatchComponents Components { get; init; } = MatchComponents.All;
    /// <summary>v5a: the references are scenes whose own colours (sea, sky, stone) are content, not look. Colour is then
    /// learned only from neutral anchors and whole-image quantiles only learn tone. Off by default (v4 behaviour).</summary>
    public bool SceneReference { get; init; }
    public void Validate()
    {
        if ((Components & ~MatchComponents.All) != 0 || !(Normalize && (NormalizeExposure || NormalizeWhiteBalance)) && !(Tone && (Components & MatchComponents.Light) != 0) && !(Color && (Components & MatchComponents.Color) != 0) && !((ColorTone ?? Color) && (Components & MatchComponents.Tone) != 0)) throw new ArgumentException("Select at least one match parameter.");
        if (Samples is < 128 or > 8192 || Passes is < 1 or > 12 || !(Normalize && (NormalizeExposure || NormalizeWhiteBalance)) && !Tone && !Color && !(ColorTone ?? Color)) throw new ArgumentException("Select a match group; use 128–8192 samples and 1–12 passes.");
    }
}
public sealed record MatchProgress(int Completed, int Total, double FitRmse);
public sealed record MatchResult(ColorRecipe Recipe, double BeforeRmse, double FitRmse, double CheckRmse, int FitSamples, int CheckSamples, int Evaluations, long ElapsedMilliseconds);
public sealed record MatchLookOrigin(string InputCamera, string ReferenceCamera, string Engine, double InitialCheckRmse)
{
    public string Metric { get; init; } = "Paired check RGB RMSE";
    public void Validate()
    {
        if (new[] { InputCamera, ReferenceCamera, Engine, Metric }.Any(s => s is null || s.Length > 80) || !double.IsFinite(InitialCheckRmse) || InitialCheckRmse < 0) throw new ArgumentException("Invalid Match Look origin.");
    }
}

/// <summary>Deterministic paired RGB baseline; fits editable parameters through the production ColorProcessor. Not an AI model.</summary>
public static class PairedMatchEngine
{
    private sealed record Parameter(double Min, double Max, double Step, Func<ColorRecipe, double> Read, Func<ColorRecipe, double, ColorRecipe> Write);
    public static MatchResult Fit(IReadOnlyList<MatchPair> pairs, MatchOptions? options = null, IProgress<MatchProgress>? progress = null, CancellationToken ct = default)
    {
        options ??= new(); options.Validate();
        if (pairs.Count is < 1 or > 16) throw new ArgumentException("Match needs 1–16 aligned image pairs.");
        foreach (var pair in pairs)
            if (pair.Width < 1 || pair.Height < 1 || (long)pair.Width * pair.Height * 4 != pair.Source.Length || pair.Reference.Length != pair.Source.Length) throw new ArgumentException("Source and reference must have the same aligned dimensions.");
        ct.ThrowIfCancellationRequested(); var timer = Stopwatch.StartNew();
        var fitSource = new List<byte>(); var fitTarget = new List<byte>(); var checkSource = new List<byte>(); var checkTarget = new List<byte>();
        // Spatially distributed deterministic samples, separate coordinates reserved for checking.
        foreach (var pair in pairs)
        {
            ct.ThrowIfCancellationRequested(); int pixels = pair.Width * pair.Height, count = Math.Min(options.Samples / pairs.Count, pixels);
            for (int k = 0; k < count; k++)
            {
                int i = (int)((k + .5) * pixels / count) * 4;
                if (pair.Source[i + 3] != 255 || pair.Reference[i + 3] != 255) continue;
                var s = k % 5 == 0 ? checkSource : fitSource; var t = k % 5 == 0 ? checkTarget : fitTarget;
                for (int c = 0; c < 4; c++) { s.Add(pair.Source[i + c]); t.Add(pair.Reference[i + c]); }
            }
        }
        if (fitSource.Count < 128 * 4 || checkSource.Count < 16 * 4) throw new ArgumentException("Not enough opaque pixels for fitting and checking. Use larger opaque photos.");
        byte[] input = fitSource.ToArray(), target = fitTarget.ToArray(), checkInput = checkSource.ToArray(), checkTargetPixels = checkTarget.ToArray();
        double Rmse(byte[] a, byte[] b) { double sum = 0; for (int i = 0; i < a.Length; i += 4) for (int c = 0; c < 3; c++) sum += Math.Pow(a[i + c] - b[i + c], 2); return Math.Sqrt(sum / (a.Length / 4 * 3)); }
        double Loss(ColorRecipe recipe) => Rmse(ColorProcessor.Apply(input, recipe, cancellationToken: ct), target);
        var optimized = Optimize(Loss, options, progress, ct); var best = optimized.Recipe;
        return new(best, Rmse(input, target), Loss(best), Rmse(ColorProcessor.Apply(checkInput, best, cancellationToken: ct), checkTargetPixels), input.Length / 4, checkInput.Length / 4, optimized.Evaluations, timer.ElapsedMilliseconds);
    }
    internal sealed record Optimization(ColorRecipe Recipe, int Evaluations);
    internal static Optimization Optimize(Func<ColorRecipe, double> loss, MatchOptions options, IProgress<MatchProgress>? progress, CancellationToken ct, bool conservative = false)
    {
        int evaluations = 0;
        double Evaluate(ColorRecipe recipe)
        {
            ct.ThrowIfCancellationRequested(); evaluations++;
            double error = loss(recipe);
            // A tiny preference for simpler recipes resolves near-identical candidates without masking measured error.
            return error + Complexity(recipe) * .001;
        }
        var best = new ColorRecipe("Matched look") { Curves = Enumerable.Range(0, 5).Select(_ => ToneCurve.Identity).ToArray(), Hsl = Enumerable.Range(0, 8).Select(_ => new HslAdjustment()).ToArray(), Balance = Enumerable.Range(0, 4).Select(_ => new ColorGrade()).ToArray() };
        var parameters = Parameters(options, conservative); double score = Evaluate(best);
        int total = parameters.Count * options.Passes, completed = 0;
        for (int pass = 0; pass < options.Passes; pass++)
        {
            foreach (var parameter in parameters)
            {
                ct.ThrowIfCancellationRequested(); double step = parameter.Step * Math.Pow(.5, pass), current = parameter.Read(best);
                ColorRecipe winner = best; double winningScore = score;
                foreach (double direction in new[] { -1d, 1d })
                {
                    double value = Math.Clamp(current + direction * step, parameter.Min, parameter.Max); if (value == current) continue;
                    var candidate = parameter.Write(best, value); double next = Evaluate(candidate);
                    if (next < winningScore - 1e-9) { winningScore = next; winner = candidate; }
                }
                // Continue along the same direction at this scale while it improves, with bounded work.
                if (winner != best)
                {
                    double direction = Math.Sign(parameter.Read(winner) - current);
                    for (int hop = 0; hop < 3; hop++)
                    {
                        double value = Math.Clamp(parameter.Read(winner) + direction * step, parameter.Min, parameter.Max);
                        var candidate = parameter.Write(winner, value); double next = Evaluate(candidate);
                        if (next >= winningScore - 1e-9) break; winner = candidate; winningScore = next;
                    }
                }
                best = winner; score = winningScore; progress?.Report(new(++completed, total, loss(best)));
            }
        }
        best.Validate();
        ct.ThrowIfCancellationRequested(); return new(best, evaluations);
    }
    private static double Complexity(ColorRecipe r) => Math.Abs(r.Exposure) + (Math.Abs(r.Contrast) + Math.Abs(r.Warmth) + Math.Abs(r.Tint) + Math.Abs(r.Saturation)) / 100 + (r.Hsl ?? []).Sum(v => (Math.Abs(v.Hue) + Math.Abs(v.Saturation) + Math.Abs(v.Lightness)) / 100) + (r.Balance ?? []).Sum(v => v.Amount / 100);
    private static double[] Offset(ColorGrade g) => [g.Amount * Math.Cos(g.Hue * Math.PI / 180), g.Amount * Math.Sin(g.Hue * Math.PI / 180)];
    private static List<Parameter> Parameters(MatchOptions options, bool conservative)
    {
        var p = new List<Parameter>();
        if (options.Normalize)
        {
            if (options.NormalizeExposure) p.Add(new(conservative ? -1.5 : -3, conservative ? 1.5 : 3, .5, r => r.Exposure, (r, v) => r with { Exposure = v }));
            if (options.NormalizeWhiteBalance)
            {
            p.Add(new(conservative ? -60 : -100, conservative ? 60 : 100, 20, r => r.Warmth, (r, v) => r with { Warmth = v }));
            p.Add(new(conservative ? -60 : -100, conservative ? 60 : 100, 20, r => r.Tint, (r, v) => r with { Tint = v }));
            }
        }
        if (options.Tone)
        {
            double range = conservative ? 50 : 100;
            if (options.Components.HasFlag(MatchComponents.Contrast)) p.Add(new(conservative ? -40 : -80, conservative ? 40 : 80, 20, r => r.Contrast, (r, v) => r with { Contrast = v }));
            if (options.Components.HasFlag(MatchComponents.Highlights)) p.Add(new(-range, range, 20, r => r.Highlights, (r, v) => r with { Highlights = v }));
            if (options.Components.HasFlag(MatchComponents.Shadows)) p.Add(new(-range, range, 20, r => r.Shadows, (r, v) => r with { Shadows = v }));
            if (options.Components.HasFlag(MatchComponents.Whites)) p.Add(new(-range, range, 20, r => r.Whites, (r, v) => r with { Whites = v }));
            if (options.Components.HasFlag(MatchComponents.Blacks)) p.Add(new(-range, range, 20, r => r.Blacks, (r, v) => r with { Blacks = v }));
        }
        if (options.Color && options.Components.HasFlag(MatchComponents.Saturation)) p.Add(new(conservative ? -50 : -100, conservative ? 50 : 100, 20, r => r.Saturation, (r, v) => r with { Saturation = v }));
        // Color Balance shadows/midtones/highlights, as in Capture One's Color Tone group. Fitted before the RGB
        // channel curves so the readable wheels carry a split tone and the curves only take the residual. Fitted as Cartesian
        // offsets (Amount·cos Hue, Amount·sin Hue) so the optimizer never has to cross the hue wrap-around.
        if (options.ColorTone ?? options.Color) for (int wheel = 1; wheel <= 3; wheel++) for (int axis = 0; axis < 2; axis++)
        {
            if (!options.Components.HasFlag(wheel == 1 ? MatchComponents.ShadowBalance : wheel == 2 ? MatchComponents.MidtoneBalance : MatchComponents.HighlightBalance)) continue;
            int w = wheel, a = axis; double range = conservative ? 40 : 80;
            p.Add(new(-range, range, 10, r => Offset(r.Balance![w])[a], (r, v) =>
            {
                var grades = r.Balance!.ToArray(); var o = Offset(grades[w]); o[a] = v;
                double amount = Math.Min(100, Math.Sqrt(o[0] * o[0] + o[1] * o[1])), hue = amount < 1e-9 ? 0 : (Math.Atan2(o[1], o[0]) * 180 / Math.PI + 360) % 360;
                grades[w] = grades[w] with { Hue = hue, Amount = amount }; return r with { Balance = grades };
            }));
        }
        // Three editable interior knots in combined RGB and each RGB channel. Luma is kept identity.
        for (int channel = 0; channel < 5; channel++)
        {
            if (channel == 1 || channel == 0 && !options.Tone || channel > 1 && !(options.ColorTone ?? options.Color)) continue;
            if (!options.Components.HasFlag(channel == 0 ? MatchComponents.RgbCurve : channel == 2 ? MatchComponents.RedCurve : channel == 3 ? MatchComponents.GreenCurve : MatchComponents.BlueCurve)) continue;
            for (int knot = 1; knot <= 3; knot++)
            {
                int c = channel, k = knot;
                p.Add(new(conservative ? k / 4d - .125 : 0, conservative ? k / 4d + .125 : 1, .08, r => r.Curves![c].Evaluate(k / 4d), (r, v) =>
                {
                    var curves = r.Curves!.ToArray(); var points = Enumerable.Range(0, 5).Select(n => new CurvePoint(n / 4d, n == k ? v : curves[c].Evaluate(n / 4d))).ToArray();
                    // Monotone tone curves avoid inverted local contrast.
                    double gap = conservative ? .0625 : 0;
                    points[k] = points[k] with { Y = Math.Clamp(v, points[k - 1].Y + gap, points[k + 1].Y - gap) };
                    curves[c] = new(points); return r with { Curves = curves };
                }));
            }
        }
        if (options.Color) for (int band = 0; band < 8; band++) for (int field = 0; field < 3; field++)
        {
            if (!options.Components.HasFlag(field == 0 ? MatchComponents.HslHue : field == 1 ? MatchComponents.HslSaturation : MatchComponents.HslLightness)) continue;
            int b = band, f = field;
            double range = conservative ? f == 0 ? 35 : 50 : 100;
            p.Add(new(-range, range, 15, r => f == 0 ? r.Hsl![b].Hue : f == 1 ? r.Hsl![b].Saturation : r.Hsl![b].Lightness, (r, v) =>
            {
                var hsl = r.Hsl!.ToArray(); hsl[b] = f == 0 ? hsl[b] with { Hue = v } : f == 1 ? hsl[b] with { Saturation = v } : hsl[b] with { Lightness = v }; return r with { Hsl = hsl };
            }));
        }
        return p;
    }
}
