namespace FotoAutomat.Core;

public sealed record RetouchSettings(double Impact = 100, double Blemishes = 0, double DarkCircles = 0, double EvenSkin = 0, double Texture = 0,
    double Contouring = 0, double TeethBrightness = 0, double TeethDesaturation = 0, double LeftIris = 0, double RightIris = 0, double LeftSclera = 0, double RightSclera = 0,
    bool BlemishNeck = false, bool SkinNeck = false)
{
    public void Validate()
    {
        if (new[] { Impact, Blemishes, DarkCircles, EvenSkin, Contouring, TeethBrightness, TeethDesaturation }.Any(v => !double.IsFinite(v) || v < 0 || v > 100) || !double.IsFinite(Texture) || Math.Abs(Texture) > 100) throw new ArgumentException("Retouch values must be between 0 and 100; Texture between −100 and 100.");
        // UI uses -30..30; retain older saved positive eye settings up to 100 unchanged.
        if (new[] { LeftIris, RightIris, LeftSclera, RightSclera }.Any(v => !double.IsFinite(v) || v < -30 || v > 100)) throw new ArgumentException("Eye settings are out of range.");
    }
}
public sealed record FaceRetouch(double X, double Y, double Width, double Height, RetouchSettings Settings,
    double LeftEyeX = .32, double LeftEyeY = .4, double RightEyeX = .68, double RightEyeY = .4, double MouthX = .5, double MouthY = .74, PhotoMask? Protection = null)
{
    public void Validate()
    {
        if (new[] { X, Y, Width, Height, LeftEyeX, LeftEyeY, RightEyeX, RightEyeY, MouthX, MouthY }.Any(v => !double.IsFinite(v)) || X < 0 || Y < 0 || Width < .01 || Height < .01 || X + Width > 1.000001 || Y + Height > 1.000001 || new[] { LeftEyeX, LeftEyeY, RightEyeX, RightEyeY, MouthX, MouthY }.Any(v => v < 0 || v > 1)) throw new ArgumentException("Face bounds and feature points are invalid.");
        Settings.Validate(); Protection?.Validate();
    }
}

/// <summary>Local, non-destructive RGB retouch within normalized face/feature regions. Not an AI segmentation model.
/// Mặt chồng nhau không cộng hiệu ứng: mỗi pixel lấy kết quả của mặt làm pixel đó thay đổi nhiều nhất.</summary>
public static class RetouchProcessor
{
    public const int MaxFaces = 20;
    /// <summary>Mặt nhận diện hẹp hơn tỉ lệ này của cạnh ngắn bị bỏ qua. Chưa đo trên ảnh booth thật; chỉnh sau khi đo.</summary>
    public const double MinFaceFraction = .04;
    // Guided filter: vùng có phương sai độ sáng lớn hơn nhiều so với eps (cạnh mắt, môi, hàm) được giữ nét.
    private const double GuidedEpsilon = 28 * 28;

    private sealed record Target(double X, double Y, double W, double H, RetouchSettings S, PhotoMask? Protection,
        double LeftEyeX, double LeftEyeY, double RightEyeX, double RightEyeY, double MouthX, double MouthY,
        (double U, double V)[]? Lips, bool Features, bool Teeth, bool Neck);

    /// <summary>Mặt người vận hành đã xác nhận (ảnh mẫu): mọi công cụ, mỗi mặt một bộ thông số.</summary>
    public static byte[] Apply(byte[] source, int width, int height, FaceRetouch[] faces, CancellationToken ct = default)
    {
        Check(source, width, height);
        foreach (var face in faces) face.Validate();
        var targets = faces.OrderByDescending(f => f.Width * f.Height).Take(MaxFaces).Select(f => new Target(f.X, f.Y, f.Width, f.Height, f.Settings, f.Protection,
            f.LeftEyeX, f.LeftEyeY, f.RightEyeX, f.RightEyeY, f.MouthX, f.MouthY, null, true, true, true));
        return Render(source, width, height, [.. targets], ct);
    }

    /// <summary>Mặt nhận diện tự động trên ảnh khách: một bộ thông số cho mọi mặt, công cụ theo mức nhận diện (Plan).</summary>
    public static byte[] Apply(byte[] source, int width, int height, IReadOnlyList<DetectedFace> faces, RetouchSettings settings, CancellationToken ct = default)
    {
        Check(source, width, height);
        settings.Validate();
        var targets = new List<Target>();
        foreach (var p in Plan(faces, width, height).Where(p => p.Skipped is null))
        {
            var f = Clamp(faces[p.Index])!;
            (double U, double V) Rel(FacePoint point) => ((point.X - f.X) / f.Width, (point.Y - f.Y) / f.Height);
            var (lx, ly) = p.Features ? Rel(f.LeftEye!) : (0, 0);
            var (rx, ry) = p.Features ? Rel(f.RightEye!) : (0, 0);
            var (mx, my) = p.Features ? Rel(f.Mouth!) : (0, 0);
            targets.Add(new(f.X, f.Y, f.Width, f.Height, settings, null, lx, ly, rx, ry, mx, my, p.Teeth ? [.. f.InnerLips!.Select(Rel)] : null, p.Features, p.Teeth, false));
        }
        return Render(source, width, height, [.. targets], ct);
    }

    /// <summary>Công cụ được chạy cho từng mặt. Chỉ khung mặt: da trong elip mặt. Landmark hợp lệ: thêm mắt, quầng thâm, contour.
    /// Viền môi trong (mesh): thêm răng. Vùng cổ cần mask da/áo nên không chạy trên ảnh khách.</summary>
    public static RetouchFacePlan[] Plan(IReadOnlyList<DetectedFace> faces, int width, int height)
    {
        var result = new RetouchFacePlan[faces.Count];
        var usable = new List<(int Index, double Area)>();
        for (int i = 0; i < faces.Count; i++)
        {
            var f = Clamp(faces[i]);
            if (f is null) result[i] = new(i, false, false, false, "Khung mặt không hợp lệ");
            else if (f.Width * width < MinFaceFraction * Math.Min(width, height)) result[i] = new(i, false, false, false, "Mặt quá nhỏ");
            else usable.Add((i, f.Width * f.Height));
        }
        var kept = usable.OrderByDescending(u => u.Area).Take(MaxFaces).Select(u => u.Index).ToHashSet();
        foreach (var (i, _) in usable)
        {
            if (!kept.Contains(i)) { result[i] = new(i, false, false, false, $"Quá {MaxFaces} mặt"); continue; }
            var f = Clamp(faces[i])!;
            bool features = f.Evidence != FaceEvidence.Box && ValidLandmarks(f, width, height);
            result[i] = new(i, true, features, features && f.Evidence == FaceEvidence.Mesh);
        }
        return result;
    }

    // Khung cắt theo mép ảnh; null nếu không còn diện tích hoặc số không hợp lệ.
    private static DetectedFace? Clamp(DetectedFace f)
    {
        if (new[] { f.X, f.Y, f.Width, f.Height }.Any(v => !double.IsFinite(v))) return null;
        double x0 = Math.Clamp(f.X, 0, 1), y0 = Math.Clamp(f.Y, 0, 1), x1 = Math.Clamp(f.X + f.Width, 0, 1), y1 = Math.Clamp(f.Y + f.Height, 0, 1);
        return x1 - x0 < .005 || y1 - y0 < .005 ? null : f with { X = x0, Y = y0, Width = x1 - x0, Height = y1 - y0 };
    }

    // Mắt trái/phải theo ảnh (trái ảnh trước). Hai mắt cách nhau 25–75% bề rộng mặt, nghiêng tối đa 25°, miệng dưới mắt, mọi điểm trong khung (nới 10%).
    private static bool ValidLandmarks(DetectedFace f, int width, int height)
    {
        FacePoint[] points = [f.LeftEye!, f.RightEye!, f.Mouth!, .. f.InnerLips ?? []];
        if (points.Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y) || p.X < f.X - f.Width * .1 || p.X > f.X + f.Width * 1.1 || p.Y < f.Y - f.Height * .1 || p.Y > f.Y + f.Height * 1.1)) return false;
        double dx = (f.RightEye!.X - f.LeftEye!.X) * width, dy = (f.RightEye.Y - f.LeftEye.Y) * height, faceWidth = f.Width * width;
        double distance = Math.Sqrt(dx * dx + dy * dy), roll = Math.Atan2(Math.Abs(dy), dx) * 180 / Math.PI;
        return dx > 0 && distance >= faceWidth * .25 && distance <= faceWidth * .75 && roll <= 25 && f.Mouth!.Y > Math.Max(f.LeftEye.Y, f.RightEye.Y);
    }

    private static void Check(byte[] source, int width, int height)
    {
        if (width <= 0 || height <= 0 || (long)width * height * 4 != source.Length) throw new ArgumentException("Retouch image is invalid.");
    }

    private static byte[] Render(byte[] source, int width, int height, Target[] targets, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var output = source.ToArray();
        var best = new float[width * height];
        Span<byte> result = stackalloc byte[3];
        foreach (var face in targets)
        {
            ct.ThrowIfCancellationRequested(); var s = face.S;
            if (s == new RetouchSettings()) continue;
            var protectedPixels = face.Protection?.Rasterize(width, height, ct);
            bool neck = face.Neck && (s.BlemishNeck || s.SkinNeck);
            int x0 = Math.Max(0, (int)(face.X * width)), x1 = Math.Min(width, (int)Math.Ceiling((face.X + face.W) * width));
            int y0 = Math.Max(0, (int)(face.Y * height)), y1 = Math.Min(height, (int)Math.Ceiling((face.Y + face.H * (neck ? 1.5 : 1)) * height));
            if (x1 <= x0 || y1 <= y0) continue;
            // Bán kính làm mịn theo bề rộng mặt, nên cùng cảm giác ở ảnh preview và ảnh full độ phân giải.
            int radius = Math.Clamp((int)Math.Round(face.W * width / 60), 1, 64);
            var mean = GuidedMean(source, width, x0, y0, x1, y1, radius, ct);
            int regionWidth = x1 - x0;
            for (int y = y0; y < y1; y++)
            {
                ct.ThrowIfCancellationRequested();
                for (int x = x0; x < x1; x++)
                {
                    int i = (y * width + x) * 4; if (source[i + 3] == 0) continue;
                    double protection = 1 - (protectedPixels?[y * width + x] ?? 0); if (protection <= 0) continue;
                    double u = ((x + .5) / width - face.X) / face.W, v = ((y + .5) / height - face.Y) / face.H;
                    double r = source[i + 2], g = source[i + 1], b = source[i], l = .2126 * r + .7152 * g + .0722 * b;
                    double skin = Skin(r, g, b) * Ellipse(u, v, .5, .5, .47, .51);
                    double neckMask = neck ? Skin(r, g, b) * Ellipse(u, v, .5, 1.12, .27, .37) : 0;
                    double left = face.Features ? Ellipse(u, v, face.LeftEyeX, face.LeftEyeY, .13, .065) : 0;
                    double right = face.Features ? Ellipse(u, v, face.RightEyeX, face.RightEyeY, .13, .065) : 0;
                    double mouth = face.Features ? Ellipse(u, v, face.MouthX, face.MouthY, .2, .08) : 0;
                    skin *= (1 - left) * (1 - right) * (1 - mouth);
                    int m = ((y - y0) * regionWidth + x - x0) * 3;
                    double mb = mean[m], mg = mean[m + 1], mr = mean[m + 2];
                    double skinAmount = Math.Clamp(skin + (s.SkinNeck ? neckMask : 0), 0, 1) * s.Impact / 100;
                    double blemishAmount = Math.Clamp(skin + (s.BlemishNeck ? neckMask : 0), 0, 1) * s.Impact / 100;
                    double meanL = .2126 * mr + .7152 * mg + .0722 * mb;
                    double underEyes = face.Features ? Math.Max(Ellipse(u, v, face.LeftEyeX, face.LeftEyeY + .09, .15, .07), Ellipse(u, v, face.RightEyeX, face.RightEyeY + .09, .15, .07)) : 0;
                    double contour = face.Features ? (Ellipse(u, v, .5, .55, .14, .35) - .6 * Math.Max(Ellipse(u, v, .15, .62, .16, .23), Ellipse(u, v, .85, .62, .16, .23))) * s.Contouring / 100 * 14 * skinAmount : 0;
                    double chroma = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
                    double teethArea = !face.Teeth ? 0 : face.Lips is { } lips ? (Inside(lips, u, v) ? 1 : 0) : mouth;
                    double teeth = teethArea * Ramp(l, 105, 190) * (1 - Ramp(chroma, 25, 65));
                    double outlier = Ramp(Math.Abs(l - meanL), 4, 20);
                    double iris = left * s.LeftIris / 100 + right * s.RightIris / 100;
                    double sclera = left * s.LeftSclera / 100 + right * s.RightSclera / 100;
                    double change = 0;
                    for (int c = 0; c < 3; c++)
                    {
                        double value = source[i + c], average = c == 0 ? mb : c == 1 ? mg : mr, detail = value - average;
                        value -= detail * s.EvenSkin / 100 * .65 * skinAmount;
                        value += detail * s.Texture / 100 * .4 * skinAmount;
                        value += (average - value) * s.Blemishes / 100 * .75 * outlier * blemishAmount;
                        value += Math.Max(0, meanL - l + 6) * s.DarkCircles / 100 * .5 * underEyes * skinAmount + contour;
                        value += (l - value) * s.TeethDesaturation / 100 * teeth + (255 - value) * .25 * s.TeethBrightness / 100 * teeth;
                        value += iris * (1 - Ramp(l, 50, 130)) * 15;
                        value += sclera * Ramp(l, 105, 185) * (1 - Ramp(chroma, 30, 70)) * (255 - value) * .25;
                        result[c] = (byte)Math.Clamp(Math.Round(source[i + c] * (1 - protection) + value * protection), 0, 255);
                        change += Math.Abs(result[c] - source[i + c]);
                    }
                    int p = y * width + x;
                    if (change <= best[p]) continue;
                    best[p] = (float)change; output[i] = result[0]; output[i + 1] = result[1]; output[i + 2] = result[2];
                }
            }
        }
        return output;
    }

    // Guided filter (He và cộng sự) dẫn bằng độ sáng: trung bình giữ cạnh, chi phí không phụ thuộc bán kính. Trả về BGR theo vùng [x0, x1) × [y0, y1).
    private static double[] GuidedMean(byte[] source, int width, int x0, int y0, int x1, int y1, int radius, CancellationToken ct)
    {
        int height = source.Length / 4 / width;
        int ex0 = Math.Max(0, x0 - radius), ey0 = Math.Max(0, y0 - radius), ex1 = Math.Min(width, x1 + radius), ey1 = Math.Min(height, y1 + radius);
        int w = ex1 - ex0, h = ey1 - ey0, n = w * h;
        var guide = new double[n];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { int i = ((y + ey0) * width + x + ex0) * 4; guide[y * w + x] = .2126 * source[i + 2] + .7152 * source[i + 1] + .0722 * source[i]; }
        var meanI = Box(guide, w, h, radius);
        var squares = new double[n]; for (int k = 0; k < n; k++) squares[k] = guide[k] * guide[k];
        var meanII = Box(squares, w, h, radius);
        int rw = x1 - x0, rh = y1 - y0; var result = new double[rw * rh * 3];
        var channel = new double[n]; var product = new double[n]; var a = new double[n]; var b = new double[n];
        for (int c = 0; c < 3; c++)
        {
            ct.ThrowIfCancellationRequested();
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { int k = y * w + x; channel[k] = source[((y + ey0) * width + x + ex0) * 4 + c]; product[k] = channel[k] * guide[k]; }
            var meanP = Box(channel, w, h, radius); var meanIP = Box(product, w, h, radius);
            for (int k = 0; k < n; k++)
            {
                double variance = meanII[k] - meanI[k] * meanI[k];
                a[k] = (meanIP[k] - meanI[k] * meanP[k]) / (variance + GuidedEpsilon); b[k] = meanP[k] - a[k] * meanI[k];
            }
            var meanA = Box(a, w, h, radius); var meanB = Box(b, w, h, radius);
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
            {
                int k = (y - ey0) * w + x - ex0;
                result[((y - y0) * rw + x - x0) * 3 + c] = meanA[k] * guide[k] + meanB[k];
            }
        }
        return result;
    }

    // Trung bình cửa sổ (2r+1)² bằng ảnh tích phân; cửa sổ bị cắt ở mép.
    private static double[] Box(double[] values, int w, int h, int r)
    {
        var integral = new double[(w + 1) * (h + 1)];
        for (int y = 0; y < h; y++)
        {
            double row = 0;
            for (int x = 0; x < w; x++) { row += values[y * w + x]; integral[(y + 1) * (w + 1) + x + 1] = integral[y * (w + 1) + x + 1] + row; }
        }
        var mean = new double[w * h];
        for (int y = 0; y < h; y++)
        {
            int ya = Math.Max(0, y - r), yb = Math.Min(h, y + r + 1);
            for (int x = 0; x < w; x++)
            {
                int xa = Math.Max(0, x - r), xb = Math.Min(w, x + r + 1);
                mean[y * w + x] = (integral[yb * (w + 1) + xb] - integral[ya * (w + 1) + xb] - integral[yb * (w + 1) + xa] + integral[ya * (w + 1) + xa]) / ((xb - xa) * (yb - ya));
            }
        }
        return mean;
    }

    private static bool Inside((double U, double V)[] polygon, double u, double v)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            if (polygon[i].V > v != polygon[j].V > v && u < (polygon[j].U - polygon[i].U) * (v - polygon[i].V) / (polygon[j].V - polygon[i].V) + polygon[i].U) inside = !inside;
        return inside;
    }
    private static double Ramp(double x, double lo, double hi) { double t = Math.Clamp((x - lo) / (hi - lo), 0, 1); return t * t * (3 - 2 * t); }
    private static double Ellipse(double x, double y, double cx, double cy, double rx, double ry) => 1 - Ramp(Math.Sqrt(Math.Pow((x - cx) / rx, 2) + Math.Pow((y - cy) / ry, 2)), .65, 1);
    private static double Skin(double r, double g, double b)
    {
        double cb = 128 - .168736 * r - .331264 * g + .5 * b, cr = 128 + .5 * r - .418688 * g - .081312 * b;
        return Ramp(cb, 65, 80) * (1 - Ramp(cb, 125, 140)) * Ramp(cr, 125, 138) * (1 - Ramp(cr, 175, 190));
    }
}
