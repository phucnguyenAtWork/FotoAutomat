using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

// Giao diện khách theo thương hiệu: màu nhấn, màu nền, ảnh nền và khung xem trước.
public partial class GeneralSettingsPanel
{
    private static readonly string[] AccentPresets = ["#A94466", "#0F766E", "#1D4ED8", "#7C3AED", "#B45309", "#111827"];
    private static readonly string[] BackgroundPresets = ["#FAF3E9", "#FFFFFF", "#F3F4F6", "#ECF6F4", "#FDF2F8", "#EFF6FF"];
    // Ảnh nền đang hiện trên trang: tên tệp đã lưu, đường dẫn đầy đủ của ảnh vừa chọn, hoặc rỗng.
    private string backgroundChoice = "";
    // Ảnh màn chờ: tên tệp đã lưu, đường dẫn ảnh vừa chọn, hoặc rỗng.
    private string loadingChoice = "";

    private void BuildSwatches()
    {
        foreach (var (panel, presets, box) in new[] { (AccentSwatches, AccentPresets, AccentHex), (BackgroundSwatches, BackgroundPresets, BackgroundHex) })
        {
            panel.Children.Clear();
            foreach (string hex in presets)
            {
                var swatch = new Button { Style = (Style)FindResource("SwatchButton"), Background = ToBrush(hex), Tag = hex };
                System.Windows.Automation.AutomationProperties.SetName(swatch, hex);
                swatch.Click += (_, _) => box.Text = hex;
                panel.Children.Add(swatch);
            }
        }
    }

    private static SolidColorBrush ToBrush(string hex)
    {
        BrandColors.TryParse(hex, out var c);
        var brush = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B));
        brush.Freeze();
        return brush;
    }

    private void OnThemeTextChanged(object sender, TextChangedEventArgs e) => RefreshTheme();

    // Khung xem trước dùng màu đang nhập nếu hợp lệ, nếu không thì giữ màu mặc định; lỗi chỉ báo khi Lưu.
    private void RefreshTheme()
    {
        if (AccentSwatches is null || PreviewSurface is null) return;
        string accent = BrandColors.TryParse(AccentHex.Text.Trim(), out _) ? AccentHex.Text.Trim() : BrandColors.DefaultAccent;
        string background = BrandColors.TryParse(BackgroundHex.Text.Trim(), out _) ? BackgroundHex.Text.Trim() : BrandColors.DefaultBackground;
        var palette = BrandColors.Palette(accent);
        var backgroundBrush = ToBrush(background);
        PreviewSurface.Background = backgroundBrush;
        PreviewOverlay.Fill = backgroundBrush;
        PreviewGlow.Fill = new RadialGradientBrush(((SolidColorBrush)ToBrush(palette.Glow)).Color, Colors.Transparent);
        PreviewStart.BorderBrush = ToBrush(palette.Soft);
        PreviewStartText.Foreground = ToBrush(palette.Action);
        PreviewAction.Background = ToBrush(palette.Action);
        PreviewProgress.Children.Clear();
        for (int i = 0; i < 6; i++)
            PreviewProgress.Children.Add(new Border { Width = i == 1 ? 16 : 6, Height = 6, CornerRadius = new CornerRadius(3), Margin = new Thickness(2, 0, 2, 0), Background = ToBrush(i == 1 ? palette.Soft : "#E5D3C4") });
        MarkSelected(AccentSwatches, AccentHex.Text);
        MarkSelected(BackgroundSwatches, BackgroundHex.Text);
    }

    private void MarkSelected(Panel swatches, string text)
    {
        foreach (Button swatch in swatches.Children)
            swatch.BorderBrush = string.Equals((string)swatch.Tag, text.Trim(), StringComparison.OrdinalIgnoreCase) ? (Brush)FindResource("TextBrush") : Brushes.Transparent;
    }

    private void OnPickBackground(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Ảnh PNG hoặc JPEG|*.png;*.jpg;*.jpeg" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try
        {
            if (new FileInfo(dialog.FileName).Length > GeneralSettingsStore.MaxBackgroundBytes) throw new ArgumentException("Ảnh nền cần nhỏ hơn 10 MB.");
            if (BrandImage.Load(dialog.FileName) is null) throw new ArgumentException("Không đọc được ảnh nền.");
            backgroundChoice = dialog.FileName; Feedback.Text = "";
        }
        catch (Exception ex) { Feedback.Text = ex.Message; }
        RefreshBackground(); Recompute();
    }

    private void OnRemoveBackground(object sender, RoutedEventArgs e) { backgroundChoice = ""; Feedback.Text = ""; RefreshBackground(); Recompute(); }

    private void OnPickLoading(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Ảnh PNG hoặc JPEG|*.png;*.jpg;*.jpeg" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try
        {
            if (new FileInfo(dialog.FileName).Length > GeneralSettingsStore.MaxBackgroundBytes) throw new ArgumentException("Ảnh màn chờ cần nhỏ hơn 10 MB.");
            if (BrandImage.Load(dialog.FileName) is null) throw new ArgumentException("Không đọc được ảnh màn chờ.");
            loadingChoice = dialog.FileName; Feedback.Text = "";
        }
        catch (Exception ex) { Feedback.Text = ex.Message; }
        RefreshLoadingImage(); Recompute();
    }

    private void OnRemoveLoading(object sender, RoutedEventArgs e) { loadingChoice = ""; Feedback.Text = ""; RefreshLoadingImage(); Recompute(); }

    private void RefreshLoadingImage()
    {
        string? path = Path.IsPathRooted(loadingChoice) ? loadingChoice : store.LoadingPath(new GeneralSettings { LoadingFile = loadingChoice });
        var image = BrandImage.Load(path);
        LoadingImage.Source = image;
        LoadingEmpty.Visibility = image is null ? Visibility.Visible : Visibility.Collapsed;
        RemoveLoadingButton.IsEnabled = loadingChoice.Length > 0;
    }

    private void RefreshBackground()
    {
        string? path = Path.IsPathRooted(backgroundChoice) ? backgroundChoice : store.BackgroundPath(new GeneralSettings { BackgroundFile = backgroundChoice });
        var image = BrandImage.Load(path);
        BackgroundImage.Source = PreviewBackdrop.Source = image;
        BackgroundEmpty.Visibility = image is null ? Visibility.Visible : Visibility.Collapsed;
        PreviewBackdrop.Visibility = PreviewOverlay.Visibility = image is null ? Visibility.Collapsed : Visibility.Visible;
        RemoveBackgroundButton.IsEnabled = backgroundChoice.Length > 0;
    }
}
