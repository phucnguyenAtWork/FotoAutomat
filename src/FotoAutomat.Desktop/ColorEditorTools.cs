using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

/// <summary>Capture One style Color Editor (8 color swatches, gradient Hue/Saturation/Lightness) and Color Balance
/// (Master · 3-Way · Shadow · Midtone · Highlight wheels). Same recipe fields as before: Hsl[8] and Balance[4].</summary>
public sealed partial class ColorEditorPanel
{
    // Display colours of the 8 HSL bands, in ColorRecipe.Hsl order.
    private static readonly (string Name, double Hue)[] Bands = [("Red", 0), ("Orange", 30), ("Yellow", 58), ("Green", 120), ("Aqua", 180), ("Blue", 225), ("Purple", 270), ("Magenta", 315)];
    private static readonly string[] BalanceViews = ["Master", "3-Way", "Shadow", "Midtone", "Highlight"];
    private int colorBand, balanceView = 1; // kept across rebuilds of the controls

    private void BuildBasicColorEditor(StackPanel group)
    {
        var swatches = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 6) }; var fields = new StackPanel();
        void Build()
        {
            swatches.Children.Clear(); fields.Children.Clear(); int band = colorBand; double h = Bands[band].Hue;
            for (int i = 0; i < Bands.Length; i++)
            {
                int index = i; bool on = i == band;
                var swatch = new Button
                {
                    Width = EditorUi.ControlHeight - 6,
                    Height = EditorUi.ControlHeight - 6,
                    MinHeight = 0,
                    Padding = new Thickness(0),
                    Margin = new Thickness(1),
                    ToolTip = Bands[i].Name,
                    Background = Brushes.Transparent,
                    BorderBrush = on ? EditorUi.Ink : Brushes.Transparent,
                    BorderThickness = new Thickness(2),
                    Content = new Border { Width = 22, Height = 22, CornerRadius = new CornerRadius(4), Background = Solid(Bands[i].Hue, .75, .88) }
                };
                AutomationProperties.SetName(swatch, Bands[i].Name);
                swatch.Click += (_, _) => { colorBand = index; Build(); }; swatches.Children.Add(swatch);
            }
            var v = CurrentRecipe.Hsl?[band] ?? new();
            AddSlider("Hue", -100, 100, v.Hue, (r, x) => WithHsl(r, band, a => a with { Hue = x }), fields, track: Gradient((h - 40, .8, .9), (h, .8, .9), (h + 40, .8, .9)));
            AddSlider("Saturation", -100, 100, v.Saturation, (r, x) => WithHsl(r, band, a => a with { Saturation = x }), fields, track: Gradient((h, 0, .78), (h, .9, .9)));
            AddSlider("Lightness", -100, 100, v.Lightness, (r, x) => WithHsl(r, band, a => a with { Lightness = x }), fields, track: Gradient((h, .8, .2), (h, .8, .9), (h, .2, 1)));
            fields.Children.Add(EditorUi.Button("Đặt lại " + Bands[band].Name, () => { UpdateRecipe(WithHsl(CurrentRecipe, band, _ => new())); Build(); }));
        }
        group.Children.Add(swatches); group.Children.Add(fields); Build();
    }

    private void BuildColorBalance()
    {
        var group = Group("Color Balance"); var tabs = new System.Windows.Controls.Primitives.UniformGrid { Rows = 1, Margin = new Thickness(0, 0, 0, 8) }; var body = new StackPanel();
        void Build()
        {
            tabs.Children.Clear(); body.Children.Clear();
            for (int i = 0; i < BalanceViews.Length; i++)
            {
                int index = i; bool on = i == balanceView;
                tabs.Children.Add(Tab(BalanceViews[i], on, () => { balanceView = index; Build(); }));
            }
            if (balanceView == 1)
            {
                // 3-Way: Midtone on top, Shadow and Highlight below (Capture One layout)
                var top = Wheel(2, 128, "Midtone"); top.HorizontalAlignment = HorizontalAlignment.Center; body.Children.Add(top);
                var row = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2 }; row.Children.Add(Wheel(1, 118, "Shadow")); row.Children.Add(Wheel(3, 118, "Highlight")); body.Children.Add(row);
                AddSlider("Blending", 0, 100, CurrentRecipe.GradingBlending, (r, v) => r with { GradingBlending = v }, body);
                AddSlider("Balance", -100, 100, CurrentRecipe.GradingBalance, (r, v) => r with { GradingBalance = v }, body);
                body.Children.Add(EditorUi.Button("Đặt lại 3-Way", () => { var r = CurrentRecipe; for (int z = 1; z <= 3; z++) r = WithBalance(r, z, _ => new()); UpdateRecipe(r); Build(); }));
            }
            else
            {
                int zone = balanceView == 0 ? 0 : balanceView - 1; // Master = Global (0); Shadow/Midtone/Highlight = 1/2/3
                var v = CurrentRecipe.Balance?[zone] ?? new();
                var wheel = new ColorGradingWheel(v.Hue, v.Amount, v.Lightness, 190) { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 8) };
                wheel.Edited += (hue, amount, light) => UpdateRecipe(WithBalance(CurrentRecipe, zone, g => g with { Hue = hue, Amount = amount, Lightness = light }));
                wheel.Released += Build; body.Children.Add(wheel);
                ColorRecipe Edit(ColorRecipe r, Func<ColorGrade, ColorGrade> change) { var next = WithBalance(r, zone, change); var g = next.Balance![zone]; wheel.Set(g.Hue, g.Amount, g.Lightness); return next; }
                AddSlider("Hue", 0, 360, v.Hue, (r, x) => Edit(r, g => g with { Hue = x }), body, track: Gradient((0, .8, .9), (60, .8, .9), (120, .8, .9), (180, .8, .9), (240, .8, .9), (300, .8, .9), (360, .8, .9)));
                AddSlider("Saturation", 0, 100, v.Amount, (r, x) => Edit(r, g => g with { Amount = x }), body, track: Gradient((v.Hue, 0, .78), (v.Hue, .9, .9)));
                AddSlider("Lightness", -100, 100, v.Lightness, (r, x) => Edit(r, g => g with { Lightness = x }), body, track: Gradient((0, 0, .15), (0, 0, 1)));
                body.Children.Add(EditorUi.Button("Đặt lại " + BalanceViews[balanceView], () => { UpdateRecipe(WithBalance(CurrentRecipe, zone, _ => new())); Build(); }));
            }
        }
        group.Children.Add(tabs); group.Children.Add(body); Build();
    }

    /// <summary>Capture One style text tab with an accent underline when selected.</summary>
    private static Button Tab(string label, bool on, Action click)
    {
        var text = new StackPanel(); text.Children.Add(new TextBlock { Text = label, HorizontalAlignment = HorizontalAlignment.Center, Foreground = on ? EditorUi.Accent : EditorUi.Muted, FontWeight = on ? FontWeights.SemiBold : FontWeights.Normal });
        text.Children.Add(new Border { Height = 2, Margin = new Thickness(4, 4, 4, 0), Background = on ? EditorUi.Accent : Brushes.Transparent });
        var tab = new Button { Content = text, Height = EditorUi.ControlHeight, MinHeight = EditorUi.ControlHeight, Padding = new Thickness(0), Margin = new Thickness(0), FontSize = 12, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent };
        tab.Click += (_, _) => click(); return tab;
    }

    private static readonly string[] LevelChannels = ["RGB", "Red", "Green", "Blue"];
    private int levelChannel; // kept across rebuilds
    private (object? Source, double[][] Bins)? levelHistogram;

    /// <summary>Levels (Capture One layout): output black/white on top, input black · midtone · input white below the
    /// histogram; every handle is draggable and every value can be typed.</summary>
    private void BuildLevelsTool()
    {
        var group = Group("Levels"); var body = new StackPanel(); group.Children.Add(body);
        void Build()
        {
            body.Children.Clear(); int ch = levelChannel; var v = CurrentRecipe.Levels?[ch] ?? new();
            var editor = new LevelsEditor(v, ch, Histogram()) { Margin = new Thickness(0, 4, 0, 4) };
            void Apply(LevelsAdjustment next) { editor.Set(next); UpdateRecipe(WithLevel(CurrentRecipe, ch, _ => next)); Sync(next); }
            TextBox Box(string tip) { var box = new TextBox { Style = (Style)FindResource("CompactNumericInput"), Width = 64, ToolTip = tip }; AutomationProperties.SetName(box, tip); return box; }
            TextBox outBlack = Box("Output black"), outWhite = Box("Output white"), inBlack = Box("Input black"), mid = Box("Midtone (gamma; 1 = không đổi, lớn hơn 1 sáng hơn)"), inWhite = Box("Input white");
            mid.TextAlignment = TextAlignment.Center;
            void Sync(LevelsAdjustment x)
            {
                var c = System.Globalization.CultureInfo.CurrentCulture;
                outBlack.Text = x.OutputBlack.ToString("0", c); outWhite.Text = x.OutputWhite.ToString("0", c); inBlack.Text = x.InputBlack.ToString("0", c); inWhite.Text = x.InputWhite.ToString("0", c); mid.Text = x.Gamma.ToString("0.00", c);
            }
            void Commit(TextBox box, Func<LevelsAdjustment, double, LevelsAdjustment> change)
            {
                void Do() { if (double.TryParse(box.Text, out double n) && double.IsFinite(n)) Apply(change(editor.Value, n)); else Sync(editor.Value); }
                box.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) { Do(); e.Handled = true; } }; box.LostKeyboardFocus += (_, _) => Do();
            }
            Commit(outBlack, (x, n) => x with { OutputBlack = Math.Clamp(Math.Round(n), 0, x.OutputWhite) });
            Commit(outWhite, (x, n) => x with { OutputWhite = Math.Clamp(Math.Round(n), x.OutputBlack, 255) });
            Commit(inBlack, (x, n) => x with { InputBlack = Math.Clamp(Math.Round(n), 0, x.InputWhite - 1) });
            Commit(inWhite, (x, n) => x with { InputWhite = Math.Clamp(Math.Round(n), x.InputBlack + 1, 255) });
            Commit(mid, (x, n) => x with { Gamma = Math.Clamp(Math.Round(n, 2), .1, 5) });
            editor.Edited += next => { UpdateRecipe(WithLevel(CurrentRecipe, ch, _ => next)); Sync(next); };
            Sync(v);
            var header = new Grid(); header.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); header.ColumnDefinitions.Add(new()); header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
            var tabs = new System.Windows.Controls.Primitives.UniformGrid { Rows = 1, Margin = new Thickness(6, 0, 6, 0) };
            for (int i = 0; i < LevelChannels.Length; i++) { int index = i; tabs.Children.Add(Tab(LevelChannels[i], i == ch, () => { levelChannel = index; Build(); })); }
            Grid.SetColumn(tabs, 1); Grid.SetColumn(outWhite, 2); header.Children.Add(outBlack); header.Children.Add(tabs); header.Children.Add(outWhite);
            var footer = new Grid(); footer.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); footer.ColumnDefinitions.Add(new()); footer.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
            mid.HorizontalAlignment = HorizontalAlignment.Center; Grid.SetColumn(mid, 1); Grid.SetColumn(inWhite, 2); footer.Children.Add(inBlack); footer.Children.Add(mid); footer.Children.Add(inWhite);
            body.Children.Add(header); body.Children.Add(editor); body.Children.Add(footer);
            body.Children.Add(EditorUi.Button("Đặt lại " + LevelChannels[ch], () => Apply(new())));
        }
        Build();
    }
    // Histogram of the source image (the input to Levels), cached per image.
    private double[][]? Histogram()
    {
        if (session.Original is not System.Windows.Media.Imaging.BitmapSource source) return null;
        if (levelHistogram is { } cached && ReferenceEquals(cached.Source, source)) return cached.Bins;
        var bgra = source.Format == PixelFormats.Bgra32 ? source : new System.Windows.Media.Imaging.FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[bgra.PixelWidth * bgra.PixelHeight * 4]; bgra.CopyPixels(pixels, bgra.PixelWidth * 4, 0);
        var bins = LevelsEditor.Compute(pixels); levelHistogram = (source, bins); return bins;
    }

    private FrameworkElement Wheel(int zone, double diameter, string label)
    {
        var v = CurrentRecipe.Balance?[zone] ?? new();
        var wheel = new ColorGradingWheel(v.Hue, v.Amount, v.Lightness, diameter) { HorizontalAlignment = HorizontalAlignment.Center };
        AutomationProperties.SetName(wheel, label);
        wheel.Edited += (hue, amount, light) => UpdateRecipe(WithBalance(CurrentRecipe, zone, g => g with { Hue = hue, Amount = amount, Lightness = light }));
        var stack = new StackPanel { Margin = new Thickness(2, 0, 2, 8) };
        stack.Children.Add(new TextBlock { Text = label, HorizontalAlignment = HorizontalAlignment.Center, Foreground = EditorUi.Muted, Margin = new Thickness(0, 0, 0, 4) });
        stack.Children.Add(wheel); return stack;
    }
    private static SolidColorBrush Solid(double h, double s, double v) { var b = new SolidColorBrush(ToColor(h, s, v)); b.Freeze(); return b; }
    private static Color ToColor(double h, double s, double v) { var (r, g, b) = ColorGradingWheel.Hsv(h, s, v); return Color.FromRgb((byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255)); }
    private static LinearGradientBrush Gradient(params (double H, double S, double V)[] stops)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, .5), EndPoint = new Point(1, .5) };
        for (int i = 0; i < stops.Length; i++) brush.GradientStops.Add(new GradientStop(ToColor(stops[i].H, stops[i].S, stops[i].V), stops.Length == 1 ? 0 : (double)i / (stops.Length - 1)));
        brush.Freeze(); return brush;
    }
}
