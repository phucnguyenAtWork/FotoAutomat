using FotoAutomat.Core;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FotoAutomat.Desktop;

/// <summary>Shared editor/customer rendering of a single layout element.</summary>
public sealed class LayoutElementVisual : FrameworkElement
{
    public static readonly DependencyProperty ElementProperty = DependencyProperty.Register(nameof(Element), typeof(PrintLayoutSlot), typeof(LayoutElementVisual), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty PreviewProperty = DependencyProperty.Register(nameof(Preview), typeof(bool), typeof(LayoutElementVisual), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public PrintLayoutSlot? Element { get => (PrintLayoutSlot?)GetValue(ElementProperty); set => SetValue(ElementProperty, value); }
    public bool Preview { get => (bool)GetValue(PreviewProperty); set => SetValue(PreviewProperty, value); }
    private string? imageData;
    private BitmapImage? image;
    protected override void OnRender(DrawingContext dc)
    {
        if (Element is not { } item || !item.IsVisible || ActualWidth <= 0 || ActualHeight <= 0) return;
        var rect = new Rect(0, 0, ActualWidth, ActualHeight);
        dc.PushClip(new RectangleGeometry(rect));
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(item.FillColor));
        switch (item.Kind)
        {
            case LayoutElementKind.SessionQr:
                // Editor-only QR motif; session delivery supplies the real encoded payload later.
                dc.DrawRectangle(Brushes.White, null, rect);
                const int modules = 21, quiet = 4;
                double unit = Math.Min(ActualWidth, ActualHeight) / (modules + quiet * 2);
                double ox = (ActualWidth - unit * modules) / 2, oy = (ActualHeight - unit * modules) / 2;
                for (int y = 0; y < modules; y++) for (int x = 0; x < modules; x++)
                {
                    bool black;
                    int fx = x >= 13 ? 14 : 0, fy = y >= 13 ? 14 : 0;
                    bool finderArea = (x < 8 && y < 8) || (x >= 13 && y < 8) || (x < 8 && y >= 13);
                    if (finderArea) { int dx = x - fx, dy = y - fy; black = dx >= 0 && dy >= 0 && dx < 7 && dy < 7 && (dx == 0 || dy == 0 || dx == 6 || dy == 6 || (dx >= 2 && dx <= 4 && dy >= 2 && dy <= 4)); }
                    else black = y == 6 ? x % 2 == 0 : x == 6 ? y % 2 == 0 : ((x * 13 + y * 7 + x * y * 3) % 11) < 5;
                    if (black) dc.DrawRectangle(Brushes.Black, null, new Rect(ox + x * unit, oy + y * unit, unit, unit));
                }
                break;
            case LayoutElementKind.Rectangle: dc.DrawRectangle(brush, null, rect); break;
            case LayoutElementKind.Ellipse: dc.DrawEllipse(brush, null, new(ActualWidth / 2, ActualHeight / 2), ActualWidth / 2, ActualHeight / 2); break;
            case LayoutElementKind.SessionDate:
            case LayoutElementKind.Text:
                var text = new FormattedText(item.Kind == LayoutElementKind.SessionDate ? "dd/mm/yyyy" : item.Content, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), item.FontSize, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip) { MaxTextWidth = ActualWidth, MaxTextHeight = ActualHeight, Trimming = TextTrimming.CharacterEllipsis };
                dc.DrawText(text, new(0, 0)); break;
            case LayoutElementKind.Image:
                if (imageData != item.Content)
                {
                    imageData = item.Content; image = null;
                    try
                    {
                        using var stream = new MemoryStream(Convert.FromBase64String(item.Content));
                        var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.DecodePixelWidth = 1200; bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze(); image = bitmap;
                    }
                    catch (Exception error) when (error is FormatException or IOException or NotSupportedException or ArgumentException) { }
                }
                if (image is not null)
                {
                    double scale = Math.Max(ActualWidth / image.PixelWidth, ActualHeight / image.PixelHeight);
                    double w = image.PixelWidth * scale, h = image.PixelHeight * scale;
                    dc.DrawImage(image, new Rect((ActualWidth - w) / 2, (ActualHeight - h) / 2, w, h));
                }
                break;
            default:
                string[] colors = ["#D9E4E5", "#E9DECF", "#DDDFEB", "#DCE5D7", "#E9DADD", "#DFDFDA"];
                dc.DrawRectangle(Preview ? Brushes.LightGray : new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[(item.PhotoNumber - 1) % colors.Length])), null, rect);
                if (Preview)
                {
                    dc.DrawEllipse(Brushes.DarkGray, null, new(ActualWidth / 2, ActualHeight * 0.36), ActualWidth * .17, ActualHeight * .19);
                    dc.DrawEllipse(Brushes.Gray, null, new(ActualWidth / 2, ActualHeight), ActualWidth * .44, ActualHeight * .4);
                }
                var number = new FormattedText(item.PhotoNumber.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 28, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(number, new((ActualWidth - number.Width) / 2, (ActualHeight - number.Height) / 2)); break;
        }
        dc.Pop();
    }
}
