using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;
public sealed class PairedMatchTests
{
    static byte[] Image(int seed, int size = 48)
    {
        var random = new Random(seed); var p = new byte[size * size * 4];
        for (int i = 0; i < p.Length; i += 4) { p[i] = (byte)random.Next(20, 230); p[i + 1] = (byte)random.Next(20, 230); p[i + 2] = (byte)random.Next(20, 230); p[i + 3] = 255; }
        return p;
    }
    static double Error(byte[] a, byte[] b) { double sum = 0; for (int i = 0; i < a.Length; i += 4) for (int c = 0; c < 3; c++) sum += Math.Pow(a[i + c] - b[i + c], 2); return Math.Sqrt(sum / (a.Length / 4 * 3)); }
    [Fact]
    public void Identity_pair_stays_exact_and_input_is_immutable()
    {
        var p = Image(1); var original = p.ToArray(); var result = PairedMatchEngine.Fit([new(p, p, 48, 48)], new(Samples: 512, Passes: 2));
        Assert.Equal(0, result.FitRmse); Assert.Equal(0, result.CheckRmse); Assert.Equal(p, ColorProcessor.Apply(p, result.Recipe)); Assert.Equal(original, p);
    }
    [Fact]
    public void Paired_recipe_improves_unseen_image_and_has_no_spatial_or_ai_effects()
    {
        var p = Image(2); var known = new ColorRecipe(Exposure: .65, Warmth: 22, Tint: -8, Contrast: 12, Saturation: 15, Highlights: -10, Shadows: 8);
        var target = ColorProcessor.Apply(p, known); var result = PairedMatchEngine.Fit([new(p, target, 48, 48)], new(Samples: 1024, Passes: 6));
        Assert.True(result.FitRmse < result.BeforeRmse * .35, $"Before {result.BeforeRmse}, fit {result.FitRmse}"); Assert.True(result.CheckRmse < result.BeforeRmse * .4);
        var unseen = Image(33); var unseenTarget = ColorProcessor.Apply(unseen, known); var output = ColorProcessor.Apply(unseen, result.Recipe);
        Assert.True(Error(output, unseenTarget) < Error(unseen, unseenTarget) * .4);
        Assert.Null(result.Recipe.Lut); Assert.Null(result.Recipe.Layers); Assert.Equal(0, result.Recipe.Sharpening); Assert.Equal(0, result.Recipe.Grain);
        Assert.NotEqual(p, output); Assert.Equal(target, ColorProcessor.Apply(p, known));
    }
    [Fact]
    public void Group_switches_leave_disabled_groups_neutral_and_results_are_deterministic()
    {
        var p = Image(3); var target = ColorProcessor.Apply(p, new(Exposure: .5)); var options = new MatchOptions(Samples: 512, Passes: 3, Tone: false, Color: false);
        var a = PairedMatchEngine.Fit([new(p, target, 48, 48)], options); var b = PairedMatchEngine.Fit([new(p, target, 48, 48)], options);
        Assert.Equal(a.FitRmse, b.FitRmse); Assert.Equal(a.Recipe.Exposure, b.Recipe.Exposure); Assert.Equal(0, a.Recipe.Contrast); Assert.Equal(0, a.Recipe.Saturation);
        Assert.All(a.Recipe.Curves!, c => Assert.Equal(ToneCurve.Identity.Points, c.Points)); Assert.All(a.Recipe.Hsl!, h => Assert.Equal(new HslAdjustment(), h));
        Assert.Equal(0, a.CheckRmse);
    }
    [Fact]
    public void Invalid_pairs_options_and_cancellation_are_rejected()
    {
        var p = Image(4); Assert.Throws<ArgumentException>(() => PairedMatchEngine.Fit([]));
        Assert.Throws<ArgumentException>(() => PairedMatchEngine.Fit([new(p, [1, 2], 48, 48)]));
        Assert.Throws<ArgumentException>(() => PairedMatchEngine.Fit([new(p, p, 48, 48)], new(Normalize: false, Tone: false, Color: false)));
        Assert.Throws<ArgumentException>(() => PairedMatchEngine.Fit([new(new byte[p.Length], p, 48, 48)]));
        Assert.Throws<OperationCanceledException>(() => PairedMatchEngine.Fit([new(p, p, 48, 48)], ct: new CancellationToken(true)));
        using var cts = new CancellationTokenSource(); var reporter = new CancelProgress(cts);
        Assert.Throws<OperationCanceledException>(() => PairedMatchEngine.Fit([new(p, p, 48, 48)], progress: reporter, ct: cts.Token));
    }
    sealed class CancelProgress(CancellationTokenSource cts) : IProgress<MatchProgress> { public void Report(MatchProgress value) => cts.Cancel(); }
    [Fact]
    public void Camera_origin_roundtrips_and_cannot_claim_invalid_quality()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var recipe = new ColorRecipe("Matched") { MatchOrigin = new("Canon R50", "Fujifilm reference", "paired-rgb-v1", 4.2) };
            var store = new ColorWorkspaceStore(path); store.Save(new(null, [recipe])); Assert.Equal(recipe.MatchOrigin, store.Load().Looks[0].MatchOrigin);
            Assert.Throws<ArgumentException>(() => (recipe with { MatchOrigin = recipe.MatchOrigin with { InitialCheckRmse = double.NaN } }).Validate());
        }
        finally { File.Delete(path); }
    }
}
