namespace FotoAutomat.Core;

public static class AdjustToolProcessor
{
    public static double HueDelta(double from, double to) => (to - from + 540) % 360 - 180;
    public static (double H, double S, double L) ToHsl(double r, double g, double b)
    {
        double hi = Math.Max(r, Math.Max(g, b)), lo = Math.Min(r, Math.Min(g, b)), d = hi - lo, l = (hi + lo) / 2;
        if (d < 1e-9) return (0, 0, l);
        double h = hi == r ? (g - b) / d : hi == g ? (b - r) / d + 2 : (r - g) / d + 4;
        return ((h * 60 + 360) % 360, d / Math.Max(1e-9, 1 - Math.Abs(2 * l - 1)), l);
    }
    public static (double R, double G, double B) FromHsl(double h, double s, double l)
    {
        h = (h % 360 + 360) % 360;
        double c = (1 - Math.Abs(2 * l - 1)) * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = l - c / 2;
        var v = h < 60 ? (c, x, 0d) : h < 120 ? (x, c, 0d) : h < 180 ? (0d, c, x) : h < 240 ? (0d, x, c) : h < 300 ? (x, 0d, c) : (c, 0d, x);
        return (v.Item1 + m, v.Item2 + m, v.Item3 + m);
    }
    internal static byte[] Apply(byte[] pixels, ColorRecipe recipe, CancellationToken ct)
    {
        var ranges = (recipe.AdvancedColors ?? []).Concat(recipe.SkinColors ?? (recipe.SkinColor is { } skin ? [skin] : [])).Where(x => x.Enabled).ToArray();
        var bw = recipe.Monochrome ? recipe.BlackWhite : null;
        if (ranges.Length == 0 && bw is null) return pixels;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            if (i % 16384 == 0) ct.ThrowIfCancellationRequested();
            double r = pixels[i + 2] / 255d, g = pixels[i + 1] / 255d, b = pixels[i] / 255d;
            foreach (var range in ranges)
            {
                var (h, s, l) = ToHsl(r, g, b); double w = range.Weight(h, s);
                if (w == 0 || range.Hue == 0 && range.Saturation == 0 && range.Lightness == 0 && range.UniformHue == 0 && range.UniformSaturation == 0 && range.UniformLightness == 0) continue;
                double hn = h + w * (range.Hue + HueDelta(h, range.CenterHue) * range.UniformHue / 100);
                double sn = Math.Clamp(s + w * (s * range.Saturation / 100 + (range.CenterSaturation - s) * range.UniformSaturation / 100), 0, 1);
                double ln = Math.Clamp(l + w * (range.Lightness / 200 + (range.TargetLightness - l) * range.UniformLightness / 100), 0, 1);
                (r, g, b) = FromHsl(hn, sn, ln);
            }
            if (bw is not null)
            {
                var (h, s, _) = ToHsl(r, g, b); double shift = 0, total = 0;
                for (int n = 0; n < 6; n++) { double w = Math.Max(0, 1 - Math.Abs(HueDelta(h, n * 60)) / 60); shift += bw.Sensitivity[n] / 100 * w; total += w; }
                double l = Math.Clamp(.2126 * r + .7152 * g + .0722 * b + shift / Math.Max(1, total) * s * .5, 0, 1);
                var shadow = FromHsl(bw.ShadowHue, bw.ShadowSaturation / 100, l);
                var highlight = FromHsl(bw.HighlightHue, bw.HighlightSaturation / 100, l);
                (r, g, b) = (shadow.R * (1 - l) + highlight.R * l, shadow.G * (1 - l) + highlight.G * l, shadow.B * (1 - l) + highlight.B * l);
            }
            pixels[i + 2] = Byte(r); pixels[i + 1] = Byte(g); pixels[i] = Byte(b);
        }
        return pixels;
    }
    private static byte Byte(double x) => (byte)Math.Clamp(Math.Round(x * 255), 0, 255);
}
