using System.Windows;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FotoAutomat.Desktop;
public sealed class ColorHistogram : FrameworkElement
{
    private readonly int[][] bins = [new int[256], new int[256], new int[256], new int[256]];
    private readonly double[][] display = Enumerable.Range(0, 4).Select(_ => new double[256]).ToArray();
    public double[][] Snapshot() => display.Select(c => c.ToArray()).ToArray();
    public ImageExposureMetadata ExposureMetadata { get; set; } = new();
    public double HighlightClipping { get; private set; }
    public double ShadowClipping { get; private set; }
    public void Update(BitmapSource? source)
    {
        foreach (var channel in bins) Array.Clear(channel);
        int visible = 0, highlights = 0, shadows = 0;
        if (source is not null)
        {
            var converted = source.Format == PixelFormats.Bgra32 ? source : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            var pixels = new byte[converted.PixelWidth * converted.PixelHeight * 4]; converted.CopyPixels(pixels, converted.PixelWidth * 4, 0);
            for (int i = 0; i < pixels.Length; i += 4) if (pixels[i + 3] > 0) { bins[0][pixels[i + 2]]++; bins[1][pixels[i + 1]]++; bins[2][pixels[i]]++; bins[3][(int)Math.Round(.2126 * pixels[i + 2] + .7152 * pixels[i + 1] + .0722 * pixels[i])]++; visible++; if (Math.Max(pixels[i], Math.Max(pixels[i + 1], pixels[i + 2])) == 255) highlights++; if (Math.Min(pixels[i], Math.Min(pixels[i + 1], pixels[i + 2])) == 0) shadows++; }
        }
        // Smooth only the plotted distribution, never the clipping measurements.
        int[] weights = [1, 4, 6, 4, 1];
        for (int c = 0; c < 4; c++) for (int x = 0; x < 256; x++)
        {
            double total = 0; int weight = 0;
            for (int k = -2; k <= 2; k++) if (x + k is >= 0 and < 256) { total += bins[c][x + k] * weights[k + 2]; weight += weights[k + 2]; }
            display[c][x] = total / weight;
        }
        HighlightClipping = visible == 0 ? 0 : highlights * 100d / visible; ShadowClipping = visible == 0 ? 0 : shadows * 100d / visible; InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight; if (w < 20 || h < 40) return;
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(35, 35, 35)), null, new Rect(0, 0, w, h));
        Text(dc, "Histogram", 6, 1, Brushes.LightGray, 11);
        Color[] colors = [Color.FromRgb(226, 129, 123), Color.FromRgb(163, 197, 143), Color.FromRgb(134, 164, 224), Color.FromRgb(180, 180, 180)];
        for (int c = 0; c < 3; c++) Text(dc, new[] { "R", "G", "B" }[c], w - 48 + c * 15, 2, new SolidColorBrush(colors[c]), 10);
        var plot = new Rect(5, 20, w - 10, h - 39); dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(29, 29, 29)), null, plot);
        var grid = new Pen(new SolidColorBrush(Color.FromRgb(51, 51, 51)), .6);
        for (int i = 1; i < 8; i++) dc.DrawLine(grid, new Point(plot.X + plot.Width * i / 8, plot.Y), new Point(plot.X + plot.Width * i / 8, plot.Bottom));
        for (int i = 1; i < 4; i++) dc.DrawLine(grid, new Point(plot.X, plot.Y + plot.Height * i / 4), new Point(plot.Right, plot.Y + plot.Height * i / 4));
        double maximum = Math.Max(1, display.Max(c => c.Max()));
        for (int channel = 3; channel >= 0; channel--)
        {
            var points = Enumerable.Range(0, 256).Select(i => new Point(plot.X + i * plot.Width / 255, plot.Bottom - (plot.Height - 3) * Math.Sqrt(display[channel][i] / maximum))).ToArray();
            var area = new StreamGeometry();
            using (var context = area.Open()) { context.BeginFigure(new Point(plot.Left, plot.Bottom), true, true); foreach (var p in points) context.LineTo(p, true, false); context.LineTo(new Point(plot.Right, plot.Bottom), true, false); }
            var color = colors[channel]; dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(channel == 3 ? (byte)30 : (byte)14, color.R, color.G, color.B)), null, area);
            if (channel == 3) continue;
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(points[0], false, false);
                foreach (var p in points.Skip(1)) context.LineTo(p, true, false);
            }
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(color), .9), geometry);
        }
        Text(dc, ExposureMetadata.Iso, 6, h - 17, Brushes.LightGray, 10);
        Text(dc, ExposureMetadata.Shutter, w / 2, h - 17, Brushes.LightGray, 10, .5);
        Text(dc, ExposureMetadata.Aperture, w - 6, h - 17, Brushes.LightGray, 10, 1);
    }
    private void Text(DrawingContext dc, string value, double x, double y, Brush brush, double size, double align = 0)
    {
        var text = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(text, new Point(x - text.Width * align, y));
    }
}
