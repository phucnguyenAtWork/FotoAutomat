using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Infrastructure;
using Microsoft.Win32;

namespace FotoAutomat.Desktop;

public partial class CameraSettingsPanel : UserControl
{
    private readonly CameraSetupStore store = new();
    private bool loading;
    private CameraSetup saved = new();
    private BoothCameraPipeline? pipeline;
    private FujifilmCameraConnection? fuji;
    private readonly Dictionary<string, ComboBox> parameterInputs = new();
    private IReadOnlyList<CameraSetting> cameraSettings = [];
    private bool previewIsLiveOrSample = true;
    private readonly System.Windows.Threading.DispatcherTimer frameTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private readonly CancellationTokenSource lifetime = new();
    private bool deviceBusy, frameBusy;
    private readonly Guid testSession = Guid.NewGuid();
    public CameraSettingsPanel()
    {
        InitializeComponent(); loading = true;
        Brand.ItemsSource = new[] { "Canon", "Fujifilm", "Nikon" };
        Rotation.ItemsSource = new[] { 0, 90, 180, 270 };
        Quality.ItemsSource = new[] { 640, 960, 1280 };
        try
        {
            saved = store.Load();
            // Preserve the existing target when this new page is first opened.
            var legacy = new CreativeDraftStore().Load().Camera;
            if (!store.Exists && legacy != "Canon EOS R50")
                saved = saved with { Brand = legacy.StartsWith("Fujifilm", StringComparison.OrdinalIgnoreCase) ? "Fujifilm" : legacy.StartsWith("Nikon", StringComparison.OrdinalIgnoreCase) ? "Nikon" : "Canon", Model = legacy };
        }
        catch (Exception e) { Feedback.Text = "Không đọc được cấu hình: " + e.Message; }
        Populate(); loading = false; ShowSettings([]);
        frameTimer.Tick += OnFrame;
        Unloaded += async (_, _) => { lifetime.Cancel(); frameTimer.Stop(); if (pipeline is not null) { try { await pipeline.DisposeAsync(); } catch (Exception e) { System.Diagnostics.Debug.WriteLine(e); } pipeline = null; } };
    }
    private void Populate()
    {
        loading = true; Brand.SelectedItem = saved.Brand; SetModels(); Model.Text = saved.Model;
        Mirror.IsChecked = saved.MirrorLiveView; Rotation.SelectedItem = saved.Rotation; Quality.SelectedItem = saved.PreviewWidth;
        SdkPath.Text = saved.SdkDirectory;
        loading = false; TransformPreview();
    }
    private void SetModels() => Model.ItemsSource = Brand.SelectedItem?.ToString() switch
    {
        "Canon" => new[] { "Canon EOS R50", "Canon EOS R100" },
        "Fujifilm" => new[] { "Fujifilm X-H2" },
        _ => Array.Empty<string>()
    };
    private void OnBrandChanged(object sender, SelectionChangedEventArgs e)
    {
        if (loading) return;
        SetModels(); Model.Text = ""; Model.SelectedIndex = Model.Items.Count > 0 ? 0 : -1;
        Feedback.Text = Brand.SelectedItem?.ToString() == "Nikon" ? "Chưa lưu · nhập model Nikon bạn dự định sử dụng." : "Chưa lưu thay đổi";
    }
    private void OnDisplayChanged(object sender, RoutedEventArgs e)
    {
        if (loading || Preview is null) return;
        if (previewIsLiveOrSample) TransformPreview(); Feedback.Text = "Chưa lưu thay đổi";
    }
    private void TransformPreview()
    {
        var transforms = new TransformGroup();
        transforms.Children.Add(new RotateTransform(Rotation.SelectedItem is int rotation ? rotation : 0));
        transforms.Children.Add(new ScaleTransform(Mirror.IsChecked == true ? -1 : 1, 1));
        Preview.LayoutTransform = transforms;
    }
    private void OnSave(object sender, RoutedEventArgs e)
    {
        try
        {
            var value = new CameraSetup { Brand = (string)Brand.SelectedItem, Model = Model.Text.Trim(), MirrorLiveView = Mirror.IsChecked == true, Rotation = (int)Rotation.SelectedItem, PreviewWidth = (int)Quality.SelectedItem, SdkDirectory = SdkPath.Text.Trim() };
            store.Save(value); saved = value;
            Feedback.Text = "Đã lưu cấu hình PC";
        }
        catch (Exception error) { Feedback.Text = error.Message; }
    }
    private void OnRevert(object sender, RoutedEventArgs e) { Populate(); Feedback.Text = "Đã khôi phục cấu hình đã lưu"; }
    private void OnSelectSdk(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Chọn thư mục Fujifilm SDK" };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) { SdkPath.Text = dialog.FolderName; Feedback.Text = "Chưa lưu đường dẫn SDK"; }
    }
    private void UpdateDeviceButtons()
    {
        var connected = pipeline?.IsReady == true;
        ConnectButton.IsEnabled = pipeline is null && !deviceBusy;
        DisconnectButton.IsEnabled = pipeline is not null && !deviceBusy;
        CaptureButton.IsEnabled = LiveButton.IsEnabled = connected && !deviceBusy;
        Brand.IsEnabled = Model.IsEnabled = SdkPath.IsEnabled = pipeline is null && !deviceBusy;
        SampleButton.IsEnabled = pipeline is null && !deviceBusy;
        SdkBrowseButton.IsEnabled = RevertButton.IsEnabled = pipeline is null && !deviceBusy;
        ReadSettingsButton.IsEnabled = connected && !deviceBusy;
        ApplySettingsButton.IsEnabled = ParameterFields.IsEnabled = connected && !deviceBusy && cameraSettings.Count > 0;
    }
    private async void OnConnect(object sender, RoutedEventArgs e)
    {
        if (deviceBusy || pipeline is not null) return;
        if (Brand.SelectedItem?.ToString() != "Fujifilm") { Feedback.Text = "Hiện chỉ có adapter Fujifilm. Chọn Fujifilm để thử USB."; return; }
        deviceBusy = true; UpdateDeviceButtons(); Feedback.Text = "Đang kết nối USB…";
        try
        {
            fuji = new FujifilmCameraConnection(SdkPath.Text.Trim());
            pipeline = new BoothCameraPipeline(fuji, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "CameraTests"));
            await pipeline.ConnectAsync(lifetime.Token);
            ConnectionStatus.Text = pipeline.Device.Model + " · USB";
            Model.Text = pipeline.Device.Model;
            await RefreshSettings();
            Feedback.Text = "Đã kết nối";
            frameTimer.Start();
        }
        catch (Exception error)
        {
            Feedback.Text = error.Message;
            if (pipeline is not null) { try { await pipeline.DisposeAsync(); } catch { } pipeline = null; }
            fuji = null; ShowSettings([]);
        }
        finally { deviceBusy = false; UpdateDeviceButtons(); }
    }
    private async void OnDisconnect(object sender, RoutedEventArgs e)
    {
        if (deviceBusy || pipeline is null) return;
        deviceBusy = true; frameTimer.Stop(); UpdateDeviceButtons();
        try { await pipeline.DisposeAsync(); Feedback.Text = "Đã ngắt kết nối"; }
        catch (Exception error) { Feedback.Text = "Lỗi đóng camera: " + error.Message; }
        finally { pipeline = null; fuji = null; ShowSettings([]); SettingsStatus.Text = "Chưa đọc thông số"; ConnectionStatus.Text = "Chưa kết nối camera"; Preview.Source = null; EmptyState.Visibility = Visibility.Visible; deviceBusy = false; UpdateDeviceButtons(); }
    }
    private void ShowCameraBytes(byte[] data, bool live)
    {
        using var stream = new MemoryStream(data);
        var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = (int)Quality.SelectedItem; image.StreamSource = stream; image.EndInit(); image.Freeze();
        Preview.Source = image; EmptyState.Visibility = Visibility.Collapsed;
        previewIsLiveOrSample = live;
        if (live) TransformPreview(); else Preview.LayoutTransform = Transform.Identity;
        PreviewLabel.Text = live ? "Live view · camera USB" : "Ảnh vừa chụp · JPEG gốc";
    }
    private async void OnFrame(object? sender, EventArgs e)
    {
        if (frameBusy || deviceBusy || pipeline?.IsReady != true) return;
        frameBusy = true;
        try { var frame = await pipeline.ReadLiveViewAsync(lifetime.Token); if (frame is not null && !deviceBusy && !lifetime.IsCancellationRequested) ShowCameraBytes(frame.Jpeg, true); }
        catch (Exception error) { frameTimer.Stop(); Feedback.Text = error.Message; ConnectionStatus.Text = "Live view đã dừng"; UpdateDeviceButtons(); }
        finally { frameBusy = false; }
    }
    private void OnLive(object sender, RoutedEventArgs e) { if (pipeline?.IsReady == true && !deviceBusy) frameTimer.Start(); }
    private async void OnCapture(object sender, RoutedEventArgs e)
    {
        if (deviceBusy || pipeline?.IsReady != true) return;
        deviceBusy = true; frameTimer.Stop(); UpdateDeviceButtons(); Feedback.Text = "Đang chụp và nhận JPEG…";
        try
        {
            var path = await pipeline.CaptureAsync(testSession, Guid.NewGuid(), lifetime.Token);
            if (!lifetime.IsCancellationRequested) { ShowCameraBytes(await File.ReadAllBytesAsync(path, lifetime.Token), false); Feedback.Text = "Đã lưu ảnh gốc: " + path; }
        }
        catch (Exception error) { Feedback.Text = error.Message; ConnectionStatus.Text = "Cần kiểm tra camera"; }
        finally { deviceBusy = false; UpdateDeviceButtons(); }
    }
    private void OnOpenSample(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Ảnh JPEG/PNG|*.jpg;*.jpeg;*.png", Title = "Ảnh kiểm tra hiển thị — không phải live view" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        try
        {
            using var stream = File.OpenRead(dialog.FileName);
            var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = (int)Quality.SelectedItem; image.StreamSource = stream; image.EndInit(); image.Freeze();
            Preview.Source = image; EmptyState.Visibility = Visibility.Collapsed;
            previewIsLiveOrSample = true;
            PreviewLabel.Text = "Ảnh kiểm tra · không phải live view";
            TransformPreview(); Feedback.Text = "Ảnh kiểm tra không được lưu vào cấu hình.";
        }
        catch (Exception error) { Feedback.Text = "Không mở được ảnh: " + error.Message; }
    }

    private void ShowSettings(IReadOnlyList<CameraSetting> settings)
    {
        cameraSettings = settings; parameterInputs.Clear(); ParameterFields.Children.Clear();
        if (settings.Count == 0)
            foreach (var label in new[] { "Chế độ chụp", "Tốc độ màn trập", "Khẩu độ", "ISO", "Cân bằng trắng", "Nhiệt độ màu" })
            {
                ParameterFields.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 12, 0, 6) });
                ParameterFields.Children.Add(new ComboBox { ItemsSource = new[] { "— Chưa đọc từ máy" }, SelectedIndex = 0, IsEnabled = false });
            }
        foreach (var setting in settings)
        {
            ParameterFields.Children.Add(new TextBlock { Text = setting.Label, Margin = new Thickness(0, 12, 0, 6) });
            var input = new ComboBox { ItemsSource = setting.Options, DisplayMemberPath = nameof(CameraSettingOption.Label),
                SelectedValuePath = nameof(CameraSettingOption.Value), SelectedValue = setting.Current,
                IsEnabled = setting.Error is null && setting.Options.Count > 0 };
            parameterInputs.Add(setting.Key, input); ParameterFields.Children.Add(input);
            if (setting.Error is not null || !setting.Options.Any(o => o.Value == setting.Current))
                ParameterFields.Children.Add(new TextBlock { Text = setting.Error is not null ? "Chưa đọc được / không khả dụng" : "Hiện tại: " + FujifilmCameraConnection.FormatSetting(setting.Key, setting.Current ?? 0),
                    TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Gray, Margin = new Thickness(0, 4, 0, 0) });
        }
    }
    private async Task RefreshSettings()
    {
        if (fuji is null) return;
        var settings = await fuji.ReadSettingsAsync(lifetime.Token);
        if (lifetime.IsCancellationRequested) return;
        ShowSettings(settings);
        SettingsStatus.Text = settings.All(s => s.Error is null) ? "Đã đọc từ camera · " + DateTime.Now.ToString("HH:mm:ss") : "Một số thông số không khả dụng.";
    }
    private async void OnReadSettings(object sender, RoutedEventArgs e)
    {
        if (deviceBusy || pipeline?.IsReady != true) return;
        deviceBusy = true; UpdateDeviceButtons();
        try { await RefreshSettings(); Feedback.Text = "Đã đọc lại thông số; bỏ các thay đổi chưa áp."; }
        catch (Exception error) { ShowSettings([]); SettingsStatus.Text = "Không đọc được thông số"; Feedback.Text = error.Message; }
        finally { deviceBusy = false; UpdateDeviceButtons(); }
    }
    private async void OnApplySettings(object sender, RoutedEventArgs e)
    {
        if (deviceBusy || pipeline?.IsReady != true || fuji is null) return;
        var changes = cameraSettings.Where(s => parameterInputs[s.Key].SelectedValue is int value && value != s.Current)
            .ToDictionary(s => s.Key, s => (int)parameterInputs[s.Key].SelectedValue);
        if (changes.Count == 0) { Feedback.Text = "Không có thông số thay đổi."; return; }
        // Check dependencies before any write; never silently discard the other edits.
        try { FujifilmCameraConnection.ValidateSettingChanges(changes); }
        catch (ArgumentException error) { Feedback.Text = error.Message; return; }
        deviceBusy = true; UpdateDeviceButtons(); Feedback.Text = "Đang áp và xác nhận thông số…";
        try
        {
            await fuji.ApplySettingsAsync(changes, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "CameraTests", "Settings"), lifetime.Token);
            await RefreshSettings(); Feedback.Text = "Đã áp lên camera và xác nhận bằng giá trị đọc lại.";
        }
        catch (Exception error)
        {
            Feedback.Text = error.Message + " Có thể đã áp một phần; hãy kiểm tra thông số đọc lại trước khi chụp.";
            try { await RefreshSettings(); } catch { ShowSettings([]); SettingsStatus.Text = "Không đọc lại được; kiểm tra kết nối camera."; }
        }
        finally { deviceBusy = false; UpdateDeviceButtons(); }
    }
    private void OnHelp(object sender, RoutedEventArgs e) => MessageBox.Show(Window.GetWindow(this),
        "1. Fujifilm: cắm một camera USB ở chế độ tether, chọn thư mục SDK rồi Kết nối.\n\n" +
        "2. Đọc từ máy lấy giá trị thực. Áp riêng mode M hoặc WB Kelvin trước; sau đó chọn ISO, tốc độ, khẩu độ, Kelvin và Áp lên máy. Thông số bị khóa có thể do mode, lens hoặc model chưa hỗ trợ.\n\n" +
        "3. Chụp thử nhận JPEG gốc. Trở lại live view để căn khung. Flash chỉ xuất hiện trong ảnh chụp thử.\n\n" +
        "4. Lưu cấu hình PC chỉ lưu thiết bị, SDK và cách hiển thị; không tự gửi lại thông số camera khi khởi động. Canon/Nikon và in test chưa tích hợp.",
        "Thiết lập máy ảnh", MessageBoxButton.OK, MessageBoxImage.Information);
}


