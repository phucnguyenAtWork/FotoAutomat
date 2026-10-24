using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

/// <summary>Đọc ảnh như ảnh mẫu của trang quản trị (xoay EXIF, profile nhúng → sRGB), ở tỉ lệ bất kỳ; ghi JPEG.</summary>
public sealed class WpfImageCodec : IImageCodec
{
    public (int Width, int Height) Probe(string path)
    {
        using var stream = File.OpenRead(path);
        var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
        return MatchImageReader.Orientation(frame) is >= 5 and <= 8 ? (frame.PixelHeight, frame.PixelWidth) : (frame.PixelWidth, frame.PixelHeight);
    }

    public (byte[] Pixels, int Width, int Height) Decode(string path, double scale)
    {
        using var stream = File.OpenRead(path);
        var frame = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var image = MatchImageReader.Normalize(frame, scale);
        var pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
        image.CopyPixels(pixels, image.PixelWidth * 4, 0);
        return (pixels, image.PixelWidth, image.PixelHeight);
    }

    public void SaveJpeg(byte[] bgra, int width, int height, string path, int quality = 92)
    {
        var encoder = new JpegBitmapEncoder { QualityLevel = quality };
        encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, bgra, width * 4)));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}

/// <summary>
/// Ghép khung offscreen giống màn Khung của khách (ô ảnh với viền, xoay, bóng; chữ/hình/ảnh qua LayoutElementVisual;
/// artwork trước hoặc sau ảnh), ở đúng DPI của khung, rồi ghép tờ in bằng PrintSheetComposer. Chạy trên luồng giao diện.
/// </summary>
public sealed class WpfFrameComposer(PrinterConfiguration printer) : IFrameComposer
{
    public void Compose(PrintLayout layout, IReadOnlyDictionary<int, SlotImage> photos, string framePath, string sheetPath)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess()) { dispatcher.Invoke(() => Compose(layout, photos, framePath, sheetPath)); return; }
        var frame = Render(layout, photos);
        SavePng(frame, framePath);
        SavePng(PrintSheetComposer.Compose(frame, layout, printer), sheetPath);
    }

    public static BitmapSource Render(PrintLayout layout, IReadOnlyDictionary<int, SlotImage> photos)
    {
        int pixelWidth = (int)Math.Round(layout.PageWidth * layout.Dpi / 100), pixelHeight = (int)Math.Round(layout.PageHeight * layout.Dpi / 100);
        var canvas = new Canvas { Width = layout.PageWidth, Height = layout.PageHeight, Background = Brush(layout.BackgroundColor) };
        var effects = new LayoutEffectConverter();
        var visible = layout.Slots.Where(s => s.IsVisible).ToArray();
        for (int i = 0; i < visible.Length; i++)
        {
            var slot = visible[i];
            var content = new Grid { ClipToBounds = true };
            if (slot.Kind == LayoutElementKind.Photo)
            {
                if (photos.TryGetValue(i, out var photo))
                    content.Children.Add(new Image { Source = BitmapSource.Create(photo.Width, photo.Height, 96, 96, PixelFormats.Bgra32, null, photo.Pixels, photo.Width * 4), Stretch = Stretch.UniformToFill });
            }
            else content.Children.Add(new LayoutElementVisual { Element = slot, Preview = true });
            var border = new Border
            {
                Width = slot.Width,
                Height = slot.Height,
                BorderBrush = Brush(slot.StrokeColor),
                BorderThickness = new Thickness(slot.StrokeWidth),
                RenderTransformOrigin = new Point(.5, .5),
                RenderTransform = new RotateTransform(slot.Rotation),
                Effect = effects.Convert(slot, typeof(System.Windows.Media.Effects.Effect), null!, System.Globalization.CultureInfo.InvariantCulture) as System.Windows.Media.Effects.Effect,
                Child = content,
            };
            Canvas.SetLeft(border, slot.X); Canvas.SetTop(border, slot.Y);
            canvas.Children.Add(border);
        }
        if (layout.ArtworkPng is not null)
        {
            var artwork = new Image { Source = LayoutWindow.DecodeArtwork(Convert.FromBase64String(layout.ArtworkPng)), Stretch = Stretch.Fill, Width = layout.PageWidth, Height = layout.PageHeight };
            Panel.SetZIndex(artwork, layout.ArtworkBehindPhotos ? -1 : 1);
            canvas.Children.Add(artwork);
        }
        // RenderTargetBitmap ở DPI của khung: kích thước DIP = pixel × 96 / DPI.
        var root = new Viewbox { Stretch = Stretch.Fill, Width = pixelWidth * 96d / layout.Dpi, Height = pixelHeight * 96d / layout.Dpi, Child = canvas };
        root.Measure(new Size(root.Width, root.Height)); root.Arrange(new Rect(0, 0, root.Width, root.Height)); root.UpdateLayout();
        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, layout.Dpi, layout.Dpi, PixelFormats.Pbgra32);
        bitmap.Render(root); bitmap.Freeze();
        return bitmap;
    }

    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

    private static void SavePng(BitmapSource image, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
