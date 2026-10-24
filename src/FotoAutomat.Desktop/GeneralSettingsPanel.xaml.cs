using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

public partial class GeneralSettingsPanel : UserControl
{
    private readonly GeneralSettingsStore store = new();
    // Logo đang hiện trên trang: tên tệp đã lưu, đường dẫn đầy đủ của ảnh vừa chọn (chép vào thư mục quản trị khi Lưu), hoặc rỗng.
    private string logoChoice = "";
    public GeneralSettingsPanel()
    {
        InitializeComponent();
        try { var settings = store.Load(); BrandName.Text = settings.BrandName; logoChoice = settings.LogoFile; AccentHex.Text = settings.AccentColor; BackgroundHex.Text = settings.BackgroundColor; backgroundChoice = settings.BackgroundFile; loadingChoice = settings.LoadingFile; FullScreen.IsChecked = settings.FullScreen; Acceleration.IsChecked = settings.HardwareAcceleration; DataFolder.Text = settings.DataDirectory; ExportFolder.Text = settings.ExportDirectory; Originals.IsChecked = settings.ExportOriginals; Prints.IsChecked = settings.ExportPrints; Gif.IsChecked = settings.ExportGif; Timelapse.IsChecked = settings.ExportTimelapse; }
        catch (Exception ex) { Feedback.Text = PinFeedback.Text = ex.Message; }
        BuildSwatches(); BuildModeCards(); RefreshPin(); RefreshLogo(); RefreshBackground(); RefreshLoadingImage(); RefreshTheme();
        ColorPreviewSession.Shared.Changed += RefreshModeCardPreviews;
        Unloaded += (_, _) => ColorPreviewSession.Shared.Changed -= RefreshModeCardPreviews;
        Loaded += (_, _) => { ColorPreviewSession.Shared.Changed -= RefreshModeCardPreviews; ColorPreviewSession.Shared.Changed += RefreshModeCardPreviews; };
    }
    private void RefreshPin() { PinStatus.Text = store.HasPin ? "Đã thiết lập PIN" : "Chưa thiết lập PIN"; CurrentPinPanel.Visibility = store.HasPin ? Visibility.Visible : Visibility.Collapsed; }
    private void OnSavePin(object sender, RoutedEventArgs e)
    {
        try { if (NewPin.Password != ConfirmPin.Password) throw new ArgumentException("PIN nhập lại chưa khớp."); store.SetPin(NewPin.Password, CurrentPin.Password); PinFeedback.Text = "Đã lưu PIN."; RefreshPin(); }
        catch (Exception ex) { PinFeedback.Text = ex.Message; }
        finally { CurrentPin.Clear(); NewPin.Clear(); ConfirmPin.Clear(); }
    }
    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog(); if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        if ((sender as FrameworkElement)?.Tag as string == "data") DataFolder.Text = dialog.FolderName; else ExportFolder.Text = dialog.FolderName;
    }
    private void OnPickLogo(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Ảnh PNG hoặc JPEG|*.png;*.jpg;*.jpeg" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try
        {
            if (new FileInfo(dialog.FileName).Length > GeneralSettingsStore.MaxLogoBytes) throw new ArgumentException("Logo cần nhỏ hơn 5 MB.");
            if (BrandImage.Load(dialog.FileName) is null) throw new ArgumentException("Không đọc được ảnh logo.");
            logoChoice = dialog.FileName; Feedback.Text = "";
        }
        catch (Exception ex) { Feedback.Text = ex.Message; }
        RefreshLogo(); Recompute();
    }
    private void OnRemoveLogo(object sender, RoutedEventArgs e) { logoChoice = ""; Feedback.Text = ""; RefreshLogo(); Recompute(); }
    private void RefreshLogo()
    {
        string? path = Path.IsPathRooted(logoChoice) ? logoChoice : store.LogoPath(new GeneralSettings { LogoFile = logoChoice });
        var image = BrandImage.Load(path);
        LogoImage.Source = PreviewHeaderLogo.Source = PreviewTitleLogo.Source = image;
        var withLogo = image is null ? Visibility.Collapsed : Visibility.Visible;
        var withoutLogo = image is null ? Visibility.Visible : Visibility.Collapsed;
        LogoImage.Visibility = PreviewHeaderLogo.Visibility = PreviewTitleLogo.Visibility = withLogo;
        LogoEmpty.Visibility = PreviewHeaderName.Visibility = PreviewTitleName.Visibility = withoutLogo;
        RemoveLogoButton.IsEnabled = logoChoice.Length > 0;
    }
    private void OnSave(object sender, RoutedEventArgs e)
    {
        var imported = new List<string>();
        try
        {
            string logoFile = logoChoice, backgroundFile = backgroundChoice, loadingFile = loadingChoice;
            if (Path.IsPathRooted(logoChoice)) imported.Add(logoFile = store.ImportLogo(logoChoice));
            if (Path.IsPathRooted(backgroundChoice)) imported.Add(backgroundFile = store.ImportBackground(backgroundChoice));
            if (Path.IsPathRooted(loadingChoice)) imported.Add(loadingFile = store.ImportLoading(loadingChoice));
            store.Save(new() { BrandName = BrandName.Text.Trim(), LogoFile = logoFile, AccentColor = AccentHex.Text.Trim(), BackgroundColor = BackgroundHex.Text.Trim(), BackgroundFile = backgroundFile, LoadingFile = loadingFile, FullScreen = FullScreen.IsChecked == true, HardwareAcceleration = Acceleration.IsChecked == true, DataDirectory = DataFolder.Text.Trim(), ExportDirectory = ExportFolder.Text.Trim(), ExportOriginals = Originals.IsChecked == true, ExportPrints = Prints.IsChecked == true, ExportGif = Gif.IsChecked == true, ExportTimelapse = Timelapse.IsChecked == true });
            SaveModeCards();
            logoChoice = logoFile; backgroundChoice = backgroundFile; loadingChoice = loadingFile; RefreshLogo(); RefreshBackground(); RefreshLoadingImage();
            var saved = store.Load(); AccentHex.Text = saved.AccentColor; BackgroundHex.Text = saved.BackgroundColor;
            Feedback.Text = "Đã lưu. Hiển thị áp dụng khi mở lại giao diện khách.";
        }
        catch (Exception ex)
        {
            foreach (string name in imported) store.DiscardImage(name);
            Feedback.Text = ex.Message;
        }
    }
}
