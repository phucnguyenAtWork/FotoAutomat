using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;
using Microsoft.Win32;
namespace FotoAutomat.Desktop;

public sealed class RefineEditorPanel : UserControl
{
    private readonly ColorPreviewSession colors;
    private readonly RefinePreviewSession session;
    private readonly Grid surface = new() { ClipToBounds = true, Background = new SolidColorBrush(Color.FromRgb(28, 30, 31)) };
    private readonly Image preview = new() { Stretch = Stretch.Uniform };
    private readonly Canvas overlay = new() { IsHitTestVisible = false };
    private readonly StackPanel controls = new();
    private readonly Image navigator = new() { Height = 126, Stretch = Stretch.Uniform };
    private readonly Canvas navigatorOverlay = new() { IsHitTestVisible = false };
    private readonly Grid navigatorSurface = new() { Height = 126, Background = Brushes.DarkSlateGray };
    private readonly Image focus = new() { Width = 180, Height = 120, Stretch = Stretch.Uniform };
    private readonly ColorHistogram histogram = new() { Height = 116 };
    private readonly TextBlock status = new() { Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock zoomLabel = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5) };
    private readonly CheckBox colorToggle = new() { Content = "Apply color preset", Margin = new Thickness(6), VerticalAlignment = VerticalAlignment.Center };
    private bool before, dustMode, showSpots = true;
    private double zoom = 1, panX, panY, spotRadius = .015;
    private Point? previous;
    private BitmapSource? shownSource;
    private readonly Dictionary<string, bool> groupStates = new();
    public RefineEditorPanel(ColorPreviewSession? colors = null, RefinePreviewSession? session = null)
    {
        this.colors = colors ?? ColorPreviewSession.Shared; this.session = session ?? (colors is null ? RefinePreviewSession.Shared : new RefinePreviewSession(colors));
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/FotoAutomat.Desktop;component/CompactColorControls.xaml", UriKind.Relative) });
        MinHeight = 560; Background = Brushes.Transparent; FontSize = 12; UseLayoutRounding = true; SnapsToDevicePixels = true;
        var root = new Grid(); root.ColumnDefinitions.Add(new() { Width = new GridLength(2.2, GridUnitType.Star) }); root.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 340, MaxWidth = 480 });
        var left = new Grid { Margin = new Thickness(0, 0, 16, 0) }; left.RowDefinitions.Add(new() { Height = GridLength.Auto }); left.RowDefinitions.Add(new()); left.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var actions = new WrapPanel(); actions.Children.Add(Button("Đổi ảnh", Import));
        var compare = new CheckBox { Content = "Before Refine", Margin = new Thickness(6), VerticalAlignment = VerticalAlignment.Center }; compare.Click += (_, _) => { before = compare.IsChecked == true; Refresh(); }; actions.Children.Add(compare);
        colorToggle.IsChecked = this.session.UseColorPreset; colorToggle.Click += (_, _) => this.session.UseColorPreset = colorToggle.IsChecked == true; actions.Children.Add(colorToggle);
        actions.Children.Add(Button("Fit", () => { zoom = 1; panX = panY = 0; Transform(); })); actions.Children.Add(Button("100%", ActualPixels));
        actions.Children.Add(Button("Undo", () => { this.session.Undo(); Build(); })); actions.Children.Add(Button("Redo", () => { this.session.Redo(); Build(); })); actions.Children.Add(zoomLabel); left.Children.Add(actions);
        EditorImageViewport.Attach(surface, preview); surface.Children.Add(overlay); Grid.SetRow(surface, 1); left.Children.Add(surface); surface.SizeChanged += (_, _) => Transform(); WireInput();
        Grid.SetRow(status, 2); left.Children.Add(status); root.Children.Add(left);
        var side = new DockPanel(); var sideCard = new Border { Background = Brushes.White, BorderBrush = EditorUi.Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10, 8, 10), Child = side }; Grid.SetColumn(sideCard, 1); root.Children.Add(sideCard); DockPanel.SetDock(histogram, Dock.Top); side.Children.Add(histogram);
        var footer = new WrapPanel(); footer.Children.Add(Button("Lưu Refine", () => { try { this.session.Save(); } catch (Exception e) { Warn(e); } })); footer.Children.Add(Button("Reset Refine", () => { this.session.Update(new()); Build(); })); DockPanel.SetDock(footer, Dock.Bottom); side.Children.Add(footer);
        side.Children.Add(new ScrollViewer { Content = controls, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        navigatorSurface.Children.Add(navigator); navigatorSurface.Children.Add(navigatorOverlay); navigatorSurface.SizeChanged += (_, _) => DrawNavigator();
        navigatorSurface.MouseLeftButtonDown += (_, e) => { Navigate(e.GetPosition(navigatorSurface)); navigatorSurface.CaptureMouse(); };
        navigatorSurface.MouseMove += (_, e) => { if (navigatorSurface.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed) Navigate(e.GetPosition(navigatorSurface)); }; navigatorSurface.MouseLeftButtonUp += (_, _) => navigatorSurface.ReleaseMouseCapture();
        navigatorSurface.TouchDown += (_, e) => { Navigate(e.GetTouchPoint(navigatorSurface).Position); e.Handled = true; };
        Content = root; Loaded += (_, _) => { this.session.Changed -= Refresh; this.session.Changed += Refresh; Build(); Refresh(); }; Unloaded += (_, _) => this.session.Changed -= Refresh;
        Build(); Refresh();
    }
    private static Button Button(string text, Action action) => EditorUi.Button(text, action);
    private void Warn(Exception e) => new WarningWindow(Window.GetWindow(this), e.Message).ShowDialog();
    private void Import()
    {
        var dialog = new OpenFileDialog { Filter = "Ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff" }; if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try { colors.LoadImage(dialog.FileName); } catch (Exception e) { Warn(e); }
    }
    private StackPanel Group(string title, bool open = true)
    {
        var content = new StackPanel { Margin = new Thickness(6, 4, 6, 6) }; controls.Children.Add(new Expander { Header = title, Content = content, IsExpanded = groupStates.GetValueOrDefault(title, open), Margin = new Thickness(0, 2, 0, 3) }); return content;
    }
    private void Slider(StackPanel group, string name, double value, double min, double max, Action<double> change)
    {
        var row = new Grid { Height = EditorUi.RowHeight }; row.ColumnDefinitions.Add(new() { Width = new GridLength(88) }); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = new GridLength(60) });
        var label = new TextBlock { Text = name, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = name };
        var slider = new Slider { Minimum = min, Maximum = max, Value = value, IsSnapToTickEnabled = false, Margin = new Thickness(4, 0, 6, 0) };
        var number = new TextBox { Text = value.ToString("0.##"), Style = (Style)FindResource("CompactNumericInput") };
        System.Windows.Automation.AutomationProperties.SetName(slider, name); System.Windows.Automation.AutomationProperties.SetName(number, name + " value");
        slider.ValueChanged += (_, _) => { number.Text = slider.Value.ToString("0.##"); change(slider.Value); };
        void Commit() { if (double.TryParse(number.Text, out var v) && double.IsFinite(v) && v >= min && v <= max) { number.ClearValue(Border.BorderBrushProperty); slider.Value = v; } else number.BorderBrush = Brushes.Red; }
        number.KeyDown += (_, e) => { if (e.Key == Key.Enter) { Commit(); e.Handled = true; } }; number.LostKeyboardFocus += (_, _) => Commit();
        Grid.SetColumn(slider, 1); Grid.SetColumn(number, 2); row.Children.Add(label); row.Children.Add(slider); row.Children.Add(number); group.Children.Add(row);
    }
    private void Setting(StackPanel group, string name, double value, Func<RefineSettings, double, RefineSettings> change, double min = 0, double max = 100) => Slider(group, name, value, min, max, v => session.Update(change(session.Workspace.Settings, v)));
    private void Build()
    {
        foreach (var group in controls.Children.OfType<Expander>()) groupStates[group.Header.ToString()!] = group.IsExpanded;
        if (navigatorSurface.Parent is Panel oldNavigator) oldNavigator.Children.Remove(navigatorSurface);
        if (focus.Parent is Panel oldFocus) oldFocus.Children.Remove(focus);
        controls.Children.Clear(); var s = session.Workspace.Settings;
        Group("Navigator").Children.Add(navigatorSurface);
        var focusGroup = Group("Focus", false); focusGroup.Children.Add(focus); focusGroup.Children.Add(Button("100% · preview pixels", ActualPixels));
        var sharp = Group("Sharpening"); Setting(sharp, "Amount", s.SharpenAmount, (r, v) => r with { SharpenAmount = v }, 0, 500); Setting(sharp, "Radius", s.Radius, (r, v) => r with { Radius = v }, .1, 3); Setting(sharp, "Threshold", s.Threshold, (r, v) => r with { Threshold = v }, 0, 20); Setting(sharp, "Halo Suppress.", s.HaloSuppression, (r, v) => r with { HaloSuppression = v });
        var noise = Group("Noise Reduction", false); Setting(noise, "Luminance", s.LuminanceNoise, (r, v) => r with { LuminanceNoise = v }); Setting(noise, "Color", s.ColorNoise, (r, v) => r with { ColorNoise = v }); Setting(noise, "Detail", s.Detail, (r, v) => r with { Detail = v });
        var grain = Group("Film Grain", false); var type = new ComboBox { ItemsSource = new[] { "Fine Grain", "Soft Grain", "Coarse Grain" }, SelectedIndex = (int)s.GrainType, Height = EditorUi.ControlHeight, MinHeight = EditorUi.ControlHeight, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) }; type.SelectionChanged += (_, _) => session.Update(session.Workspace.Settings with { GrainType = (FilmGrainType)type.SelectedIndex }); grain.Children.Add(type); Setting(grain, "Impact", s.GrainImpact, (r, v) => r with { GrainImpact = v }); Setting(grain, "Granularity", s.Granularity, (r, v) => r with { Granularity = v });
        var dust = Group("Dust Removal", false); dust.Children.Add(Button(dustMode ? "Done · Remove Dust" : "Remove Dust · click spots", () => { dustMode = !dustMode; surface.Cursor = dustMode ? Cursors.Cross : Cursors.Hand; Build(); })); Slider(dust, "Spot size (%)", spotRadius * 100, .2, 10, v => spotRadius = v / 100);
        var show = new CheckBox { Content = "Show dust spots", IsChecked = showSpots, MinHeight = EditorUi.RowHeight }; show.Click += (_, _) => { showSpots = show.IsChecked == true; DrawOverlay(); }; dust.Children.Add(show);
        dust.Children.Add(Button("Clear dust spots", () => { session.Update(session.Workspace.Settings with { DustSpots = null }); DrawOverlay(); }));
        var fringe = Group("Purple Fringing", false); Setting(fringe, "Amount", s.PurpleFringing, (r, v) => r with { PurpleFringing = v });
        var moire = Group("Moiré", false); Setting(moire, "Amount", s.MoireAmount, (r, v) => r with { MoireAmount = v }); Setting(moire, "Pattern", s.MoirePattern, (r, v) => r with { MoirePattern = v }, 1, 32);
    }
    private Rect ImageRect() => EditorImageViewport.Cover(surface, colors.Original, zoom, panX, panY);
    private void ActualPixels()
    {
        if (colors.Original is not { } b || surface.ActualWidth == 0) return;
        zoom = 1 / Math.Max(surface.ActualWidth / b.PixelWidth, surface.ActualHeight / b.PixelHeight); panX = panY = 0; Transform();
    }
    private void Transform()
    {
        EditorImageViewport.Apply(surface, colors.Original, ref zoom, ref panX, ref panY, 16, preview);
        if (colors.Original is { } b) zoomLabel.Text = $"{Math.Max(surface.ActualWidth / b.PixelWidth, surface.ActualHeight / b.PixelHeight) * zoom * 100:0}%";
        DrawOverlay(); DrawNavigator(); UpdateFocus();
    }
    private void Refresh()
    {
        if (shownSource != colors.Original) { shownSource = colors.Original; zoom = 1; panX = panY = 0; Build(); }
        preview.Source = before ? session.Before : session.Preview; navigator.Source = preview.Source; histogram.ExposureMetadata = colors.ExposureMetadata; histogram.Update(preview.Source as BitmapSource);
        colorToggle.ToolTip = "Preset: " + colors.Published.Looks[colors.Published.DefaultIndex].Name;
        status.Text = session.Busy ? "Đang cập nhật Refine…" : session.Status; Transform();
    }
    private void DrawOverlay()
    {
        overlay.Children.Clear(); var rect = ImageRect(); if (rect.IsEmpty || !showSpots) return;
        foreach (var spot in session.Workspace.Settings.DustSpots ?? [])
        {
            double size = spot.Radius * Math.Min(rect.Width, rect.Height) * 2;
            var circle = new System.Windows.Shapes.Ellipse { Width = size, Height = size, Stroke = Brushes.Turquoise, StrokeThickness = 1 }; Canvas.SetLeft(circle, rect.X + spot.X * rect.Width - size / 2); Canvas.SetTop(circle, rect.Y + spot.Y * rect.Height - size / 2); overlay.Children.Add(circle);
        }
    }
    private Rect NavigatorRect()
    {
        if (colors.Original is not { } b || navigatorSurface.ActualWidth == 0) return Rect.Empty;
        double fit = Math.Min(navigatorSurface.ActualWidth / b.PixelWidth, navigatorSurface.ActualHeight / b.PixelHeight), w = b.PixelWidth * fit, h = b.PixelHeight * fit;
        return new((navigatorSurface.ActualWidth - w) / 2, (navigatorSurface.ActualHeight - h) / 2, w, h);
    }
    private void DrawNavigator()
    {
        navigatorOverlay.Children.Clear(); var rect = ImageRect(); var nav = NavigatorRect(); if (rect.IsEmpty || nav.IsEmpty) return;
        var visible = Rect.Intersect(rect, new Rect(0, 0, surface.ActualWidth, surface.ActualHeight)); if (visible.IsEmpty) return;
        var box = new Border { Width = visible.Width / rect.Width * nav.Width, Height = visible.Height / rect.Height * nav.Height, BorderBrush = Brushes.Turquoise, BorderThickness = new Thickness(1) };
        Canvas.SetLeft(box, nav.X + (visible.X - rect.X) / rect.Width * nav.Width); Canvas.SetTop(box, nav.Y + (visible.Y - rect.Y) / rect.Height * nav.Height); navigatorOverlay.Children.Add(box);
    }
    private void Navigate(Point p)
    {
        var nav = NavigatorRect(); var rect = ImageRect(); if (nav.IsEmpty || rect.IsEmpty || !nav.Contains(p)) return;
        panX = (.5 - (p.X - nav.X) / nav.Width) * rect.Width; panY = (.5 - (p.Y - nav.Y) / nav.Height) * rect.Height; Transform();
    }
    private void UpdateFocus()
    {
        if (preview.Source is not BitmapSource b) { focus.Source = null; return; } var rect = ImageRect(); if (rect.IsEmpty) return;
        int w = Math.Min(180, b.PixelWidth), h = Math.Min(120, b.PixelHeight);
        int x = Math.Clamp((int)((surface.ActualWidth / 2 - rect.X) / rect.Width * b.PixelWidth) - w / 2, 0, b.PixelWidth - w), y = Math.Clamp((int)((surface.ActualHeight / 2 - rect.Y) / rect.Height * b.PixelHeight) - h / 2, 0, b.PixelHeight - h);
        focus.Source = new CroppedBitmap(b, new Int32Rect(x, y, w, h));
    }
    private void Down(Point p)
    {
        var rect = ImageRect(); if (rect.IsEmpty || !rect.Contains(p)) return;
        if (dustMode)
        {
            try { session.Update(session.Workspace.Settings with { DustSpots = (session.Workspace.Settings.DustSpots ?? []).Append(new DustSpot((p.X - rect.X) / rect.Width, (p.Y - rect.Y) / rect.Height, spotRadius)).ToArray() }); } catch (Exception e) { Warn(e); }
        }
        else previous = p;
    }
    private void Move(Point p) { if (previous is not { } last) return; panX += p.X - last.X; panY += p.Y - last.Y; previous = p; Transform(); }
    private void WireInput()
    {
        surface.Cursor = Cursors.Hand;
        surface.MouseWheel += (_, e) => { zoom = Math.Clamp(zoom * (e.Delta > 0 ? 1.2 : 1 / 1.2), 1, 16); Transform(); e.Handled = true; };
        surface.MouseLeftButtonDown += (_, e) => { Down(e.GetPosition(surface)); surface.CaptureMouse(); e.Handled = true; }; surface.MouseMove += (_, e) => { if (surface.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed) Move(e.GetPosition(surface)); }; surface.MouseLeftButtonUp += (_, _) => { previous = null; surface.ReleaseMouseCapture(); }; surface.LostMouseCapture += (_, _) => previous = null;
        surface.TouchDown += (_, e) => { Down(e.GetTouchPoint(surface).Position); surface.CaptureTouch(e.TouchDevice); e.Handled = true; }; surface.TouchMove += (_, e) => { if (e.TouchDevice.Captured == surface) Move(e.GetTouchPoint(surface).Position); e.Handled = true; }; surface.TouchUp += (_, e) => { previous = null; surface.ReleaseTouchCapture(e.TouchDevice); e.Handled = true; }; surface.LostTouchCapture += (_, _) => previous = null;
    }
}
