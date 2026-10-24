namespace FotoAutomat.Core;

/// <summary>Kích thước pixel của một ô ảnh khi in (theo DPI của khung).</summary>
public sealed record SlotTarget(int Width, int Height)
{
    public double Aspect => Width / (double)Height;
    public static SlotTarget Of(PrintLayoutSlot slot, int dpi) =>
        new(Math.Max(1, (int)Math.Round(slot.Width * dpi / 100)), Math.Max(1, (int)Math.Round(slot.Height * dpi / 100)));
}

/// <summary>
/// Chế độ chụp trên một ảnh khách. Mọi hiệu ứng chạy trên cả khung ảnh (vignette, mist/glow, grain theo cạnh ngắn),
/// rồi mới cắt giữa vào ô như màn Khung (UniformToFill). Đường in chỉ khác đường full 26 MP ở tỉ lệ thu nhỏ trước khi xử lý.
/// </summary>
public static class PhotoPipeline
{
    /// <summary>Vùng cắt giữa theo tỉ lệ ô, cùng cách UniformToFill của màn Khung.</summary>
    public static (double X, double Y, double Width, double Height) CenterCrop(double width, double height, double aspect) =>
        width / height > aspect ? ((width - height * aspect) / 2, 0, height * aspect, height) : (0, (height - width / aspect) / 2, width, width / aspect);

    /// <summary>Tỉ lệ thu nhỏ khung ảnh để phần cắt giữa vừa đúng pixel ô in. Không phóng to (tối đa 1).</summary>
    public static double PrintScale(int frameWidth, int frameHeight, SlotTarget slot) =>
        Math.Min(1, slot.Width / CenterCrop(frameWidth, frameHeight, slot.Aspect).Width);

    /// <summary>Retouch → màu → Refine (Clean, Finish). Mặt theo tọa độ chuẩn hóa của khung; seed cố định theo ảnh.</summary>
    public static byte[] Render(byte[] bgra, int width, int height, ShootingMode mode, IReadOnlyList<DetectedFace> faces, int seed, CancellationToken ct = default)
    {
        var pixels = mode.Retouch != new RetouchSettings() && faces.Count > 0 ? RetouchProcessor.Apply(bgra, width, height, faces, mode.Retouch, ct) : bgra;
        pixels = ColorProcessor.Apply(pixels, mode.Color, width, height, ct);
        if (mode.Refine == new RefineSettings()) return pixels;
        pixels = RefineProcessor.Clean(pixels, width, height, mode.Refine, ct);
        return RefineProcessor.Finish(pixels, width, height, mode.Refine, seed, ct);
    }

    /// <summary>Cắt giữa theo tỉ lệ ô rồi đưa về đúng pixel ô: trung bình nhiều mẫu khi thu nhỏ, song tuyến khi phóng to.</summary>
    public static byte[] CropToSlot(byte[] bgra, int width, int height, SlotTarget slot, CancellationToken ct = default)
    {
        if (width <= 0 || height <= 0 || (long)width * height * 4 != bgra.Length) throw new ArgumentException("Ảnh không hợp lệ.");
        var (cx, cy, cw, ch) = CenterCrop(width, height, slot.Aspect);
        var output = new byte[slot.Width * slot.Height * 4];
        double stepX = cw / slot.Width, stepY = ch / slot.Height;
        int n = Math.Clamp((int)Math.Ceiling(Math.Max(stepX, stepY)), 1, 8);
        Span<double> sum = stackalloc double[4];
        for (int ty = 0; ty < slot.Height; ty++)
        {
            ct.ThrowIfCancellationRequested();
            for (int tx = 0; tx < slot.Width; tx++)
            {
                sum.Clear();
                for (int sy = 0; sy < n; sy++)
                    for (int sx = 0; sx < n; sx++)
                    {
                        double x = cx + (tx + (sx + .5) / n) * stepX - .5, y = cy + (ty + (sy + .5) / n) * stepY - .5;
                        int x0 = Math.Clamp((int)Math.Floor(x), 0, width - 1), y0 = Math.Clamp((int)Math.Floor(y), 0, height - 1);
                        int x1 = Math.Min(x0 + 1, width - 1), y1 = Math.Min(y0 + 1, height - 1);
                        double fx = Math.Clamp(x - x0, 0, 1), fy = Math.Clamp(y - y0, 0, 1);
                        for (int c = 0; c < 4; c++)
                            sum[c] += (bgra[(y0 * width + x0) * 4 + c] * (1 - fx) + bgra[(y0 * width + x1) * 4 + c] * fx) * (1 - fy)
                                    + (bgra[(y1 * width + x0) * 4 + c] * (1 - fx) + bgra[(y1 * width + x1) * 4 + c] * fx) * fy;
                    }
                int o = (ty * slot.Width + tx) * 4;
                for (int c = 0; c < 4; c++) output[o + c] = (byte)Math.Clamp(Math.Round(sum[c] / (n * n)), 0, 255);
            }
        }
        return output;
    }

    /// <summary>Chế độ chụp đã lưu trong snapshot phiên.</summary>
    public static ShootingMode ModeOf(SessionSnapshot snapshot) => new(
        System.Text.Json.JsonSerializer.Deserialize<ColorRecipe>(snapshot.RecipeJson) ?? new ColorRecipe(),
        snapshot.Retouch ?? new(), snapshot.Refine ?? new(), snapshot.Features ?? ModeFeatures.Default, snapshot.ModeNotes ?? []);
}

/// <summary>Đọc/ghi ảnh cho dịch vụ xử lý: ảnh đã xoay theo EXIF và đổi về sRGB, BGRA8.</summary>
public interface IImageCodec
{
    (int Width, int Height) Probe(string path);
    (byte[] Pixels, int Width, int Height) Decode(string path, double scale);
    void SaveJpeg(byte[] bgra, int width, int height, string path, int quality = 92);
}

/// <summary>Ảnh đã xử lý cho một ô, đúng pixel ô (SlotTarget).</summary>
public sealed record SlotImage(byte[] Pixels, int Width, int Height);

/// <summary>Ghép khung với ảnh từng ô (theo vị trí trong danh sách ô hiển thị) và ghi file khung + file tờ in.</summary>
public interface IFrameComposer
{
    void Compose(PrintLayout layout, IReadOnlyDictionary<int, SlotImage> photos, string framePath, string sheetPath);
}
