namespace FotoAutomat.Core;

/// <summary>Trạng thái của một phiên khách đã thanh toán. Phiên chỉ tồn tại từ lúc tiền (hoặc voucher) đã chắc chắn.</summary>
public enum CustomerStage { Paid, Capturing, Selecting, Framing, Completed, NeedsReview, Compensated }

public enum ShotState { Requested, Received, Kept, Discarded }

/// <summary>Số tiền tính bằng đồng. Method: "cash", "qr" hoặc "voucher" (miễn phí).</summary>
public sealed record SessionPayment(int Price, int Discount, int AmountDue, string Method, string? VoucherCode = null, string? Reference = null)
{
    public void Validate()
    {
        if (Price < 0 || Discount < 0 || Discount > Price || AmountDue != Price - Discount) throw new ArgumentException("Số tiền phiên không hợp lệ.");
        if (Method is not ("cash" or "qr" or "voucher")) throw new ArgumentException("Phương thức thanh toán không hợp lệ.");
        if (Method == "voucher" && (AmountDue != 0 || string.IsNullOrEmpty(VoucherCode))) throw new ArgumentException("Phiên miễn phí cần voucher phủ hết số tiền.");
    }
}

/// <summary>Cấu hình lúc khách trả tiền. Đổi cấu hình quản trị sau đó không ảnh hưởng phiên đang chạy.</summary>
/// <summary>Cấu hình cố định lúc trả tiền. RecipeJson là màu của chế độ chụp (đã chuyển Grain/Sharpening sang Refine);
/// Retouch/Refine là phần còn lại của chế độ, không chứa tọa độ mặt hay điểm bụi của ảnh mẫu.</summary>
public sealed record SessionSnapshot(string PackageName, int Copies, int Shots, int RetakeLimit, string PresetId, string PresetName, string RecipeJson, string? LayoutJson, string CaptureJson,
    RetouchSettings? Retouch = null, RefineSettings? Refine = null, string[]? ModeNotes = null, ModeFeatures? Features = null);

public sealed record CustomerSession(Guid Id, DateTimeOffset StartedAt, CustomerStage Stage, int Revision, SessionPayment Payment, SessionSnapshot Snapshot)
{
    public Guid? CompensatesSessionId { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    /// <summary>Mã ngẫu nhiên, khó đoán, dùng làm link QR giao ảnh cho cả nhóm.</summary>
    public string? ShareToken { get; init; }
    public bool IsOpen => Stage is CustomerStage.Paid or CustomerStage.Capturing or CustomerStage.Selecting or CustomerStage.Framing;

    public CustomerSession MoveTo(CustomerStage next, DateTimeOffset now)
    {
        bool valid = (Stage, next) switch
        {
            (CustomerStage.Paid, CustomerStage.Capturing) => true,
            (CustomerStage.Capturing, CustomerStage.Selecting) => true,
            (CustomerStage.Selecting, CustomerStage.Framing) => true,
            (CustomerStage.Framing, CustomerStage.Selecting or CustomerStage.Completed) => true,
            (_, CustomerStage.NeedsReview) when IsOpen => true,
            (CustomerStage.NeedsReview, CustomerStage.Compensated) => true,
            _ => false
        };
        if (!valid) throw new InvalidOperationException($"Không chuyển được phiên từ {Stage} sang {next}.");
        return this with { Stage = next, Revision = Revision + 1, CompletedAt = next == CustomerStage.Completed ? now : CompletedAt };
    }
}

public sealed record SessionShot(Guid Id, Guid SessionId, int Seq, string? Path, ShotState State, DateTimeOffset At);

/// <summary>Ảnh yêu thích và ảnh gán vào từng ô, theo số thứ tự ảnh đã giữ (1, 2, …). Frame: vị trí khung khách chọn trong danh sách khung của snapshot.</summary>
public sealed record SessionSelection(int[] Favorites, int?[] Slots, int Frame = 0);

public sealed record SessionSummary(CustomerSession Session, int KeptShots, int DiscardedShots, string? VoucherCode);

/// <summary>Một nguồn dữ liệu cho phiên khách: giao dịch, voucher, từng shot, lựa chọn và nhật ký chuyển trạng thái.</summary>
public interface ICustomerSessionStore
{
    /// <summary>Tạo phiên Paid, ghi giao dịch và trừ lượt voucher trong cùng một transaction.</summary>
    CustomerSession StartPaid(SessionSnapshot snapshot, SessionPayment payment);
    CustomerSession Get(Guid id);
    CustomerSession Transition(CustomerSession expected, CustomerStage next, string? note = null);
    void RecordShotRequested(Guid sessionId, Guid shotId, int seq);
    void RecordShotReceived(Guid shotId, string? path);
    void MarkShot(Guid shotId, ShotState state);
    IReadOnlyList<SessionShot> Shots(Guid sessionId);
    void SaveSelection(Guid sessionId, SessionSelection selection);
    SessionSelection? Selection(Guid sessionId);
    /// <summary>Phiên còn mở khi khởi động (máy tắt, app đóng giữa chừng) chuyển NeedsReview. Không tự làm lại gì.</summary>
    int RecoverInterrupted();
    IReadOnlyList<SessionSummary> List(DateTimeOffset? from = null, DateTimeOffset? to = null);
}

/// <summary>Lớp mỏng cho giao diện khách: giữ phiên hiện tại và ghi từng mốc. Không gọi thiết bị.</summary>
public sealed class CustomerSessionService(ICustomerSessionStore store)
{
    private int nextSeq;
    public CustomerSession? Current { get; private set; }

    public CustomerSession StartPaid(SessionSnapshot snapshot, SessionPayment payment)
    {
        if (Current is { IsOpen: true }) throw new InvalidOperationException("Đang có phiên chưa kết thúc.");
        Current = store.StartPaid(snapshot, payment);
        nextSeq = 0;
        return Current;
    }

    public void Advance(CustomerStage next, string? note = null)
    {
        if (Current is null || Current.Stage == next) return;
        Current = store.Transition(Current, next, note);
    }

    public Guid RequestShot()
    {
        if (Current is null) throw new InvalidOperationException("Chưa có phiên.");
        var shotId = Guid.NewGuid();
        store.RecordShotRequested(Current.Id, shotId, ++nextSeq);
        return shotId;
    }

    public void ShotReceived(Guid shotId, string? path) => store.RecordShotReceived(shotId, path);
    public IReadOnlyList<SessionShot> Shots() => Current is null ? [] : store.Shots(Current.Id);
    public void Keep(Guid shotId) => store.MarkShot(shotId, ShotState.Kept);
    public void Discard(Guid shotId) => store.MarkShot(shotId, ShotState.Discarded);

    public void SaveSelection(SessionSelection selection)
    {
        if (Current is not null) store.SaveSelection(Current.Id, selection);
    }

    /// <summary>Lỗi thiết bị hoặc đóng giữa chừng sau khi đã thu tiền: nhân viên xử lý (voucher bù).</summary>
    public void Fail(string reason)
    {
        if (Current is { IsOpen: true }) Current = store.Transition(Current, CustomerStage.NeedsReview, reason);
    }

    public void End() { Current = null; nextSeq = 0; }
}
