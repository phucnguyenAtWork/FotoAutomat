namespace FotoAutomat.Core;

internal static class ExtendedColorProcessor
{
    public static byte[] Tone(byte[] pixels, ColorRecipe recipe, CancellationToken ct)
    {
        bool curve = recipe.CurveHighlights != 0 || recipe.CurveLights != 0 || recipe.CurveDarks != 0 || recipe.CurveShadows != 0;
        if (!curve && recipe.Calibration?.Any(v => v.Hue != 0 || v.Saturation != 0) != true) return pixels;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            if (i % 16384 == 0) ct.ThrowIfCancellationRequested();
            double r = pixels[i + 2] / 255d, g = pixels[i + 1] / 255d, b = pixels[i] / 255d;
            double l = .2126 * r + .7152 * g + .0722 * b;
            double shift = (recipe.CurveShadows * Weight(l, .125) + recipe.CurveDarks * Weight(l, .375) + recipe.CurveLights * Weight(l, .625) + recipe.CurveHighlights * Weight(l, .875)) / 100 * .2 * 4 * l * (1 - l);
            if (recipe.Calibration is { } c)
            {
                double dr = r - l, dg = g - l, db = b - l;
                // Primary opponent axes: hue rotation and chroma gain, without RAW camera profiles.
                r += dr * c[0].Saturation / 100 + (dg - db) * c[0].Hue / 300;
                g += dg * c[1].Saturation / 100 + (db - dr) * c[1].Hue / 300;
                b += db * c[2].Saturation / 100 + (dr - dg) * c[2].Hue / 300;
            }
            pixels[i + 2] = Byte(r + shift); pixels[i + 1] = Byte(g + shift); pixels[i] = Byte(b + shift);
        }
        return pixels;
    }
    private static double Weight(double x, double center) => Math.Max(0, 1 - Math.Abs(x - center) / .25);
    public static byte[] Finish(byte[] source, int width, int height, ColorRecipe r, CancellationToken ct)
    {
        // Grain và Sharpening của recipe không còn áp ở đây: ShootingMode.Compose chuyển chúng sang Refine.
        if (r.Vignette == 0 && (r.VignetteEv ?? 0) == 0) return source;
        var result = source.ToArray();
        for (int y = 0; y < height; y++)
        {
            ct.ThrowIfCancellationRequested();
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                double radius = Math.Pow((x + .5 - width / 2d) / (width / 2d), 2) + Math.Pow((y + .5 - height / 2d) / (height / 2d), 2);
                double vignette = r.Vignette / 100 * .45 * Math.Clamp((radius - .25) / 1.75, 0, 1);
                for (int c = 0; c < 3; c++)
                {
                    double value = source[i + c] / 255d;
                    double spatialRadius = r.VignetteMethod == VignetteMethod.EllipticOnCrop ? radius
                        : (Math.Pow(x + .5 - width / 2d, 2) + Math.Pow(y + .5 - height / 2d, 2)) / Math.Pow((r.VignetteMethod == VignetteMethod.CircularOnCrop ? Math.Min(width, height) : Math.Max(width, height)) / 2d, 2);
                    double evGain = r.VignetteEv is { } ev ? Math.Pow(2, ev * Math.Clamp((spatialRadius - .25) / 1.75, 0, 1)) : 1;
                    double baseValue = value + (r.VignetteEv is null ? vignette : 0);
                    double linear = baseValue <= .04045 ? baseValue / 12.92 : Math.Pow((baseValue + .055) / 1.055, 2.4);
                    double lit = linear * evGain;
                    result[i + c] = Byte(r.VignetteEv is null || evGain == 1 ? baseValue : lit <= .0031308 ? lit * 12.92 : 1.055 * Math.Pow(lit, 1 / 2.4) - .055);
                }
            }
        }
        return result;
    }
    private static byte Byte(double v) => (byte)Math.Clamp(Math.Round(v * 255), 0, 255);
}
