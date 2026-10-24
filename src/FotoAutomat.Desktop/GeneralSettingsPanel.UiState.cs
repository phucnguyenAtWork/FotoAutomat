using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FotoAutomat.Desktop;

// Trạng thái "chưa lưu" và nút Hoàn tác của thanh lưu. Chỉ đọc/ghi lại giá trị trên giao diện từ store có sẵn.
// Logo (logoChoice) nằm trong ảnh chụp trạng thái nên chọn hoặc xóa logo cũng tính là thay đổi chưa lưu.
public partial class GeneralSettingsPanel
{
    private string savedSnapshot = "";
    private bool tracking;

    private static string Join(string brand, string logo, string accent, string background, string backgroundFile, string loadingFile, bool fullScreen, bool acceleration, string data, string export, bool originals, bool prints, bool gif, bool timelapse) =>
        string.Join("|", brand.Trim(), logo, accent.Trim().ToUpperInvariant(), background.Trim().ToUpperInvariant(), backgroundFile, loadingFile, fullScreen, acceleration, data.Trim(), export.Trim(), originals, prints, gif, timelapse);

    private string Current() =>
        Join(BrandName.Text, logoChoice, AccentHex.Text, BackgroundHex.Text, backgroundChoice, loadingChoice, FullScreen.IsChecked == true, Acceleration.IsChecked == true, DataFolder.Text, ExportFolder.Text,
            Originals.IsChecked == true, Prints.IsChecked == true, Gif.IsChecked == true, Timelapse.IsChecked == true) + "|" + ModeCardsKey(ModeCardsFromFields());

    // Đọc store; fillFields = true thì đưa giá trị đã lưu lên các ô (Hoàn tác).
    private void ReadStored(bool fillFields)
    {
        var settings = store.Load();
        if (fillFields)
        {
            BrandName.Text = settings.BrandName; logoChoice = settings.LogoFile; RefreshLogo(); AccentHex.Text = settings.AccentColor; BackgroundHex.Text = settings.BackgroundColor; backgroundChoice = settings.BackgroundFile; RefreshBackground(); loadingChoice = settings.LoadingFile; RefreshLoadingImage(); FullScreen.IsChecked = settings.FullScreen; Acceleration.IsChecked = settings.HardwareAcceleration;
            DataFolder.Text = settings.DataDirectory; ExportFolder.Text = settings.ExportDirectory;
            Originals.IsChecked = settings.ExportOriginals; Prints.IsChecked = settings.ExportPrints; Gif.IsChecked = settings.ExportGif; Timelapse.IsChecked = settings.ExportTimelapse;
            BuildModeCards();
        }
        savedSnapshot = Join(settings.BrandName, settings.LogoFile, settings.AccentColor, settings.BackgroundColor, settings.BackgroundFile, settings.LoadingFile, settings.FullScreen, settings.HardwareAcceleration, settings.DataDirectory, settings.ExportDirectory,
            settings.ExportOriginals, settings.ExportPrints, settings.ExportGif, settings.ExportTimelapse) + "|" + SavedModeCardsKey();
    }

    private void Recompute()
    {
        bool dirty = Current() != savedSnapshot;
        DirtyText.Text = dirty ? "Có thay đổi chưa lưu" : "Đã lưu";
        RevertButton.IsEnabled = dirty;
    }

    private void OnPanelLoaded(object sender, RoutedEventArgs e)
    {
        if (tracking) return;
        tracking = true;
        try { ReadStored(false); }
        catch (Exception ex) { Feedback.Text = ex.Message; }
        AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) => Recompute()));
        AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler((_, _) => Recompute()));
        AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler((_, _) => Recompute()));
        // Đăng ký sau khi XAML đã gắn OnSave nên chạy sau OnSave: đọc lại store để biết lưu có thành công không.
        SaveButton.Click += (_, _) =>
        {
            try { ReadStored(false); }
            catch (Exception ex) { Feedback.Text = ex.Message; }
            Recompute();
        };
        Recompute();
    }

    private void OnRevert(object sender, RoutedEventArgs e)
    {
        try { ReadStored(true); Feedback.Text = ""; }
        catch (Exception ex) { Feedback.Text = ex.Message; }
        Recompute();
    }
}
