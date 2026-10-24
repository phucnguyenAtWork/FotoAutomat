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
    public sealed record PresetChoice(int Index, string Name, System.Windows.Media.Imaging.BitmapSource? Image, bool Selected);
    public PresetChoice[] FilterChoices => ColorPreviewSession.Shared.Published.Looks.Select((r, i) => new PresetChoice(i, r.Name, ColorPreviewSession.Shared.PublishedPreviews.ElementAtOrDefault(i), flow.FilterIndex == i)).ToArray();
    public System.Windows.Media.Imaging.BitmapSource? SelectedColorImage => cameraPipeline is not null
        ? (flow.Screen == CustomerScreen.Capture && !flow.IsReviewingCapture ? cameraPreview : capturedPreviews.ElementAtOrDefault(flow.Screen == CustomerScreen.Capture ? flow.CapturedCount - 1 : flow.SelectedPhoto - 1))
        : (flow.Screen == CustomerScreen.Capture ? ColorPreviewSession.Shared.LivePreviews : ColorPreviewSession.Shared.PublishedPreviews).ElementAtOrDefault(flow.FilterIndex);
    // Thương hiệu của bên mua key: logo nếu đã chọn, nếu không thì tên thương hiệu.
    public string BrandTitle { get; private set; } = "Foto Hunter";
    public System.Windows.Media.Imaging.BitmapSource? BrandLogo { get; private set; }
    public bool HasBrandLogo => BrandLogo is not null;
    public bool HasNoBrandLogo => BrandLogo is null;
    public string BrandFooter => BrandTitle.ToUpper(CultureInfo.CurrentCulture) + " PHOTOBOOTH";
    private readonly BoothCameraPipeline? cameraPipeline;
    private readonly CancellationTokenSource cameraLifetime = new();
    private readonly DispatcherTimer liveViewTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly List<System.Windows.Media.Imaging.BitmapSource> capturedPreviews = [];
    private System.Windows.Media.Imaging.BitmapSource? cameraPreview;
    private Guid cameraSession = Guid.NewGuid();
    private ColorRecipe? cameraRecipe;
    private readonly CameraSetup cameraSetup;
    private bool cameraBusy, previewBusy;
    private void OnColorPreviewChanged() { var library = ColorPreviewSession.Shared.Published; flow.ConfigureFilters(library.Looks.Length, library.DefaultIndex); PropertyChanged?.Invoke(this, new(string.Empty)); }
    private void OnPresetChoice(object sender, RoutedEventArgs e) { if (sender is FrameworkElement { Tag: int index }) flow.SelectFilter(index); }
    private readonly CustomerPreviewFlow flow;
    private readonly Stopwatch elapsedClock = new();
    private TimeSpan previousTick;
    public int FrameArtworkZIndex { get; }
    public System.Windows.Media.Imaging.BitmapImage? FrameArtwork { get; }
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private CustomerScreen? displayedScreen;
    private WindowState previousWindowState;
    private bool authorizedExit, promptingPin;

    public MainWindow() : this(null) { }
    public MainWindow(BoothCameraPipeline? cameraPipeline)
    {
        this.cameraPipeline = cameraPipeline;
        cameraSetup = cameraPipeline is null ? new CameraSetup() : new CameraSetupStore().Load();
        InitializeComponent();
        PrintLayout? layout = new PrintLayoutStore().LoadActive();
        if (layout is not null) layout = LegacyStripMigration.Convert(layout);
        FrameArtworkZIndex = layout?.ArtworkBehindPhotos == true ? -1 : 1;
        if (layout?.ArtworkPng is not null) FrameArtwork = LayoutWindow.DecodeArtwork(Convert.FromBase64String(layout.ArtworkPng));
        var library = ColorPreviewSession.Shared.Published;
        var sessionPrices = new BoothManagementStore().SessionPrices();
        int printCopies = new PrinterConfigurationStore().Load().Copies;
        flow = new CustomerPreviewFlow(new CaptureConfigurationStore(CaptureConfigurationStore.DefaultPath).Load(), layout, library.Looks.Length, library.DefaultIndex, cameraPipeline is not null,
            [new PreviewPackage(printCopies, sessionPrices.Small, "Khổ nhỏ"), new PreviewPackage(printCopies, sessionPrices.Large, "Khổ lớn")]);
        DataContext = flow;
        ColorPreviewSession.Shared.Changed += OnColorPreviewChanged;
        flow.PropertyChanged += OnFlowChanged;
        timer.Tick += OnTick;
        liveViewTimer.Tick += OnLiveView;
        Closed += OnClosed;
        var settingsStore = new GeneralSettingsStore();
        var settings = settingsStore.Load();
        BrandTitle = settings.BrandName;
        BrandLogo = BrandImage.Load(settingsStore.LogoPath(settings));
        Brand.Text = settings.BrandName;
        BrandLogoImage.Source = BrandLogo;
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
            var path = await cameraPipeline.CaptureAsync(cameraSession, Guid.NewGuid(), cameraLifetime.Token);
            var bytes = await File.ReadAllBytesAsync(path, cameraLifetime.Token);
            var image = await Task.Run(() => RenderCameraImage(bytes, false), cameraLifetime.Token);
            if (cameraLifetime.IsCancellationRequested) return;
            capturedPreviews.Add(image);
            flow.CompleteCameraCapture(path);
        }
        catch (Exception error)
        {
            if (!cameraLifetime.IsCancellationRequested && flow.CaptureRequested) flow.FailCameraCapture("Không nhận được ảnh: " + error.Message);
        }
        finally { cameraBusy = false; }
    }
    private async void OnLiveView(object? sender, EventArgs e)
    {
        if (cameraPipeline is null || previewBusy || cameraBusy || flow.Screen != CustomerScreen.Capture || flow.IsReviewingCapture) return;
        previewBusy = true;
        try
        {
            var frame = await cameraPipeline.ReadLiveViewAsync(cameraLifetime.Token);
            if (frame is not null)
            {
                var image = await Task.Run(() => RenderCameraImage(frame.Jpeg, true), cameraLifetime.Token);
                if (!cameraLifetime.IsCancellationRequested) { cameraPreview = image; PropertyChanged?.Invoke(this, new(nameof(SelectedColorImage))); }
            }
        }
        catch (Exception error)
        {
            liveViewTimer.Stop(); timer.Stop();
            if (!cameraLifetime.IsCancellationRequested) MessageBox.Show(this, "Live view đã dừng. " + error.Message, "Camera");
        }
        finally { previewBusy = false; }
    }
    private static System.Windows.Media.Imaging.BitmapImage DecodeCameraImage(byte[] bytes, int width)
    {
        using var stream = new MemoryStream(bytes);
        var image = new System.Windows.Media.Imaging.BitmapImage();
        image.BeginInit(); image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = width; image.StreamSource = stream; image.EndInit(); image.Freeze(); return image;
    }
    private System.Windows.Media.Imaging.BitmapSource RenderCameraImage(byte[] bytes, bool live)
    {
        var image = DecodeCameraImage(bytes, live ? cameraSetup.PreviewWidth : 1280);
        var converted = new System.Windows.Media.Imaging.FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        int w = converted.PixelWidth, h = converted.PixelHeight;
        var pixels = new byte[w * h * 4]; converted.CopyPixels(pixels, w * 4, 0);
        if (cameraRecipe is { } recipe) pixels = ColorProcessor.Apply(pixels, live ? ColorProcessor.ForLiveView(recipe) : recipe, w, h, cameraLifetime.Token);
        var result = System.Windows.Media.Imaging.BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, pixels, w * 4);
        result.Freeze();
        if (!live) return result;
        var transform = new TransformGroup();
        transform.Children.Add(new RotateTransform(cameraSetup.Rotation));
        transform.Children.Add(new ScaleTransform(cameraSetup.MirrorLiveView ? -1 : 1, 1));
        transform.Freeze();
        var display = new System.Windows.Media.Imaging.TransformedBitmap(result, transform);
        display.Freeze(); return display;
    }
    public System.Windows.Media.Imaging.BitmapSource? PhotoPreview(int number) => cameraPipeline is null ? SelectedColorImage : capturedPreviews.ElementAtOrDefault(number - 1);
    private void OnFlowChanged(object? sender, PropertyChangedEventArgs e) { OnColorPreviewChanged(); RefreshScreen(); }

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
        ScreenContent.ContentTemplate = (DataTemplate)FindResource(flow.Screen.ToString());
        var dark = flow.Screen == CustomerScreen.Capture;
        Canvas.Background = (Brush)FindResource(dark ? "Ink" : "Cream");
        Brand.Foreground = (Brush)FindResource(dark ? "Cream" : "Ink");
        // Logo không đổi màu theo nền; màn chụp nền tối dùng tên thương hiệu màu Cream.
        bool showHeader = flow.Screen != CustomerScreen.Welcome, useLogo = HasBrandLogo && !dark;
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
            case 'p': flow.SelectPhoto(index); break;
        }
    }

    private async void OnNext(object sender, RoutedEventArgs e)
    {
        if (cameraBusy) return;
        if (cameraPipeline is not null && flow.Screen == CustomerScreen.Instructions)
        {
            cameraBusy = true;
            try
            {
                if (!cameraPipeline.IsReady) await cameraPipeline.ConnectAsync(cameraLifetime.Token);
                cameraRecipe = ColorPreviewSession.Shared.Published.Looks[flow.FilterIndex];
                liveViewTimer.Start();
            }
            catch (Exception error) { if (!cameraLifetime.IsCancellationRequested) MessageBox.Show(this, error.Message, "Không kết nối được camera"); return; }
            finally { cameraBusy = false; }
        }
        if (!cameraLifetime.IsCancellationRequested) flow.Next();
    }
    private void OnBack(object sender, RoutedEventArgs e) => flow.Back();
    private void OnRestart(object sender, RoutedEventArgs e)
    {
        if (cameraBusy) return;
        cameraSession = Guid.NewGuid(); capturedPreviews.Clear(); cameraPreview = null; flow.Restart();
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
        cameraLifetime.Cancel(); liveViewTimer.Stop(); liveViewTimer.Tick -= OnLiveView;
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
