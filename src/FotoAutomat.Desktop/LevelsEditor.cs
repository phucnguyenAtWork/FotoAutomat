using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

/// <summary>Capture One style Levels: histogram with input handles at the bottom (black · midtone · white) and output
/// handles at the top (black · white). Midpoint markers follow the bounds and cannot be dragged independently.
/// The input marker preserves existing Gamma (t = 0.5^Gamma). Double-click resets the channel.</summary>
public sealed class LevelsEditor : FrameworkElement
{
    private const double Pad = 10, Top = 18, Bottom = 20;
    private enum Handle { None, InBlack, InWhite, OutBlack, OutWhite }
    private LevelsAdjustment value;
    private readonly int channel;
    private readonly double[][]? histogram; // [R, G, B, Luma][256], normalised to 0..1
    private Handle dragging;
    public event Action<LevelsAdjustment>? Edited;
    public event Action? Released;
    public LevelsEditor(LevelsAdjustment value, int channel, double[][]? histogram)
    {
        this.value = value; this.channel = channel; this.histogram = histogram; Height = 160; Focusable = true; Cursor = Cursors.SizeWE;
        MouseLeftButtonDown += (_, e) => { if (e.ClickCount == 2) { Set(new()); Edited?.Invoke(this.value); Released?.Invoke(); e.Handled = true; return; } if (Begin(e.GetPosition(this))) { CaptureMouse(); e.Handled = true; } };
        MouseMove += (_, e) => { if (IsMouseCaptured) Drag(e.GetPosition(this).X); };
        MouseLeftButtonUp += (_, e) => { if (!IsMouseCaptured) return; ReleaseMouseCapture(); dragging = Handle.None; Released?.Invoke(); e.Handled = true; };
        TouchDown += (_, e) => { if (Begin(e.GetTouchPoint(this).Position)) CaptureTouch(e.TouchDevice); e.Handled = true; };
        TouchMove += (_, e) => { if (e.TouchDevice.Captured == this) Drag(e.GetTouchPoint(this).Position.X); e.Handled = true; };
        TouchUp += (_, e) => { if (e.TouchDevice.Captured != this) return; ReleaseTouchCapture(e.TouchDevice); dragging = Handle.None; Released?.Invoke(); e.Handled = true; };
    }
    public LevelsAdjustment Value => value;
    public void Set(LevelsAdjustment next) { value = next; InvalidateVisual(); }
    private double X(double level) => Pad + level / 255 * (ActualWidth - 2 * Pad);
    private double Level(double x) => Math.Clamp((x - Pad) / Math.Max(1, ActualWidth - 2 * Pad) * 255, 0, 255);
    private double MidLevel => value.InputBlack + (value.InputWhite - value.InputBlack) * Math.Pow(.5, value.Gamma);
    private bool Begin(Point p)
    {
        dragging = Handle.None;
        // Only the black and white bounds are interactive; midpoint markers are read-only.
        (Handle Kind, double X)[] candidates = p.Y < Top + 4
            ? [(Handle.OutBlack, X(value.OutputBlack)), (Handle.OutWhite, X(value.OutputWhite))]
            : [(Handle.InBlack, X(value.InputBlack)), (Handle.InWhite, X(value.InputWhite))];
        var best = candidates.OrderBy(c => Math.Abs(c.X - p.X)).First();
        if (Math.Abs(best.X - p.X) > 22) return false;
        dragging = best.Kind; return true;
    }
    private void Drag(double x)
    {
        if (dragging == Handle.None) return;
        double level = Math.Round(Level(x));
        value = dragging switch
        {
            Handle.InBlack => value with { InputBlack = Math.Min(level, value.InputWhite - 1) },
            Handle.InWhite => value with { InputWhite = Math.Max(level, value.InputBlack + 1) },
            Handle.OutBlack => value with { OutputBlack = Math.Min(level, value.OutputWhite) },
            Handle.OutWhite => value with { OutputWhite = Math.Max(level, value.OutputBlack) },
            _ => value
        };
        InvalidateVisual(); Edited?.Invoke(value);
    }
    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight, y0 = Top, y1 = h - Bottom;
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(36, 38, 39)), null, new Rect(0, 0, w, h));
        if (histogram is not null)
        {
            Color[] colors = [Color.FromRgb(235, 80, 80), Color.FromRgb(90, 210, 110), Color.FromRgb(90, 140, 245), Color.FromRgb(200, 200, 200)];
            int[] shown = channel == 0 ? [3, 0, 1, 2] : [channel - 1];
            foreach (int c in shown)
            {
                var geometry = new StreamGeometry();
                using (var g = geometry.Open())
                {
                    g.BeginFigure(new Point(X(0), y1), true, true);
                    for (int i = 0; i < 256; i++) g.LineTo(new Point(X(i), y1 - histogram[c][i] * (y1 - y0 - 4)), true, false);
                    g.LineTo(new Point(X(255), y1), true, false);
                }
                geometry.Freeze();
                bool fill = c == 3 || channel != 0;
                dc.DrawGeometry(fill ? new SolidColorBrush(Color.FromArgb(70, colors[c].R, colors[c].G, colors[c].B)) : null, new Pen(new SolidColorBrush(Color.FromArgb(220, colors[c].R, colors[c].G, colors[c].B)), 1.2), geometry);
            }
        }
        var line = new Pen(new SolidColorBrush(Color.FromRgb(150, 150, 150)), 1);
        // output → input connectors (left and right), midtone marker
        dc.DrawLine(line, new Point(X(value.OutputBlack), y0), new Point(X(value.InputBlack), y1));
        dc.DrawLine(line, new Point(X(value.OutputWhite), y0), new Point(X(value.InputWhite), y1));
        double mx = X(MidLevel), outputMid = X((value.OutputBlack + value.OutputWhite) / 2);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(225, 135, 38)), 1.2), new Point(outputMid, y0 + 6), new Point(mx, y1));
        dc.DrawEllipse(Brushes.LightGray, null, new Point(outputMid, y0 + 6), 3, 3);
        // handles: output on top (pointing down), input at the bottom (pointing up)
        Triangle(dc, X(value.OutputBlack), 2, true, Brushes.Black); Triangle(dc, X(value.OutputWhite), 2, true, Brushes.White);
        Triangle(dc, X(value.InputBlack), h - 2, false, Brushes.Black); Triangle(dc, mx, h - 2, false, Brushes.Gray); Triangle(dc, X(value.InputWhite), h - 2, false, Brushes.White);
    }
    private static void Triangle(DrawingContext dc, double x, double tipBase, bool down, Brush fill)
    {
        // 14 px wide marker; tipBase is the outer edge (y = 2 at the top, y = height − 2 at the bottom)
        var g = new StreamGeometry();
        using (var c = g.Open())
        {
            if (down) { c.BeginFigure(new Point(x - 7, tipBase), true, true); c.LineTo(new Point(x + 7, tipBase), true, false); c.LineTo(new Point(x + 7, tipBase + 8), true, false); c.LineTo(new Point(x, tipBase + 14), true, false); c.LineTo(new Point(x - 7, tipBase + 8), true, false); }
            else { c.BeginFigure(new Point(x - 7, tipBase), true, true); c.LineTo(new Point(x + 7, tipBase), true, false); c.LineTo(new Point(x + 7, tipBase - 8), true, false); c.LineTo(new Point(x, tipBase - 14), true, false); c.LineTo(new Point(x - 7, tipBase - 8), true, false); }
        }
        g.Freeze(); dc.DrawGeometry(fill, new Pen(Brushes.Gray, 1), g);
    }
    /// <summary>R, G, B and luma histograms of a BGRA image, each scaled to its highest bin (ignoring the clipped ends).</summary>
    public static double[][] Compute(byte[] bgra)
    {
        var counts = new double[4][]; for (int c = 0; c < 4; c++) counts[c] = new double[256];
        for (int i = 0; i + 3 < bgra.Length; i += 4)
        {
            byte b = bgra[i], g = bgra[i + 1], r = bgra[i + 2];
            counts[0][r]++; counts[1][g]++; counts[2][b]++; counts[3][(int)Math.Round(.2126 * r + .7152 * g + .0722 * b)]++;
        }
        foreach (var bins in counts)
        {
            // light smoothing, then normalise by the highest interior bin so a clipped spike does not flatten the rest
            var smooth = bins.Select((_, i) => (bins[Math.Max(0, i - 1)] + 2 * bins[i] + bins[Math.Min(255, i + 1)]) / 4).ToArray();
            double max = Math.Max(1, smooth.Skip(2).Take(252).Max());
            for (int i = 0; i < 256; i++) bins[i] = Math.Min(1, smooth[i] / max);
        }
        return counts;
    }
}
