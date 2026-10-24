using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FotoAutomat.Desktop;

internal static class EditorImageViewport
{
    public static void Attach(Panel surface, Image image)
    {
        // Canvas gives an oversized image its full layout slot before the viewport clips it.
        // A Grid can clip the image's layout slot, then move that clip along with a pan transform.
        var canvas = new Canvas { IsHitTestVisible = image.IsHitTestVisible };
        canvas.Children.Add(image); surface.Children.Add(canvas);
    }
    public static Rect Cover(FrameworkElement surface, BitmapSource? source, double zoom, double panX, double panY)
    {
        if (source is null || surface.ActualWidth <= 0 || surface.ActualHeight <= 0) return Rect.Empty;
        double scale = Math.Max(surface.ActualWidth / source.PixelWidth, surface.ActualHeight / source.PixelHeight);
        double width = source.PixelWidth * scale * zoom, height = source.PixelHeight * scale * zoom;
        return new Rect((surface.ActualWidth - width) / 2 + panX, (surface.ActualHeight - height) / 2 + panY, width, height);
    }

    public static void Apply(FrameworkElement surface, BitmapSource? source, ref double zoom, ref double panX, ref double panY, double maxZoom, params Image[] images)
    {
        zoom = Math.Clamp(zoom, 1, maxZoom);
        var bounds = Cover(surface, source, 1, 0, 0);
        if (bounds.IsEmpty) return;
        double limitX = Math.Max(0, (bounds.Width * zoom - surface.ActualWidth) / 2), limitY = Math.Max(0, (bounds.Height * zoom - surface.ActualHeight) / 2);
        panX = Math.Clamp(panX, -limitX, limitX); panY = Math.Clamp(panY, -limitY, limitY);
        foreach (var image in images)
        {
            image.Width = bounds.Width * zoom; image.Height = bounds.Height * zoom;
            image.HorizontalAlignment = HorizontalAlignment.Left; image.VerticalAlignment = VerticalAlignment.Top;
            // Dimensions already follow the pixel aspect ratio; fill also avoids DPI metadata letterboxing.
            image.Stretch = Stretch.Fill; image.RenderTransform = Transform.Identity;
            Canvas.SetLeft(image, (surface.ActualWidth - image.Width) / 2 + panX);
            Canvas.SetTop(image, (surface.ActualHeight - image.Height) / 2 + panY);
        }
    }
}
