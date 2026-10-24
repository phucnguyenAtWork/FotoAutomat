using FotoAutomat.Core;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FotoAutomat.Desktop;

public static class LayoutThumbnail
{
    public static ImageSource Create(PrintLayout layout)
    {
        var group = new DrawingGroup();
        using (var dc = group.Open())
        {
            dc.DrawRectangle(new SolidColorBrush((Color)ColorConverter.ConvertFromString(layout.BackgroundColor)), new Pen(Brushes.LightGray, 2), new Rect(0, 0, layout.PageWidth, layout.PageHeight));
            BitmapImage? artwork = null;
            if (layout.ArtworkPng is not null)
            {
                try
                {
                    using var stream = new MemoryStream(Convert.FromBase64String(layout.ArtworkPng));
                    artwork = new BitmapImage(); artwork.BeginInit(); artwork.CacheOption = BitmapCacheOption.OnLoad;
                    artwork.DecodePixelHeight = 120; artwork.StreamSource = stream; artwork.EndInit(); artwork.Freeze();
                }
                catch (Exception ex) when (ex is FormatException or IOException or NotSupportedException) { artwork = null; }
            }
            if (artwork is not null && layout.ArtworkBehindPhotos) dc.DrawImage(artwork, new Rect(0, 0, layout.PageWidth, layout.PageHeight));
            string[] colors = ["#D9E4E5", "#E9DECF", "#DDDFEB", "#DCE5D7"];
            foreach (var slot in layout.Slots.Where(s => s.Kind == LayoutElementKind.Photo))
            {
                dc.DrawRectangle(new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[(slot.PhotoNumber - 1) % 4])), null, new Rect(slot.X, slot.Y, slot.Width, slot.Height));
                var number = new FormattedText(slot.PhotoNumber.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 44, Brushes.DimGray, 1);
                dc.DrawText(number, new Point(slot.X + (slot.Width - number.Width) / 2, slot.Y + (slot.Height - number.Height) / 2));
            }
            if (artwork is not null && !layout.ArtworkBehindPhotos) dc.DrawImage(artwork, new Rect(0, 0, layout.PageWidth, layout.PageHeight));
        }
        group.Freeze(); var image = new DrawingImage(group); image.Freeze(); return image;
    }
}
