using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

public sealed class ColorRangeWheel : FrameworkElement
{
    public ColorRangeAdjustment Value { get; private set; }
    public event Action<ColorRangeAdjustment>? Edited;
    public event Action? Released;
    private bool boundary;
    private readonly bool skin;
    private int arc;
    private BitmapSource? selectionGlow;
    public ColorRangeWheel(ColorRangeAdjustment value, bool skin = false)
    {
        this.skin = skin;
        Value = value; Cursor = Cursors.Cross; Focusable = true;
        MouseLeftButtonDown += (_, e) => { var p = e.GetPosition(this); Begin(p); CaptureMouse(); Edit(p); e.Handled = true; };
        MouseMove += (_, e) => { if (IsMouseCaptured) Edit(e.GetPosition(this)); };
        MouseLeftButtonUp += (_, _) => { if (!IsMouseCaptured) return; ReleaseMouseCapture(); Released?.Invoke(); };
        TouchDown += (_, e) => { var p = e.GetTouchPoint(this).Position; Begin(p); CaptureTouch(e.TouchDevice); Edit(p); e.Handled = true; };
        TouchMove += (_, e) => { if (e.TouchDevice.Captured == this) Edit(e.GetTouchPoint(this).Position); e.Handled = true; };
        TouchUp += (_, e) => { ReleaseTouchCapture(e.TouchDevice); Released?.Invoke(); e.Handled = true; };
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        double side = double.IsFinite(availableSize.Width) ? availableSize.Width : 360;
        return new Size(side, side);
    }
    private double Radius => Math.Max(1, Math.Min(ActualWidth, ActualHeight) / 2 - (skin ? 32 : 10));
    private void Begin(Point p) { arc = skin && Distance(p) > Radius + 5 ? (p.X < Center.X ? -1 : 1) : 0; boundary = Distance(p) > Radius * .85; }
    private Point Center => new(ActualWidth / 2, ActualHeight / 2);
    private double Distance(Point p) => (p - Center).Length;
    private Point At(double hue, double distance) => Center + new Vector(Math.Cos(hue * Math.PI / 180) * distance, -Math.Sin(hue * Math.PI / 180) * distance);
    public void Set(ColorRangeAdjustment value)
    {
        if (Value.CenterHue != value.CenterHue || Value.CenterSaturation != value.CenterSaturation || Value.Width != value.Width || Value.SaturationRadius != value.SaturationRadius || Value.Smoothness != value.Smoothness) selectionGlow = null;
        Value = value; InvalidateVisual();
    }
    private BitmapSource SelectionGlow()
    {
        if (selectionGlow is not null) return selectionGlow;
        const int size = 320;
        var pixels = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            double dx = (x + .5 - size / 2d) / (size / 2d), dy = (size / 2d - y - .5) / (size / 2d);
            double saturation = Math.Sqrt(dx * dx + dy * dy);
            if (saturation > 1) continue;
            double hue = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
            double weight = Value.Weight(hue, saturation);
            double edge = Math.Clamp((1 - saturation) / .035, 0, 1);
            var color = AdjustToolProcessor.FromHsl(hue, saturation, .65);
            int i = (y * size + x) * 4;
            pixels[i] = (byte)(color.B * 255); pixels[i + 1] = (byte)(color.G * 255); pixels[i + 2] = (byte)(color.R * 255);
            pixels[i + 3] = (byte)(205 * weight * edge);
        }
        selectionGlow = BitmapSource.Create(size, size, 96, 96, PixelFormats.Bgra32, null, pixels, size * 4);
        selectionGlow.Freeze(); return selectionGlow;
    }
    private void Edit(Point p)
    {
        if (arc != 0)
        {
            double angle = Math.Atan2(Center.Y - p.Y, Math.Abs(p.X - Center.X)) * 180 / Math.PI;
            double t = Math.Clamp(angle / 55, -1, 1);
            Set(arc == 1 ? Value with { Lightness = Math.Round(t * 100) } : Value with { Saturation = Math.Round(t >= 0 ? t * 200 : t * 100) });
            Edited?.Invoke(Value); return;
        }
        double h = (Math.Atan2(Center.Y - p.Y, p.X - Center.X) * 180 / Math.PI + 360) % 360;
        Set(boundary ? Value with { Width = Math.Clamp(Math.Abs(AdjustToolProcessor.HueDelta(Value.CenterHue, h)), 1, 180) }
            : Value with { CenterHue = h, CenterSaturation = Math.Clamp(Distance(p) / Radius, 0, 1) });
        Edited?.Invoke(Value);
    }
    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(43, 43, 43)), null, new Rect(RenderSize), 6, 6);
        dc.DrawImage(SelectionGlow(), new Rect(Center.X - Radius, Center.Y - Radius, Radius * 2, Radius * 2));
        for (int h = 0; h < 360; h += 2)
        {
            var c = AdjustToolProcessor.FromHsl(h, 1, .5); var brush = new SolidColorBrush(Color.FromRgb((byte)(c.R * 255), (byte)(c.G * 255), (byte)(c.B * 255)));
            dc.DrawLine(new Pen(brush, 3), At(h, Radius), At(h + 2, Radius));
        }
        var shape = new StreamGeometry();
        double inner = Math.Max(0, Value.CenterSaturation - Value.SaturationRadius) * Radius;
        double outer = Math.Min(1, Value.CenterSaturation + Value.SaturationRadius) * Radius;
        using (var context = shape.Open())
        {
            context.BeginFigure(At(Value.CenterHue - Value.Width, inner), true, true);
            for (double h = Value.CenterHue - Value.Width; h <= Value.CenterHue + Value.Width; h += 2) context.LineTo(At(h, outer), true, false);
            context.LineTo(At(Value.CenterHue + Value.Width, outer), true, false);
            for (double h = Value.CenterHue + Value.Width; h >= Value.CenterHue - Value.Width; h -= 2) context.LineTo(At(h, inner), true, false);
        }
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(210, 235, 225, 213)), 1), shape);
        foreach (double h in new[] { Value.CenterHue - Value.Width, Value.CenterHue + Value.Width }) dc.DrawEllipse(EditorUi.Accent, null, At(h, Radius), 4, 4);
        dc.DrawEllipse(EditorUi.Ink, new Pen(Brushes.White, 1), At(Value.CenterHue, Value.CenterSaturation * Radius), 5, 5);
        if (skin)
        {
            void DrawArc(bool left, double amount)
            {
                double radius = Radius + 18;
                Point P(double angle) => Center + new Vector((left ? -1 : 1) * radius * Math.Cos(angle * Math.PI / 180), -radius * Math.Sin(angle * Math.PI / 180));
                for (int a = -55; a < 55; a += 2)
                {
                    double t = (a + 55) / 110d;
                    var rgb = AdjustToolProcessor.FromHsl(Value.CenterHue, t, Value.TargetLightness);
                    var c = left ? Color.FromRgb((byte)(rgb.R * 255), (byte)(rgb.G * 255), (byte)(rgb.B * 255)) : Color.FromRgb((byte)(t * 255), (byte)(t * 255), (byte)(t * 255));
                    dc.DrawLine(new Pen(new SolidColorBrush(c), 4), P(a), P(a + 2));
                }
                var knob = P(Math.Clamp(amount, -1, 1) * 55);
                dc.DrawEllipse(Brushes.White, new Pen(Brushes.Black, 1), knob, 4, 4);
            }
            DrawArc(true, Value.Saturation >= 0 ? Value.Saturation / 200 : Value.Saturation / 100);
            DrawArc(false, Value.Lightness / 100);
        }
    }
}
