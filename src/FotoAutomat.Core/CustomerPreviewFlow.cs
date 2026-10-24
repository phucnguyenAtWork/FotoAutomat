using System.ComponentModel;
using System.Globalization;

namespace FotoAutomat.Core;

public enum CustomerScreen { Welcome, Filter, Layout, Payment, QrPayment, CashPayment, Instructions, Capture, Selection, Beauty, Frame, Printing, Delivery }
public sealed record PreviewPackage(int Quantity, int Price, string Name = "")
{
    public string Label => (Name.Length > 0 ? Name : $"{Quantity} bản") + (SingleStrip is { } strip ? strip ? " 2 × 6" : " 4 × 6" : "") + " · " + PriceLabel;
    public string PriceLabel => Price.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + "đ";
    /// <summary>true: khổ 2 × 6; false: khổ 4 × 6; null: gói không gắn khổ, dùng mọi khung.</summary>
    public bool? SingleStrip { get; init; }
}
public sealed record PreviewSlot(int Index, int? Photo, int PhotoNumber = 1, double X = 0, double Y = 0, double Width = 160, double Height = 220, PrintLayoutSlot? Appearance = null)
{
    public bool IsPhoto => Appearance is null || Appearance.Kind == LayoutElementKind.Photo;
    public string Label => Photo is { } number ? $"Ảnh mẫu {number}" : "+";
}
public sealed record CopyChoice(int Count, bool Selected) { public string Tag => "c" + Count; }
public sealed record FrameChoice(int Index, string Name, bool Selected) { public string Tag => "l" + Index; }
/// <summary>Voucher đã kiểm tra hợp lệ. Discount tính bằng đồng; Free miễn phí cả lượt.</summary>
public sealed record AppliedVoucher(string Code, int Discount, bool Free);
public sealed record PreviewPhoto(int Number, bool Favorite)
{
    public string Label => $"Ảnh mẫu {Number}" + (Favorite ? " ✓" : "");
}

/// <summary>Customer timing and selection preview; all photos and payments are simulated.</summary>
public sealed class CustomerPreviewFlow : INotifyPropertyChanged
{
    private readonly CaptureConfiguration configuration;
    private readonly IReadOnlyList<PrintLayout> frames;
    private PrintLayout? activeLayout => FrameChoices.ElementAtOrDefault(LayoutIndex);
    public bool UsesSampleFrames => activeLayout is null;
    public double FrameHeight => activeLayout?.PageHeight ?? 600;
    public double FrameWidth => activeLayout?.PageWidth ?? 400;
    public string FrameBackground => activeLayout?.BackgroundColor ?? "#FFFFFF";
    public string FrameTitle => activeLayout?.Name ?? "Chọn khung và ghép ảnh";
    private TimedCapture? capture;
    private readonly bool requireCameraAcknowledgement;
    private readonly List<string> capturedPaths = [];
    public IReadOnlyList<string> CapturedPaths => capturedPaths.AsReadOnly();
    public bool CaptureRequested => capture?.AwaitingCapture == true && capture.CaptureError is null;
    public string? CaptureError => capture?.CaptureError;
    public bool IsReviewingCapture => capture?.Reviewing == true;
    private CustomerScreen paymentScreen = CustomerScreen.QrPayment;
    private readonly HashSet<int> favorites = [];
    private int filterCount = 3, defaultFilter;
    private readonly IReadOnlyList<PreviewPackage> options;
    public CustomerPreviewFlow(CaptureConfiguration? configuration = null, PrintLayout? activeLayout = null, int filterCount = 3, int defaultFilter = 0, bool requireCameraAcknowledgement = false, IReadOnlyList<PreviewPackage>? packages = null, IReadOnlyList<PrintLayout>? frames = null)
    {
        if (packages is not null && packages.Any(p => p.Quantity < 1 || p.Price < 0)) throw new ArgumentException("Invalid package.");
        options = packages is { Count: > 0 } ? packages : Packages;
        this.requireCameraAcknowledgement = requireCameraAcknowledgement;
        this.configuration = configuration ?? new();
        this.configuration.Validate();
        this.frames = frames ?? (activeLayout is null ? [] : [activeLayout]);
        foreach (var frame in this.frames) frame.Validate();
        PackageIndex = FirstAvailablePackage();
        ConfigureFilters(filterCount, defaultFilter); FilterIndex = this.defaultFilter;
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public static IReadOnlyList<PreviewPackage> Packages { get; } = [new(2, 80000), new(4, 140000), new(6, 210000), new(8, 280000)];
    public IReadOnlyList<PreviewPackage> PackageOptions => options;
    public CustomerScreen Screen { get; private set; }
    public int FilterIndex { get; private set; }
    public int LayoutIndex { get; private set; }
    public int PackageIndex { get; private set; }
    public int CapturedCount => capture?.CapturedCount ?? 0;
    public int Countdown => capture?.PhaseSeconds ?? configuration.CountdownSeconds;
    public int SelectedPhoto { get; private set; } = 1;
    private int beautyLevel;
    public int BeautyLevel { get => beautyLevel; set { beautyLevel = Math.Clamp(value, 0, 100); Changed(); } }
    public string BeautyPhotoLabel => $"Ảnh mẫu {SelectedPhoto}";
    public string DeliverySummary => $"{CapturedCount} ảnh chụp · {favorites.Count} ảnh đã chọn · {Copies} bản in";
    public int PrintProgress { get; private set; }
    private bool externalPrint;
    /// <summary>Lỗi tạo file in; khách không hoàn tất được và cần nhân viên.</summary>
    public string PrintError { get; private set; } = "";
    public bool HasPrintError => PrintError.Length > 0;
    /// <summary>Tiến độ in do dịch vụ xử lý ảnh báo (thay cho mô phỏng).</summary>
    public void ReportPrintProgress(int percent)
    {
        if (Screen != CustomerScreen.Printing) return;
        externalPrint = true; PrintProgress = Math.Clamp(percent, 0, 100); Changed();
    }
    public void FailPrint(string message)
    {
        if (Screen != CustomerScreen.Printing) return;
        externalPrint = true; PrintError = message; Changed();
    }
    public PreviewSlot[] Slots { get; private set; } = [];
    public PreviewPhoto[] Photos => Enumerable.Range(1, CapturedCount).Select(n => new PreviewPhoto(n, favorites.Contains(n))).ToArray();
    public PreviewPhoto[] FavoritePhotos => Photos.Where(p => p.Favorite).ToArray();
    public AppliedVoucher? Voucher { get; private set; }
    public int Price => options[PackageIndex].Price;
    // Tính lại theo khổ đang chọn, nên đổi khổ sau khi nhập voucher vẫn đúng.
    public int Discount => Voucher is null ? 0 : Voucher.Free ? Price : Math.Min(Price, Voucher.Discount);
    public int AmountDue => Price - Discount;
    public bool IsFree => AmountDue == 0;
    public bool NeedsPayment => !IsFree;
    public bool HasVoucher => Voucher is not null;
    public string VoucherSummary => Voucher is null ? "" : Voucher.Free ? "Miễn phí lượt chụp" : "Giảm " + FormatVnd(Discount);
    public string LoadingCaption => configuration.RetakeLimit > 0 ? $"{configuration.Shots} ảnh · chụp lại tối đa {configuration.RetakeLimit} lần" : $"{configuration.Shots} ảnh";
    public string Amount => FormatVnd(AmountDue);
    public string PriceLabel => FormatVnd(Price);
    public string DiscountLabel => Discount > 0 ? "−" + FormatVnd(Discount) : "";
    public static string FormatVnd(int value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + "đ";
    public void ApplyVoucher(AppliedVoucher voucher) { if (Screen != CustomerScreen.Payment) return; Voucher = voucher; Changed(); }
    public void ClearVoucher() { if (Screen != CustomerScreen.Payment || Voucher is null) return; Voucher = null; Changed(); }
    /// <summary>Voucher phủ hết số tiền: bỏ qua tiền mặt/QR và vào phiên chụp.</summary>
    public void StartFree()
    {
        if (Screen != CustomerScreen.Payment || !IsFree) return;
        paymentScreen = CustomerScreen.Payment; Screen = CustomerScreen.Instructions; Changed();
    }
    public int RetakeLimit => configuration.RetakeLimit;
    public int RetakesLeft => capture?.RetakesLeft ?? configuration.RetakeLimit;
    public bool CanRetake => Screen == CustomerScreen.Capture && capture?.CanRetake == true;
    public string RetakeLabel => $"Chụp lại – còn {RetakesLeft} shots";
    public int ShotCount => configuration.Shots;
    public void RetakeLast()
    {
        if (!CanRetake) return;
        capture!.Retake();
        if (capturedPaths.Count > CapturedCount) capturedPaths.RemoveAt(capturedPaths.Count - 1);
        Changed();
    }
    public string LayoutName => activeLayout?.Name ?? (LayoutIndex == 0 ? "Khung 3 ảnh" : "Khung 4 ảnh");
    public string OrderSummary => $"{Copies} bản in · {configuration.Shots} ảnh chụp";
    /// <summary>Khung đã phát hành cùng khổ với gói đang chọn; khách chọn một khung ở bước Khung.</summary>
    public IReadOnlyList<PrintLayout> FrameChoices => [.. frames.Where(f => options[PackageIndex].SingleStrip is not { } strip || f.SingleStrip == strip)];
    public bool HasFrameChoice => FrameChoices.Count > 1;
    public PrintLayout? CurrentFrame => activeLayout;
    public FrameChoice[] FrameChoiceItems => [.. FrameChoices.Select((f, i) => new FrameChoice(i, f.Name, i == LayoutIndex))];
    public CopyChoice[] CopyChoices => [.. CopyOptions.Select(c => new CopyChoice(c, c == Copies))];
    /// <summary>Khổ chưa có khung nào được phát hành thì không chọn được (trừ khi chưa phát hành khung nào: dùng khung mẫu).</summary>
    public bool IsPackageAvailable(int index) => index >= 0 && index < options.Count && (frames.Count == 0 || options[index].SingleStrip is not { } strip || frames.Any(f => f.SingleStrip == strip));
    public bool[] PackageAvailability => [.. Enumerable.Range(0, options.Count).Select(IsPackageAvailable)];
    private int FirstAvailablePackage() => Enumerable.Range(0, options.Count).FirstOrDefault(IsPackageAvailable);
    /// <summary>Số tấm in. Khổ 2 × 6 in hai dải trên một tờ rồi cắt, nên chỉ số chẵn.</summary>
    public int Copies { get; private set; } = 2;
    public int[] CopyOptions => options[PackageIndex].SingleStrip == true ? [2, 4, 6, 8] : [2, 3, 4, 5, 6, 7, 8];
    public void SelectCopies(int copies) { if (Screen == CustomerScreen.Layout && CopyOptions.Contains(copies)) { Copies = copies; Changed(); } }
    public string PackageHeading => $"Chọn khổ in · {configuration.Shots} ảnh chụp";
    public string CaptureInstructions => $"Bạn được chụp {configuration.Shots} ảnh, xem lại mỗi ảnh {configuration.ReviewSeconds} giây"
        + (configuration.RetakeLimit > 0 ? $" và chụp lại tối đa {configuration.RetakeLimit} lần." : ".");
    public string FilterName => $"Bộ lọc {FilterIndex + 1}";
    public string CaptureLabel => CaptureError ?? (CaptureRequested ? "Đang nhận ảnh từ camera…" : NormalCaptureLabel);
    private string NormalCaptureLabel => capture?.Reviewing == true ? $"Ảnh {CapturedCount}/{configuration.Shots} · Xem lại {Countdown} giây" : $"Chuẩn bị chụp ảnh {CapturedCount + 1}/{configuration.Shots}";
    public string CaptureDisplay => CaptureError is not null ? "Đã dừng" : CaptureRequested ? "…" : NormalCaptureDisplay;
    // Ảnh thật đang xem lại thì không che chữ lên ảnh; bản xem trước dùng ảnh mẫu nên ghi rõ.
    private string NormalCaptureDisplay => capture?.Reviewing == true ? (requireCameraAcknowledgement ? "" : $"Ảnh mẫu {CapturedCount}") : Countdown.ToString(CultureInfo.InvariantCulture);
    public bool ShowSessionClock => Screen == CustomerScreen.Capture && capture?.ShowSessionClock == true;
    public string SessionClock => capture?.RemainingSeconds == 0 ? "Hết giờ · Hoàn tất ảnh đang chụp…" : $"Thời gian chụp còn lại: {TimeSpan.FromSeconds(capture?.RemainingSeconds ?? 420):mm\\:ss}";
    public bool CanSelectPhotos => capture?.Finished == true;
    public bool CanChooseFrame => Screen == CustomerScreen.Selection && favorites.Count > 0;
    public bool CanPrint => Screen == CustomerScreen.Frame && Slots.Any(x => x.IsPhoto) && Slots.Where(x => x.IsPhoto).All(x => x.Photo.HasValue);
    public bool CanFinishPrint => Screen == CustomerScreen.Printing && PrintProgress == 100 && PrintError.Length == 0;
    public string SelectionHint => Screen == CustomerScreen.Selection ? $"Đã chọn {favorites.Count} ảnh yêu thích. Chạm lại để bỏ chọn." : $"Đang chọn ảnh {SelectedPhoto}. Chạm một ô để đặt ảnh vào khung.";
    public int ProgressStep => Screen switch
    {
        CustomerScreen.Welcome => -1,
        CustomerScreen.Filter => 0,
        CustomerScreen.Layout => 1,
        CustomerScreen.Payment => 2,
        CustomerScreen.QrPayment or CustomerScreen.CashPayment => 3,
        CustomerScreen.Instructions => 4,
        CustomerScreen.Capture => 5,
        CustomerScreen.Selection => 6,
        CustomerScreen.Beauty => 7,
        CustomerScreen.Frame => 8,
        CustomerScreen.Printing => 9,
        _ => 10
    };
    public void ConfigureFilters(int count, int defaultIndex = 0) { if (count is < 1 or > 100 || defaultIndex < 0 || defaultIndex >= count) throw new ArgumentException("Invalid preset library."); filterCount = count; defaultFilter = defaultIndex; FilterIndex = Math.Min(FilterIndex, count - 1); }
    public void SelectFilter(int index) { if (Screen == CustomerScreen.Filter && index >= 0 && index < filterCount) { FilterIndex = index; Changed(); } }
    public void SelectPackage(int index)
    {
        if (Screen != CustomerScreen.Layout || !IsPackageAvailable(index)) return;
        PackageIndex = index; LayoutIndex = 0; Slots = [];
        if (!CopyOptions.Contains(Copies)) Copies = CopyOptions.FirstOrDefault(c => c >= Copies, CopyOptions[^1]);
        Changed();
    }
    public void SelectLayout(int index)
    {
        int count = FrameChoices.Count > 0 ? FrameChoices.Count : 2;
        if (Screen != CustomerScreen.Frame || index < 0 || index >= count) return;
        LayoutIndex = index;
        Slots = activeLayout is not null
            ? activeLayout.Slots.Where(slot => slot.IsVisible).Select((slot, i) => new PreviewSlot(i, null, slot.PhotoNumber, slot.X, slot.Y, slot.Width, slot.Height, slot)).ToArray()
            : Enumerable.Range(0, index == 0 ? 3 : 4).Select(i => index == 0
                ? new PreviewSlot(i, null, i + 1, 24, 24 + i * 184, 352, 172)
                : new PreviewSlot(i, null, i + 1, 24 + i % 2 * 182, 24 + i / 2 * 270, 170, 258)).ToArray();
        Changed();
    }
    public void ChoosePayment(bool cash)
    {
        if (Screen != CustomerScreen.Payment) return;
        paymentScreen = cash ? CustomerScreen.CashPayment : CustomerScreen.QrPayment;
        Screen = paymentScreen; Changed();
    }
    public void Next()
    {
        if (Screen == CustomerScreen.Instructions) capture = new(configuration, requireCameraAcknowledgement);
        Screen = Screen switch
        {
            CustomerScreen.Welcome => CustomerScreen.Filter,
            CustomerScreen.Filter => CustomerScreen.Layout,
            CustomerScreen.Layout when IsPackageAvailable(PackageIndex) => CustomerScreen.Payment,
            CustomerScreen.QrPayment or CustomerScreen.CashPayment => CustomerScreen.Instructions,
            CustomerScreen.Instructions => CustomerScreen.Capture,
            CustomerScreen.Selection when CanChooseFrame => CustomerScreen.Beauty,
            CustomerScreen.Beauty => CustomerScreen.Frame,
            CustomerScreen.Frame when CanPrint => CustomerScreen.Printing,
            CustomerScreen.Printing when CanFinishPrint => CustomerScreen.Delivery,
            _ => Screen
        };
        if (Screen == CustomerScreen.Frame && Slots.Length == 0) SelectLayout(LayoutIndex);
        Changed();
    }
    public void Back()
    {
        Screen = Screen switch
        {
            CustomerScreen.Filter => CustomerScreen.Welcome,
            CustomerScreen.Layout => CustomerScreen.Filter,
            CustomerScreen.Payment => CustomerScreen.Layout,
            CustomerScreen.QrPayment or CustomerScreen.CashPayment => CustomerScreen.Payment,
            CustomerScreen.Instructions => paymentScreen,
            CustomerScreen.Frame => CustomerScreen.Beauty,
            CustomerScreen.Beauty => CustomerScreen.Selection,
            _ => Screen
        };
        Changed();
    }
    public void Tick(TimeSpan? elapsed = null)
    {
        if (Screen == CustomerScreen.Capture && capture is not null)
        {
            capture.Advance(elapsed ?? TimeSpan.FromSeconds(1));
            if (capture.Finished)
            {
                favorites.Clear();
                for (int i = 1; i <= CapturedCount; i++) favorites.Add(i);
                SelectedPhoto = 1;
                Screen = CustomerScreen.Selection;
            }
            Changed();
        }
        else if (Screen == CustomerScreen.Printing && !externalPrint && PrintProgress < 100) { PrintProgress = Math.Min(100, PrintProgress + 20); Changed(); }
    }
    public void CompleteCameraCapture(string originalPath)
    {
        if (!requireCameraAcknowledgement || Screen != CustomerScreen.Capture || !CaptureRequested) throw new InvalidOperationException("No camera capture requested.");
        ArgumentException.ThrowIfNullOrWhiteSpace(originalPath);
        if (capturedPaths.Contains(originalPath, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("Each capture must have a unique original file.");
        capture!.ConfirmCapture(); capturedPaths.Add(originalPath); Changed();
    }
    public void FailCameraCapture(string message)
    {
        if (!requireCameraAcknowledgement || Screen != CustomerScreen.Capture || !CaptureRequested) throw new InvalidOperationException("No camera capture requested.");
        capture!.FailCapture(message); Changed();
    }
    public void SelectPhoto(int number)
    {
        if (number < 1 || number > CapturedCount) return;
        if (Screen == CustomerScreen.Selection)
        {
            if (!favorites.Add(number)) favorites.Remove(number);
            Slots = [];
            SelectedPhoto = favorites.Order().FirstOrDefault(1);
        }
        else if (Screen is (CustomerScreen.Frame or CustomerScreen.Beauty) && favorites.Contains(number)) SelectedPhoto = number;
        else return;
        Changed();
    }
    public void AssignSlot(int index)
    {
        if (Screen != CustomerScreen.Frame || index < 0 || index >= Slots.Length || !favorites.Contains(SelectedPhoto)) return;
        if (!Slots[index].IsPhoto) return;
        int source = Slots[index].PhotoNumber;
        Slots = Slots.Select(x => x.IsPhoto && x.PhotoNumber == source ? x with { Photo = SelectedPhoto } : x).ToArray(); Changed();
    }
    public void Restart()
    {
        Screen = CustomerScreen.Welcome; paymentScreen = CustomerScreen.QrPayment; Voucher = null;
        FilterIndex = LayoutIndex = PrintProgress = 0; externalPrint = false; PrintError = "";
        FilterIndex = defaultFilter; PackageIndex = FirstAvailablePackage(); Copies = 2;
        capturedPaths.Clear();
        BeautyLevel = 0; SelectedPhoto = 1; capture = null; favorites.Clear(); Slots = []; Changed();
    }
    private void Changed() => PropertyChanged?.Invoke(this, new(string.Empty));
}
