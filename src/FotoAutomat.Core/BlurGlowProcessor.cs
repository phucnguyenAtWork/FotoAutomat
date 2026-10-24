namespace FotoAutomat.Core;

/// <summary>Linear-light diffusion. Three sliding box passes approximate a Gaussian in O(pixels).
/// Radii are relative to the shorter image side; transparent RGB never contributes to light.</summary>
internal static class BlurGlowProcessor
{
    private static readonly float[] Linear = Enumerable.Range(0, 256).Select(i =>
    {
        double v = i / 255d;
        return (float)(v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4));
    }).ToArray();

    public static byte[] Apply(byte[] source, int width, int height, BlurGlowSettings? settings, CancellationToken ct)
    {
        if (settings is not { IsActive: true } s) return source;
        ct.ThrowIfCancellationRequested();
        var pixels = new float[source.Length];
        for (int i = 0; i < source.Length; i += 4)
        {
            if (i % 16384 == 0) ct.ThrowIfCancellationRequested();
            float a = source[i + 3] / 255f;
            for (int c = 0; c < 3; c++) pixels[i + c] = Linear[source[i + c]] * a;
            pixels[i + 3] = a;
        }
        float[]? mist = s.MistAmount > 0 ? Blur(pixels, width, height, s.MistSpread, ct) : null;
        float[]? gaussian = s.GaussianMix > 0 && s.GaussianRadius > 0 ? Blur(pixels, width, height, s.GaussianRadius, ct) : null;
        float[]? glow = null;
        if (s.GlowAmount > 0)
        {
            var highlights = new float[pixels.Length];
            double threshold = s.Threshold / 100;
            double feather = .001 + s.Softness / 100 * .4;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                if (i % 16384 == 0) ct.ThrowIfCancellationRequested();
                double l = (.2126 * pixels[i + 2] + .7152 * pixels[i + 1] + .0722 * pixels[i]) / Math.Max(1e-6, pixels[i + 3]);
                double t = Math.Clamp((l - threshold) / feather, 0, 1);
                float weight = (float)(t * t * (3 - 2 * t));
                for (int c = 0; c < 3; c++) highlights[i + c] = pixels[i + c] * weight;
            }
            glow = Blur(highlights, width, height, s.GlowRadius, ct);
        }
        var output = source.ToArray();
        for (int i = 0; i < source.Length; i += 4)
        {
            if (i % 16384 == 0) ct.ThrowIfCancellationRequested();
            if (source[i + 3] == 0) continue;
            double luma = .2126 * Linear[source[i + 2]] + .7152 * Linear[source[i + 1]] + .0722 * Linear[source[i]];
            double protect = 1 - s.PreserveBlacks / 100 * Math.Pow(1 - luma, 4);
            for (int c = 0; c < 3; c++)
            {
                double v = Linear[source[i + c]];
                if (gaussian is not null)
                    v += (gaussian[i + c] / Math.Max(1e-6, gaussian[i + 3]) - v) * s.GaussianMix / 100;
                if (mist is not null)
                {
                    double light = mist[i + c] * .55 + s.Haze / 100 * .12;
                    v += (1 - v) * light * s.MistAmount / 100 * protect;
                }
                if (glow is not null)
                {
                    double tint = c == 2 ? 1 + s.Warmth / 200 : c == 0 ? 1 - s.Warmth / 200 : 1;
                    v += (1 - v) * Math.Clamp(glow[i + c] * tint * s.GlowAmount / 100, 0, 1);
                }
                v = Math.Clamp(v, 0, 1);
                output[i + c] = (byte)Math.Clamp(Math.Round(255 * (v <= .0031308 ? 12.92 * v : 1.055 * Math.Pow(v, 1 / 2.4) - .055)), 0, 255);
            }
        }
        return output;
    }

    private static float[] Blur(float[] source, int width, int height, double amount, CancellationToken ct)
    {
        double sigma = Math.Min(width, height) * .02 * amount / 100;
        int low = (int)Math.Floor(Math.Sqrt(4 * sigma * sigma + 1));
        if (low % 2 == 0) low--;
        low = Math.Max(1, low);
        int count = Math.Clamp((int)Math.Round((12 * sigma * sigma - 3 * low * low - 12 * low - 9) / (-4d * low - 4)), 0, 3);
        var a = source.ToArray(); var b = new float[a.Length];
        for (int pass = 0; pass < 3; pass++)
        {
            int radius = ((pass < count ? low : low + 2) - 1) / 2;
            if (radius == 0) continue;
            Box(a, b, width, height, radius, true, ct);
            Box(b, a, width, height, radius, false, ct);
        }
        return a;
    }

    private static void Box(float[] source, float[] target, int width, int height, int radius, bool horizontal, CancellationToken ct)
    {
        int length = horizontal ? width : height, lines = horizontal ? height : width;
        int step = horizontal ? 4 : width * 4;
        for (int line = 0; line < lines; line++)
        {
            ct.ThrowIfCancellationRequested();
            int start = horizontal ? line * width * 4 : line * 4;
            for (int c = 0; c < 4; c++)
            {
                double sum = 0;
                for (int k = -radius; k <= radius; k++) sum += source[start + Math.Clamp(k, 0, length - 1) * step + c];
                for (int x = 0; x < length; x++)
                {
                    target[start + x * step + c] = (float)(sum / (2 * radius + 1));
                    sum += source[start + Math.Clamp(x + radius + 1, 0, length - 1) * step + c]
                         - source[start + Math.Clamp(x - radius, 0, length - 1) * step + c];
                }
            }
        }
    }
}
