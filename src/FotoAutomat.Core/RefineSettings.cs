namespace FotoAutomat.Core;

public enum FilmGrainType { Fine, Soft, Coarse }
public sealed record DustSpot(double X, double Y, double Radius = .015)
{
    public void Validate()
    {
        if (!double.IsFinite(X) || !double.IsFinite(Y) || !double.IsFinite(Radius) || X < 0 || X > 1 || Y < 0 || Y > 1 || Radius < .002 || Radius > .1) throw new ArgumentException("Dust spot is outside the image or too large.");
    }
}
public sealed record RefineSettings(double SharpenAmount = 0, double Radius = 1.2, double Threshold = 0, double HaloSuppression = 0,
    double LuminanceNoise = 0, double ColorNoise = 0, double Detail = 50, FilmGrainType GrainType = FilmGrainType.Fine,
    double GrainImpact = 0, double Granularity = 50, double PurpleFringing = 0, double MoireAmount = 0, double MoirePattern = 8,
    DustSpot[]? DustSpots = null)
{
    public void Validate()
    {
        if (!double.IsFinite(SharpenAmount) || SharpenAmount < 0 || SharpenAmount > 500 || !double.IsFinite(Radius) || Radius < .1 || Radius > 3 || !double.IsFinite(Threshold) || Threshold < 0 || Threshold > 20 || !Enum.IsDefined(GrainType)
            || new[] { HaloSuppression, LuminanceNoise, ColorNoise, Detail, GrainImpact, Granularity, PurpleFringing, MoireAmount }.Any(v => !double.IsFinite(v) || v < 0 || v > 100)
            || !double.IsFinite(MoirePattern) || MoirePattern < 1 || MoirePattern > 32 || DustSpots?.Length > 1000) throw new ArgumentException("Refine values are outside the supported range.");
        foreach (var spot in DustSpots ?? []) spot.Validate();
    }
}

/// <summary>RGB detail processing. No RAW reconstruction or AI dust detection.
/// Clean (giảm nhiễu, moiré, viền tím, bụi) chạy trên ảnh chính; Finish (sharpen, grain) chạy cho từng đầu ra (ô in, ảnh tải về).
/// Bán kính giảm nhiễu và sharpen tính theo pixel của ảnh đang xử lý; kích thước hạt grain tính theo cạnh ngắn nên cùng cảm giác ở mọi độ phân giải.</summary>
public static class RefineProcessor
{
    /// <summary>Cạnh ngắn tham chiếu cho kích thước hạt grain: hạt có cùng tỉ lệ so với ảnh ở preview, ô in và ảnh full.</summary>
    public const double GrainReferenceShortEdge = 1000;

    /// <summary>Cả hai bước liền nhau (preview quản trị): Clean rồi Finish.</summary>
    public static byte[] Apply(byte[] source, int width, int height, RefineSettings settings, CancellationToken ct = default, int seed = 0) =>
        Finish(Clean(source, width, height, settings, ct), width, height, settings, seed, ct);

    /// <summary>Mẫu hạt riêng cho từng ảnh, cố định để render lại cho cùng kết quả.</summary>
    public static int SeedOf(Guid photoId) => BitConverter.ToInt32(photoId.ToByteArray(), 0);

    /// <summary>Giảm nhiễu, moiré, viền tím và điểm bụi (tọa độ chuẩn hóa). Không đổi ảnh nguồn.</summary>
    public static byte[] Clean(byte[] source, int width, int height, RefineSettings settings, CancellationToken ct = default)
    {
        Check(source, width, height);
        settings.Validate(); ct.ThrowIfCancellationRequested(); var result = source.ToArray();
        if (settings.LuminanceNoise != 0 || settings.ColorNoise != 0 || settings.MoireAmount != 0 || settings.PurpleFringing != 0)
        {
            int radius = settings.MoireAmount > 0 ? Math.Clamp((int)Math.Round(settings.MoirePattern / 4), 1, 8) : 2;
            double rangeVariance = 2 * Math.Pow(8 + (100 - settings.Detail) * .4, 2);
            for (int y = 0; y < height; y++)
            {
                ct.ThrowIfCancellationRequested();
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4; if (source[i + 3] == 0) continue;
                    double l = Luma(source, i), weight = 0, mb = 0, mg = 0, mr = 0;
                    for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++)
                    {
                        int j = (Math.Clamp(y + dy, 0, height - 1) * width + Math.Clamp(x + dx, 0, width - 1)) * 4;
                        if (source[j + 3] == 0) continue;
                        double d = Luma(source, j) - l;
                        double w = Math.Exp(-d * d / rangeVariance);
                        mb += source[j] * w; mg += source[j + 1] * w; mr += source[j + 2] * w; weight += w;
                    }
                    mb /= weight; mg /= weight; mr /= weight; double meanL = .0722 * mb + .7152 * mg + .2126 * mr;
                    double nl = l + (meanL - l) * settings.LuminanceNoise / 100;
                    double chromaBlend = 1 - (1 - settings.ColorNoise / 100) * (1 - settings.MoireAmount / 100);
                    double purple = Math.Clamp((Math.Min(source[i + 2], source[i]) - source[i + 1] - 8) / 50d, 0, 1) * Math.Clamp(Math.Abs(l - meanL) / 12, 0, 1) * settings.PurpleFringing / 100;
                    for (int c = 0; c < 3; c++)
                    {
                        double average = c == 0 ? mb : c == 1 ? mg : mr;
                        double chroma = (source[i + c] - l) * (1 - chromaBlend) + (average - meanL) * chromaBlend;
                        result[i + c] = Byte(nl + chroma * (1 - purple));
                    }
                }
            }
        }
        foreach (var spot in settings.DustSpots ?? [])
        {
            ct.ThrowIfCancellationRequested(); var input = result.ToArray(); double cx = spot.X * width, cy = spot.Y * height, radius = Math.Max(1, spot.Radius * Math.Min(width, height));
            double b = 0, g = 0, r = 0, count = 0;
            int x0 = Math.Max(0, (int)(cx - radius * 1.8)), x1 = Math.Min(width, (int)Math.Ceiling(cx + radius * 1.8));
            int y0 = Math.Max(0, (int)(cy - radius * 1.8)), y1 = Math.Min(height, (int)Math.Ceiling(cy + radius * 1.8));
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
            {
                double distance = Math.Sqrt(Math.Pow(x + .5 - cx, 2) + Math.Pow(y + .5 - cy, 2)) / radius; int i = (y * width + x) * 4;
                if (distance < 1.2 || distance > 1.8 || input[i + 3] == 0) continue; b += input[i]; g += input[i + 1]; r += input[i + 2]; count++;
            }
            if (count == 0) continue;
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
            {
                int i = (y * width + x) * 4; if (input[i + 3] == 0) continue;
                double distance = Math.Sqrt(Math.Pow(x + .5 - cx, 2) + Math.Pow(y + .5 - cy, 2)) / radius;
                double a = Math.Clamp((1 - distance) / .35, 0, 1); a = a * a * (3 - 2 * a);
                result[i] = Byte(input[i] * (1 - a) + b / count * a); result[i + 1] = Byte(input[i + 1] * (1 - a) + g / count * a); result[i + 2] = Byte(input[i + 2] * (1 - a) + r / count * a);
            }
        }
        return result;
    }

    /// <summary>Sharpen theo lưới pixel của đầu ra (chạy sau khi resize), rồi grain. Không đổi ảnh nguồn.</summary>
    public static byte[] Finish(byte[] source, int width, int height, RefineSettings settings, int seed = 0, CancellationToken ct = default)
    {
        Check(source, width, height);
        settings.Validate(); ct.ThrowIfCancellationRequested(); var result = source;
        if (settings.SharpenAmount > 0)
        {
            var input = result; var blurred = Gaussian(input, width, height, settings.Radius, ct); result = input.ToArray();
            for (int y = 0; y < height; y++)
            {
                ct.ThrowIfCancellationRequested();
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4; if (input[i + 3] == 0) continue;
                    if (Math.Abs(Luma(input, i) - Luma(blurred, i)) < settings.Threshold) continue;
                    for (int c = 0; c < 3; c++)
                    {
                        double value = input[i + c] + (input[i + c] - blurred[i + c]) * settings.SharpenAmount / 100;
                        int lo = 255, hi = 0;
                        for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) { int j = (Math.Clamp(y + dy, 0, height - 1) * width + Math.Clamp(x + dx, 0, width - 1)) * 4; if (input[j + 3] == 0) continue; lo = Math.Min(lo, input[j + c]); hi = Math.Max(hi, input[j + c]); }
                        result[i + c] = Byte(value + (Math.Clamp(value, lo, hi) - value) * settings.HaloSuppression / 100);
                    }
                }
            }
        }
        if (settings.GrainImpact > 0)
        {
            if (ReferenceEquals(result, source)) result = source.ToArray();
            // Ô hạt tính theo đơn vị tham chiếu; tọa độ tâm pixel quy về đơn vị đó nên mẫu hạt trùng nhau giữa các độ phân giải.
            double cell = 1 + settings.Granularity / 100 * (settings.GrainType == FilmGrainType.Coarse ? 6 : settings.GrainType == FilmGrainType.Soft ? 3 : 1);
            double scale = GrainReferenceShortEdge / Math.Min(width, height);
            for (int y = 0; y < height; y++)
            {
                ct.ThrowIfCancellationRequested();
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4; if (result[i + 3] == 0) continue;
                    double fx = (x + .5) * scale / cell, fy = (y + .5) * scale / cell; int ix = (int)fx, iy = (int)fy; double u = fx - ix, v = fy - iy;
                    double n = (Noise(ix, iy, seed) * (1 - u) + Noise(ix + 1, iy, seed) * u) * (1 - v) + (Noise(ix, iy + 1, seed) * (1 - u) + Noise(ix + 1, iy + 1, seed) * u) * v;
                    double grain = n * settings.GrainImpact / 100 * (settings.GrainType == FilmGrainType.Soft ? 18 : 30);
                    for (int c = 0; c < 3; c++) result[i + c] = Byte(result[i + c] + grain);
                }
            }
        }
        return result;
    }
    private static byte[] Gaussian(byte[] input, int width, int height, double sigma, CancellationToken ct)
    {
        int radius = (int)Math.Ceiling(sigma * 3); var kernel = Enumerable.Range(-radius, radius * 2 + 1).Select(x => Math.Exp(-x * x / (2 * sigma * sigma))).ToArray();
        var horizontal = new double[input.Length]; var output = input.ToArray();
        for (int pass = 0; pass < 2; pass++) for (int y = 0; y < height; y++)
        {
            ct.ThrowIfCancellationRequested();
            for (int x = 0; x < width; x++) for (int c = 0; c < 3; c++)
            {
                double sum = 0, weight = 0; int i = (y * width + x) * 4;
                for (int d = -radius; d <= radius; d++) { int j = (Math.Clamp(y + (pass == 1 ? d : 0), 0, height - 1) * width + Math.Clamp(x + (pass == 0 ? d : 0), 0, width - 1)) * 4; if (input[j + 3] == 0) continue; double w = kernel[d + radius]; sum += (pass == 0 ? input[j + c] : horizontal[j + c]) * w; weight += w; }
                if (pass == 0) horizontal[i + c] = weight == 0 ? input[i + c] : sum / weight; else output[i + c] = Byte(weight == 0 ? input[i + c] : sum / weight);
            }
        }
        return output;
    }
    private static double Luma(byte[] p, int i) => .0722 * p[i] + .7152 * p[i + 1] + .2126 * p[i + 2];
    private static byte Byte(double v) => (byte)Math.Clamp(Math.Round(v), 0, 255);
    private static double Noise(int x, int y, int seed) { uint h = unchecked((uint)(x * 374761393 + y * 668265263 + seed * 1442695041 + 1274126177)); h = unchecked((h ^ (h >> 13)) * 1274126177u); return (h & 65535) / 32767.5 - 1; }
    private static void Check(byte[] source, int width, int height)
    {
        if (width <= 0 || height <= 0 || (long)width * height * 4 != source.Length) throw new ArgumentException("Invalid Refine image.");
    }
}
