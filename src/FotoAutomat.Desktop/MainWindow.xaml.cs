using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
using System.Diagnostics;
using System.IO;

namespace FotoAutomat.Desktop;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public System.Windows.Media.Imaging.BitmapSource? SelectedColorImage => CameraMode
        ? (flow.Screen == CustomerScreen.Capture && !flow.IsReviewingCapture ? cameraPreview : capturedPreviews.ElementAtOrDefault(flow.Screen == CustomerScreen.Capture ? flow.CapturedCount - 1 : flow.SelectedPhoto - 1))
        : (flow.Screen == CustomerScreen.Capture ? ColorPreviewSession.Shared.LivePreviews : ColorPreviewSession.Shared.PublishedPreviews).ElementAtOrDefault(flow.FilterIndex);
    // Thương hiệu của bên mua key: logo nếu đã chọn, nếu không thì tên thương hiệu.
    public string BrandTitle { get; private set; } = "Foto Hunter";
    public System.Windows.Media.Imaging.BitmapSource? BrandLogo { get; private set; }
    public bool HasBrandLogo => BrandLogo is not null;
    public bool HasNoBrandLogo => BrandLogo is null;
    public string BrandFooter => BrandTitle.ToUpper(CultureInfo.CurrentCulture) + " PHOTOBOOTH";
    private string previewNoticeText = "";
    private Brush brandBackground = Brushes.Transparent;

    // Màu và nền theo thương hiệu. Các màu nhấn của CustomerTheme được đổi tại chỗ để mọi StaticResource dùng chúng đổi theo.
    private void ApplyBrandTheme(GeneralSettings settings, string? backgroundPath)
    {
        BrandPalette palette;
        string background;
        try { palette = BrandColors.Palette(settings.AccentColor); background = BrandColors.Normalize(settings.BackgroundColor); }
        catch (ArgumentException) { palette = BrandColors.Palette(BrandColors.DefaultAccent); background = BrandColors.DefaultBackground; }
        SetBrandColor("ActionRose", palette.Action); SetBrandColor("Rose", palette.Soft); SetBrandColor("Wine", palette.Deep); SetBrandColor("Blush", palette.Tint);
        Resources["BrandGlowColor"] = ToColor(palette.Glow);
        Resources["KeypadAccent"] = new SolidColorBrush(ToColor(palette.Action));
        Resources["KeypadAccentPressed"] = new SolidColorBrush(ToColor(palette.Deep));
        var brush = new SolidColorBrush(ToColor(background));
        brush.Freeze();
        brandBackground = brush;
        BrandOverlay.Fill = brush;
        BrandBackdrop.Source = BrandImage.Load(backgroundPath);
    }

    private void SetBrandColor(string key, string hex)
    {
        if (FindResource(key) is SolidColorBrush { IsFrozen: false } brush) brush.Color = ToColor(hex);
        else Resources[key] = new SolidColorBrush(ToColor(hex));
    }

    private static Color ToColor(string hex)
    {
        BrandColors.TryParse(hex, out var c);
        return Color.FromRgb(c.R, c.G, c.B);
    }
    private readonly CancellationTokenSource cameraLifetime = new();
    private readonly DispatcherTimer liveViewTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly List<System.Windows.Media.Imaging.BitmapSource> capturedPreviews = [];
    private System.Windows.Media.Imaging.BitmapSource? cameraPreview;
    private Guid cameraSession = Guid.NewGuid();
    private ColorRecipe? cameraRecipe;
    private readonly CameraSetup cameraSetup;
    private bool cameraBusy, previewBusy;
    private void OnColorPreviewChanged() { var library = ColorPreviewSession.Shared.Published; flow.ConfigureFilters(library.Looks.Length, library.DefaultIndex); SyncFilterCards(); PropertyChanged?.Invoke(this, new(string.Empty)); }
    private void OnPresetChoice(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { Tag: int index }) flow.SelectFilter(index); }
    private readonly CustomerPreviewFlow flow;
    private readonly Stopwatch elapsedClock = new();
    private TimeSpan previousTick;
    // Khung khách đang chọn (theo khổ); ảnh nền khung giải mã một lần cho mỗi khung.
    public int FrameArtworkZIndex => flow.CurrentFrame?.ArtworkBehindPhotos == true ? -1 : 1;
    public System.Windows.Media.Imaging.BitmapImage? FrameArtwork => flow.CurrentFrame is { ArtworkPng: { } png } frame
        ? frameArtwork.TryGetValue(frame, out var cached) ? cached : frameArtwork[frame] = LayoutWindow.DecodeArtwork(Convert.FromBase64String(png))
        : null;
    private readonly Dictionary<PrintLayout, System.Windows.Media.Imaging.BitmapImage> frameArtwork = new(ReferenceEqualityComparer.Instance);
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private CustomerScreen? displayedScreen;
    private WindowState previousWindowState;
    private bool authorizedExit, promptingPin;

    public MainWindow() : this(CustomerCamera.Resolve()) { }
    internal MainWindow(CustomerCamera camera)
    {
        this.camera = camera;
        cameraSetup = CameraMode ? new CameraSetupStore().Load() : new CameraSetup();
        InitializeComponent();
        PrintLayout[] frames = [.. new PrintLayoutStore().LoadPublished().Select(LegacyStripMigration.Convert)];
        var library = ColorPreviewSession.Shared.Published;
        var sessionPrices = new BoothManagementStore().SessionPrices();
        var captureConfiguration = new CaptureConfigurationStore(CaptureConfigurationStore.DefaultPath).Load();
        flow = new CustomerPreviewFlow(captureConfiguration, null, library.Looks.Length, library.DefaultIndex, CameraMode,
            [new PreviewPackage(2, sessionPrices.Small, "Khổ nhỏ") { SingleStrip = true }, new PreviewPackage(2, sessionPrices.Large, "Khổ lớn") { SingleStrip = false }], frames);
        DataContext = flow;
        InitializeBoothScreens();
        InitializeSessions(captureConfiguration);
        ColorPreviewSession.Shared.Changed += OnColorPreviewChanged;
        flow.PropertyChanged += OnFlowChanged;
        timer.Tick += OnTick;
        liveViewTimer.Tick += OnLiveView;
        Closed += OnClosed;
        Loaded += OnCameraWindowLoaded;
        previewNoticeText = camera.Problem is null ? PreviewNotice.Text : PreviewNotice.Text + " · " + camera.Problem;
        PreviewNotice.Text = previewNoticeText;
        var settingsStore = new GeneralSettingsStore();
        var settings = settingsStore.Load();
        BrandTitle = settings.BrandName;
        BrandLogo = BrandImage.Load(settingsStore.LogoPath(settings));
        ApplyBrandTheme(settings, settingsStore.BackgroundPath(settings));
        brandLoadingImage = BrandImage.Load(settingsStore.LoadingPath(settings));
        Brand.Text = settings.BrandName;
        BrandLogoImage.Source = BrandLogo;
        SyncFilterCards();
        RefreshScreen();
        if (settings.FullScreen) { previousWindowState = WindowState.Normal; WindowStyle = WindowStyle.None; WindowState = WindowState.Maximized; }
        SourceInitialized += (_, _) => { if (System.Windows.Interop.HwndSource.FromHwnd(new System.Windows.Interop.WindowInteropHelper(this).Handle)?.CompositionTarget is { } target) target.RenderMode = settings.HardwareAcceleration ? System.Windows.Interop.RenderMode.Default : System.Windows.Interop.RenderMode.SoftwareOnly; };
        Closing += (_, e) => { if (authorizedExit) return; if (!AuthenticateAdmin()) { e.Cancel = true; return; } authorizedExit = true; ShowAdmin(); };
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        TimeSpan now = elapsedClock.Elapsed;
        TimeSpan delta = now - previousTick;
        previousTick = now;
        flow.Tick(delta);
        if (cameraPipeline is null || cameraBusy || !flow.CaptureRequested) return;
        cameraBusy = true;
        try
        {
            var shotId = RequestShot();
            var path = await cameraPipeline.CaptureAsync(cameraSession, shotId ?? Guid.NewGuid(), cameraLifetime.Token);
            var bytes = await File.ReadAllBytesAsync(path, cameraLifetime.Token);
            var image = await Task.Run(() => RenderCameraImage(bytes, 1280, cameraRecipe, false), cameraLifetime.Token);
            if (cameraLifetime.IsCancellationRequested) return;
            capturedPreviews.Add(image);
            ShotReceived(shotId, path);
            flow.CompleteCameraCapture(path);
        }
        catch (Exception error)
        {
            if (!cameraLifetime.IsCancellationRequested && flow.CaptureRequested)
            {
                FailSession("camera: " + error.Message);
                flow.FailCameraCapture("Không nhận được ảnh. Vui lòng liên hệ nhân viên.");
            }
        }
        finally { cameraBusy = false; }
    }
    public System.Windows.Media.Imaging.BitmapSource? PhotoPreview(int number) => !CameraMode ? SelectedColorImage : capturedPreviews.ElementAtOrDefault(number - 1);
    private void OnFlowChanged(object? sender, PropertyChangedEventArgs e) { TrackSession(); OnColorPreviewChanged(); RefreshScreen(); }

    private void RefreshScreen()
    {
        if (flow.Screen == CustomerScreen.Capture && !flow.CanSelectPhotos && flow.CaptureError is null ||
            flow.Screen == CustomerScreen.Printing && !flow.CanFinishPrint)
        {
            if (!timer.IsEnabled) { elapsedClock.Restart(); previousTick = TimeSpan.Zero; timer.Start(); }
        }
        else
            timer.Stop();
        if (displayedScreen == flow.Screen) return;
        displayedScreen = flow.Screen;
        OnScreenShown(flow.Screen);
        ScreenContent.ContentTemplate = (DataTemplate)FindResource(flow.Screen.ToString());
        var dark = flow.Screen == CustomerScreen.Capture;
        // Màn chụp dùng toàn bộ khung màn hình cho live view; header và tiến trình ẩn.
        Grid.SetRow(ScreenContent, dark ? 0 : 1);
        Grid.SetRowSpan(ScreenContent, dark ? 2 : 1);
        ProgressMarks.Visibility = dark ? Visibility.Collapsed : Visibility.Visible;
        Canvas.Background = dark ? (Brush)FindResource("Ink") : brandBackground;
        BrandBackdrop.Visibility = BrandOverlay.Visibility = !dark && BrandBackdrop.Source is not null ? Visibility.Visible : Visibility.Collapsed;
        Brand.Foreground = (Brush)FindResource(dark ? "Cream" : "Ink");
        // Logo không đổi màu theo nền; màn chụp nền tối dùng tên thương hiệu màu Cream.
        bool showHeader = flow.Screen != CustomerScreen.Welcome && !dark, useLogo = HasBrandLogo && !dark;
        Brand.Visibility = showHeader && !useLogo ? Visibility.Visible : Visibility.Hidden;
        BrandLogoImage.Visibility = showHeader && useLogo ? Visibility.Visible : Visibility.Hidden;
        ProgressMarks.Children.Clear();
        for (var i = 0; i < 11 && flow.ProgressStep >= 0; i++)
        {
            ProgressMarks.Children.Add(new Border
            {
                Width = i == flow.ProgressStep ? 23 : 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(4),
                Background = (Brush)FindResource(i == flow.ProgressStep ? "Rose" : "Line")
            });
        }
        Title = $"FotoAutomat | UI preview | {flow.Screen}";
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            ScreenContent.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))));
    }

    private void OnChoice(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag } || tag.Length < 2 ||
            !int.TryParse(tag.AsSpan(1), out var index)) return;
        switch (tag[0])
        {
            case 'f': flow.SelectFilter(index); break;
            case 'l': flow.SelectLayout(index); break;
            case 'q': flow.SelectPackage(index); break;
            case 'c': flow.SelectCopies(index); break;
            case 'p': flow.SelectPhoto(index); break;
        }
    }

    private void OnNext(object sender, RoutedEventArgs e)
    {
        if (cameraBusy) return;
        if (CameraMode && flow.Screen == CustomerScreen.Welcome && !CameraReady) return;
        // Màn chờ (Instructions) tự chuyển sang chụp; không đi qua nút.
        if (flow.Screen == CustomerScreen.Instructions) return;
        if (flow.Screen is CustomerScreen.CashPayment or CustomerScreen.QrPayment && !StartPaidSession(flow.Screen == CustomerScreen.CashPayment ? "cash" : "qr")) { flow.Back(); return; }
        if (!cameraLifetime.IsCancellationRequested) flow.Next();
    }
    private void OnBack(object sender, RoutedEventArgs e) => flow.Back();
    private void OnRestart(object sender, RoutedEventArgs e)
    {
        if (cameraBusy) return;
        cameraSession = Guid.NewGuid(); capturedPreviews.Clear(); cameraPreview = null; VoucherMessage = ""; flow.Restart();
    }
    private void OnResetBeauty(object sender, RoutedEventArgs e) => flow.BeautyLevel = 0;
    private void OnPhoto(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: int number }) flow.SelectPhoto(number);
    }
    private void OnPaymentChoice(object sender, RoutedEventArgs e) =>
        flow.ChoosePayment((sender as FrameworkElement)?.Tag as string == "cash");
    private void OnSlot(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: int index }) flow.AssignSlot(index);
    }

    private void OnWindowKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            if (WindowStyle == WindowStyle.None) OnAdminUnlock(this, new RoutedEventArgs());
            else
            {
                previousWindowState = WindowState;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && WindowStyle == WindowStyle.None)
        {
            OnAdminUnlock(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private bool AuthenticateAdmin()
    {
        if (promptingPin) return false;
        promptingPin = true;
        try { return new PinWindow { Owner = this }.ShowDialog() == true; }
        finally { promptingPin = false; }
    }
    private void ShowAdmin()
    {
        var admin = Owner as LayoutWindow ?? Application.Current.Windows.OfType<LayoutWindow>().FirstOrDefault() ?? new LayoutWindow();
        admin.Show(); admin.Activate(); Application.Current.MainWindow = admin;
    }
    private void OnAdminUnlock(object sender, RoutedEventArgs e)
    {
        if (!AuthenticateAdmin()) return;
        authorizedExit = true; ShowAdmin(); Close();
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        cameraLifetime.Cancel(); liveViewTimer.Stop(); liveViewTimer.Tick -= OnLiveView; cameraRetry.Stop(); loadingTimer.Stop(); idleTimer.Stop();
        // Đóng booth khi phiên đã thu tiền chưa xong: nhân viên xử lý bằng voucher bù.
        if (sessions?.Current is { IsOpen: true }) FailSession("closed");
        timer.Stop();
        timer.Tick -= OnTick;
        flow.PropertyChanged -= OnFlowChanged;
        ColorPreviewSession.Shared.Changed -= OnColorPreviewChanged;
        if (cameraPipeline is not null) try { await cameraPipeline.DisposeAsync(); } catch (Exception error) { Debug.WriteLine(error); }
    }
}

public sealed class CustomerPhotoConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length >= 2 && values[0] is int number && values[1] is MainWindow window ? window.PhotoPreview(number) : null;
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class IndexEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int index && int.TryParse(parameter?.ToString(), out var target) && index == target;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
