using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;
using FotoAutomat.Core;
using DetectedFace = FotoAutomat.Core.DetectedFace;

namespace FotoAutomat.Desktop;

/// <summary>Bộ nhận diện của Windows: chỉ có khung mặt (mức Box). Dự phòng khi thiếu model MediaPipe.</summary>
public sealed class WindowsFaceAnalyzer : IFaceAnalyzer
{
    public string Name => "Windows";
    public async Task<IReadOnlyList<DetectedFace>> DetectAsync(byte[] bgra, int width, int height, CancellationToken cancellationToken = default) =>
        [.. (await WindowsFaceDetection.DetectAsync(bgra, width, height, cancellationToken)).Select(f => new DetectedFace(f.X, f.Y, f.Width, f.Height))];
}

/// <summary>Mặt cho trang Retouch (ảnh mẫu): MediaPipe nếu có model, lỗi thì dùng Windows. Điểm mắt/miệng nhận diện được thay vị trí mặc định.</summary>
public static class SampleFaceDetection
{
    private static readonly Lazy<IFaceAnalyzer?> mediaPipe = new(() =>
    {
        try { return Infrastructure.MediaPipeFaceAnalyzer.Available() ? new Infrastructure.MediaPipeFaceAnalyzer() : null; }
        catch (Exception error) { System.Diagnostics.Debug.WriteLine(error); return null; }
    });

    /// <summary>Bộ nhận diện cho ảnh khách: MediaPipe nếu có model, không thì Windows (chỉ khung mặt).</summary>
    public static IFaceAnalyzer CustomerAnalyzer() => mediaPipe.Value ?? new WindowsFaceAnalyzer();

    public static async Task<(FaceRetouch[] Faces, string Source)> DetectAsync(byte[] pixels, int width, int height, CancellationToken ct = default)
    {
        IReadOnlyList<DetectedFace>? found = null; string source = "Windows";
        if (mediaPipe.Value is { } analyzer)
        {
            try { found = await analyzer.DetectAsync(pixels, width, height, ct); source = analyzer.Name; }
            catch (Exception error) when (error is not OperationCanceledException) { System.Diagnostics.Debug.WriteLine(error); }
        }
        found ??= await new WindowsFaceAnalyzer().DetectAsync(pixels, width, height, ct);
        return ([.. found.Where(f => f.Width > 0 && f.Height > 0).OrderBy(f => f.X).ThenBy(f => f.Y).Take(RetouchProcessor.MaxFaces).Select(ToSample)], source);
    }

    // Khung cắt theo mép ảnh; điểm đổi sang tọa độ theo khung mặt, ngoài khung thì giữ mặc định để người vận hành căn lại.
    private static FaceRetouch ToSample(DetectedFace f)
    {
        double x = Math.Clamp(f.X, 0, .99), y = Math.Clamp(f.Y, 0, .99), w = Math.Min(f.Width, 1 - x), h = Math.Min(f.Height, 1 - y);
        var face = new FaceRetouch(x, y, w, h, new());
        (double U, double V)? Rel(FacePoint? p) => p is null ? null : ((p.X - x) / w, (p.Y - y) / h) is var (u, v) && u is >= 0 and <= 1 && v is >= 0 and <= 1 ? (u, v) : null;
        if (Rel(f.LeftEye) is { } l) face = face with { LeftEyeX = l.U, LeftEyeY = l.V };
        if (Rel(f.RightEye) is { } r) face = face with { RightEyeX = r.U, RightEyeY = r.V };
        if (Rel(f.Mouth) is { } m) face = face with { MouthX = m.U, MouthY = m.V };
        return face;
    }
}

public static class WindowsFaceDetection
{
    public static async Task<FaceRetouch[]> DetectAsync(byte[] pixels, int width, int height, CancellationToken ct = default)
    {
        if (width <= 0 || height <= 0 || (long)width * height * 4 != pixels.Length) throw new ArgumentException("Invalid face detection image.");
        ct.ThrowIfCancellationRequested();
        if (!FaceDetector.IsSupported || !FaceDetector.IsBitmapPixelFormatSupported(BitmapPixelFormat.Gray8)) throw new NotSupportedException("Face detection is unavailable on this Windows device. Add face manually.");
        var gray = new byte[width * height]; for (int i = 0; i < gray.Length; i++) gray[i] = (byte)(.2126 * pixels[i * 4 + 2] + .7152 * pixels[i * 4 + 1] + .0722 * pixels[i * 4]);
        using var bitmap = new SoftwareBitmap(BitmapPixelFormat.Gray8, width, height); bitmap.CopyFromBuffer(gray.AsBuffer());
        var detector = await FaceDetector.CreateAsync().AsTask(ct); var found = await detector.DetectFacesAsync(bitmap).AsTask(ct);
        return found.Where(f => f.FaceBox.Width >= width * .01 && f.FaceBox.Height >= height * .01 && f.FaceBox.X + f.FaceBox.Width <= width && f.FaceBox.Y + f.FaceBox.Height <= height)
            .OrderBy(f => f.FaceBox.X).ThenBy(f => f.FaceBox.Y).Take(20).Select(f => new FaceRetouch(f.FaceBox.X / (double)width, f.FaceBox.Y / (double)height, f.FaceBox.Width / (double)width, f.FaceBox.Height / (double)height, new())).ToArray();
    }
}
