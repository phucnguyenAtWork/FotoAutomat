namespace FotoAutomat.Core;

internal static class AdvancedColorProcessor
{
    private static readonly double[] HueCenters = [0, 30, 60, 120, 180, 240, 270, 300];
    public static byte[] Apply(byte[] source, ColorRecipe recipe, int width, int height, CancellationToken ct)
    {
        var result = BeforeColorEditor(source, recipe, width, height, ct);
        result = AdjustToolProcessor.Apply(result, recipe, ct);
        if (recipe.Clarity != 0 || recipe.Structure != 0 || recipe.Dehaze != 0) result = Spatial(result, width, height, recipe, ct);
        result = BlurGlowProcessor.Apply(result, width, height, recipe.BlurGlow, ct);
        result = ExtendedColorProcessor.Finish(result, width, height, recipe, ct);
        foreach (var layer in recipe.Layers ?? [])
        {
            ct.ThrowIfCancellationRequested(); if (!layer.Enabled || layer.Opacity == 0) continue;
            var adjusted = ColorProcessor.Apply(result, layer.Recipe, width, height, ct);
            var mask = layer.Mask.Rasterize(width, height, ct);
            for (int i = 0; i < mask.Length; i++)
            {
                if (i % 4096 == 0) ct.ThrowIfCancellationRequested();
                double alpha = mask[i] * layer.Opacity;
                for (int c = 0; c < 3; c++) result[i * 4 + c] = (byte)Math.Clamp(Math.Round(result[i * 4 + c] * (1 - alpha) + adjusted[i * 4 + c] * alpha), 0, 255);
            }
        }
        return result;
    }

    /// <summary>Tone, Levels, Curves, HSL and Color Balance: the stages that run before the Color Editor ranges.</summary>
    public static byte[] BeforeColorEditor(byte[] source, ColorRecipe recipe, int width, int height, CancellationToken ct)
    {
        if ((long)width * height * 4 != source.Length || width <= 0 || height <= 0) throw new ArgumentException("Kích thước ảnh không hợp lệ.");
        var result = source;
        result = ExtendedColorProcessor.Tone(result, recipe, ct);
        if (recipe.Levels is not null || recipe.Curves is not null || recipe.Hsl is not null || recipe.Balance is not null)
        {
            var tables = Enumerable.Range(0, 3).Select(c => Enumerable.Range(0, 256).Select(v =>
            {
                double x = v / 255d;
                if (recipe.Levels is { } levels) { x = Level(x, levels[0]); x = Level(x, levels[c + 1]); }
                if (recipe.Curves is { } curves) { x = curves[0].Evaluate(x); x = curves[c + 2].Evaluate(x); }
                return x;
            }).ToArray()).ToArray();
            bool hslActive = recipe.Hsl?.Any(v => v.Hue != 0 || v.Saturation != 0 || v.Lightness != 0) == true;
            // Neutral wheels change nothing; skip the per-pixel work so matched presets stay cheap in live view.
            bool balanceActive = recipe.Balance?.Any(v => v.Amount != 0 || v.Lightness != 0) == true;
            for (int i = 0; i < result.Length; i += 4)
            {
                if (i % 16384 == 0) ct.ThrowIfCancellationRequested();
                double r = tables[0][result[i + 2]], g = tables[1][result[i + 1]], b = tables[2][result[i]];
                if (recipe.Curves is { } curves)
                {
                    double luma = .2126 * r + .7152 * g + .0722 * b, target = curves[1].Evaluate(luma);
                    r += target - luma; g += target - luma; b += target - luma;
                }
                if (hslActive)
                {
                    var (h, s, l) = ToHsl(r, g, b); double dh = 0, ds = 0, dl = 0;
                    double total = 0;
                    for (int n = 0; n < 8; n++)
                    {
                        double distance = Math.Abs(h - HueCenters[n]); distance = Math.Min(distance, 360 - distance);
                        double weight = Math.Max(0, 1 - distance / 60); total += weight;
                        dh += recipe.Hsl![n].Hue * .6 * weight; ds += recipe.Hsl[n].Saturation / 100 * weight; dl += recipe.Hsl[n].Lightness / 100 * .5 * weight;
                    }
                    if (total > 0) (r, g, b) = FromHsl((h + dh / total + 360) % 360, Math.Clamp(s * (1 + ds / total), 0, 1), Math.Clamp(l + dl / total * s, 0, 1));
                }
                if (balanceActive && recipe.Balance is { } grades)
                {
                    double l = Math.Clamp(.2126 * r + .7152 * g + .0722 * b, 0, 1);
                    for (int n = 0; n < 4; n++)
                    {
                        var color = FromHsl(grades[n].Hue, 1, .5);
                        double biased = Math.Clamp(l + recipe.GradingBalance / 200, 0, 1), power = 1 + (100 - recipe.GradingBlending) / 50;
                        double weight = n switch { 0 => 1, 1 => Math.Pow(1 - biased, power), 2 => Math.Pow(4 * biased * (1 - biased), power / 2), _ => Math.Pow(biased, power) };
                        double scale = grades[n].Amount / 100 * .25 * weight;
                        double lift = grades[n].Lightness / 100 * .2 * weight;
                        r += (color.R - .5) * scale + lift; g += (color.G - .5) * scale + lift; b += (color.B - .5) * scale + lift;
                    }
                }
                result[i + 2] = Byte(r); result[i + 1] = Byte(g); result[i] = Byte(b);
            }
        }
        return result;
    }
    private static double Level(double x, LevelsAdjustment v) => (v.OutputBlack + (v.OutputWhite - v.OutputBlack) * Math.Pow(Math.Clamp((x * 255 - v.InputBlack) / (v.InputWhite - v.InputBlack), 0, 1), 1 / v.Gamma)) / 255;
    private static byte[] Spatial(byte[] source, int width, int height, ColorRecipe recipe, CancellationToken ct)
    {
        var integral = new double[checked((width + 1) * (height + 1))];
        for (int y = 0; y < height; y++)
        {
            ct.ThrowIfCancellationRequested(); double row = 0;
            for (int x = 0; x < width; x++) { int i = (y * width + x) * 4; row += (.2126 * source[i + 2] + .7152 * source[i + 1] + .0722 * source[i]) / 255; integral[(y + 1) * (width + 1) + x + 1] = integral[y * (width + 1) + x + 1] + row; }
        }
        double Mean(int x, int y, int radius)
        {
            int x0 = Math.Max(0, x - radius), x1 = Math.Min(width, x + radius + 1), y0 = Math.Max(0, y - radius), y1 = Math.Min(height, y + radius + 1), stride = width + 1;
            return (integral[y1 * stride + x1] - integral[y0 * stride + x1] - integral[y1 * stride + x0] + integral[y0 * stride + x0]) / ((x1 - x0) * (y1 - y0));
        }
        var output = source.ToArray(); int large = Math.Max(2, Math.Min(width, height) / 30), small = Math.Max(1, Math.Min(width, height) / 250);
        for (int y = 0; y < height; y++)
        {
            ct.ThrowIfCancellationRequested();
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                double luma = (.2126 * source[i + 2] + .7152 * source[i + 1] + .0722 * source[i]) / 255, local = Mean(x, y, large);
                double clarityWeight = recipe.ClarityMethod switch { ClarityMethod.Natural => 4 * luma * (1 - luma), ClarityMethod.Punch => 1.7, ClarityMethod.Neutral => .8, _ => 1 };
                double detail = recipe.Clarity / 100 * (luma - local) * 1.5 * clarityWeight + recipe.Structure / 100 * (luma - Mean(x, y, small));
                double minimum = Math.Min(source[i], Math.Min(source[i + 1], source[i + 2])) / 255d;
                double veil = Math.Min(.7, minimum * .65 + local * .15), amount = recipe.Dehaze / 100;
                for (int c = 0; c < 3; c++)
                {
                    double v = source[i + c] / 255d;
                    v = amount >= 0 ? (v - veil * amount * .8) / Math.Max(.3, 1 - veil * amount) : v * (1 + amount * .4) - amount * .4;
                    if (recipe.DehazeShadowMode == ShadowToneMode.Manual && amount != 0)
                    {
                        var tone = AdjustToolProcessor.FromHsl(recipe.DehazeShadowHue, recipe.DehazeShadowSaturation / 100, .5);
                        double channel = c == 2 ? tone.R : c == 1 ? tone.G : tone.B;
                        v += (channel - .5) * Math.Abs(amount) * (1 - luma) * .25;
                    }
                    output[i + c] = Byte(v + detail);
                }
            }
        }
        return output;
    }
    private static (double H, double S, double L) ToHsl(double r, double g, double b)
    {
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), d = max - min, l = (max + min) / 2;
        if (d < 1e-9) return (0, 0, l);
        double h = max == r ? (g - b) / d : max == g ? (b - r) / d + 2 : (r - g) / d + 4;
        return ((h * 60 + 360) % 360, d / Math.Max(1e-9, 1 - Math.Abs(2 * l - 1)), l);
    }
    private static (double R, double G, double B) FromHsl(double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = l - c / 2;
        var v = h < 60 ? (c, x, 0d) : h < 120 ? (x, c, 0d) : h < 180 ? (0d, c, x) : h < 240 ? (0d, x, c) : h < 300 ? (x, 0d, c) : (c, 0d, x);
        return (v.Item1 + m, v.Item2 + m, v.Item3 + m);
    }
    private static byte Byte(double v) => (byte)Math.Clamp(Math.Round(v * 255), 0, 255);
}
