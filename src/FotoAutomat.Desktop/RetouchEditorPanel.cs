using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;
using Microsoft.Win32;

namespace FotoAutomat.Desktop;
public sealed class RetouchEditorPanel : UserControl
{
    private readonly ColorPreviewSession colors;
    private readonly RetouchPreviewSession session;
    private readonly Image preview = new() { Stretch = Stretch.Uniform };
    private readonly Grid surface = new() { ClipToBounds = true, Background = new SolidColorBrush(Color.FromRgb(28, 30, 31)) };
    private readonly Canvas overlay = new() { IsHitTestVisible = false };
    private readonly StackPanel controls = new();
    private readonly WrapPanel faceCards = new();
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) };
    private readonly ColorHistogram histogram = new() { Height = 116 };
    private readonly CheckBox applyColor = new() { Content = "Apply color preset", Margin = new Thickness(6), VerticalAlignment = VerticalAlignment.Center };
    private readonly ComboBox tool = new() { ItemsSource = new[] { "Pan", "Add face", "Protect", "Erase protection" }, SelectedIndex = 0, Height = EditorUi.ControlHeight, MinHeight = EditorUi.ControlHeight, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 6), Width = 150 };
    private int selected, eyeTarget;
    private bool before, showFaces = true, showProtection = true;
    private double zoom = 1, panX, panY, brushSize = .025;
    private Point? start, previous;
    private BitmapSource? shownSource;
    private int faceCount = -1;
    public RetouchEditorPanel(ColorPreviewSession? colors = null, RetouchPreviewSession? session = null)
    {
        this.colors = colors ?? ColorPreviewSession.Shared; this.session = session ?? (colors is null ? RetouchPreviewSession.Shared : new RetouchPreviewSession(colors));
        MinHeight = 560; FontSize = 12; Background = Brushes.Transparent; UseLayoutRounding = true; SnapsToDevicePixels = true;
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/FotoAutomat.Desktop;component/CompactColorControls.xaml", UriKind.Relative) });
        var root = new Grid(); root.ColumnDefinitions.Add(new() { Width = new GridLength(2.2, GridUnitType.Star) }); root.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 340, MaxWidth = 480 });
        var left = new Grid { Margin = new Thickness(0, 0, 16, 0) }; left.RowDefinitions.Add(new() { Height = GridLength.Auto }); left.RowDefinitions.Add(new()); left.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var actions = new WrapPanel(); actions.Children.Add(Button("Đổi ảnh", Import));
        var compare = new CheckBox { Content = "Before Retouch", Margin = new Thickness(6), VerticalAlignment = VerticalAlignment.Center }; compare.Click += (_, _) => { before = compare.IsChecked == true; Refresh(); }; actions.Children.Add(compare);
        applyColor.IsChecked = this.session.UseColorPreset; applyColor.Click += (_, _) => this.session.UseColorPreset = applyColor.IsChecked == true; actions.Children.Add(applyColor);
        actions.Children.Add(Button("Fit", () => { zoom = 1; panX = panY = 0; Transform(); }));
        actions.Children.Add(Button("Undo", () => { this.session.Undo(); Build(); Refresh(); })); actions.Children.Add(Button("Redo", () => { this.session.Redo(); Build(); Refresh(); })); actions.Children.Add(tool); left.Children.Add(actions);
        EditorImageViewport.Attach(surface, preview); surface.Children.Add(overlay); Grid.SetRow(surface, 1); left.Children.Add(surface); surface.SizeChanged += (_, _) => Transform(); WireInput();
        Grid.SetRow(status, 2); left.Children.Add(status); root.Children.Add(left);
        var side = new DockPanel(); var sideCard = new Border { Background = Brushes.White, BorderBrush = EditorUi.Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10, 8, 10), Child = side }; Grid.SetColumn(sideCard, 1); root.Children.Add(sideCard); DockPanel.SetDock(histogram, Dock.Top); side.Children.Add(histogram);
        var footer = new WrapPanel(); footer.Children.Add(Button("Lưu Retouch", () => { try { this.session.Save(); } catch (Exception e) { Warn(e); } })); footer.Children.Add(Button("Reset face", () => Edit(f => f with { Settings = new() }, rebuild: true))); DockPanel.SetDock(footer, Dock.Bottom); side.Children.Add(footer);
        side.Children.Add(new ScrollViewer { Content = controls, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); Content = root;
        Loaded += (_, _) => { this.session.Changed -= Refresh; this.session.Changed += Refresh; Build(); Refresh(); }; Unloaded += (_, _) => this.session.Changed -= Refresh;
        Build(); Refresh();
    }
    private FaceRetouch? Face => selected >= 0 && selected < session.Workspace.Faces.Length ? session.Workspace.Faces[selected] : null;
    private void Warn(Exception e) => new WarningWindow(Window.GetWindow(this), e.Message).ShowDialog();
    private static Button Button(string text, Action action) => EditorUi.Button(text, action);
    private void Import()
    {
        var dialog = new OpenFileDialog { Filter = "Ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff" }; if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try { colors.LoadImage(dialog.FileName); zoom = 1; panX = panY = 0; Transform(); } catch (Exception e) { Warn(e); }
    }
    private StackPanel Group(string title, bool expanded = true) { var p = new StackPanel { Margin = new Thickness(6, 4, 6, 6) }; controls.Children.Add(new Expander { Header = title, Content = p, IsExpanded = expanded, Margin = new Thickness(0, 2, 0, 3) }); return p; }
    private void Edit(Func<FaceRetouch, FaceRetouch> edit, bool history = true, bool rebuild = false)
    {
        if (Face is not { } face) return; var faces = session.Workspace.Faces.ToArray(); faces[selected] = edit(face); session.Update(faces, history); if (rebuild) Build();
    }
    private void Slider(StackPanel group, string title, double value, double min, double max, Action<double> change)
    {
        var row = new Grid { Height = EditorUi.RowHeight }; row.ColumnDefinitions.Add(new() { Width = new GridLength(88) }); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = new GridLength(60) });
        var label = new TextBlock { Text = title, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = title };
        var slider = new Slider { Minimum = min, Maximum = max, Value = value, Height = 30, Margin = new Thickness(4, 0, 6, 0) };
        var number = new TextBox { Text = value.ToString("0.##"), Style = (Style)FindResource("CompactNumericInput") };
        System.Windows.Automation.AutomationProperties.SetName(slider, title); System.Windows.Automation.AutomationProperties.SetName(number, title + " value");
        slider.ValueChanged += (_, _) => { number.Text = slider.Value.ToString("0.##"); change(slider.Value); };
        void Commit() { if (double.TryParse(number.Text, out var x) && double.IsFinite(x) && x >= min && x <= max) { number.ClearValue(Border.BorderBrushProperty); slider.Value = x; } else number.BorderBrush = Brushes.Red; }
        number.KeyDown += (_, e) => { if (e.Key == Key.Enter) { Commit(); e.Handled = true; } }; number.LostKeyboardFocus += (_, _) => Commit();
        Grid.SetColumn(slider, 1); Grid.SetColumn(number, 2); row.Children.Add(label); row.Children.Add(slider); row.Children.Add(number); group.Children.Add(row);
    }
    private void Setting(StackPanel group, string label, double value, Func<RetouchSettings, double, RetouchSettings> change, double min = 0, double max = 100) => Slider(group, label, value, min, max, x => Edit(f => f with { Settings = change(f.Settings, x) }));
    private void Check(StackPanel group, string text, bool value, Action<bool> change) { var c = new CheckBox { Content = text, IsChecked = value, MinHeight = EditorUi.RowHeight }; c.Click += (_, _) => change(c.IsChecked == true); group.Children.Add(c); }
    private void Build()
    {
        if (faceCards.Parent is Panel previousParent) previousParent.Children.Remove(faceCards);
        selected = Math.Clamp(selected, 0, Math.Max(0, session.Workspace.Faces.Length - 1)); controls.Children.Clear();
        var selector = Group("Retouch Face Selector"); selector.Children.Add(faceCards); BuildCards();
        selector.Children.Add(Button("Detect faces", async () => { try { await session.DetectAsync(); Build(); } catch (Exception e) { Warn(e); } }));
        selector.Children.Add(Button("Add face", () => tool.SelectedIndex = 1)); Check(selector, "Show detected faces", showFaces, v => { showFaces = v; DrawOverlay(); });
        if (Face is not { } face) return;
        selector.Children.Add(Button("Delete face", () => { session.Update(session.Workspace.Faces.Where((_, i) => i != selected).ToArray()); Build(); }));
        var skin = Group("Retouch Face Skin"); var s = face.Settings;
        Setting(skin, "Impact", s.Impact, (r, v) => r with { Impact = v }); Setting(skin, "Blemishes", s.Blemishes, (r, v) => r with { Blemishes = v }); Check(skin, "Include Neck Area", s.BlemishNeck, v => Edit(f => f with { Settings = f.Settings with { BlemishNeck = v } }));
        Setting(skin, "Dark Circles", s.DarkCircles, (r, v) => r with { DarkCircles = v });
        var even = Group("Even Skin"); Setting(even, "Amount", s.EvenSkin, (r, v) => r with { EvenSkin = v }); Setting(even, "Texture", s.Texture, (r, v) => r with { Texture = v }, -100); Check(even, "Include Neck Area", s.SkinNeck, v => Edit(f => f with { Settings = f.Settings with { SkinNeck = v } })); Setting(even, "Contouring", s.Contouring, (r, v) => r with { Contouring = v });
        var teeth = Group("Retouch Teeth"); Setting(teeth, "Brightness", s.TeethBrightness, (r, v) => r with { TeethBrightness = v }); Setting(teeth, "Desaturation", s.TeethDesaturation, (r, v) => r with { TeethDesaturation = v });
        var eyes = Group("Retouch Eyes"); var targets = new ComboBox { ItemsSource = new[] { "Both", "Left", "Right" }, SelectedIndex = eyeTarget, Height = EditorUi.ControlHeight, MinHeight = EditorUi.ControlHeight, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) }; targets.SelectionChanged += (_, _) => { eyeTarget = targets.SelectedIndex; Build(); }; eyes.Children.Add(targets);
        Setting(eyes, "Iris", eyeTarget == 2 ? s.RightIris : s.LeftIris, (r, v) => r with { LeftIris = eyeTarget == 2 ? r.LeftIris : v, RightIris = eyeTarget == 1 ? r.RightIris : v }, -30, 30);
        Setting(eyes, "Sclera", eyeTarget == 2 ? s.RightSclera : s.LeftSclera, (r, v) => r with { LeftSclera = eyeTarget == 2 ? r.LeftSclera : v, RightSclera = eyeTarget == 1 ? r.RightSclera : v }, -30, 30);
        var protection = Group("Protect areas", false); protection.Children.Add(Button("Protect brush", () => tool.SelectedIndex = 2)); protection.Children.Add(Button("Erase protection", () => tool.SelectedIndex = 3));
        Slider(protection, "Brush size", brushSize * 100, 1, 15, v => brushSize = v / 100); Check(protection, "Show protection", showProtection, v => { showProtection = v; DrawOverlay(); }); protection.Children.Add(Button("Clear protection", () => Edit(f => f with { Protection = null })));
    }
    private void BuildCards()
    {
        faceCards.Children.Clear(); if (colors.Original is not { } image) return;
        for (int i = 0; i < session.Workspace.Faces.Length; i++)
        {
            var f = session.Workspace.Faces[i]; int x = Math.Clamp((int)(f.X * image.PixelWidth), 0, image.PixelWidth - 1), y = Math.Clamp((int)(f.Y * image.PixelHeight), 0, image.PixelHeight - 1);
            var crop = new CroppedBitmap(image, new Int32Rect(x, y, Math.Clamp((int)(f.Width * image.PixelWidth), 1, image.PixelWidth - x), Math.Clamp((int)(f.Height * image.PixelHeight), 1, image.PixelHeight - y)));
            var content = new StackPanel();
            var thumbnail = new Image { Source = crop, Width = 96, Height = 112, Stretch = Stretch.Uniform };
            RenderOptions.SetBitmapScalingMode(thumbnail, BitmapScalingMode.HighQuality);
            content.Children.Add(new Border { Background = EditorUi.Surface, Child = thumbnail });
            content.Children.Add(new TextBlock { Text = (i + 1).ToString(), TextAlignment = TextAlignment.Center, FontSize = 13, Margin = new Thickness(0, 4, 0, 0) }); int index = i;
            var button = Button("", () => { selected = index; Build(); Refresh(); });
            button.Height = double.NaN; button.Width = 112; button.Padding = new Thickness(6); button.VerticalContentAlignment = VerticalAlignment.Center;
            button.Content = content; button.BorderBrush = i == selected ? Brushes.Teal : Brushes.LightGray; button.BorderThickness = new Thickness(2);
            System.Windows.Automation.AutomationProperties.SetName(button, $"Chọn khuôn mặt {i + 1}");
            faceCards.Children.Add(button);
        }
    }
    private void Refresh()
    {
        if (shownSource != colors.Original || faceCount != session.Workspace.Faces.Length) { shownSource = colors.Original; faceCount = session.Workspace.Faces.Length; Build(); }
        preview.Source = before ? session.Before : session.Preview; histogram.ExposureMetadata = colors.ExposureMetadata; histogram.Update(preview.Source as BitmapSource);
        applyColor.ToolTip = "Preset: " + session.ColorPresetName;
        status.Text = session.Detecting ? "Đang nhận diện khuôn mặt…" : session.Busy ? "Đang cập nhật Retouch…" : session.Status;
        Transform();
    }
    private Rect ImageRect() => EditorImageViewport.Cover(surface, colors.Original, zoom, panX, panY);
    private void Transform()
    {
        EditorImageViewport.Apply(surface, colors.Original, ref zoom, ref panX, ref panY, 8, preview);
        DrawOverlay();
    }
    private void DrawOverlay()
    {
        overlay.Children.Clear(); var rect = ImageRect(); if (rect.IsEmpty) return;
        if (showFaces) for (int i = 0; i < session.Workspace.Faces.Length; i++)
        {
            var f = session.Workspace.Faces[i]; var box = new Border { Width = f.Width * rect.Width, Height = f.Height * rect.Height, BorderBrush = i == selected ? Brushes.Turquoise : Brushes.White, BorderThickness = new Thickness(1), Child = new TextBlock { Text = (i + 1).ToString(), Foreground = Brushes.White, Background = Brushes.Teal, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Padding = new Thickness(3) } }; Canvas.SetLeft(box, rect.X + f.X * rect.Width); Canvas.SetTop(box, rect.Y + f.Y * rect.Height); overlay.Children.Add(box);
        }
        if (Face is { } face)
        {
            if (showProtection && face.Protection?.Dabs is { Length: > 0 } && colors.Original is { } source)
            {
                var mask = face.Protection.Rasterize(source.PixelWidth, source.PixelHeight); var pixels = new byte[mask.Length * 4]; for (int i = 0; i < mask.Length; i++) { pixels[i * 4 + 2] = 255; pixels[i * 4 + 3] = (byte)(mask[i] * 100); }
                var bitmap = BitmapSource.Create(source.PixelWidth, source.PixelHeight, 96, 96, PixelFormats.Bgra32, null, pixels, source.PixelWidth * 4); var image = new Image { Source = bitmap, Width = rect.Width, Height = rect.Height }; Canvas.SetLeft(image, rect.X); Canvas.SetTop(image, rect.Y); overlay.Children.Add(image);
            }
        }
    }
    private void WireInput()
    {
        void Down(Point p) { if (!ImageRect().Contains(p)) return; start = previous = p; if (tool.SelectedIndex is 2 or 3) { session.BeginStroke(); Paint(p); } }
        void Move(Point p)
        {
            if (start is null || previous is not { } last) return;
            if (tool.SelectedIndex == 0) { panX += p.X - last.X; panY += p.Y - last.Y; Transform(); }
            else if (tool.SelectedIndex is 2 or 3) Paint(p); previous = p;
        }
        void Up(Point p)
        {
            if (start is { } origin && tool.SelectedIndex == 1)
            {
                var rect = ImageRect(); double x = Math.Clamp((Math.Min(origin.X, p.X) - rect.X) / rect.Width, 0, 1), y = Math.Clamp((Math.Min(origin.Y, p.Y) - rect.Y) / rect.Height, 0, 1), right = Math.Clamp((Math.Max(origin.X, p.X) - rect.X) / rect.Width, 0, 1), bottom = Math.Clamp((Math.Max(origin.Y, p.Y) - rect.Y) / rect.Height, 0, 1);
                if (right - x >= .01 && bottom - y >= .01) { try { session.Update(session.Workspace.Faces.Append(new FaceRetouch(x, y, right - x, bottom - y, new())).ToArray()); selected = session.Workspace.Faces.Length - 1; Build(); } catch (Exception e) { Warn(e); } }
            }
            session.EndStroke(); start = previous = null;
        }
        surface.MouseWheel += (_, e) => { zoom = Math.Clamp(zoom * (e.Delta > 0 ? 1.2 : 1 / 1.2), 1, 8); if (zoom == 1) panX = panY = 0; Transform(); e.Handled = true; };
        surface.MouseLeftButtonDown += (_, e) => { Down(e.GetPosition(surface)); surface.CaptureMouse(); e.Handled = true; };
        surface.MouseMove += (_, e) => { if (surface.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed) Move(e.GetPosition(surface)); };
        surface.MouseLeftButtonUp += (_, e) => { Up(e.GetPosition(surface)); surface.ReleaseMouseCapture(); };
        surface.TouchDown += (_, e) => { Down(e.GetTouchPoint(surface).Position); surface.CaptureTouch(e.TouchDevice); e.Handled = true; };
        surface.TouchMove += (_, e) => { if (e.TouchDevice.Captured == surface) Move(e.GetTouchPoint(surface).Position); e.Handled = true; };
        surface.TouchUp += (_, e) => { Up(e.GetTouchPoint(surface).Position); surface.ReleaseTouchCapture(e.TouchDevice); e.Handled = true; };
        surface.LostMouseCapture += (_, _) => { session.EndStroke(); start = previous = null; };
        surface.LostTouchCapture += (_, _) => { session.EndStroke(); start = previous = null; };
    }
    private void Paint(Point p)
    {
        if (Face is not { } face) return; var rect = ImageRect(); if (!rect.Contains(p)) return;
        var mask = face.Protection ?? new PhotoMask(PhotoMaskKind.Brush); if ((mask.Dabs?.Length ?? 0) > 9800) return;
        var dabs = (mask.Dabs ?? []).ToList(); var last = previous ?? p; int count = Math.Clamp((int)Math.Ceiling((p - last).Length / Math.Max(1, Math.Min(rect.Width, rect.Height) * brushSize / 4)), 1, 100);
        for (int i = 1; i <= count; i++) { var point = last + (p - last) * (i / (double)count); if (rect.Contains(point)) dabs.Add(new((point.X - rect.X) / rect.Width, (point.Y - rect.Y) / rect.Height, brushSize, .8, tool.SelectedIndex == 3)); }
        Edit(f => f with { Protection = mask with { Dabs = dabs.ToArray() } }, history: false);
    }
}
