using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace FotoAutomat.Desktop;

public sealed record MatchImage(string Path, BitmapSource Bitmap, string ColorHandling)
{
    public string Name => System.IO.Path.GetFileName(Path);
    public byte[] Pixels() { var p = new byte[Bitmap.PixelWidth * Bitmap.PixelHeight * 4]; Bitmap.CopyPixels(p, Bitmap.PixelWidth * 4, 0); return p; }
}
public static class MatchImageReader
{
    public static MatchImage Read(string path)
    {
        if (new FileInfo(path).Length > 40_000_000) throw new ArgumentException("Ảnh Match tối đa 40 MB.");
        using var stream = File.OpenRead(path); var frame = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        if ((long)frame.PixelWidth * frame.PixelHeight > 40_000_000) throw new ArgumentException("Ảnh Match tối đa 40 triệu pixel.");
        var image = Normalize(frame, Math.Min(1, 960d / Math.Max(frame.PixelWidth, frame.PixelHeight)));
        string handling = frame.ColorContexts is { Count: > 0 } ? "Embedded profile → sRGB" : "No embedded profile · assumed sRGB";
        return new(System.IO.Path.GetFullPath(path), image, handling);
    }

    /// <summary>EXIF orientation (1–8); 1 khi không có.</summary>
    public static int Orientation(BitmapFrame frame)
    {
        if (frame.Metadata is BitmapMetadata metadata)
            foreach (string query in new[] { "/app1/ifd/{ushort=274}", "/ifd/{ushort=274}" })
            {
                try { if (metadata.GetQuery(query) is { } v) return Convert.ToInt32(v); } catch (Exception e) when (e is ArgumentException or NotSupportedException) { }
            }
        return 1;
    }

    /// <summary>Xoay theo EXIF, thu nhỏ theo tỉ lệ (tối đa 1), profile nhúng → sRGB, BGRA8. Dùng chung cho ảnh mẫu và ảnh khách.</summary>
    public static BitmapSource Normalize(BitmapFrame frame, double scale)
    {
        BitmapSource image = frame;
        var transform = Orientation(frame) switch
        {
            2 => new Matrix(-1, 0, 0, 1, 0, 0),
            3 => new Matrix(-1, 0, 0, -1, 0, 0),
            4 => new Matrix(1, 0, 0, -1, 0, 0),
            5 => new Matrix(0, 1, 1, 0, 0, 0),
            6 => new Matrix(0, 1, -1, 0, 0, 0),
            7 => new Matrix(0, -1, -1, 0, 0, 0),
            8 => new Matrix(0, -1, 1, 0, 0, 0),
            _ => Matrix.Identity
        };
        if (!transform.IsIdentity) image = new TransformedBitmap(image, new MatrixTransform(transform));
        if (scale < 1) image = new TransformedBitmap(image, new ScaleTransform(scale, scale));
        image = frame.ColorContexts is { Count: > 0 } contexts
            ? new ColorConvertedBitmap(image, contexts[0], new ColorContext(PixelFormats.Bgra32), PixelFormats.Bgra32)
            : new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        image.Freeze(); return image;
    }
}
