using System.Text.Json;
using FotoAutomat.Core;

// Repeatable synthetic baseline, not a camera-quality benchmark or similarity percentage.
static byte[] Image(int seed)
{
    var random = new Random(seed); var pixels = new byte[64 * 64 * 4];
    for (int i = 0; i < pixels.Length; i += 4) { for (int c = 0; c < 3; c++) pixels[i + c] = (byte)random.Next(20, 230); pixels[i + 3] = 255; }
    return pixels;
}
static double Rmse(byte[] a, byte[] b)
{
    double sum = 0; for (int i = 0; i < a.Length; i += 4) for (int c = 0; c < 3; c++) sum += Math.Pow(a[i + c] - b[i + c], 2); return Math.Sqrt(sum / (a.Length / 4 * 3));
}
var cases = new[]
{
    new ColorRecipe("identity"),
    new ColorRecipe("warm-tone", Exposure: .65, Contrast: 12, Warmth: 22, Tint: -8, Saturation: 15, Highlights: -10, Shadows: 8),
    new ColorRecipe("cool-muted", Exposure: -.35, Contrast: -15, Warmth: -25, Tint: 12, Saturation: -25, Highlights: -15, Blacks: 8)
};
foreach (var known in cases)
{
    var pairs = new[] { 7, 11 }.Select(seed => { var p = Image(seed); return new MatchPair(p, ColorProcessor.Apply(p, known), 64, 64); }).ToArray();
    var fit = PairedMatchEngine.Fit(pairs); var unseen = Image(99); var expected = ColorProcessor.Apply(unseen, known);
    double before = Rmse(unseen, expected), after = Rmse(ColorProcessor.Apply(unseen, fit.Recipe), expected);
    bool passed = known.Name == "identity" ? after == 0 : after < before * .4;
    Console.WriteLine(JsonSerializer.Serialize(new { Case = known.Name, Kind = "synthetic controlled tone", Metric = "sRGB RGB RMSE, byte levels; not percent", FitBefore = fit.BeforeRmse, FitAfter = fit.FitRmse, CheckWithinPairs = fit.CheckRmse, UnseenImageBefore = before, UnseenImageAfter = after, fit.Evaluations, fit.ElapsedMilliseconds, Passed = passed, Recipe = fit.Recipe }));
    if (!passed) Environment.ExitCode = 1;
}
