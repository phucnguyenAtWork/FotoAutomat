using System.Diagnostics;
namespace FotoAutomat.Core;

public sealed record MatchRegion(double X, double Y, double Width, double Height);
public sealed record MatchPhoto(byte[] Pixels, int Width, int Height)
{
    public MatchRegion[] Regions { get; init; } = [];
}
public sealed record ReferenceMatchResult(ColorRecipe Recipe, double BeforeDistributionRmse, double AfterDistributionRmse, int SourceSamples, int ReferenceSamples, int Evaluations, long ElapsedMilliseconds)
{
    public bool UsedRegions { get; init; }
    public double? BeforeRegionRmse { get; init; }
    public double? AfterRegionRmse { get; init; }
    public int ReferenceCount { get; init; } = 1;
    public bool SceneReference { get; init; }
    public string Engine => SceneReference ? (UsedRegions ? ReferenceMatchEngine.SceneRegionEngineId : ReferenceMatchEngine.SceneEngineId)
        : UsedRegions ? ReferenceMatchEngine.RegionEngineId : ReferenceMatchEngine.EngineId;
}

/// <summary>Unpaired baseline: fit editable parameters to RGB, luma, opponent-chroma and saturation quantiles.
/// No spatial correspondence, semantic understanding or recovery of original camera settings.</summary>
public static class ReferenceMatchEngine
{
    // v4: Color Tone also fits Color Balance wheels; several references are averaged. Scores are not comparable with v2/v3.
    public const string EngineId = "reference-chroma-v4";
    public const string RegionEngineId = "reference-region-v4";
    // v5a, opt-in (MatchOptions.SceneReference): neutral-anchor colour, tone-only quantiles. Better for scene references,
    // worse when the reference shares the source's kind of content. See docs/ai-match-look/v5a-content-separation-2026-10-15.md.
    public const string SceneEngineId = "reference-anchor-v5a";
    public const string SceneRegionEngineId = "reference-region-v5a";
    public const string MetricName = "RGB/chroma quantile RMSE (not held-out)";
    /// <summary>v5b step 1 (automatic reference type): when the source shows people but no reference does, the references are
    /// scenes (landscape, architecture) whose own colours are content, so the scene engine (v5a) is used; otherwise the
    /// same-content engine (v4). Uses face regions detected on every image; a missed face falls back to v4 behaviour
    /// unless it is the reference's face that was missed.</summary>
    public static bool IsSceneReference(MatchPhoto source, IReadOnlyList<MatchPhoto> references) =>
        source.Regions.Length > 0 && references.Count > 0 && references.All(r => r.Regions.Length == 0);
    public static ReferenceMatchResult Fit(MatchPhoto source, MatchPhoto reference, MatchOptions? options = null, IProgress<MatchProgress>? progress = null, CancellationToken ct = default)
        => FitReferences(source, [reference], options, progress, ct);
    /// <summary>Several references describe one look on different content, like Capture One's multiple references.
    /// Each reference is weighted equally; Capture One's similarity weighting is not public and is not replicated.</summary>
    public static ReferenceMatchResult FitReferences(MatchPhoto source, IReadOnlyList<MatchPhoto> references, MatchOptions? options = null, IProgress<MatchProgress>? progress = null, CancellationToken ct = default)
    {
        options ??= new(); options.Validate(); ct.ThrowIfCancellationRequested(); var timer = Stopwatch.StartNew();
        if (references is null || references.Count is < 1 or > 8) throw new ArgumentException("Match Look needs 1–8 reference images.");
        byte[] input = Sample(source, options.Samples, ct), regionInput = RegionSamples(source, ct); bool scene = options.SceneReference;
        double[] Gram(byte[] pixels) => scene ? AnchorGram(pixels) : LookGram(pixels);
        var targets = references.Select(r => { var sample = Sample(r, options.Samples, ct); return (Quantiles: Quantiles(sample), LookGram: Gram(sample), Region: RegionSamples(r, ct), Count: sample.Length / 4); }).ToArray();
        bool useRegions = regionInput.Length >= 128 && targets.Any(t => t.Region.Length >= 128);
        var region = targets.Select(t => useRegions && t.Region.Length >= 128 ? Quantiles(t.Region) : null).ToArray();
        // v5a: whole-image quantiles only learn tone, since a scene's per-channel colour is mostly content. Face regions
        // pair like with like and keep the full colour weights.
        var wholeImage = scene ? ToneOnly : MetricWeights;
        double Distribution(double[][] all, double[][]? faces) => targets.Select((t, k) => faces is not null && region[k] is { } r ? .3 * Distance(all, t.Quantiles, wholeImage) + .7 * Distance(faces, r) : Distance(all, t.Quantiles, wholeImage)).Average();
        // Saturation growth is only penalized beyond what the references themselves show, so tinted looks stay reachable.
        double sourceSaturation = Quantiles(input)[6][16];
        double allowedGrowth = .05 + Math.Max(0, targets.Average(t => t.Quantiles[6][16]) - sourceSaturation) / 255;
        // v5a: non-skin chroma may grow (in /255 units) as far as the reference's neutral anchors are tinted; a vivid
        // scene does not license a vivid portrait, a toned grey wall does.
        double allowedChroma = scene ? .05 + Math.Max(0, targets.Average(t => AnchorChroma(t.LookGram)) - AnchorChroma(AnchorGram(input))) / 255 : 0;
        var skinMask = Enumerable.Range(0, input.Length / 4).Select(k => IsSkinColor(input[k * 4 + 2], input[k * 4 + 1], input[k * 4])).ToArray();
        double? RegionScore(double[][] faces) => region.Any(r => r is not null) ? region.Where(r => r is not null).Average(r => Distance(faces, r!)) : null;
        double Loss(ColorRecipe recipe)
        {
            var rendered = ColorProcessor.Apply(input, recipe, cancellationToken: ct);
            // Penalize destructive changes and newly clipped channels instead of blindly chasing a different background.
            double change = 0, hueDrift = 0, saturationGrowth = 0, skinSaturation = 0, skinHue = 0; int newlyClipped = 0, skinPixels = 0;
            for (int i = 0; i < input.Length; i += 4) for (int c = 0; c < 3; c++)
            {
                change += Math.Pow(rendered[i + c] - input[i + c], 2);
                if (rendered[i + c] >= 250 && input[i + c] < 245 || rendered[i + c] <= 5 && input[i + c] > 10) newlyClipped++;
            }
            for (int i = 0; i < input.Length; i += 4)
            {
                double chroma = Math.Max(input[i], Math.Max(input[i + 1], input[i + 2])) - Math.Min(input[i], Math.Min(input[i + 1], input[i + 2]));
                double Saturation(byte[] p) { double max = Math.Max(p[i], Math.Max(p[i + 1], p[i + 2])), min = Math.Min(p[i], Math.Min(p[i + 1], p[i + 2])); return max == 0 ? 0 : (max - min) / max; }
                double Angle(byte[] p) => Math.Atan2(Math.Sqrt(3) * (p[i + 1] - p[i]), 2d * p[i + 2] - p[i + 1] - p[i]) * 180 / Math.PI;
                double HueDelta() { double d = Math.Abs(Angle(rendered) - Angle(input)); return Math.Min(d, 360 - d); }
                if (skinMask[i / 4])
                {
                    // User choice A: a look must not make skin worse. Skin is averaged over skin pixels only, so a
                    // vivid background cannot outweigh it; the optimizer has to use per-band HSL instead of global saturation.
                    skinPixels++; skinSaturation += Math.Max(0, Saturation(rendered) - Saturation(input) - .03); skinHue += Math.Max(0, HueDelta() - 5);
                    continue;
                }
                // Elsewhere tints and split tones are allowed: saturation may grow as far as the references show,
                // and hue drift only counts on clearly coloured content.
                if (scene)
                {
                    double renderedChroma = Math.Max(rendered[i], Math.Max(rendered[i + 1], rendered[i + 2])) - Math.Min(rendered[i], Math.Min(rendered[i + 1], rendered[i + 2]));
                    saturationGrowth += Math.Max(0, (renderedChroma - chroma) / 255 - allowedChroma);
                }
                else saturationGrowth += Math.Max(0, Saturation(rendered) - Saturation(input) - allowedGrowth);
                if (chroma < 24) continue;
                hueDrift += Math.Max(0, HueDelta() - 10) * Math.Clamp((chroma - 24) / 24, 0, 1);
            }
            int channels = input.Length / 4 * 3;
            double distribution = Distribution(Quantiles(rendered), useRegions ? Quantiles(ColorProcessor.Apply(regionInput, recipe, cancellationToken: ct)) : null);
            var gram = Gram(rendered); distribution += targets.Average(t => LookGramDistance(gram, t.LookGram));
            double skinPenalty = skinPixels == 0 ? 0 : (400 * skinSaturation + 3 * skinHue) / skinPixels;
            return distribution + .2 * Math.Sqrt(change / channels) + 120d * newlyClipped / channels + (.6 * hueDrift + 100 * saturationGrowth) / (input.Length / 4) + skinPenalty;
        }
        double Global(byte[] pixels) { var q = Quantiles(pixels); return targets.Average(t => Distance(q, t.Quantiles)); }
        double before = Global(input);
        var optimized = PairedMatchEngine.Optimize(Loss, options, progress, ct, conservative: true);
        double after = Global(ColorProcessor.Apply(input, optimized.Recipe, cancellationToken: ct)); ct.ThrowIfCancellationRequested();
        return new(optimized.Recipe, before, after, input.Length / 4, targets.Sum(t => t.Count), optimized.Evaluations, timer.ElapsedMilliseconds)
        {
            UsedRegions = useRegions,
            SceneReference = scene,
            ReferenceCount = references.Count,
            BeforeRegionRmse = useRegions ? RegionScore(Quantiles(regionInput)) : null,
            AfterRegionRmse = useRegions ? RegionScore(Quantiles(ColorProcessor.Apply(regionInput, optimized.Recipe, cancellationToken: ct))) : null
        };
    }
    // Equal samples per region: large faces must not dominate smaller faces. No paired pixel correspondence.
    private static byte[] RegionSamples(MatchPhoto photo, CancellationToken ct)
    {
        if (photo.Regions is null || photo.Regions.Length > 20) throw new ArgumentException("Invalid Match regions.");
        var samples = new List<byte>();
        foreach (var r in photo.Regions)
        {
            if (!double.IsFinite(r.X + r.Y + r.Width + r.Height) || r.X < 0 || r.Y < 0 || r.Width <= 0 || r.Height <= 0 || r.X + r.Width > 1.000001 || r.Y + r.Height > 1.000001) throw new ArgumentException("Invalid Match region bounds.");
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                ct.ThrowIfCancellationRequested();
                // Central lower face ellipse reduces hair/background contamination without claiming a skin mask.
                double u = (x + .5) / 16, v = (y + .5) / 16;
                if (Math.Pow((u - .5) / .5, 2) + Math.Pow((v - .5) / .5, 2) > 1) continue;
                int px = Math.Clamp((int)((r.X + r.Width * (.2 + .6 * u)) * photo.Width), 0, photo.Width - 1);
                int py = Math.Clamp((int)((r.Y + r.Height * (.3 + .6 * v)) * photo.Height), 0, photo.Height - 1);
                int i = (py * photo.Width + px) * 4;
                if (photo.Pixels[i + 3] != 255) continue;
                for (int c = 0; c < 4; c++) samples.Add(photo.Pixels[i + c]);
            }
        }
        return samples.ToArray();
    }
    private static byte[] Sample(MatchPhoto photo, int budget, CancellationToken ct)
    {
        if (photo.Width < 1 || photo.Height < 1 || photo.Pixels is null || (long)photo.Width * photo.Height * 4 != photo.Pixels.Length) throw new ArgumentException("Invalid BGRA photo dimensions.");
        int pixels = photo.Pixels.Length / 4, count = Math.Min(budget, pixels); var sample = new List<byte>(count * 4);
        for (int k = 0; k < count; k++)
        {
            ct.ThrowIfCancellationRequested(); int i = (int)((k + .5) * pixels / count) * 4;
            if (photo.Pixels[i + 3] != 255) continue;
            for (int c = 0; c < 4; c++) sample.Add(photo.Pixels[i + c]);
        }
        if (sample.Count < 128 * 4) throw new ArgumentException("Reference Match needs at least 128 sampled opaque pixels in each photo.");
        return sample.ToArray();
    }
    // Include within-pixel opponent chroma and saturation: RGB marginal histograms alone lose channel relationships.
    // Still global statistics, not semantic look similarity.
    private static double[][] Quantiles(byte[] pixels)
    {
        var result = new double[7][]; int count = pixels.Length / 4;
        for (int c = 0; c < result.Length; c++)
        {
            var values = new double[count];
            for (int i = 0; i < count; i++)
            {
                double b = pixels[i * 4], g = pixels[i * 4 + 1], r = pixels[i * 4 + 2], max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
                values[i] = c switch { < 3 => pixels[i * 4 + c], 3 => .2126 * r + .7152 * g + .0722 * b, 4 => r - g, 5 => b - (r + g) / 2, _ => max == 0 ? 0 : 255 * (max - min) / max };
            }
            Array.Sort(values);
            result[c] = new double[33];
            for (int k = 0; k <= 32; k++)
            {
                double position = k * (count - 1) / 32d; int lo = (int)position, hi = Math.Min(lo + 1, count - 1);
                result[c][k] = values[lo] + (values[hi] - values[lo]) * (position - lo);
            }
        }
        return result;
    }
    // Look-gram: mean opponent chroma per luminance band. Marginal quantiles cannot tell a teal-shadow/orange-highlight
    // split tone from its reverse; colour conditioned on brightness can. Layout: [a(8) | b(8) | band fraction(8)].
    private const int Bands = 8;
    private static double[] LookGram(byte[] pixels)
    {
        var g = new double[Bands * 3]; int count = pixels.Length / 4;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            double b = pixels[i], gr = pixels[i + 1], r = pixels[i + 2];
            int band = Math.Min(Bands - 1, (int)((.2126 * r + .7152 * gr + .0722 * b) * Bands / 256));
            g[band] += r - gr; g[Bands + band] += b - (r + gr) / 2; g[2 * Bands + band]++;
        }
        for (int k = 0; k < Bands; k++) { double n = g[2 * Bands + k]; if (n > 0) { g[k] /= n; g[Bands + k] /= n; } g[2 * Bands + k] = n / count; }
        return g;
    }
    // Neutral-anchor look-gram (v5a): per luminance band, the mean opponent chroma of the band's anchors, pixels with
    // chroma ≤ AnchorChromaCap (clouds, white walls, grey stone, shadows). A grade tints these; saturated scene content
    // (sea, sky, yellow stone) is never an anchor. Layout: [a(8) | b(8) | anchor fraction(8) | anchor chroma(8)].
    // The cap of 80/255 was fixed before evaluation from synthetic separation (toned neutrals ~30–60, sea ~150); real dusk
    // skies or seas can fall below it.
    private const double AnchorChromaCap = 80;
    private static double[] AnchorGram(byte[] pixels)
    {
        var g = new double[Bands * 4]; int count = pixels.Length / 4;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            double b = pixels[i], gr = pixels[i + 1], r = pixels[i + 2], chroma = Math.Max(r, Math.Max(gr, b)) - Math.Min(r, Math.Min(gr, b));
            if (chroma > AnchorChromaCap) continue;
            int band = Math.Min(Bands - 1, (int)((.2126 * r + .7152 * gr + .0722 * b) * Bands / 256));
            g[band] += r - gr; g[Bands + band] += b - (r + gr) / 2; g[2 * Bands + band]++; g[3 * Bands + band] += chroma;
        }
        for (int k = 0; k < Bands; k++) { double n = g[2 * Bands + k]; if (n > 0) { g[k] /= n; g[Bands + k] /= n; g[3 * Bands + k] /= n; } g[2 * Bands + k] = n / count; }
        return g;
    }
    private static double AnchorChroma(double[] gram)
    {
        double sum = 0, n = 0; for (int k = 0; k < Bands; k++) { sum += gram[2 * Bands + k] * gram[3 * Bands + k]; n += gram[2 * Bands + k]; }
        return n > 0 ? sum / n : 0;
    }
    // Bands are weighted by how much (anchor) content both images have there; empty bands carry no evidence.
    private static double LookGramDistance(double[] a, double[] b)
    {
        double sum = 0, weights = 0;
        for (int k = 0; k < Bands; k++)
        {
            double w = Math.Min(a[2 * Bands + k], b[2 * Bands + k]); weights += w;
            sum += w * (Math.Pow(a[k] - b[k], 2) + Math.Pow(a[Bands + k] - b[Bands + k], 2));
        }
        return weights > 0 ? Math.Sqrt(sum / (2 * weights)) : 0;
    }
    // Colour-only skin key (BT.601 YCbCr; Chai & Ngan 1999 ranges, plus a chroma floor). Colour-only on purpose: the same rule can be
    // reproduced by a baked .cube, unlike a spatial face mask.
    internal static bool IsSkinColor(double r, double g, double b)
    {
        double y = .299 * r + .587 * g + .114 * b, cb = 128 - .168736 * r - .331264 * g + .5 * b, cr = 128 + .5 * r - .418688 * g - .081312 * b;
        // Chroma floor keeps warm near-neutrals (beige walls, sensor noise) out: real skin sits roughly at 45–90 /255.
        double chroma = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
        return chroma >= 30 && y is >= 30 and <= 245 && cb is >= 77 and <= 127 && cr is >= 133 and <= 173;
    }
    // Reported metric weights (unchanged since v2): R, G, B, luma, R−G, B−(R+G)/2, saturation.
    private static readonly double[] MetricWeights = [.4, .4, .4, .7, 1.4, 1.4, 1.4];
    private static readonly double[] ToneOnly = [0, 0, 0, .7, 0, 0, 0];
    private static double Distance(double[][] a, double[][] b, double[]? channelWeights = null)
    {
        double sum = 0, weights = 0; channelWeights ??= MetricWeights;
        for (int c = 0; c < a.Length; c++)
        {
            double weight = channelWeights[c]; weights += weight;
            for (int k = 0; k <= 32; k++) sum += weight * Math.Pow(a[c][k] - b[c][k], 2);
        }
        return Math.Sqrt(sum / (33 * weights));
    }
}
