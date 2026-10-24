using FotoAutomat.Core;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FotoAutomat.Desktop;

public static class LegacyStripMigration
{
    // Only collapse provably duplicated legacy sheets. Source files remain untouched until Save.
    public static PrintLayout Convert(PrintLayout layout)
    {
        if (!layout.CutIntoStrips || layout.SingleStrip) return layout;
        var left = layout.Slots.Where(s => s.X < 200).ToArray();
        var right = layout.Slots.Where(s => s.X >= 200).Select(s => s with { X = s.X - 200 }).ToArray();
        if (left.Length == 0 || !left.SequenceEqual(right)) return layout;
        string? artwork = layout.ArtworkPng;
        if (artwork is not null)
        {
            var image = ArtworkImport.Read(System.Convert.FromBase64String(artwork)).Image;
            if (image.PixelWidth % 2 != 0 || Math.Abs(image.PixelWidth / (double)image.PixelHeight - 2.0 / 3) > .01) return layout;
            var pixels = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
            int stride = pixels.PixelWidth * 4, half = stride / 2;
            byte[] data = new byte[stride * pixels.PixelHeight]; pixels.CopyPixels(data, stride, 0);
            for (int y = 0; y < pixels.PixelHeight; y++)
                if (!data.AsSpan(y * stride, half).SequenceEqual(data.AsSpan(y * stride + half, half))) return layout;
            var crop = new CroppedBitmap(image, new Int32Rect(0, 0, image.PixelWidth / 2, image.PixelHeight));
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(crop));
            using var output = new MemoryStream(); encoder.Save(output); artwork = System.Convert.ToBase64String(output.ToArray());
        }
        var result = layout with { CutIntoStrips = false, SingleStrip = true, Slots = left, ArtworkPng = artwork };
        result.Validate(); return result;
    }
}
