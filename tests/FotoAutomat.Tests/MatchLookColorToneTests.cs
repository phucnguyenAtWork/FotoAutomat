using FotoAutomat.Core;
using Xunit.Abstractions;

namespace FotoAutomat.Tests;

public sealed class MatchLookColorToneTests(ITestOutputHelper output)
{
    // Mostly neutral content across the full tonal range, so a split tone is visible in shadows and highlights.
    static byte[] Scene(int seed, int count = 1024)
    {
        var random = new Random(seed); var p = new byte[count * 4];
        for (int i = 0; i < p.Length; i += 4)
        {
            int l = random.Next(15, 240);
            for (int c = 0; c < 3; c++) p[i + c] = (byte)Math.Clamp(l + random.Next(-12, 13), 0, 255);
            p[i + 3] = 255;
        }
        return p;
    }
    static ColorRecipe SplitTone() => new("Split") { Balance = [new(), new(190, 45, 0), new(), new(35, 45, 0)] };
    static double[] Offset(ColorGrade g) => [g.Amount * Math.Cos(g.Hue * Math.PI / 180), g.Amount * Math.Sin(g.Hue * Math.PI / 180)];
    static double Cosine(double[] a, double[] b) => (a[0] * b[0] + a[1] * b[1]) / Math.Max(1e-9, Math.Sqrt((a[0] * a[0] + a[1] * a[1]) * (b[0] * b[0] + b[1] * b[1])));

    [Fact]
    public void Split_tone_reference_is_recovered_mostly_by_color_balance_wheels()
    {
        var source = Scene(1); var reference = ColorProcessor.Apply(Scene(2), SplitTone());
        var result = ReferenceMatchEngine.Fit(new(source, 32, 32), new(reference, 32, 32), new(Normalize: false, Tone: false, Color: false) { ColorTone = true });
        var balance = result.Recipe.Balance!; var expected = SplitTone().Balance!;
        output.WriteLine($"before {result.BeforeDistributionRmse:0.00} after {result.AfterDistributionRmse:0.00}; shadows {balance[1]}; highlights {balance[3]}");
        Assert.True(result.AfterDistributionRmse < result.BeforeDistributionRmse * .5, $"{result.BeforeDistributionRmse} → {result.AfterDistributionRmse}");
        Assert.True(Cosine(Offset(balance[1]), Offset(expected[1])) > .9, $"shadows {balance[1]}");
        Assert.True(Cosine(Offset(balance[3]), Offset(expected[3])) > .9, $"highlights {balance[3]}");
        Assert.Equal(ReferenceMatchEngine.EngineId, result.Engine);
    }

    [Fact]
    public void Several_references_of_one_look_are_matched_together()
    {
        var look = new ColorRecipe(Exposure: .35, Warmth: 20, Contrast: 10, Saturation: -15);
        MatchPhoto Ref(int seed) => new(ColorProcessor.Apply(Scene(seed, 600), look), 20, 30);
        var source = new MatchPhoto(Scene(3), 32, 32); var options = new MatchOptions(Passes: 3);
        var many = ReferenceMatchEngine.FitReferences(source, [Ref(4), Ref(5), Ref(6)], options);
        Assert.Equal(3, many.ReferenceCount); Assert.Equal(1800, many.ReferenceSamples);
        Assert.True(many.AfterDistributionRmse < many.BeforeDistributionRmse * .5, $"{many.BeforeDistributionRmse} → {many.AfterDistributionRmse}");
        double Warm(byte[] p) { double d = 0; for (int i = 0; i < p.Length; i += 4) d += p[i + 2] - p[i]; return d / (p.Length / 4); }
        Assert.True(Warm(ColorProcessor.Apply(source.Pixels, many.Recipe)) > Warm(source.Pixels) + 3, "a warm look must make the source warmer");
        var again = ReferenceMatchEngine.FitReferences(source, [Ref(4), Ref(5), Ref(6)], options);
        Assert.Equal(many.AfterDistributionRmse, again.AfterDistributionRmse);
        Assert.Throws<ArgumentException>(() => ReferenceMatchEngine.FitReferences(source, Array.Empty<MatchPhoto>()));
        Assert.Throws<ArgumentException>(() => ReferenceMatchEngine.FitReferences(source, Enumerable.Range(0, 9).Select(Ref).ToArray()));
    }

    [Fact]
    public void Vivid_reference_does_not_push_skin_hue_or_saturation_option_A()
    {
        // Portrait-like source: half skin tones, half neutral background. Reference: a vivid, saturated scene.
        var random = new Random(7); var source = new byte[1024 * 4]; var skin = new bool[1024];
        for (int k = 0; k < 1024; k++)
        {
            int i = k * 4; skin[k] = k % 2 == 0;
            if (skin[k]) { int l = random.Next(-25, 26); source[i + 2] = (byte)(205 + l); source[i + 1] = (byte)(150 + l); source[i] = (byte)(120 + l); }
            else { int l = random.Next(30, 220); source[i] = source[i + 1] = source[i + 2] = (byte)l; }
            source[i + 3] = 255;
        }
        var vivid = new byte[1024 * 4];
        for (int i = 0; i < vivid.Length; i += 4) { int hue = random.Next(6); for (int c = 0; c < 3; c++) vivid[i + c] = (byte)((hue + c) % 3 == 0 ? random.Next(200, 256) : random.Next(0, 60)); vivid[i + 3] = 255; }
        var result = ReferenceMatchEngine.Fit(new(source, 32, 32), new(vivid, 32, 32));
        var rendered = ColorProcessor.Apply(source, result.Recipe);
        double Sat(byte[] p, int i) { double max = Math.Max(p[i], Math.Max(p[i + 1], p[i + 2])), min = Math.Min(p[i], Math.Min(p[i + 1], p[i + 2])); return max == 0 ? 0 : (max - min) / max; }
        double Hue(byte[] p, int i) => Math.Atan2(Math.Sqrt(3) * (p[i + 1] - p[i]), 2d * p[i + 2] - p[i + 1] - p[i]) * 180 / Math.PI;
        double satGrowth = 0, hueShift = 0; int n = 0;
        for (int k = 0; k < 1024; k++)
        {
            if (!skin[k]) continue; int i = k * 4; n++;
            satGrowth += Sat(rendered, i) - Sat(source, i);
            double d = Math.Abs(Hue(rendered, i) - Hue(source, i)); hueShift += Math.Min(d, 360 - d);
        }
        output.WriteLine($"skin saturation growth {satGrowth / n:0.000}, hue shift {hueShift / n:0.0}°; recipe sat {result.Recipe.Saturation}, vib {result.Recipe.Vibrance}");
        Assert.True(satGrowth / n < .06, $"skin saturation grew {satGrowth / n:0.000}");
        Assert.True(hueShift / n < 8, $"skin hue shifted {hueShift / n:0.0}°");
    }

    // Seascape reference: 70% saturated sea/sky blue (content), 30% clouds/stone (neutral anchors).
    static byte[] Seascape(int seed)
    {
        var random = new Random(seed); var p = new byte[1024 * 4];
        for (int i = 0; i < p.Length; i += 4)
        {
            if (random.NextDouble() < .7) { int l = random.Next(-30, 31); p[i + 2] = (byte)(40 + l / 2); p[i + 1] = (byte)(110 + l); p[i] = (byte)(190 + l); }
            else { int l = random.Next(40, 235); for (int c = 0; c < 3; c++) p[i + c] = (byte)Math.Clamp(l + random.Next(-4, 5), 0, 255); }
            p[i + 3] = 255;
        }
        return p;
    }
    static double NeutralShift(byte[] before, byte[] after, Func<byte[], int, double> axis)
    {
        double sum = 0; int n = 0;
        for (int i = 0; i < before.Length; i += 4)
        {
            int max = Math.Max(before[i], Math.Max(before[i + 1], before[i + 2])), min = Math.Min(before[i], Math.Min(before[i + 1], before[i + 2]));
            if (max - min > 20) continue; sum += axis(after, i) - axis(before, i); n++;
        }
        return sum / n;
    }

    // v4 (default) treats a seascape's own blue as the look: ungraded sea pushes portrait neutrals about +92 towards blue.
    // v5a (MatchOptions.SceneReference, opt-in) learns colour only from neutral anchors. It fixes this case but is worse
    // when the reference shares the source's kind of content, so it stays opt-in until v5b can tell the cases apart.
    [Fact]
    public void Scene_reference_mode_does_not_transfer_blue_content_but_transfers_its_grade()
    {
        var source = Scene(11); var photo = new MatchPhoto(source, 32, 32); var scene = new MatchOptions { SceneReference = true };
        double Blue(byte[] p, int i) => p[i] - p[i + 2];   // B − R on BGRA
        var plain = ReferenceMatchEngine.Fit(photo, new(Seascape(12), 32, 32), scene);
        double plainShift = NeutralShift(source, ColorProcessor.Apply(source, plain.Recipe), Blue);
        var warm = ReferenceMatchEngine.Fit(photo, new(ColorProcessor.Apply(Seascape(12), new ColorRecipe(Warmth: 35)), 32, 32), scene);
        double warmShift = NeutralShift(source, ColorProcessor.Apply(source, warm.Recipe), Blue);
        output.WriteLine($"v5a ungraded sea: neutral B−R shift {plainShift:0.0}; warm-graded sea: {warmShift:0.0}");
        Assert.Equal(ReferenceMatchEngine.SceneEngineId, plain.Engine);
        Assert.True(Math.Abs(plainShift) < 4, $"ungraded blue content pushed the portrait {plainShift:0.0} towards blue");
        Assert.True(warmShift < plainShift - 4, $"warm grade not transferred: {warmShift:0.0} vs {plainShift:0.0}");
    }

    [Fact]
    public void Default_mode_is_v4_and_still_has_the_known_scene_content_weakness()
    {
        var source = Scene(11); var result = ReferenceMatchEngine.Fit(new(source, 32, 32), new(Seascape(12), 32, 32));
        double shift = NeutralShift(source, ColorProcessor.Apply(source, result.Recipe), (p, i) => p[i] - p[i + 2]);
        Assert.Equal(ReferenceMatchEngine.EngineId, result.Engine);
        // Documents the limitation; when v5b fixes it, change this test deliberately rather than loosening it.
        Assert.True(shift > 20, $"v4 behaviour changed: shift {shift:0.0}");
    }

    [Fact]
    public void Auto_reference_type_treats_faceless_references_on_a_portrait_as_scenes()
    {
        MatchRegion[] face = [new(.3, .2, .3, .4)];
        var portrait = new MatchPhoto(Scene(11), 32, 32) { Regions = face }; var faceless = new MatchPhoto(Scene(11), 32, 32);
        var sea = new MatchPhoto(Seascape(12), 32, 32); var otherPortrait = new MatchPhoto(Scene(13), 32, 32) { Regions = face };
        Assert.True(ReferenceMatchEngine.IsSceneReference(portrait, [sea]));
        Assert.True(ReferenceMatchEngine.IsSceneReference(portrait, [sea, new MatchPhoto(Seascape(14), 32, 32)]));
        Assert.False(ReferenceMatchEngine.IsSceneReference(portrait, [sea, otherPortrait]));   // any portrait reference → same content
        Assert.False(ReferenceMatchEngine.IsSceneReference(faceless, [sea]));                  // landscape → landscape stays v4
        Assert.False(ReferenceMatchEngine.IsSceneReference(portrait, []));
        // End to end: the auto decision on a portrait + ungraded sea gives the scene engine and no blue cast.
        var result = ReferenceMatchEngine.Fit(faceless, sea, new MatchOptions { SceneReference = ReferenceMatchEngine.IsSceneReference(portrait, [sea]) });
        Assert.Equal(ReferenceMatchEngine.SceneEngineId, result.Engine);
        Assert.True(Math.Abs(NeutralShift(Scene(11), ColorProcessor.Apply(Scene(11), result.Recipe), (p, i) => p[i] - p[i + 2])) < 4);
    }

    [Fact]
    public void Impact_scales_color_balance_and_matched_recipe_bakes_to_cube()
    {
        var recipe = SplitTone();
        var half = MatchRecipeComposer.Compose(recipe, 50);
        Assert.Equal(22.5, half.Balance![1].Amount, 6); Assert.Equal(190, half.Balance[1].Hue);
        Assert.Equal(new byte[] { 90, 90, 90, 255 }, ColorProcessor.Apply([90, 90, 90, 255], MatchRecipeComposer.Compose(recipe, 0)));
        var lut = RecipeLutBaker.Bake(half, 17);
        Assert.Empty(lut.Excluded); Assert.Equal(17, lut.Lut.Size);
    }
}
