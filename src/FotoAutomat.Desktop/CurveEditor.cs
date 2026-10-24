using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;
public sealed class CurveEditor : FrameworkElement
{
    public ToneCurve Curve { get; private set; } = ToneCurve.Identity;
    public event Action<ToneCurve>? Edited;
    public event Action<CurvePoint?>? SelectionChanged;
    public CurvePoint? SelectedPoint => selected >= 0 && selected < Curve.Points.Length ? Curve.Points[selected] : null;
    public void SetSelectedCoordinates(double input, double output)
    {
        if (SelectedPoint is null || !double.IsFinite(input) || !double.IsFinite(output)) return;
        MoveSelected(new Point(input / 255, output / 255));
    }
    private int selected = -1;
    private Point? hover;
    private double[][] distribution = [];
    public int Channel { get; init; }
    private Rect Plot => new(8, 8, Math.Max(1, ActualWidth - 16), Math.Max(1, ActualHeight - 38));
    public void SetHistogram(double[][] values) { distribution = values; InvalidateVisual(); }
    private Point Normalized(Point p) => new(Math.Clamp((p.X - Plot.X) / Plot.Width, 0, 1), Math.Clamp(1 - (p.Y - Plot.Y) / Plot.Height, 0, 1));
    private Point Screen(double x, double y) => new(Plot.X + x * Plot.Width, Plot.Bottom - y * Plot.Height);
    public CurveEditor(ToneCurve curve)
    {
        Curve = curve; Height = 240; Focusable = true; Cursor = Cursors.Cross;
        MouseLeftButtonDown += (_, e) => { if (!Plot.Contains(e.GetPosition(this))) return; Select(e.GetPosition(this)); CaptureMouse(); e.Handled = true; };
        MouseMove += (_, e) => { var p = e.GetPosition(this); hover = Plot.Contains(p) ? Normalized(p) : null; if (IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed) Drag(p); InvalidateVisual(); };
        MouseLeave += (_, _) => { hover = null; InvalidateVisual(); };
        MouseLeftButtonUp += (_, e) => { ReleaseMouseCapture(); e.Handled = true; };
        TouchDown += (_, e) => { if (!Plot.Contains(e.GetTouchPoint(this).Position)) return; Select(e.GetTouchPoint(this).Position); CaptureTouch(e.TouchDevice); e.Handled = true; };
        TouchMove += (_, e) => { if (e.TouchDevice.Captured == this) Drag(e.GetTouchPoint(this).Position); e.Handled = true; };
        TouchUp += (_, e) => { ReleaseTouchCapture(e.TouchDevice); e.Handled = true; };
        KeyDown += (_, e) => { if (e.Key is Key.Delete or Key.Back) { RemoveSelected(); e.Handled = true; } };
    }
    public void RemoveSelected()
    {
        if (selected <= 0 || selected >= Curve.Points.Length - 1) return;
        var remaining = Curve.Points.Where((_, i) => i != selected).ToArray(); selected = -1; Set(new(remaining));
    }
    private void Select(Point position)
    {
        Focus(); selected = Array.FindIndex(Curve.Points, p => Math.Abs(Screen(p.X, p.Y).X - position.X) < 10 && Math.Abs(Screen(p.X, p.Y).Y - position.Y) < 10);
        if (selected < 0 && Curve.Points.Length < 32)
        {
            double x = Math.Clamp(Normalized(position).X, .001, .999), y = Math.Clamp(Normalized(position).Y, 0, 1);
            if (Curve.Points.Any(p => Math.Abs(p.X - x) < .005)) return;
            var points = Curve.Points.Append(new(x, y)).OrderBy(p => p.X).ToArray(); selected = Array.FindIndex(points, p => p.X == x); Set(new(points));
        }
        SelectionChanged?.Invoke(SelectedPoint); InvalidateVisual();
    }
    private void Drag(Point position)
    {
        MoveSelected(Normalized(position));
    }
    private void MoveSelected(Point value)
    {
        if (SelectedPoint is null) return;
        var points = Curve.Points.ToArray();
        double low = selected == 0 ? 0 : points[selected - 1].X + .000001;
        double high = selected == points.Length - 1 ? 1 : points[selected + 1].X - .000001;
        if (low > high) return;
        points[selected] = new(Math.Clamp(value.X, low, high), Math.Clamp(value.Y, 0, 1)); Set(new(points));
    }
    private void Set(ToneCurve value) { Curve = value; if (selected >= 0 && selected < Curve.Points.Length) hover = new Point(Curve.Points[selected].X, Curve.Points[selected].Y); InvalidateVisual(); SelectionChanged?.Invoke(SelectedPoint); Edited?.Invoke(value); }
    protected override void OnRender(DrawingContext dc)
    {
        if (ActualWidth < 20 || ActualHeight < 40) return;
        var plot = Plot;
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(37, 37, 37)), null, new Rect(RenderSize));
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(29, 29, 29)), null, plot);
        var grid = new Pen(new SolidColorBrush(Color.FromRgb(49, 49, 49)), .6);
        for (int i = 1; i < 4; i++) { dc.DrawLine(grid, Screen(i / 4d, 0), Screen(i / 4d, 1)); dc.DrawLine(grid, Screen(0, i / 4d), Screen(1, i / 4d)); }
        Brush[] colors = [Brushes.IndianRed, Brushes.DarkSeaGreen, Brushes.CornflowerBlue, Brushes.Gray];
        int[] channels = Channel == 0 ? [0, 1, 2] : [Channel == 1 ? 3 : Channel - 2];
        double max = distribution.Length == 4 ? Math.Max(1, channels.Max(c => distribution[c].Max())) : 1;
        if (distribution.Length == 4) foreach (int c in channels)
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(Screen(0, Math.Sqrt(distribution[c][0] / max) * .96), false, false);
                for (int x = 1; x < 256; x++) context.LineTo(Screen(x / 255d, Math.Sqrt(distribution[c][x] / max) * .96), true, false);
            }
            dc.DrawGeometry(null, new Pen(colors[c], .8), geometry);
        }
        dc.DrawLine(new Pen(Brushes.DimGray, .7), Screen(0, 0), Screen(1, 1));
        Brush curveColor = Channel >= 2 ? colors[Channel - 2] : Brushes.LightGray;
        dc.DrawLine(new Pen(curveColor, 1.5), Screen(0, Curve.Points[0].Y), Screen(Curve.Points[0].X, Curve.Points[0].Y));
        dc.DrawLine(new Pen(curveColor, 1.5), Screen(Curve.Points[^1].X, Curve.Points[^1].Y), Screen(1, Curve.Points[^1].Y));
        for (int i = 1; i < Curve.Points.Length; i++) dc.DrawLine(new Pen(curveColor, 1.5), Screen(Curve.Points[i - 1].X, Curve.Points[i - 1].Y), Screen(Curve.Points[i].X, Curve.Points[i].Y));
        for (int i = 0; i < Curve.Points.Length; i++)
        {
            var point = Screen(Curve.Points[i].X, Curve.Points[i].Y);
            dc.DrawRectangle(i == selected ? Brushes.DarkOrange : Brushes.Transparent, new Pen(Brushes.Peru, 1), new Rect(point.X - 3, point.Y - 3, 6, 6));
        }
        string input = "--", output = "--";
        if (hover is { } h)
        {
            double y = IsMouseCaptured && selected >= 0 ? Curve.Points[selected].Y : Curve.Evaluate(h.X);
            var point = Screen(h.X, y);
            var guide = new Pen(Brushes.Peru, .7) { DashStyle = DashStyles.Dot };
            dc.DrawLine(guide, Screen(h.X, 0), Screen(h.X, 1));
            dc.DrawLine(guide, Screen(0, y), Screen(1, y));
            dc.DrawEllipse(Brushes.DarkOrange, null, point, 3, 3);
            input = Math.Round(h.X * 255).ToString(); output = Math.Round(y * 255).ToString();
        }
        var text = new FormattedText($"Input: {input}     Output: {output}", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, Brushes.Silver, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(text, new Point(8, ActualHeight - 23));
    }
}
