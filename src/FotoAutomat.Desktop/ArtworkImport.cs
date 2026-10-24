using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FotoAutomat.Desktop;

public sealed record ImportedArtwork(byte[] Bytes, BitmapImage Image, bool Strips, bool Opaque);

public static class ArtworkImport
{
    public const string FileFilter = "Tệp ảnh|*.png;*.jpg;*.jpeg;*.jfif;*.bmp;*.dib;*.gif;*.tif;*.tiff;*.ico;*.wdp;*.hdp;*.jxr;*.webp;*.avif";
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    { ".png", ".jpg", ".jpeg", ".jfif", ".bmp", ".dib", ".gif", ".tif", ".tiff", ".ico", ".wdp", ".hdp", ".jxr", ".webp", ".avif" };

    public static BitmapFrame ReadFrame(string path)
    {
        if (!Extensions.Contains(Path.GetExtension(path)))
            throw new InvalidDataException("Chọn tệp ảnh được hỗ trợ; không nhận RAW, HEIC, âm thanh hoặc video.");
        if (new FileInfo(path).Length > 20_000_000) throw new InvalidDataException("Chọn ảnh nhỏ hơn 20 MB.");
        try
        {
            using var stream = File.OpenRead(path);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            // Accept standard raster decoders and explicitly recognized optional web codecs.
            bool standard = decoder is PngBitmapDecoder or JpegBitmapDecoder or BmpBitmapDecoder
                or GifBitmapDecoder or TiffBitmapDecoder or IconBitmapDecoder or WmpBitmapDecoder;
            stream.Position = 0;
            Span<byte> header = stackalloc byte[12];
            int count = stream.Read(header);
            bool webp = count == 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8);
            bool avif = count == 12 && header[4..8].SequenceEqual("ftyp"u8) && header[8..12].SequenceEqual("avif"u8);
            if (!standard && !webp && !avif) throw new InvalidDataException("Định dạng ảnh không được hỗ trợ.");
            var frame = decoder.Frames[0];
            if ((long)frame.PixelWidth * frame.PixelHeight > 24_000_000)
                throw new InvalidDataException("Chọn ảnh tối đa 24 triệu pixel.");
            frame.Freeze();
            return frame;
        }
        catch (Exception error) when (error is NotSupportedException or System.Runtime.InteropServices.COMException)
        {
            throw new InvalidDataException("Không đọc được định dạng ảnh này. Với WebP/AVIF, máy cần codec tương thích; có thể xuất ảnh thành PNG hoặc JPEG.", error);
        }
    }

    public static ImportedArtwork ReadFile(string path)
    {
        var frame = ReadFrame(path);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(frame);
        using var stream = new MemoryStream();
        encoder.Save(stream);
        if (stream.Length > 20_000_000) throw new InvalidDataException("Ảnh sau chuyển đổi vượt 20 MB. Giảm kích thước ảnh trước khi nhập.");
        return Read(stream.ToArray());
    }
    public static ImportedArtwork Read(byte[] bytes)
    {
        byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length < 8 || !bytes.AsSpan(0, 8).SequenceEqual(signature)) throw new InvalidDataException("Chọn tệp PNG hợp lệ.");
        var input = Decode(bytes, 1200);
        var format = FotoAutomat.Core.ArtworkPaperFormat.Detect(input.PixelWidth, input.PixelHeight);
        bool strips = format.SingleStrip;
        var pixels = new FormatConvertedBitmap(input, PixelFormats.Bgra32, null, 0);
        byte[] data = new byte[pixels.PixelWidth * pixels.PixelHeight * 4];
        pixels.CopyPixels(data, pixels.PixelWidth * 4, 0);
        bool opaque = true;
        for (int i = 3; i < data.Length; i += 4) if (data[i] < 255) { opaque = false; break; }
        return new(bytes, input, strips, opaque);
    }
    private static BitmapImage Decode(byte[] bytes, int width)
    {
        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.DecodePixelWidth = width;
        image.StreamSource = stream; image.EndInit(); image.Freeze(); return image;
    }
}
