using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;
public sealed class ReferenceMatchTests
{
    [Fact]
    public void Region_priority_matches_subject_despite_different_backgrounds()
    {
        var source = new byte[64 * 64 * 4]; var target = new byte[source.Length];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        {
            int i = (y * 64 + x) * 4; bool subject = x >= 16 && x < 48 && y >= 16 && y < 48;
            source[i] = subject ? (byte)100 : (byte)215; source[i + 1] = subject ? (byte)135 : (byte)205; source[i + 2] = subject ? (byte)165 : (byte)195; source[i + 3] = 255;
            target[i] = subject ? (byte)92 : (byte)60; target[i + 1] = subject ? (byte)116 : (byte)80; target[i + 2] = subject ? (byte)140 : (byte)110; target[i + 3] = 255;
        }
        var region = new MatchRegion(.25, .25, .5, .5);
        var a = new MatchPhoto(source, 64, 64) { Regions = [region] }; var b = new MatchPhoto(target, 64, 64) { Regions = [region] };
        var options = new MatchOptions(Passes: 3, Tone: false, Color: false) { NormalizeWhiteBalance = false, ColorTone = false };
        var global = ReferenceMatchEngine.Fit(a with { Regions = [] }, b with { Regions = [] }, options);
        var regional = ReferenceMatchEngine.Fit(a, b, options);
        double SubjectError(ColorRecipe recipe)
        {
            var p = ColorProcessor.Apply(source, recipe); int i = (32 * 64 + 32) * 4;
            return Enumerable.Range(0, 3).Sum(c => Math.Abs(p[i + c] - target[i + c]));
        }
        Assert.True(regional.UsedRegions); Assert.Equal(ReferenceMatchEngine.RegionEngineId, regional.Engine);
        Assert.True(regional.AfterRegionRmse < regional.BeforeRegionRmse);
        Assert.True(SubjectError(regional.Recipe) < SubjectError(global.Recipe), "Region priority did not protect subject from background bias.");
        Assert.Equal(100, source[(32 * 64 + 32) * 4]);
    }
    [Fact]
    public void Missing_regions_fall_back_and_invalid_region_bounds_are_rejected()
    {
        var a = new MatchPhoto(Pixels(2), 32, 32); var b = a with { Regions = [new(.1, .1, .8, .8)] };
        var result = ReferenceMatchEngine.Fit(a, b, new(Passes: 1)); Assert.False(result.UsedRegions); Assert.Null(result.AfterRegionRmse); Assert.Equal(0, result.AfterDistributionRmse);
        Assert.Throws<ArgumentException>(() => ReferenceMatchEngine.Fit(a with { Regions = [new(-.1, 0, .5, .5)] }, b));
        Assert.Throws<ArgumentException>(() => ReferenceMatchEngine.Fit(a with { Regions = [new(0, 0, double.NaN, .5)] }, b));
    }
    [Fact]
    public void Equal_RGB_marginals_with_wrong_channel_relationships_are_not_a_perfect_match()
    {
        var neutral = new byte[1024 * 4]; var unrelated = new byte[neutral.Length];
        for (int i = 0; i < 1024; i++)
        {
            byte value = (byte)(40 + i % 160); for (int c = 0; c < 3; c++) neutral[i * 4 + c] = unrelated[i * 4 + c] = value;
            neutral[i * 4 + 3] = unrelated[i * 4 + 3] = 255;
        }
        // Reverse only green: identical per-channel histograms, but gray pixels turn into saturated colors.
        for (int i = 0; i < 1024; i++) unrelated[i * 4 + 1] = neutral[(1023 - i) * 4 + 1];
        var result = ReferenceMatchEngine.Fit(new(neutral, 32, 32), new(unrelated, 16, 64), new(Passes: 1));
        Assert.True(result.BeforeDistributionRmse > 10, "Independent RGB marginals incorrectly report perfect color similarity.");
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Repeating_a_match_after_a_different_reference_preserves_output(bool scene)
    {
        var pixels = Pixels(71); var untouched = pixels.ToArray();
        var source = new MatchPhoto(pixels, 32, 32);
        var reference = new MatchPhoto(Pixels(72), 32, 32);
        var options = new MatchOptions(Passes: 1) { SceneReference = scene };
        var first = ReferenceMatchEngine.Fit(source, reference, options);
        _ = ReferenceMatchEngine.Fit(source, new MatchPhoto(Pixels(73), 32, 32), options);
        var repeated = ReferenceMatchEngine.Fit(source, reference, options);
        Assert.Equal(first.AfterDistributionRmse, repeated.AfterDistributionRmse);
        Assert.Equal(ColorProcessor.Apply(pixels, first.Recipe), ColorProcessor.Apply(pixels, repeated.Recipe));
        Assert.Equal(untouched, pixels);
    }
    static byte[] Pixels(int seed, int count = 1024)
    {
        var random = new Random(seed); var p = new byte[count * 4];
        for (int i = 0; i < p.Length; i += 4) { for (int c = 0; c < 3; c++) p[i + c] = (byte)random.Next(25, 225); p[i + 3] = 255; }
        return p;
    }
    static byte[] Reverse(byte[] p)
    {
        var reversed = new byte[p.Length]; for (int i = 0; i < p.Length / 4; i++) Array.Copy(p, i * 4, reversed, p.Length - (i + 1) * 4, 4); return reversed;
    }
    [Fact]
    public void Bright_reference_does_not_blindly_clip_the_input_to_white()
    {
        var p = Pixels(12); var bright = Enumerable.Repeat((byte)255, p.Length).ToArray();
        var result = ReferenceMatchEngine.Fit(new(p, 32, 32), new(bright, 64, 16)); var rendered = ColorProcessor.Apply(p, result.Recipe);
        int clipped = 0; for (int i = 0; i < p.Length; i += 4) for (int c = 0; c < 3; c++) if (rendered[i + c] >= 250 || rendered[i + c] <= 5) clipped++;
        Assert.True(clipped / (p.Length / 4d * 3) < .05, "Reference introduced excessive clipping.");
        Assert.True(result.AfterDistributionRmse < result.BeforeDistributionRmse);
    }
    [Fact]
    public void Same_distribution_different_orientation_and_positions_stays_neutral()
    {
        var p = Pixels(1); var original = p.ToArray(); var reference = Reverse(p); var result = ReferenceMatchEngine.Fit(new(p, 64, 16), new(reference, 16, 64), new(Passes: 2));
        Assert.Equal(0, result.BeforeDistributionRmse); Assert.Equal(0, result.AfterDistributionRmse);
        Assert.Equal(p, ColorProcessor.Apply(p, result.Recipe)); Assert.Equal(original, p); Assert.Equal(Reverse(original), reference);
    }
    [Fact]
    public void Unpaired_known_tone_improves_distribution_and_recipe_is_editable()
    {
        var p = Pixels(3); var target = Reverse(ColorProcessor.Apply(p, new ColorRecipe(Exposure: .5, Warmth: 18, Contrast: 8, Saturation: -10)));
        var result = ReferenceMatchEngine.Fit(new(p, 64, 16), new(target, 16, 64));
        Assert.True(result.BeforeDistributionRmse > 10);
        Assert.True(result.AfterDistributionRmse < result.BeforeDistributionRmse * .35, $"Before {result.BeforeDistributionRmse}, after {result.AfterDistributionRmse}");
        Assert.Null(result.Recipe.Lut); Assert.Null(result.Recipe.Layers); Assert.Equal(0, result.Recipe.Sharpening);
        Assert.Equal(p.Length, ColorProcessor.Apply(p, result.Recipe, 64, 16).Length); result.Recipe.Validate();
    }
    [Fact]
    public void Independent_pixel_counts_and_aspect_ratios_are_accepted()
    {
        var p = Pixels(4, 1024); var reference = Pixels(5, 600); var options = new MatchOptions(Samples: 512, Passes: 2);
        var a = ReferenceMatchEngine.Fit(new(p, 64, 16), new(reference, 20, 30), options);
        var b = ReferenceMatchEngine.Fit(new(p, 64, 16), new(reference, 20, 30), options);
        Assert.Equal(a.AfterDistributionRmse, b.AfterDistributionRmse); Assert.Equal(a.Recipe.Exposure, b.Recipe.Exposure);
        Assert.True(a.AfterDistributionRmse <= a.BeforeDistributionRmse + .01); Assert.Equal(512, a.SourceSamples); Assert.Equal(512, a.ReferenceSamples);
    }
    [Fact]
    public void Transparent_photos_invalid_sizes_options_and_cancel_are_rejected()
    {
        var p = Pixels(7); var photo = new MatchPhoto(p, 32, 32);
        Assert.Throws<ArgumentException>(() => ReferenceMatchEngine.Fit(photo with { Width = 99 }, photo));
        Assert.Throws<ArgumentException>(() => ReferenceMatchEngine.Fit(photo, photo with { Pixels = new byte[p.Length] }));
        Assert.Throws<ArgumentException>(() => ReferenceMatchEngine.Fit(photo, photo, new(Normalize: false, Tone: false, Color: false)));
        Assert.Throws<OperationCanceledException>(() => ReferenceMatchEngine.Fit(photo, photo, ct: new CancellationToken(true)));
        using var cts = new CancellationTokenSource(); Assert.Throws<OperationCanceledException>(() => ReferenceMatchEngine.Fit(photo, photo, progress: new CancelProgress(cts), ct: cts.Token));
    }
    sealed class CancelProgress(CancellationTokenSource cts) : IProgress<MatchProgress> { public void Report(MatchProgress p) => cts.Cancel(); }
    [Fact]
    public void Reference_origin_retains_metric_and_legacy_paired_origin_still_loads()
    {
        var referenceOrigin = new MatchLookOrigin("Input", "Reference", "reference-quantile-rgb-v1", 2) { Metric = "Reference RGB quantile RMSE (not held-out)" };
        var recipe = new ColorRecipe("Reference") { MatchOrigin = referenceOrigin }; string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new ColorWorkspaceStore(path); store.Save(new(null, [recipe])); Assert.Equal(referenceOrigin, store.Load().Looks[0].MatchOrigin);
        }
        finally { File.Delete(path); }
        var old = System.Text.Json.JsonSerializer.Deserialize<MatchLookOrigin>("{\"InputCamera\":\"A\",\"ReferenceCamera\":\"B\",\"Engine\":\"paired-rgb-v1\",\"InitialCheckRmse\":1}")!;
        Assert.Equal("Paired check RGB RMSE", old.Metric); old.Validate();
    }
}
