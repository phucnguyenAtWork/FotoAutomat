using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;

public sealed class RefineTests
{
    const int Size = 32;
    static byte[] Image(Func<int, int, (byte B, byte G, byte R)> color)
    {
        var p = new byte[Size * Size * 4];
        for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++) { int i = (y * Size + x) * 4; var c = color(x, y); p[i] = c.B; p[i + 1] = c.G; p[i + 2] = c.R; p[i + 3] = 180; }
        return p;
    }
    [Fact]
    public void Neutral_and_inactive_parameters_do_not_change_original_or_alpha()
    {
        var p = Image((x, y) => ((byte)(x * 7), (byte)(y * 7), 120)); var original = p.ToArray();
        Assert.Equal(p, RefineProcessor.Apply(p, Size, Size, new(Radius: 3, Threshold: 20, Detail: 0, Granularity: 100, MoirePattern: 32)));
        var result = RefineProcessor.Apply(p, Size, Size, new(SharpenAmount: 100, GrainImpact: 50));
        Assert.Equal(original, p); for (int i = 3; i < p.Length; i += 4) Assert.Equal(p[i], result[i]);
    }
    [Fact]
    public void Sharpening_threshold_radius_and_halo_suppression_control_edges()
    {
        var p = Image((x, _) => { byte v = (byte)(x < 16 ? 110 : 145); return (v, v, v); });
        var sharpened = RefineProcessor.Apply(p, Size, Size, new(SharpenAmount: 100));
        Assert.True(sharpened[(16 * Size + 15) * 4] < 110); Assert.True(sharpened[(16 * Size + 16) * 4] > 145);
        Assert.Equal(p, RefineProcessor.Apply(p, Size, Size, new(SharpenAmount: 100, Threshold: 20)));
        var suppressed = RefineProcessor.Apply(p, Size, Size, new(SharpenAmount: 100, HaloSuppression: 100));
        Assert.All(suppressed.Where((_, i) => i % 4 != 3), v => Assert.InRange(v, (byte)110, (byte)145));
        Assert.NotEqual(sharpened, RefineProcessor.Apply(p, Size, Size, new(SharpenAmount: 100, Radius: 3)));
    }
    [Fact]
    public void Noise_and_moire_reduce_variations_and_detail_controls_preservation()
    {
        var p = Image((x, y) => { byte v = (byte)(120 + ((x + y) % 2 == 0 ? 8 : -8)); return (v, v, v); });
        var reduced = RefineProcessor.Apply(p, Size, Size, new(LuminanceNoise: 100, Detail: 0));
        Assert.True(Math.Abs(reduced[(15 * Size + 15) * 4] - 120) < 8);
        Assert.NotEqual(reduced, RefineProcessor.Apply(p, Size, Size, new(LuminanceNoise: 100, Detail: 100)));
        var chroma = Image((x, y) => (x % 2 == 0 ? (byte)150 : (byte)110, 128, y % 2 == 0 ? (byte)150 : (byte)110));
        Assert.NotEqual(chroma, RefineProcessor.Apply(chroma, Size, Size, new(ColorNoise: 100)));
        Assert.NotEqual(chroma, RefineProcessor.Apply(chroma, Size, Size, new(MoireAmount: 100, MoirePattern: 8)));
    }
    [Fact]
    public void Grain_is_repeatable_and_type_and_granularity_change_pattern()
    {
        var p = Image((_, _) => (128, 128, 128)); var s = new RefineSettings(GrainImpact: 70);
        var a = RefineProcessor.Apply(p, Size, Size, s); Assert.NotEqual(p, a); Assert.Equal(a, RefineProcessor.Apply(p, Size, Size, s));
        Assert.NotEqual(a, RefineProcessor.Apply(p, Size, Size, s with { GrainType = FilmGrainType.Coarse }));
        Assert.NotEqual(a, RefineProcessor.Apply(p, Size, Size, s with { Granularity = 0 }));
    }
    private static byte[] Gray(int size) => Enumerable.Range(0, size * size).SelectMany(_ => new byte[] { 128, 128, 128, 255 }).ToArray();
    [Fact]
    public void Clean_then_finish_equals_apply_and_neither_modifies_the_input()
    {
        var p = Image((x, y) => ((byte)(x * 7 % 256), (byte)(y * 5 % 256), 120)); var copy = p.ToArray();
        var s = new RefineSettings(SharpenAmount: 120, LuminanceNoise: 40, GrainImpact: 50, DustSpots: [new(.5, .5, .05)]);
        var clean = RefineProcessor.Clean(p, Size, Size, s);
        Assert.Equal(RefineProcessor.Apply(p, Size, Size, s, seed: 7), RefineProcessor.Finish(clean, Size, Size, s, 7));
        var cleanCopy = clean.ToArray(); RefineProcessor.Finish(clean, Size, Size, s with { SharpenAmount = 0 }, 7);
        Assert.Equal(copy, p); Assert.Equal(cleanCopy, clean);
    }
    [Fact]
    public void Grain_pattern_follows_photo_seed()
    {
        var s = new RefineSettings(GrainImpact: 80); var p = Gray(64);
        var a = RefineProcessor.Finish(p, 64, 64, s, 1);
        Assert.Equal(a, RefineProcessor.Finish(p, 64, 64, s, 1));
        Assert.NotEqual(a, RefineProcessor.Finish(p, 64, 64, s, 2));
        var id = Guid.NewGuid(); Assert.Equal(RefineProcessor.SeedOf(id), RefineProcessor.SeedOf(id));
    }
    [Fact]
    public void Grain_keeps_the_same_pattern_and_size_relative_to_the_image_at_any_resolution()
    {
        var s = new RefineSettings(GrainImpact: 80, GrainType: FilmGrainType.Soft, Granularity: 100);
        var small = RefineProcessor.Finish(Gray(250), 250, 250, s, 3);
        var large = RefineProcessor.Finish(Gray(500), 500, 500, s, 3);
        // Thu ảnh lớn về cỡ ảnh nhỏ (trung bình 2 × 2) rồi so tương quan với ảnh nhỏ.
        double[] a = new double[250 * 250], b = new double[250 * 250];
        for (int y = 0; y < 250; y++) for (int x = 0; x < 250; x++)
        {
            a[y * 250 + x] = small[(y * 250 + x) * 4];
            b[y * 250 + x] = (large[(2 * y * 500 + 2 * x) * 4] + large[(2 * y * 500 + 2 * x + 1) * 4] + large[((2 * y + 1) * 500 + 2 * x) * 4] + large[((2 * y + 1) * 500 + 2 * x + 1) * 4]) / 4d;
        }
        double ma = a.Average(), mb = b.Average(), cov = 0, va = 0, vb = 0;
        for (int i = 0; i < a.Length; i++) { cov += (a[i] - ma) * (b[i] - mb); va += (a[i] - ma) * (a[i] - ma); vb += (b[i] - mb) * (b[i] - mb); }
        Assert.True(cov / Math.Sqrt(va * vb) > .9, $"correlation {cov / Math.Sqrt(va * vb):0.000}");
        Assert.InRange(Math.Sqrt(vb / va), .8, 1.2); // cùng biên độ sau khi thu nhỏ
    }
    [Fact]
    public void Selected_dust_spot_is_filled_without_modifying_outside_or_transparent_pixels()
    {
        var p = Image((_, _) => (160, 160, 160)); int center = (16 * Size + 16) * 4; p[center] = p[center + 1] = p[center + 2] = 20;
        var result = RefineProcessor.Apply(p, Size, Size, new(DustSpots: [new(16.5 / Size, 16.5 / Size, .09)]));
        Assert.Equal(160, result[center]); Assert.Equal(p[0], result[0]);
        p[center + 3] = 0; Assert.Equal(p[center], RefineProcessor.Apply(p, Size, Size, new(SharpenAmount: 100, GrainImpact: 100, ColorNoise: 100, DustSpots: [new(.5, .5, .09)]))[center]);
    }
    [Fact]
    public void Fringing_only_reduces_purple_chroma_near_luminance_edges()
    {
        var p = Image((x, _) => x == 16 ? ((byte)210, (byte)100, (byte)210) : ((byte)220, (byte)220, (byte)220));
        var result = RefineProcessor.Apply(p, Size, Size, new(PurpleFringing: 100)); int i = (16 * Size + 16) * 4;
        Assert.True(result[i] - result[i + 1] < p[i] - p[i + 1]); Assert.Equal(p[0], result[0]);
    }
    [Fact]
    public void Storage_rejects_invalid_values_before_overwriting_and_cancellation_is_observed()
    {
        var p = Image((_, _) => (100, 100, 100));
        Assert.Throws<ArgumentException>(() => RefineProcessor.Apply(p, Size, Size, new(Radius: double.NaN)));
        Assert.Throws<ArgumentException>(() => RefineProcessor.Apply(p, Size, Size, new(DustSpots: [new(-1, .5)])));
        Assert.Throws<OperationCanceledException>(() => RefineProcessor.Apply(p, Size, Size, new(), new CancellationToken(true)));
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new RefineWorkspaceStore(path); var settings = new RefineSettings(SharpenAmount: 113, GrainType: FilmGrainType.Coarse, DustSpots: [new(.5, .5)]); store.Save(new("sample", settings));
            Assert.Equal(settings.SharpenAmount, store.Load().Settings.SharpenAmount); Assert.Equal(settings.DustSpots, store.Load().Settings.DustSpots);
            Assert.Throws<ArgumentException>(() => store.Save(new("other", new(MoirePattern: 0)))); Assert.Equal("sample", store.Load().SamplePath);
        }
        finally { File.Delete(path); File.Delete(path + ".tmp"); }
    }
}
