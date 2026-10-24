using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

public sealed partial class ColorEditorPanel
{
    private Action<Point>? colorPick;
    private Button? colorPickButton;
    private Action? refreshColorSample;
    private readonly Dictionary<(string Look, int Layer, bool Skin, int Range), (object? Source, Color Color, Point Point)> colorSamples = new();

    private void CancelColorPick()
    {
        colorPick = null;
        preview.Cursor = null;
        if (colorPickButton is { } button) button.Background = Brushes.Transparent;
        colorPickButton = null;
    }

    private void WireColorPicker(FrameworkElement surface)
    {
        void Pick(Point p)
        {
            if (UnmaskedColorPreview is not BitmapSource bitmap || colorPick is null) return;
            double width = preview.ActualWidth, height = preview.ActualHeight;
            if (width <= 0 || height <= 0) return;
            var rect = new Rect((preview.ActualWidth - width) / 2, (preview.ActualHeight - height) / 2, width, height);
            if (!rect.Contains(p)) return;
            var point = new Point((p.X - rect.X) / width, (p.Y - rect.Y) / height);
            var action = colorPick;
            CancelColorPick();
            action(point);
        }
        surface.PreviewMouseLeftButtonDown += (_, e) => { if (colorPick is null) return; Pick(e.GetPosition(preview)); e.Handled = true; };
        surface.PreviewTouchDown += (_, e) => { if (colorPick is null) return; Pick(e.GetTouchPoint(preview).Position); e.Handled = true; };
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape && colorPick is not null) { CancelColorPick(); e.Handled = true; } };
        Unloaded += (_, _) => CancelColorPick();
    }

    private static Color SampleColor(BitmapSource bitmap, Point point)
    {
        var source = bitmap.Format == PixelFormats.Bgra32 ? bitmap : new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        int x = Math.Clamp((int)(point.X * source.PixelWidth), 0, source.PixelWidth - 1);
        int y = Math.Clamp((int)(point.Y * source.PixelHeight), 0, source.PixelHeight - 1);
        byte[] pixel = new byte[4]; source.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
        return Color.FromRgb(pixel[2], pixel[1], pixel[0]);
    }

    // Colour a range appended to this list receives at the Color Editor stage (after Retouch, tone, curves, HSL, balance
    // and the ranges before it). Falls back to the preview only when no sample is loaded.
    private Color StageColor(bool skin, int index, Point point)
    {
        if (RangeStage(skin, index) is { } stage)
        {
            int x = Math.Clamp((int)(point.X * stage.Width), 0, stage.Width - 1), y = Math.Clamp((int)(point.Y * stage.Height), 0, stage.Height - 1);
            int i = (y * stage.Width + x) * 4;
            return Color.FromRgb(stage.Pixels[i + 2], stage.Pixels[i + 1], stage.Pixels[i]);
        }
        return UnmaskedColorPreview is BitmapSource bitmap ? SampleColor(bitmap, point) : Colors.Gray;
    }

    private Button ColorPickerButton(bool skin, Action rebuild)
    {
        var button = Button("", () => { });
        button.Width = 32; button.Height = 32; button.Padding = new Thickness(5);
        button.Content = new System.Windows.Shapes.Path { Data = Geometry.Parse("M 3,12 L 11,4 L 15,8 L 7,16 L 2,17 Z M 10,3 L 12,1 L 18,7 L 16,9 Z M 4,12 L 7,15"), Stroke = EditorUi.Ink, StrokeThickness = 1.5, Width = 20, Height = 20 };
        System.Windows.Automation.AutomationProperties.SetName(button, skin ? "Hút màu Skin Tone" : "Hút màu Advanced");
        button.IsEnabled = Ranges(CurrentRecipe, skin).Length < 32;
        button.Click += (_, _) =>
        {
            bool wasActive = colorPickButton == button; CancelColorPick();
            if (wasActive) return;
            painting = false; colorPickButton = button; button.Background = new SolidColorBrush(Color.FromRgb(230, 244, 242)); preview.Cursor = Cursors.Cross;
            colorPick = point =>
            {
                var recipe = CurrentRecipe;
                var ranges = Ranges(recipe, skin);
                if (ranges.Length >= 32) return;
                var color = StageColor(skin, ranges.Length, point);
                var (h, s, l) = AdjustToolProcessor.ToHsl(color.R / 255d, color.G / 255d, color.B / 255d);
                var picked = (skin ? new ColorRangeAdjustment(Width: 20, SaturationRadius: .4, Smoothness: 25) : new ColorRangeAdjustment()) with { CenterHue = h, CenterSaturation = s, TargetLightness = l };
                int index = ranges.Length;
                ranges = ranges.Append(picked).ToArray();
                if (skin) { skinRange = index; recipe = recipe with { SkinColor = null, SkinColors = ranges }; }
                else { advancedRange = index; recipe = recipe with { AdvancedColors = ranges }; }
                colorSamples[(recipe.PresetId, selectedLayer, skin, index)] = (session.Original, color, point);
                UpdateRecipe(recipe); rebuild();
            };
        };
        return button;
    }

    private FrameworkElement ColorSampleReadout(bool skin)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 8, 0, 8) };
        var chips = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2 };
        var sampled = new Border { Height = 28, Margin = new Thickness(0, 0, 3, 4) };
        var current = new Border { Height = 28, Margin = new Thickness(3, 0, 0, 4) };
        chips.Children.Add(sampled); chips.Children.Add(current); panel.Children.Add(chips);
        var values = new Grid { Margin = new Thickness(0, 3, 0, 0) };
        foreach (double width in new[] { 24d, 1, 1, 18, 24, 1, 1 })
            values.ColumnDefinitions.Add(new() { Width = width == 1 ? new GridLength(1, GridUnitType.Star) : new GridLength(width) });
        for (int row = 0; row < 3; row++) values.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var numbers = new TextBlock[6, 2];
        string[] names = ["R", "G", "B", "H", "S", "L"];
        for (int i = 0; i < names.Length; i++)
        {
            int row = i % 3, column = i < 3 ? 0 : 4;
            var label = new TextBlock { Text = names[i] + ":", FontSize = 12, Margin = new Thickness(0, 2, 0, 2) };
            Grid.SetRow(label, row); Grid.SetColumn(label, column); values.Children.Add(label);
            for (int j = 0; j < 2; j++)
            {
                var number = new TextBlock { Text = "—", FontSize = 12, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 2, 6, 2) };
                System.Windows.Automation.AutomationProperties.SetName(number, names[i] + (j == 0 ? " mẫu đã chọn" : " preview"));
                Grid.SetRow(number, row); Grid.SetColumn(number, column + j + 1); values.Children.Add(number); numbers[i, j] = number;
            }
        }
        panel.Children.Add(values);
        void RefreshSample()
        {
            if (!colorSamples.TryGetValue((CurrentRecipe.PresetId, selectedLayer, skin, RangeIndex(skin)), out var sample) || !ReferenceEquals(sample.Source, session.Original) || UnmaskedColorPreview is not BitmapSource bitmap)
            { foreach (var number in numbers) number.Text = "—"; sampled.Background = current.Background = Brushes.Transparent; return; }
            var a = sample.Color; var b = SampleColor(bitmap, sample.Point);
            var ah = AdjustToolProcessor.ToHsl(a.R / 255d, a.G / 255d, a.B / 255d); var bh = AdjustToolProcessor.ToHsl(b.R / 255d, b.G / 255d, b.B / 255d);
            sampled.Background = new SolidColorBrush(a); current.Background = new SolidColorBrush(b);
            double[] left = [a.R, a.G, a.B, ah.H, ah.S * 100, ah.L * 100];
            double[] right = [b.R, b.G, b.B, bh.H, bh.S * 100, bh.L * 100];
            for (int i = 0; i < 6; i++) { numbers[i, 0].Text = left[i].ToString("0.#"); numbers[i, 1].Text = right[i].ToString("0.#"); }
        }
        refreshColorSample = RefreshSample; RefreshSample(); return panel;
    }
}
