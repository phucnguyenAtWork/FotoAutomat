using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

// Ghi phiên khách vào booth.db ở từng mốc. Phiên chỉ được tạo khi khách đã thanh toán (hoặc voucher phủ hết);
// trước đó là giỏ hàng trong bộ nhớ và tự hủy nếu không ai chạm màn hình.
public partial class MainWindow
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(120);
    private CustomerSessionService sessions = null!;
    private CaptureConfiguration capturedConfiguration = new();
    private Guid? pendingShot;
    private int lastCapturedCount;
    private CustomerScreen lastFlowScreen = CustomerScreen.Welcome;
    private DateTime lastInput = DateTime.UtcNow;
    private readonly DispatcherTimer idleTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    private PhotoProcessingService? photos;

    private void InitializeSessions(CaptureConfiguration configuration)
    {
        sessions = new CustomerSessionService(new SqliteCustomerSessionStore());
        capturedConfiguration = configuration;
        try
        {
            photos = new PhotoProcessingService(new WpfImageCodec(), new WpfFrameComposer(new PrinterConfigurationStore().Load()), SampleFaceDetection.CustomerAnalyzer());
            photos.Recover();
            Closed += (_, _) => photos.Dispose();
        }
        catch (Exception error) { Debug.WriteLine(error); photos = null; }
        foreach (var input in new[] { PreviewMouseDownEvent, PreviewTouchDownEvent, PreviewKeyDownEvent, PreviewStylusDownEvent })
            AddHandler(input, new RoutedEventHandler((_, _) => lastInput = DateTime.UtcNow), true);
        idleTimer.Tick += OnIdleTick;
        idleTimer.Start();
    }

    // Khách bỏ đi trước khi trả tiền: quay về màn chào, không ghi gì.
    private void OnIdleTick(object? sender, EventArgs e)
    {
        bool beforePayment = flow.Screen is CustomerScreen.Filter or CustomerScreen.Layout or CustomerScreen.Payment or CustomerScreen.QrPayment or CustomerScreen.CashPayment;
        if (!beforePayment || sessions.Current is not null || DateTime.UtcNow - lastInput < IdleTimeout) return;
        lastInput = DateTime.UtcNow;
        VoucherInput = ""; VoucherMessage = ""; RaiseVoucher();
        flow.Restart();
    }

    // Các khung khách được chọn (cùng khổ) tại lúc trả tiền; lựa chọn của khách lưu vị trí khung trong danh sách này.
    private string? FrameSnapshot() => flow.FrameChoices.Count == 0 ? null : JsonSerializer.Serialize(flow.FrameChoices.Select(layout => new
    {
        layout.Name,
        layout.Dpi,
        layout.Landscape,
        layout.SingleStrip,
        PhotoSlots = layout.Slots.Where(s => s.IsVisible && s.Kind == LayoutElementKind.Photo).Select(s => new { s.PhotoNumber, s.X, s.Y, s.Width, s.Height }),
        ArtworkSha256 = layout.ArtworkPng is null ? null : Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(layout.ArtworkPng))),
    }));

    // Tạo phiên Paid, ghi giao dịch và trừ voucher trong một transaction. Lỗi (ví dụ mã vừa bị dùng) thì không có phiên.
    private bool StartPaidSession(string method)
    {
        try
        {
            var looks = ColorPreviewSession.Shared.Published.Looks;
            var look = looks[Math.Clamp(flow.FilterIndex, 0, looks.Length - 1)];
            var package = flow.PackageOptions[flow.PackageIndex];
            var preview = ColorPreviewSession.Shared;
            var mode = ShootingMode.Compose(look, ShootingMode.RetouchOf(preview.SavedRetouch.Faces), preview.SavedRefine.Settings, FeaturesOf(look));
            var snapshot = new SessionSnapshot(package.Name.Length > 0 ? package.Name : package.Label, flow.Copies, capturedConfiguration.Shots, capturedConfiguration.RetakeLimit,
                look.PresetId, look.Name, JsonSerializer.Serialize(mode.Color), FrameSnapshot(), JsonSerializer.Serialize(capturedConfiguration),
                mode.Retouch, mode.Refine, mode.Notes.Length == 0 ? null : mode.Notes, mode.Features);
            var payment = new SessionPayment(flow.Price, flow.Discount, flow.AmountDue, method, flow.Voucher?.Code, method == "voucher" ? null : "mô phỏng");
            var session = sessions.StartPaid(snapshot, payment);
            cameraSession = session.Id;
            lastCapturedCount = 0; pendingShot = null;
            return true;
        }
        catch (Exception error)
        {
            Debug.WriteLine(error);
            VoucherMessage = error is InvalidOperationException or ArgumentException ? error.Message : "Không ghi được phiên. Vui lòng liên hệ nhân viên.";
            RaiseVoucher();
            return false;
        }
    }

    // Ghi dữ liệu không được làm treo giao diện khách; lỗi ghi được ghi log để nhân viên đối soát.
    private void Record(Action action)
    {
        try { action(); }
        catch (Exception error) { Debug.WriteLine(error); PreviewNotice.Text = previewNoticeText + " · Lỗi ghi phiên: " + error.Message; }
    }

    private Guid? RequestShot()
    {
        if (sessions.Current is null) return null;
        Guid? id = null;
        Record(() => id = sessions.RequestShot());
        return id;
    }

    private void ShotReceived(Guid? shotId, string? path)
    {
        if (shotId is not { } id) return;
        Record(() => sessions.ShotReceived(id, path));
        pendingShot = id;
    }

    private void DiscardPendingShot()
    {
        if (pendingShot is { } id) Record(() => sessions.Discard(id));
        pendingShot = null;
    }

    private void FailSession(string reason) => Record(() => sessions.Fail(reason));

    // Theo dõi luồng khách: shot đã xem xong thì giữ, đổi màn thì chuyển trạng thái phiên và lưu lựa chọn.
    private void TrackSession()
    {
        if (sessions.Current is null) { lastFlowScreen = flow.Screen; return; }
        bool reviewing = flow.Screen == CustomerScreen.Capture && flow.IsReviewingCapture;
        if (!CameraMode && reviewing && flow.CapturedCount > lastCapturedCount) ShotReceived(RequestShot(), null);
        if (pendingShot is { } kept && !reviewing) { Record(() => sessions.Keep(kept)); pendingShot = null; }
        lastCapturedCount = flow.CapturedCount;
        if (flow.Screen == lastFlowScreen) return;
        var previous = lastFlowScreen;
        lastFlowScreen = flow.Screen;
        switch (flow.Screen)
        {
            case CustomerScreen.Capture: Record(() => sessions.Advance(CustomerStage.Capturing)); break;
            case CustomerScreen.Selection: Record(() => sessions.Advance(CustomerStage.Selecting)); break;
            case CustomerScreen.Beauty when previous == CustomerScreen.Selection: SaveSelection(); break;
            case CustomerScreen.Frame: Record(() => sessions.Advance(CustomerStage.Framing)); break;
            case CustomerScreen.Printing: SaveSelection(); StartPrint(); break;
            case CustomerScreen.Delivery:
                Record(() => sessions.Advance(CustomerStage.Completed));
                if (photos is not null && sessions.Current is { } done) Record(() => photos.EnqueueFull(done.Id));
                break;
            case CustomerScreen.Welcome:
                if (sessions.Current is { IsOpen: true }) FailSession("restarted");
                sessions.End(); pendingShot = null; lastCapturedCount = 0;
                break;
        }
    }

    // Tạo file khung và tờ in từ ảnh gốc theo chế độ của phiên. Không có ảnh camera (bản xem trước) thì giữ tiến độ mô phỏng.
    private async void StartPrint()
    {
        if (photos is null || sessions.Current is not { } session || flow.CurrentFrame is not { } frame || flow.CapturedPaths.Count == 0) return;
        try
        {
            var slots = PhotoProcessingService.MapSlots(flow.Slots.Where(s => s.IsPhoto && s.Photo is int).Select(s => (s.Index, s.Photo!.Value)), flow.CapturedPaths, sessions.Shots());
            flow.ReportPrintProgress(0);
            await photos.RenderPrintAsync(session.Id, PhotoPipeline.ModeOf(session.Snapshot), frame, slots, new Progress<double>(p => flow.ReportPrintProgress((int)Math.Round(p * 100))));
        }
        catch (Exception error)
        {
            Debug.WriteLine(error);
            FailSession("print: " + error.Message);
            flow.FailPrint("Không tạo được ảnh in. Vui lòng liên hệ nhân viên.");
        }
    }

    private void SaveSelection() => Record(() => sessions.SaveSelection(new SessionSelection(
        [.. flow.FavoritePhotos.Select(p => p.Number)],
        [.. flow.Slots.Where(s => s.IsPhoto).Select(s => s.Photo)], flow.LayoutIndex)));
}
