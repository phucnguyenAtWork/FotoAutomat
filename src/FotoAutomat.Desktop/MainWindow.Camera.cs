using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

/// <summary>Thẻ chế độ chụp trên màn khách: ảnh xem trước của bộ lọc, tiêu đề và mô tả do quản trị đặt.</summary>
public sealed class FilterCard(int index, string title, string description) : INotifyPropertyChanged
{
    private BitmapSource? image;
    private bool selected;
    public event PropertyChangedEventHandler? PropertyChanged;
    public int Index { get; } = index;
    public string Title { get; } = title;
    public string Description { get; } = description;
    public bool HasDescription => Description.Length > 0;
    public BitmapSource? Image { get => image; set { if (image == value) return; image = value; PropertyChanged?.Invoke(this, new(nameof(Image))); } }
    public bool Selected { get => selected; set { if (selected == value) return; selected = value; PropertyChanged?.Invoke(this, new(nameof(Selected))); } }
}

// Màn khách quanh máy ảnh: kết nối khi mở booth, màn chờ trước khi chụp, khuôn vùng in trên live view,
// chụp lại và voucher ở bước thanh toán.
public partial class MainWindow
{
    // Bằng thời lượng thanh tiến trình của màn chờ (Storyboard trong template Instructions).
    private static readonly TimeSpan LoadingDuration = TimeSpan.FromSeconds(4);
    private readonly CustomerCamera camera;
    private BoothCameraPipeline? cameraPipeline;
    private readonly DispatcherTimer cameraRetry = new() { Interval = TimeSpan.FromSeconds(5) };
    private readonly DispatcherTimer loadingTimer = new() { Interval = LoadingDuration };
    private readonly VoucherStore voucherStore = new();
    private Dictionary<string, ModeCard> modeCards = [];
    // Ảnh được đặt vào ô bằng cách lấp đầy và cắt giữa, nên khuôn là hình chữ nhật cùng tỉ lệ ô, căn giữa khung hình.
    // Khung chọn sau khi chụp, nên khuôn theo khung đầu tiên của khổ khách đã chọn.
    private double? captureSlotAspect => flow.CurrentFrame?.Slots.FirstOrDefault(s => s.IsVisible && s.Kind == LayoutElementKind.Photo && s.Width > 0 && s.Height > 0) is { } slot
        ? slot.Width / slot.Height : null;
    private bool connectingCamera;

    public ObservableCollection<FilterCard> FilterCards { get; } = [];
    // Ảnh màn chờ do quản trị chọn; chưa chọn thì dùng ảnh xem trước của chế độ khách vừa chọn.
    private BitmapSource? brandLoadingImage;
    public BitmapSource? LoadingPhoto => brandLoadingImage ?? ColorPreviewSession.Shared.PublishedPreviews.ElementAtOrDefault(flow.FilterIndex);
    private bool CameraMode => camera.Create is not null;
    public bool CameraReady { get; private set; }
    public bool CanStart => !CameraMode || CameraReady;
    public string CameraStatus { get; private set; } = "";
    public bool HasCameraStatus => CameraStatus.Length > 0;

    private void InitializeBoothScreens()
    {
        try { modeCards = new ModeCardStore().Load(); }
        catch (Exception error) { Debug.WriteLine(error); }
        loadingTimer.Tick += async (_, _) => { loadingTimer.Stop(); await ProceedFromLoadingAsync(); };
    }

    private ModeFeatures FeaturesOf(ColorRecipe look) => modeCards.TryGetValue(look.PresetId, out var card) ? card.EffectiveFeatures : ModeFeatures.Default;
    // Phần màu của chế độ theo tính năng người vận hành bật (LUT, Adjust, Blur & Glow).
    private ColorRecipe ModeColor(ColorRecipe look) => ShootingMode.Compose(look, null, null, FeaturesOf(look)).Color;

    private void SyncFilterCards()
    {
        var looks = ColorPreviewSession.Shared.Published.Looks;
        string TitleOf(ColorRecipe look) => modeCards.TryGetValue(look.PresetId, out var card) && card.Title.Length > 0 ? card.Title : look.Name;
        string DescriptionOf(ColorRecipe look) => modeCards.TryGetValue(look.PresetId, out var card) ? card.Description : "";
        if (FilterCards.Count != looks.Length || FilterCards.Where((card, i) => card.Title != TitleOf(looks[i]) || card.Description != DescriptionOf(looks[i])).Any())
        {
            FilterCards.Clear();
            for (int i = 0; i < looks.Length; i++) FilterCards.Add(new FilterCard(i, TitleOf(looks[i]), DescriptionOf(looks[i])));
        }
        foreach (var card in FilterCards)
        {
            card.Selected = card.Index == flow.FilterIndex;
            card.Image = ColorPreviewSession.Shared.PublishedPreviews.ElementAtOrDefault(card.Index);
        }
    }

    private void OnScreenShown(CustomerScreen screen)
    {
        loadingTimer.Stop();
        if (screen == CustomerScreen.Instructions) loadingTimer.Start();
        if (screen == CustomerScreen.Payment) { VoucherInput = ""; RaiseVoucher(); }
    }

    // Màn chờ thay cho bước hướng dẫn: hết thời gian thì vào chụp, nhưng chỉ khi máy ảnh đã sẵn sàng.
    // Khách đã thanh toán nên không quay lại; máy ảnh lỗi thì giữ màn chờ với lời nhắn nhân viên và thử lại.
    private async Task ProceedFromLoadingAsync()
    {
        if (flow.Screen != CustomerScreen.Instructions || cameraBusy || cameraLifetime.IsCancellationRequested) return;
        if (CameraMode && !await EnsureCameraAsync()) { if (flow.Screen == CustomerScreen.Instructions) loadingTimer.Start(); return; }
        var looks = ColorPreviewSession.Shared.Published.Looks;
        cameraRecipe = ModeColor(looks[Math.Clamp(flow.FilterIndex, 0, looks.Length - 1)]);
        flow.Next();
    }

    private async void OnCameraWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (!CameraMode) return;
        cameraRetry.Tick += async (_, _) => { if (flow.Screen == CustomerScreen.Welcome) await EnsureCameraAsync(); };
        await EnsureCameraAsync();
    }

    // Một kết nối hỏng không kết nối lại được, nên mỗi lần thử tạo kết nối mới.
    private async Task<bool> EnsureCameraAsync()
    {
        if (cameraPipeline?.IsReady == true) return true;
        if (connectingCamera || camera.Create is null || cameraLifetime.IsCancellationRequested) return false;
        connectingCamera = true;
        SetCameraState(false, "Đang chuẩn bị máy ảnh…");
        try
        {
            if (cameraPipeline is not null) await cameraPipeline.DisposeAsync();
            cameraPipeline = camera.Create();
            await cameraPipeline.ConnectAsync(cameraLifetime.Token);
            cameraRetry.Stop();
            SetCameraState(true, "");
            liveViewTimer.Start();
            return true;
        }
        catch (Exception error) when (!cameraLifetime.IsCancellationRequested)
        {
            Debug.WriteLine(error);
            ReportCameraProblem("Máy ảnh chưa sẵn sàng. Vui lòng liên hệ nhân viên.", error.Message);
            return false;
        }
        catch (OperationCanceledException) { return false; }
        finally { connectingCamera = false; }
    }

    private void ReportCameraProblem(string customerMessage, string detail)
    {
        liveViewTimer.Stop();
        SetCameraState(false, customerMessage);
        PreviewNotice.Text = previewNoticeText + " · Máy ảnh: " + detail;
        cameraRetry.Start();
    }

    private void SetCameraState(bool ready, string status)
    {
        CameraReady = ready; CameraStatus = status;
        if (ready) PreviewNotice.Text = previewNoticeText;
        foreach (var name in new[] { nameof(CameraReady), nameof(CanStart), nameof(CameraStatus), nameof(HasCameraStatus) }) PropertyChanged?.Invoke(this, new(name));
    }

    private async void OnLiveView(object? sender, EventArgs e)
    {
        if (cameraPipeline is not { IsReady: true } pipeline || previewBusy || cameraBusy) return;
        if (flow.Screen != CustomerScreen.Capture || flow.IsReviewingCapture) return;
        previewBusy = true;
        try
        {
            var frame = await pipeline.ReadLiveViewAsync(cameraLifetime.Token);
            if (frame is null) return;
            var looks = ColorPreviewSession.Shared.Published.Looks;
            var recipe = ColorProcessor.ForLiveView(ModeColor(looks[Math.Clamp(flow.FilterIndex, 0, looks.Length - 1)]));
            var image = await Task.Run(() => RenderCameraImage(frame.Jpeg, cameraSetup.PreviewWidth, recipe, true), cameraLifetime.Token);
            if (cameraLifetime.IsCancellationRequested) return;
            cameraPreview = image;
            RaiseCaptureFrame();
        }
        catch (Exception error) when (!cameraLifetime.IsCancellationRequested)
        {
            ReportCameraProblem("Máy ảnh bị ngắt. Vui lòng liên hệ nhân viên.", error.Message);
            if (flow.CaptureRequested)
            {
                FailSession("live view: " + error.Message);
                flow.FailCameraCapture("Máy ảnh bị ngắt. Vui lòng liên hệ nhân viên.");
            }
        }
        catch (OperationCanceledException) { }
        finally { previewBusy = false; }
    }

    // ===== Khung chụp: live view đầy đủ khung cảm biến, phủ tối phần nằm ngoài vùng sẽ in. =====
    public double CaptureFrameWidth => SelectedColorImage?.PixelWidth ?? 1500;
    public double CaptureFrameHeight => SelectedColorImage?.PixelHeight ?? 1000;
    public bool HasCaptureMask => captureSlotAspect is not null;
    public double CaptureGuideStroke => Math.Max(2, CaptureFrameWidth / 320);
    public Thickness CaptureFrameOutline => new(Math.Max(1, CaptureFrameWidth / 640));

    // Gạch chéo nhạt cho phần có chụp nhưng không in. Khung nằm trong Viewbox nên kích thước gạch tính theo bề ngang khung,
    // để trên màn hình gạch luôn mảnh và thưa như nhau dù ảnh live view rộng bao nhiêu pixel.
    public Brush CaptureHatch
    {
        get
        {
            double tile = CaptureFrameWidth / 48, pen = CaptureFrameWidth / 1400;
            var lines = new GeometryGroup();
            lines.Children.Add(new LineGeometry(new Point(0, tile), new Point(tile, 0)));
            lines.Children.Add(new LineGeometry(new Point(-tile / 4, tile / 4), new Point(tile / 4, -tile / 4)));
            lines.Children.Add(new LineGeometry(new Point(tile * 3 / 4, tile * 5 / 4), new Point(tile * 5 / 4, tile * 3 / 4)));
            var brush = new DrawingBrush(new GeometryDrawing(null, new Pen(new SolidColorBrush(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF)), pen), lines))
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, tile, tile),
                ViewportUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(0, 0, tile, tile),
                ViewboxUnits = BrushMappingMode.Absolute,
            };
            return Freeze(brush);
        }
    }

    private Rect? CaptureGuideRect
    {
        get
        {
            if (captureSlotAspect is not double aspect) return null;
            double w = CaptureFrameWidth, h = CaptureFrameHeight;
            double innerW = w / h > aspect ? h * aspect : w, innerH = w / h > aspect ? h : w / aspect;
            return new Rect((w - innerW) / 2, (h - innerH) / 2, innerW, innerH);
        }
    }

    public Geometry? CaptureMask => CaptureGuideRect is { } inner
        ? Freeze(new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(new Rect(0, 0, CaptureFrameWidth, CaptureFrameHeight)), new RectangleGeometry(inner)))
        : null;

    public Geometry? CaptureGuide => CaptureGuideRect is { } inner ? Freeze(new RectangleGeometry(inner)) : null;

    private void RaiseCaptureFrame()
    {
        foreach (var name in new[] { nameof(SelectedColorImage), nameof(CaptureFrameWidth), nameof(CaptureFrameHeight), nameof(CaptureMask), nameof(CaptureGuide), nameof(CaptureGuideStroke), nameof(CaptureFrameOutline), nameof(CaptureHatch) })
            PropertyChanged?.Invoke(this, new(name));
    }

    // Bỏ ảnh đang xem lại; ảnh gốc và biên nhận trên đĩa giữ nguyên.
    private void OnRetake(object sender, RoutedEventArgs e)
    {
        if (cameraBusy || !flow.CanRetake) return;
        DiscardPendingShot();
        flow.RetakeLast();
        if (capturedPreviews.Count > flow.CapturedCount) capturedPreviews.RemoveAt(capturedPreviews.Count - 1);
        RaiseCaptureFrame();
    }

    // ===== Voucher ở bước thanh toán =====
    public string VoucherInput { get; private set; } = "";
    public string VoucherMessage { get; private set; } = "";
    public bool HasVoucherMessage => VoucherMessage.Length > 0;
    public bool CanEnterVoucher => VoucherInput.Length == 6;
    public bool CanEraseVoucher => VoucherInput.Length > 0;
    public string VoucherBoxText => VoucherInput.Length > 0 ? VoucherInput : flow.Voucher?.Code ?? "";
    public bool ShowsAppliedVoucher => VoucherInput.Length == 0 && flow.Voucher is not null;

    private void RaiseVoucher()
    {
        foreach (var name in new[] { nameof(VoucherInput), nameof(VoucherMessage), nameof(HasVoucherMessage), nameof(CanEnterVoucher), nameof(CanEraseVoucher), nameof(VoucherBoxText), nameof(ShowsAppliedVoucher) })
            PropertyChanged?.Invoke(this, new(name));
    }

    private void OnVoucherKey(object? sender, KeypadEventArgs e)
    {
        if (flow.Screen != CustomerScreen.Payment) return;
        if (e.Key == '\r') { ApplyVoucher(); return; }
        if (e.Key == '\b') { if (VoucherInput.Length > 0) VoucherInput = VoucherInput[..^1]; }
        else if (VoucherInput.Length < 6) VoucherInput += e.Key;
        VoucherMessage = "";
        RaiseVoucher();
    }

    private void ApplyVoucher()
    {
        if (VoucherInput.Length != 6) return;
        try
        {
            var voucher = voucherStore.Check(VoucherInput);
            flow.ApplyVoucher(voucher);
            VoucherMessage = "";
            VoucherInput = "";
        }
        catch (Exception error) when (error is ArgumentException or IOException or InvalidDataException) { VoucherMessage = error.Message; }
        RaiseVoucher();
    }

    private void OnClearVoucher(object sender, RoutedEventArgs e) { flow.ClearVoucher(); VoucherMessage = ""; RaiseVoucher(); }

    private void OnStartFree(object sender, RoutedEventArgs e)
    {
        if (!flow.IsFree || !StartPaidSession("voucher")) return;
        flow.StartFree();
    }

    private static (byte[] Pixels, int Width, int Height) DecodeBgra(byte[] jpeg, int width)
    {
        var converted = new FormatConvertedBitmap(DecodeCameraImage(jpeg, width), PixelFormats.Bgra32, null, 0);
        int w = converted.PixelWidth, h = converted.PixelHeight;
        var pixels = new byte[w * h * 4];
        converted.CopyPixels(pixels, w * 4, 0);
        return (pixels, w, h);
    }

    private BitmapSource RenderCameraImage(byte[] bytes, int width, ColorRecipe? recipe, bool live)
    {
        var (pixels, w, h) = DecodeBgra(bytes, width);
        if (recipe is not null) pixels = ColorProcessor.Apply(pixels, recipe, w, h, cameraLifetime.Token);
        var result = Freeze(BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, pixels, w * 4));
        return live ? OrientLive(result) : result;
    }

    // Live view xoay và lật như gương theo cấu hình máy ảnh; ảnh đã chụp giữ nguyên hướng.
    private BitmapSource OrientLive(BitmapSource source)
    {
        var transform = new TransformGroup();
        transform.Children.Add(new RotateTransform(cameraSetup.Rotation));
        transform.Children.Add(new ScaleTransform(cameraSetup.MirrorLiveView ? -1 : 1, 1));
        transform.Freeze();
        return Freeze(new TransformedBitmap(source, transform));
    }

    private static T Freeze<T>(T value) where T : Freezable { value.Freeze(); return value; }

    private static BitmapImage DecodeCameraImage(byte[] bytes, int width)
    {
        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = width; image.StreamSource = stream; image.EndInit(); image.Freeze(); return image;
    }
}
