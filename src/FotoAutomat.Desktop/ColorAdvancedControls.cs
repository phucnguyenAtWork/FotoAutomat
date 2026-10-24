using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

public sealed partial class ColorEditorPanel
{
    private CurveEditor? activeCurveEditor;
    private int curveChannel;
    private int selectedLayer = -1;
    private bool showMask, painting, erase;
    private double brushRadius = .05, brushStrength = .7;
    private Point? lastPaint;
    private readonly Image maskOverlay = new() { IsHitTestVisible = false, Stretch = Stretch.Uniform };
    private ColorRecipe CurrentRecipe => CurrentLayer?.Recipe ?? session.Workspace.Looks[selected];
    private PhotoAdjustmentLayer? CurrentLayer => selectedLayer >= 0 && session.Workspace.Looks[selected].Layers is { } layers && selectedLayer < layers.Length ? layers[selectedLayer] : null;
    private void UpdateRecipe(ColorRecipe value)
    {
        if (selectedLayer < 0) session.Update(selected, value);
        else { var root = session.Workspace.Looks[selected]; var layers = root.Layers!.ToArray(); layers[selectedLayer] = layers[selectedLayer] with { Recipe = value }; session.Update(selected, root with { Layers = layers }); }
    }
    private void UpdateLayer(PhotoAdjustmentLayer value)
    {
        var root = session.Workspace.Looks[selected]; var layers = root.Layers!.ToArray(); layers[selectedLayer] = value; session.Update(selected, root with { Layers = layers });
    }
    private StackPanel Group(string title, bool expanded = false)
    {
        var panel = new StackPanel { Margin = new Thickness(6, 4, 6, 6) };
        var expander = new Expander { Style = (Style)FindResource("AdjustGroupExpander"), Tag = title, Header = new TextBlock { Text = title, Margin = new Thickness(0), FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center }, MinHeight = EditorUi.ControlHeight, VerticalContentAlignment = VerticalAlignment.Center, Content = panel, IsExpanded = expanded, Padding = new Thickness(4), Margin = new Thickness(0, 1, 0, 3), FontSize = EditorUi.TextSize };
        var host = new Grid(); host.Children.Add(expander);
        if (title != "Layers / Masks")
        {
            var reset = new Button { Style = (Style)FindResource("AdjustResetButton"), Margin = new Thickness(0, 1, 0, 0), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
            var icon = new System.Windows.Shapes.Path { Width = 14, Height = 14, Data = Geometry.Parse("M 3,6 C 3,0 13,0 13,7 C 13,11 10,13 7,13 M 0,4 L 3,7 L 6,4"), StrokeThickness = 1.5, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round };
            icon.SetBinding(System.Windows.Shapes.Path.StrokeProperty, new System.Windows.Data.Binding("Foreground") { Source = reset });
            reset.Content = icon;
            System.Windows.Automation.AutomationProperties.SetName(reset, "Reset " + title);
            reset.Click += (_, e) => { ResetAdjustGroup(title); e.Handled = true; };
            host.Children.Add(reset);
        }
        controls.Children.Add(host); return panel;
    }
    private void ResetAdjustGroup(string title)
    {
        var states = controls.Children.OfType<Grid>().SelectMany(g => g.Children.OfType<Expander>()).ToDictionary(e => (string)e.Tag, e => e.IsExpanded);
        double offset = toolScroll.VerticalOffset;
        var r = CurrentRecipe;
        var next = title switch
        {
            "White Balance" => r with { WhiteBalance = new(), Temperature = null, Warmth = 0, Tint = 0 },
            "Exposure" => r with { Exposure = 0, Contrast = 0, Brightness = 0, Saturation = 0 },
            "High Dynamic Range" => r with { Highlights = 0, Shadows = 0, Whites = 0, Blacks = 0 },
            "Levels" => r with { Levels = null },
            "Tone Curve" => r with { Curves = null, CurveHighlights = 0, CurveLights = 0, CurveDarks = 0, CurveShadows = 0 },
            "Color Editor" => r with { Hsl = null, AdvancedColors = null, SkinColor = null, SkinColors = null },
            "Color Balance" => r with { Balance = null, GradingBlending = 50, GradingBalance = 0 },
            "Black & White" => r with { Monochrome = false, BlackWhite = null },
            "Blur & Glow" => r with { BlurGlow = null },
            "Clarity" => r with { Clarity = 0, Structure = 0, ClarityMethod = ClarityMethod.Classic },
            "Dehaze" => r with { Dehaze = 0, DehazeShadowMode = ShadowToneMode.Auto, DehazeShadowHue = 0, DehazeShadowSaturation = 0 },
            "Vignetting" => r with { Vignette = 0, VignetteEv = null, VignetteMethod = VignetteMethod.EllipticOnCrop },
            _ => r
        };
        UpdateRecipe(next); BuildControls();
        foreach (var e in controls.Children.OfType<Grid>().SelectMany(g => g.Children.OfType<Expander>()))
            if (states.TryGetValue((string)e.Tag, out bool open)) e.IsExpanded = open;
        toolScroll.UpdateLayout(); toolScroll.ScrollToVerticalOffset(offset);
    }
    private static ComboBox Choice(StackPanel panel, string[] options, Action<int> changed)
    {
        var combo = EditorUi.Combo(options); panel.Children.Add(combo);
        combo.SelectionChanged += (_, _) => changed(combo.SelectedIndex); return combo;
    }
    private void BuildAdvancedControls()
    {
        BuildLevelsTool();
        var curves = Group("Tone Curve"); var curveFields = new StackPanel();
        void BuildCurve()
        {
            curveFields.Children.Clear(); var editor = new CurveEditor(CurrentRecipe.Curves?[curveChannel] ?? ToneCurve.Identity) { Channel = curveChannel };
            activeCurveEditor = editor; editor.SetHistogram(histogram.Snapshot());
            editor.Edited += value => { var values = CurrentRecipe.Curves?.ToArray() ?? Enumerable.Range(0, 5).Select(_ => ToneCurve.Identity).ToArray(); values[curveChannel] = value; UpdateRecipe(CurrentRecipe with { Curves = values }); };
            curveFields.Children.Add(editor);
            curveFields.Children.Add(CurveCoordinates(editor));
            curveFields.Children.Add(Button("Xóa điểm", editor.RemoveSelected));
            curveFields.Children.Add(Button("Đặt lại kênh", () => { var values = CurrentRecipe.Curves?.ToArray() ?? Enumerable.Range(0, 5).Select(_ => ToneCurve.Identity).ToArray(); values[curveChannel] = ToneCurve.Identity; UpdateRecipe(CurrentRecipe with { Curves = values }); BuildCurve(); }));
        }
        AddSlider("Highlights", -100, 100, CurrentRecipe.CurveHighlights, (r, v) => r with { CurveHighlights = v }, curves);
        AddSlider("Lights", -100, 100, CurrentRecipe.CurveLights, (r, v) => r with { CurveLights = v }, curves);
        AddSlider("Darks", -100, 100, CurrentRecipe.CurveDarks, (r, v) => r with { CurveDarks = v }, curves);
        AddSlider("Shadows", -100, 100, CurrentRecipe.CurveShadows, (r, v) => r with { CurveShadows = v }, curves);
        var tabs = new System.Windows.Controls.Primitives.UniformGrid { Columns = 5, Margin = new Thickness(0, 4, 0, 4) };
        string[] names = ["RGB", "Luma", "Red", "Green", "Blue"];
        void UpdateTabs() { for (int i = 0; i < tabs.Children.Count; i++) { var tab = (Button)tabs.Children[i]; tab.Foreground = i == curveChannel ? EditorUi.Accent : EditorUi.Muted; tab.BorderBrush = i == curveChannel ? EditorUi.Accent : Brushes.Transparent; } }
        for (int i = 0; i < names.Length; i++)
        {
            int channel = i;
            var tab = new Button { Content = names[i], Background = Brushes.Transparent, BorderThickness = new Thickness(0, 0, 0, 2), Padding = new Thickness(0), MinHeight = 32, FontSize = 12 };
            tab.Click += (_, _) => { curveChannel = channel; UpdateTabs(); BuildCurve(); }; tabs.Children.Add(tab);
        }
        UpdateTabs(); curves.Children.Insert(0, tabs); curves.Children.Insert(1, curveFields); BuildCurve();
        BuildColorEditor();
        BuildColorBalance();

    }
    private static ColorRecipe WithLevel(ColorRecipe r, int i, Func<LevelsAdjustment, LevelsAdjustment> edit) { var a = r.Levels?.ToArray() ?? Enumerable.Range(0, 4).Select(_ => new LevelsAdjustment()).ToArray(); a[i] = edit(a[i]); return r with { Levels = a }; }
    private static ColorRecipe WithHsl(ColorRecipe r, int i, Func<HslAdjustment, HslAdjustment> edit) { var a = r.Hsl?.ToArray() ?? Enumerable.Range(0, 8).Select(_ => new HslAdjustment()).ToArray(); a[i] = edit(a[i]); return r with { Hsl = a }; }
    private static ColorRecipe WithBalance(ColorRecipe r, int i, Func<ColorGrade, ColorGrade> edit) { var a = r.Balance?.ToArray() ?? Enumerable.Range(0, 4).Select(_ => new ColorGrade()).ToArray(); a[i] = edit(a[i]); return r with { Balance = a }; }
    private void BuildLayerControls()
    {
        var group = Group("Layers / Masks", selectedLayer >= 0); var layers = session.Workspace.Looks[selected].Layers ?? [];
        var selector = EditorUi.Combo(new[] { "Toàn ảnh" }.Concat(layers.Select(v => (v.Enabled ? "● " : "○ ") + v.Name)), selectedLayer + 1);
        selector.SelectionChanged += (_, _) => { selectedLayer = selector.SelectedIndex - 1; painting = false; BuildControls(); Refresh(); }; group.Children.Add(selector);
        group.Children.Add(Button("+ Thêm lớp chỉnh", () =>
        {
            if (layers.Length >= 16) { new WarningWindow(Window.GetWindow(this), "Tối đa 16 lớp chỉnh ảnh.").ShowDialog(); return; }
            var root = session.Workspace.Looks[selected]; session.Update(selected, root with { Layers = (root.Layers ?? []).Append(new PhotoAdjustmentLayer($"Layer {layers.Length + 1}", new(), new(PhotoMaskKind.Full))).ToArray() }); selectedLayer = layers.Length; BuildControls(); Refresh();
        }));
        if (CurrentLayer is not { } layer) return;
        var enabled = EditorUi.Check("Bật lớp", layer.Enabled); enabled.Click += (_, _) => UpdateLayer(CurrentLayer! with { Enabled = enabled.IsChecked == true }); group.Children.Add(enabled);
        int editingLayer = selectedLayer;
        var name = new TextBox { Text = layer.Name, MaxLength = 80, Height = EditorUi.ControlHeight, VerticalContentAlignment = VerticalAlignment.Center, Padding = new Thickness(6, 0, 6, 0), Margin = new Thickness(0, 0, 0, 6) }; name.LostKeyboardFocus += (_, _) => { if (editingLayer == selectedLayer && CurrentLayer is { } current && !string.IsNullOrWhiteSpace(name.Text)) UpdateLayer(current with { Name = name.Text.Trim() }); }; group.Children.Add(name);
        AddSlider("Opacity", 0, 100, layer.Opacity * 100, (r, x) => { UpdateLayer(CurrentLayer! with { Opacity = x / 100 }); return r; }, group);
        var maskKind = EditorUi.Combo(["Toàn ảnh", "Brush", "Radial", "Gradient"], (int)layer.Mask.Kind);
        maskKind.SelectionChanged += (_, _) => { UpdateLayer(CurrentLayer! with { Mask = CurrentLayer!.Mask with { Kind = (PhotoMaskKind)maskKind.SelectedIndex } }); painting = false; BuildControls(); Refresh(); }; group.Children.Add(maskKind);
        var overlay = EditorUi.Check("Hiện mask", showMask); overlay.Click += (_, _) => { showMask = overlay.IsChecked == true; RefreshMask(); }; group.Children.Add(overlay);
        var invert = EditorUi.Check("Đảo mask", layer.Mask.Inverted); invert.Click += (_, _) => UpdateLayer(CurrentLayer! with { Mask = CurrentLayer!.Mask with { Inverted = invert.IsChecked == true } }); group.Children.Add(invert);
        if (layer.Mask.Kind == PhotoMaskKind.Brush)
        {
            var draw = EditorUi.Check("Vẽ mask", painting); draw.Click += (_, _) => { painting = draw.IsChecked == true; original = false; }; group.Children.Add(draw);
            var eraser = EditorUi.Check("Tẩy", erase); eraser.Click += (_, _) => erase = eraser.IsChecked == true; group.Children.Add(eraser);
            AddSlider("Brush size", 1, 40, brushRadius * 100, (r, v) => { brushRadius = v / 100; return r; }, group);
            AddSlider("Brush flow", 1, 100, brushStrength * 100, (r, v) => { brushStrength = v / 100; return r; }, group);
            group.Children.Add(Button("Xóa mask", () => UpdateLayer(CurrentLayer! with { Mask = CurrentLayer!.Mask with { Dabs = [] } })));
        }
        if (layer.Mask.Kind is PhotoMaskKind.Radial or PhotoMaskKind.Linear)
        {
            AddSlider("Center X", 0, 100, layer.Mask.CenterX * 100, (r, v) => { UpdateLayer(CurrentLayer! with { Mask = CurrentLayer!.Mask with { CenterX = v / 100 } }); return r; }, group);
            AddSlider("Center Y", 0, 100, layer.Mask.CenterY * 100, (r, v) => { UpdateLayer(CurrentLayer! with { Mask = CurrentLayer!.Mask with { CenterY = v / 100 } }); return r; }, group);
            AddSlider("Radius", 1, 100, layer.Mask.Radius * 100, (r, v) => { UpdateLayer(CurrentLayer! with { Mask = CurrentLayer!.Mask with { Radius = v / 100 } }); return r; }, group);
            if (layer.Mask.Kind == PhotoMaskKind.Linear) AddSlider("Angle", -180, 180, layer.Mask.Angle, (r, v) => { UpdateLayer(CurrentLayer! with { Mask = CurrentLayer!.Mask with { Angle = v } }); return r; }, group);
        }
        if (layer.Mask.Kind != PhotoMaskKind.Full) AddSlider("Feather", 1, 100, layer.Mask.Feather * 100, (r, v) => { UpdateLayer(CurrentLayer! with { Mask = CurrentLayer!.Mask with { Feather = v / 100 } }); return r; }, group);
        group.Children.Add(Button("Lên trên", () => MoveLayer(-1))); group.Children.Add(Button("Xuống dưới", () => MoveLayer(1)));
        group.Children.Add(Button("Xóa lớp", () => { var root = session.Workspace.Looks[selected]; session.Update(selected, root with { Layers = root.Layers!.Where((_, i) => i != selectedLayer).ToArray() }); selectedLayer = -1; painting = false; BuildControls(); Refresh(); }));
    }
    private void MoveLayer(int direction)
    {
        var root = session.Workspace.Looks[selected]; var layers = root.Layers!.ToArray(); int next = selectedLayer + direction; if (next < 0 || next >= layers.Length) return;
        (layers[next], layers[selectedLayer]) = (layers[selectedLayer], layers[next]); selectedLayer = next; session.Update(selected, root with { Layers = layers }); BuildControls(); Refresh();
    }
    private Rect ImageRect(FrameworkElement surface) => EditorImageViewport.Cover(surface, session.Original, zoom, panX, panY);
    private void WireMaskInput(FrameworkElement surface)
    {
        surface.MouseLeftButtonDown += (_, e) => { if (!painting) return; lastPaint = null; Paint(e.GetPosition(surface), surface); surface.CaptureMouse(); e.Handled = true; };
        surface.MouseMove += (_, e) => { if (painting && surface.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed) Paint(e.GetPosition(surface), surface); };
        surface.MouseLeftButtonUp += (_, _) => { surface.ReleaseMouseCapture(); lastPaint = null; };
        surface.TouchDown += (_, e) => { if (!painting) return; lastPaint = null; Paint(e.GetTouchPoint(surface).Position, surface); surface.CaptureTouch(e.TouchDevice); e.Handled = true; };
        surface.TouchMove += (_, e) => { if (painting && e.TouchDevice.Captured == surface) Paint(e.GetTouchPoint(surface).Position, surface); e.Handled = true; };
        surface.TouchUp += (_, e) => { surface.ReleaseTouchCapture(e.TouchDevice); lastPaint = null; };
    }
    private void Paint(Point point, FrameworkElement surface)
    {
        var rect = ImageRect(surface); if (!rect.Contains(point) || CurrentLayer is not { Mask.Kind: PhotoMaskKind.Brush } layer) return;
        var dabs = (layer.Mask.Dabs ?? []).ToList(); if (dabs.Count >= 9900) return;
        var current = new Point((point.X - rect.X) / rect.Width, (point.Y - rect.Y) / rect.Height);
        var previous = lastPaint ?? current;
        double distance = Math.Sqrt(Math.Pow((current.X - previous.X) * rect.Width, 2) + Math.Pow((current.Y - previous.Y) * rect.Height, 2));
        int steps = Math.Clamp((int)Math.Ceiling(distance / Math.Max(1, brushRadius * Math.Min(rect.Width, rect.Height) * .25)), 1, 100);
        for (int i = 1; i <= steps; i++) dabs.Add(new(previous.X + (current.X - previous.X) * i / steps, previous.Y + (current.Y - previous.Y) * i / steps, brushRadius, brushStrength, erase));
        lastPaint = current; UpdateLayer(layer with { Mask = layer.Mask with { Dabs = dabs.ToArray() } });
    }
    private void RefreshMask()
    {
        if (!showMask || CurrentLayer is not { } layer || session.Original is not { } source) { maskOverlay.Source = null; return; }
        var mask = layer.Mask.Rasterize(source.PixelWidth, source.PixelHeight); var pixels = new byte[mask.Length * 4];
        for (int i = 0; i < mask.Length; i++) { pixels[i * 4 + 2] = 255; pixels[i * 4 + 3] = (byte)(mask[i] * 110); }
        var bitmap = BitmapSource.Create(source.PixelWidth, source.PixelHeight, 96, 96, PixelFormats.Bgra32, null, pixels, source.PixelWidth * 4); bitmap.Freeze(); maskOverlay.Source = bitmap;
    }
}
