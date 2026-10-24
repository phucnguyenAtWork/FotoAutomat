using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FotoAutomat.Desktop;

/// <summary>Capture One style color balance wheel: drag on the disk for hue (angle) and amount (distance), drag the arc on
/// the right for lightness (up is brighter). Double-click the disk resets hue/amount, double-click the arc resets lightness.
/// Edited fires while dragging; Released fires once when the gesture ends.</summary>
public sealed class ColorGradingWheel : FrameworkElement
{
    private const double ArcGap = 9, ArcSpan = 55; // arc radius = disk radius + gap; ±55° around the right side
    private static readonly BitmapSource Disk = MakeDisk();
    private static readonly Pen Border = Frozen(new Pen(new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)), 1));
    private double hue, amount, lightness;
    private bool draggingArc;
    public event Action<double, double, double>? Edited;
    public event Action? Released;
    public ColorGradingWheel(double hue, double amount, double lightness = 0, double diameter = 150)
    {
        this.hue = hue; this.amount = amount; this.lightness = lightness;
        Height = diameter; Width = diameter + 2 * ArcGap + 8; Focusable = true; Cursor = Cursors.Hand;
        MouseLeftButtonDown += (_, e) => { if (e.ClickCount == 2) { Reset(e.GetPosition(this)); e.Handled = true; return; } CaptureMouse(); Begin(e.GetPosition(this)); e.Handled = true; };
        MouseMove += (_, e) => { if (IsMouseCaptured) Pick(e.GetPosition(this)); };
        MouseLeftButtonUp += (_, e) => { if (!IsMouseCaptured) return; ReleaseMouseCapture(); Released?.Invoke(); e.Handled = true; };
        TouchDown += (_, e) => { CaptureTouch(e.TouchDevice); Begin(e.GetTouchPoint(this).Position); e.Handled = true; };
        TouchMove += (_, e) => { if (e.TouchDevice.Captured == this) Pick(e.GetTouchPoint(this).Position); e.Handled = true; };
        TouchUp += (_, e) => { ReleaseTouchCapture(e.TouchDevice); Released?.Invoke(); e.Handled = true; };
    }
    public void Set(double hue, double amount, double lightness) { this.hue = hue; this.amount = amount; this.lightness = lightness; InvalidateVisual(); }
    private double Radius => Height / 2;
    private Point Center => new(Radius, Radius);
    private bool OnArc(Point p) { var c = Center; return p.X > c.X && Math.Sqrt((p.X - c.X) * (p.X - c.X) + (p.Y - c.Y) * (p.Y - c.Y)) > Radius + 2; }
    private void Begin(Point p) { draggingArc = OnArc(p); Pick(p); }
    private void Pick(Point p)
    {
        double x = (p.X - Center.X) / Radius, y = (p.Y - Center.Y) / Radius;
        if (draggingArc) lightness = Math.Round(-Math.Clamp(Math.Atan2(y, Math.Max(x, 1e-6)) * 180 / Math.PI, -ArcSpan, ArcSpan) / ArcSpan * 100);
        else { hue = Math.Round((Math.Atan2(y, x) * 180 / Math.PI + 360) % 360, 1); amount = Math.Round(Math.Clamp(Math.Sqrt(x * x + y * y) * 100, 0, 100), 1); }
        InvalidateVisual(); Edited?.Invoke(hue, amount, lightness);
    }
    private void Reset(Point p)
    {
        if (OnArc(p)) lightness = 0; else hue = amount = 0;
        InvalidateVisual(); Edited?.Invoke(hue, amount, lightness); Released?.Invoke();
    }
    protected override void OnRender(DrawingContext dc)
    {
        var c = Center; double r = Radius;
        dc.PushClip(new EllipseGeometry(c, r, r)); dc.DrawImage(Disk, new Rect(0, 0, 2 * r, 2 * r)); dc.Pop();
        dc.DrawEllipse(null, Border, c, r, r);
        // centre crosshair
        dc.DrawEllipse(null, new Pen(Brushes.White, 1), c, 6, 6); dc.DrawEllipse(Brushes.White, null, c, 1.5, 1.5);
        var puck = new Point(c.X + Math.Cos(hue * Math.PI / 180) * amount / 100 * r, c.Y + Math.Sin(hue * Math.PI / 180) * amount / 100 * r);
        dc.DrawEllipse(null, new Pen(Brushes.Black, 3), puck, 6, 6); dc.DrawEllipse(null, new Pen(Brushes.White, 1.5), puck, 6, 6);
        // lightness arc: dark at the bottom, white at the top
        double ar = r + ArcGap; Point At(double degrees) => new(c.X + ar * Math.Cos(degrees * Math.PI / 180), c.Y + ar * Math.Sin(degrees * Math.PI / 180));
        var arc = new StreamGeometry();
        using (var g = arc.Open()) { g.BeginFigure(At(-ArcSpan), false, false); g.ArcTo(At(ArcSpan), new Size(ar, ar), 0, false, SweepDirection.Clockwise, true, false); }
        arc.Freeze();
        var shade = new LinearGradientBrush(Colors.White, Color.FromRgb(40, 40, 40), 90) { MappingMode = BrushMappingMode.RelativeToBoundingBox }; shade.Freeze();
        dc.DrawGeometry(null, new Pen(shade, 3) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, arc);
        dc.DrawLine(new Pen(Brushes.Gray, 1), At(0) with { X = At(0).X - 3 }, At(0) with { X = At(0).X + 3 }); // zero mark
        var knob = At(-lightness / 100 * ArcSpan);
        dc.DrawEllipse(Brushes.White, new Pen(Brushes.Black, 1.5), knob, 5, 5);
    }
    private static Pen Frozen(Pen pen) { pen.Freeze(); return pen; }
    private static BitmapSource MakeDisk()
    {
        const int size = 256; var pixels = new byte[size * size * 4];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            double dx = (x + .5 - size / 2d) / (size / 2d), dy = (y + .5 - size / 2d) / (size / 2d), s = Math.Min(1, Math.Sqrt(dx * dx + dy * dy));
            // hue by angle, saturation by radius, on a mid-dark base like Capture One (centre is neutral grey)
            double h = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360, v = .45 + .45 * s;
            var (r, g, b) = Hsv(h, s, v);
            int i = (y * size + x) * 4; pixels[i] = (byte)(b * 255); pixels[i + 1] = (byte)(g * 255); pixels[i + 2] = (byte)(r * 255); pixels[i + 3] = 255;
        }
        var image = BitmapSource.Create(size, size, 96, 96, PixelFormats.Bgra32, null, pixels, size * 4); image.Freeze(); return image;
    }
    internal static (double R, double G, double B) Hsv(double h, double s, double v)
    {
        h = (h % 360 + 360) % 360; double c = v * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = v - c;
        var (r, g, b) = h < 60 ? (c, x, 0d) : h < 120 ? (x, c, 0d) : h < 180 ? (0d, c, x) : h < 240 ? (0d, x, c) : h < 300 ? (x, 0d, c) : (c, 0d, x);
        return (r + m, g + m, b + m);
    }
}
