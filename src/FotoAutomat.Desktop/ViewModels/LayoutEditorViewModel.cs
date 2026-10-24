using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.IO;
using System.Text.Json;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop.ViewModels;

public abstract class ObservableViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public sealed class EditorCommand(Action<object?> action) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => action(parameter);
}

public sealed partial class PhotoSlotViewModel : ObservableViewModel
{
    public const double MinimumQrSize = 55; // 0.55 inch at 100 layout units per inch.
    public double PixelX { get => X * 3; set { X = value / 3; Changed(); } }
    public double PixelY { get => Y * 3; set { Y = value / 3; Changed(); } }
    public double PixelWidth { get => Width * 3; set { Width = value / 3; Changed(); } }
    public double PixelHeight { get => Height * 3; set { Height = value / 3; Changed(); } }
    public PhotoSlotViewModel()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not (nameof(Appearance) or nameof(DisplayAppearance))) { Changed(nameof(Appearance)); Changed(nameof(DisplayAppearance)); }
            if (e.PropertyName == nameof(X)) Changed(nameof(PixelX));
            if (e.PropertyName == nameof(Y)) Changed(nameof(PixelY));
            if (e.PropertyName == nameof(Width)) Changed(nameof(PixelWidth));
            if (e.PropertyName == nameof(Height)) Changed(nameof(PixelHeight));
        };
    }
    public double PageWidth { get; set; } = 400;
    public double PageHeight { get; set; } = 600;
    private double x, y, width = 160, height = 220;
    private bool selected;
    private bool strips;
    private int photoNumber;
    public int Number { get; init; }
    public int PhotoNumber { get => photoNumber == 0 ? Number : photoNumber; set { if (value is < 1 or > 99) throw new ArgumentException("Số ảnh từ 1 đến 99."); photoNumber = value; Changed(); Changed(nameof(Label)); Changed(nameof(PlaceholderBrush)); Changed(nameof(LayerLabel)); } }
    public string Label => IsPhoto ? $"Ô {Number}" : IsSessionQr ? "Mã QR" : IsSessionDate ? "Ngày in" : $"{KindLabel} {Number}";
    public bool Strips { get => strips; set { strips = value; if (strips && x > 180 && x < 200) x = 180; Width = Width; X = X; } }
    public double X { get => x; set { x = Math.Clamp(double.IsFinite(value) ? value : 0, 0, PageWidth - Width); if (Strips && x < 200 && x + Width > 200) x = 200 - Width; Changed(); } }
    public double Y { get => y; set { y = Math.Clamp(double.IsFinite(value) ? value : 0, 0, PageHeight - Height); Changed(); } }
    public double Width { get => width; set => Resize(value, height, true); }
    public double Height { get => height; set => Resize(width, value, false); }
    private void Resize(double requestedWidth, double requestedHeight, bool byWidth)
    {
        double maxW = (Strips && X < 200 ? 200 : PageWidth) - X, maxH = PageHeight - Y;
        if (IsSessionQr)
        {
            double available = Math.Min(maxW, maxH);
            if (available < MinimumQrSize) return;
            double requested = byWidth ? requestedWidth : requestedHeight;
            width = height = Math.Clamp(double.IsFinite(requested) ? requested : MinimumQrSize, MinimumQrSize, available);
            Changed(nameof(Width)); Changed(nameof(Height)); return;
        }
        double ratio = width / height;
        if (KeepAspect)
        {
            double minW = Math.Max(20, 20 * ratio), upperW = Math.Min(maxW, maxH * ratio);
            if (upperW < minW) return;
            width = Math.Clamp(double.IsFinite(byWidth ? requestedWidth : requestedHeight)
                ? (byWidth ? requestedWidth : requestedHeight * ratio) : minW, minW, upperW);
            height = width / ratio;
        }
        else
        {
            width = Math.Clamp(double.IsFinite(requestedWidth) ? requestedWidth : 20, 20, maxW);
            height = Math.Clamp(double.IsFinite(requestedHeight) ? requestedHeight : 20, 20, maxH);
        }
        Changed(nameof(Width)); Changed(nameof(Height));
    }
    public bool IsSelected { get => selected; set { selected = value; Changed(); } }
}

public sealed record LayoutPreset(string Name, string Subtitle, int Count, bool Strips = false, bool Landscape = false, string Pattern = "stack")
{
    public PrintLayout CreateLayout()
    {
        double w = Landscape ? 600 : Strips ? 200 : 400, h = Landscape ? 400 : 600;
        int columns = Pattern == "row" ? Count : Pattern == "grid" ? 2 : 1;
        int rows = (int)Math.Ceiling(Count / (double)columns);
        double sw = (w - 48 - (columns - 1) * 12) / columns, sh = (h - 108 - (rows - 1) * 12) / rows;
        if (Pattern == "square") sh = sw = Math.Min(sw, sh);
        var slots = Enumerable.Range(0, Count).Select(i => new PrintLayoutSlot(i + 1, 24 + i % columns * (sw + 12), 24 + i / columns * (sh + 12), sw, sh)).ToArray();
        return new(Name, false, slots, SingleStrip: Strips, Landscape: Landscape);
    }

    public IEnumerable<int> Thumbnails => Enumerable.Range(1, Count).Select(n => n);
    public int Columns => Count >= 4 ? 2 : 1;
}

public sealed record NavigationEntry(string Group, string Title, string Description);
public sealed record FeatureArea(string Title, string Description);

public sealed partial class LayoutEditorViewModel : ObservableViewModel
{
    public ObservableCollection<SavedLayoutEntry> SavedLayouts { get; } = [];
    public SavedLayoutEntry? SelectedSavedLayout { get; set; }
    private bool artworkBehindPhotos;
    public bool ArtworkBehindPhotos { get => artworkBehindPhotos; set { artworkBehindPhotos = value; Changed(); Changed(nameof(ArtworkZIndex)); } }
    public int ArtworkZIndex => ArtworkBehindPhotos ? -1 : 1;
    private bool preview;
    public bool Preview { get => preview; set { preview = value; Changed(); Changed(nameof(IsEditing)); } }
    public bool IsEditing => !Preview;
    public ICommand DuplicateSlotCommand => new EditorCommand(_ =>
    {
        if (SelectedSlot is not { } source || Preview) return;
        if (Slots.Count >= 64 || Slots.Count(s => s.IsPhoto) >= 16) { Warn("Tối đa 16 ô ảnh trên một tờ."); return; }
        var copy = PhotoSlotViewModel.From(source.Export() with { Locked = false },
            Slots.Select(s => s.Number).Max() + 1, false, PageWidth);
        copy.X += 12; copy.Y += 12;
        Slots.Add(copy); SelectedSlot = copy;
        Status = "Đã nhân đôi ô. Đổi số ảnh nếu muốn dùng một ảnh khách khác.";
    });
    public ICommand AlignSlotCommand => new EditorCommand(parameter =>
    {
        if (SelectedSlot is not { } slot || slot.Locked || Preview) return;
        double left = 0;
        double areaWidth = PageWidth;
        switch (parameter?.ToString())
        {
            case "Left": slot.X = left; break;
            case "Center": slot.X = left + (areaWidth - slot.Width) / 2; break;
            case "Right": slot.X = left + areaWidth - slot.Width; break;
            case "Top": slot.Y = 0; break;
            case "Middle": slot.Y = (PageHeight - slot.Height) / 2; break;
            case "Bottom": slot.Y = PageHeight - slot.Height; break;
        }
    });
    public ICommand DistributeSlotsCommand => new EditorCommand(_ =>
    {
        if (SelectedSlot is not { } selected || selected.Locked || Preview) return;
        var column = Slots.Where(s => !s.Locked && Math.Abs(s.X - selected.X) < 1).OrderBy(s => s.Y).ToArray();
        if (column.Length < 3) { Warn("Căn các ô cùng vị trí X. Cần ít nhất 3 ô để chia đều khoảng cách dọc."); return; }
        double top = column[0].Y, bottom = column[^1].Y + column[^1].Height;
        double gap = (bottom - top - column.Sum(s => s.Height)) / (column.Length - 1);
        if (gap < 0) { Warn("Các ô chưa đủ chỗ. Giảm chiều cao hoặc kéo giãn khoảng từ ô đầu đến ô cuối."); return; }
        foreach (var slot in column) { slot.Y = top; top += slot.Height + gap; }
        Status = "Đã chia đều khoảng cách dọc giữa các ô cùng vị trí X.";
    });
    private bool cutIntoStrips;
    public bool CutIntoStrips
    {
        get => cutIntoStrips;
        set { cutIntoStrips = value; foreach (PhotoSlotViewModel slot in Slots) slot.Strips = value; Changed(); Changed(nameof(PaperSummary)); }
    }
    public string PaperSummary => SingleStrip ? "Một mẫu 2 × 6 inch · 600 × 1800 px" : "Một mẫu 4 × 6 inch · 1200 × 1800 px";
    private System.Windows.Media.ImageSource? artwork;
    private byte[]? artworkBytes;
    public System.Windows.Media.ImageSource? Artwork { get => artwork; private set { artwork = value; Changed(); Changed(nameof(HasArtwork)); } }
    public bool HasArtwork => Artwork is not null;
    public void SetArtwork(byte[]? bytes, System.Windows.Media.ImageSource? image) { artworkBytes = bytes; Artwork = image; }
    public PrintLayout ExportLayout() => new(TemplateName, CutIntoStrips,
        Slots.Select(slot => slot.Export()).ToArray(),
        artworkBytes is null ? null : Convert.ToBase64String(artworkBytes), ArtworkBehindPhotos, SingleStrip, BackgroundColor, Dpi, landscape);
    public void LoadLayout(PrintLayout layout)
    {
        layout.Validate(); OrientationIndex = layout.Landscape ? 1 : 0; Dpi = layout.Dpi; SingleStrip = layout.SingleStrip; BackgroundColor = layout.BackgroundColor; ArtworkBehindPhotos = layout.ArtworkBehindPhotos; SelectedSlot = null; Slots.Clear(); TemplateName = layout.Name; CutIntoStrips = layout.CutIntoStrips;
        preset = null; Changed(nameof(SelectedPreset));
        foreach (PrintLayoutSlot slot in layout.Slots)
            Slots.Add(PhotoSlotViewModel.From(slot, Slots.Count + 1, CutIntoStrips, PageWidth, PageHeight));
        SelectedSlot = Slots.FirstOrDefault();
    }
    private readonly CaptureConfigurationStore captureStore = new(CaptureConfigurationStore.DefaultPath);
    private int captureCountdown = 3;
    private CaptureConfiguration savedCapture = new();
    private string sessionMinutes = "7";
    private string reviewSeconds = "7";
    public string SessionMinutes { get => sessionMinutes; set { sessionMinutes = value; Changed(); Changed(nameof(CaptureDirty)); Changed(nameof(CaptureInputError)); } }
    public string ReviewSeconds { get => reviewSeconds; set { reviewSeconds = value; Changed(); Changed(nameof(CaptureDirty)); Changed(nameof(CaptureInputError)); } }
    public string CaptureInputError => ValidDuration(SessionMinutes) && ValidDuration(ReviewSeconds)
        ? "" : "Nhập số nguyên từ 1 đến 60.";
    private static bool ValidDuration(string text) => int.TryParse(text, out int value) && value is >= 1 and <= 60;
    public int[] CountdownChoices { get; } = Enumerable.Range(1, 29).ToArray();
    public int CaptureCountdown { get => captureCountdown; set { captureCountdown = value; Changed(); Changed(nameof(CaptureDirty)); } }
    public bool CaptureDirty => CaptureCountdown != savedCapture.CountdownSeconds || SessionMinutes != savedCapture.SessionMinutes.ToString() || ReviewSeconds != savedCapture.ReviewSeconds.ToString();
    public bool IsCaptureSettings => Section == "Cài đặt chế độ chụp";
    public ICommand SaveCaptureCommand { get; }
    public ICommand CancelCaptureCommand { get; }
    private LayoutPreset? preset;
    private PhotoSlotViewModel? selectedSlot;
    private string templateName = "Khung mới";
    private string section = "Bố cục in";
    private string status = "";
    public NavigationEntry[] Sections { get; } =
    [
        new("TỔNG QUAN", "Tổng quan", "Trạng thái booth, thiết bị và phiên chụp. Hiện đang dùng dữ liệu mô phỏng."),
        new("CHỤP ẢNH", "Máy ảnh", "Fujifilm X-H2 / Canon EOS R50 / EOS R100. SDK chưa kết nối; hỗ trợ từng model cần kiểm chứng trên thiết bị."),
        new("CHỤP ẢNH", "Cài đặt chế độ chụp", "Chọn thời gian chụp và thời gian xem ảnh cho mỗi phiên."),
        new("BỘ MÀU", "Bộ lọc màu", "Không gian tạo và tinh chỉnh preset cho giao diện khách hàng. Các công cụ dưới đây mới là bố cục chức năng, chưa xử lý hoặc lưu màu thật."),
        new("BỘ MÀU", "Chế độ cho khách", "Tên, mô tả và tính năng áp cho ảnh khách của từng bộ lọc đã phát hành."),
        new("KHUNG & IN", "Bố cục in", "Thiết kế vị trí các ô ảnh trên trang in."),
        new("KHUNG & IN", "Thư viện khung", "Quản lý artwork và các khung cho khách lựa chọn. Nhập, lưu và chọn khung trong thư viện bố cục."),
        new("KHUNG & IN", "Máy in", "Chọn máy in, khổ giấy và số bản in. Chưa kết nối máy in thật."),
        new("VẬN HÀNH", "Phiên chụp & thư viện", "Khu vực quản lý phiên chụp và ảnh đã lưu. Chưa triển khai thư viện ảnh."),
        new("VẬN HÀNH", "Bảng giá & thanh toán", "Gói chụp, giá dịch vụ và cấu hình máy nhận tiền. Chưa có giao dịch thật."),
        new("VẬN HÀNH", "Cài đặt chung", "Tên booth, thư mục ảnh và các thiết lập vận hành. Cấu hình hiện tại chỉ lưu bản nháp."),
        new("VẬN HÀNH", "Màn hình & thương hiệu", "Logo, nền và nội dung giao diện khách. Hiện dùng bộ preview Foto Hunter."),
        new("VẬN HÀNH", "Quyền quản trị", "Bảo mật truy cập quản trị. Thiết lập PIN mở khóa giao diện khách."),
        new("VẬN HÀNH", "Nhật ký hệ thống", "Khu vực dự kiến hiển thị lỗi và lịch sử hoạt động. Chưa có trình xem nhật ký."),
        new("VẬN HÀNH", "Thông tin ứng dụng", "FotoAutomat · WPF / .NET 10. Bản mô phỏng và xem thử giao diện."),
    ];
    public ObservableCollection<LayoutPreset> Presets { get; } = [
new("2 x 6 - 3 ô", "", 3, true), new("2 x 6 - 4 ô", "", 4, true), new("2 x 6 - 2 ô", "", 2, true),
new("4 x 6 - 3 ô", "", 3), new("4 x 6 - ô vuông", "", 1, Pattern: "square"), new("4 x 6 - 1 ô", "", 1),
new("6 x 4 - 3 ô lưới", "", 3, Landscape: true, Pattern: "grid"), new("6 x 4 - 1 ô", "", 1, Landscape: true),
new("6 x 4 - 3 ô ngang", "", 3, Landscape: true, Pattern: "row"), new("4 x 6 - 4 ô", "", 4, Pattern: "grid")];
    public ObservableCollection<PhotoSlotViewModel> Slots { get; } = [];
    public string Section { get => section; set { if (value is null) return; section = value; Changed(); Changed(nameof(IsLayout)); Changed(nameof(IsWorkspaceTools)); Changed(nameof(IsManagement)); Changed(nameof(IsGeneralSettings)); Changed(nameof(IsCaptureSettings)); Changed(nameof(IsPrinterSettings)); Changed(nameof(SectionDescription)); Changed(nameof(SectionGroup)); Changed(nameof(FeatureAreas)); } }
    public bool IsWorkspaceTools => Section is "Máy ảnh" or "Bộ lọc màu" or "Thông tin ứng dụng" or "Nhật ký hệ thống";
    public bool IsGeneralSettings => Section is "Cài đặt chung" or "Quyền quản trị" or "Màn hình & thương hiệu" or "Chế độ cho khách";
    public bool IsManagement => Section is "Tổng quan" or "Bảng giá & thanh toán" or "Phiên chụp & thư viện";
    public bool IsLayout => Section is "Bố cục in";
    public string SectionDescription => Sections.FirstOrDefault(entry => entry.Title == Section)?.Description ?? "";
    public string SectionGroup => Sections.FirstOrDefault(entry => entry.Title == Section)?.Group ?? "";
    public FeatureArea[] FeatureAreas => Section == "Bộ lọc màu"
        ? [new("01 / Refine", "Tinh chỉnh phơi sáng, tương phản, cân bằng trắng và sắc độ."),
           new("02 / Retouch", "Điều chỉnh da và mức độ làm đẹp; tách biệt với công thức màu."),
           new("03 / Match look", "Dùng ảnh tham chiếu để tạo gợi ý màu, sau đó tinh chỉnh thủ công."),
           new("04 / Preset cho khách", "Dự kiến lưu tên, ảnh xem trước và phiên bản preset; chỉ preset được duyệt mới đưa ra giao diện khách.")]
        : [];
    public string TemplateName { get => templateName; set { templateName = value; Changed(); } }
    private bool statusIsWarning;
    public bool StatusIsWarning { get => statusIsWarning; private set { statusIsWarning = value; Changed(); } }
    public event Action<string>? WarningRequested;
    public string Status { get => status; set { status = value; StatusIsWarning = false; Changed(); } }
    public void Warn(string message) { Status = message; StatusIsWarning = true; WarningRequested?.Invoke(message); }
    public Action? PresetOpened { get; set; }
    public Func<bool>? ConfirmReplaceLayout { get; set; }
    public LayoutPreset? SelectedPreset
    {
        get => preset;
        set
        {
            if (value is null || value == preset) return;
            if (ConfirmReplaceLayout?.Invoke() == false) { Changed(); return; }
            preset = value; Changed(); Apply(value); PresetOpened?.Invoke();
        }
    }
    public PhotoSlotViewModel? SelectedSlot
    {
        get => selectedSlot;
        set { if (selectedSlot is not null) selectedSlot.IsSelected = false; selectedSlot = value; if (value is not null) value.IsSelected = true; Changed(); Changed(nameof(HasSelection)); }
    }
    public bool HasSelection => SelectedSlot is not null;
    public ICommand SelectSlotCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand AddSlotCommand { get; }
    public ICommand RemoveSlotCommand { get; }
    public LayoutEditorViewModel()
    {
        try
        {
            savedCapture = captureStore.Load();
            captureCountdown = savedCapture.CountdownSeconds;
            sessionMinutes = savedCapture.SessionMinutes.ToString();
            reviewSeconds = savedCapture.ReviewSeconds.ToString();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        { Warn($"Không đọc được cấu hình chụp: {ex.Message}"); }
        SaveCaptureCommand = new EditorCommand(_ =>
        {
            try
            {
                if (CaptureInputError.Length > 0) { Warn(CaptureInputError); return; }
                CaptureConfiguration value = captureStore.Load() with { CountdownSeconds = CaptureCountdown, SessionMinutes = int.Parse(SessionMinutes), ReviewSeconds = int.Parse(ReviewSeconds) };
                captureStore.Save(value); savedCapture = value; Changed(nameof(CaptureDirty));
                SessionMinutes = value.SessionMinutes.ToString();
                ReviewSeconds = value.ReviewSeconds.ToString();
                Status = "Đã lưu cấu hình chụp. Áp dụng khi mở lại cửa sổ khách.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            { Warn($"Không lưu được: {ex.Message}"); }
        });
        CancelCaptureCommand = new EditorCommand(_ =>
        {
            CaptureCountdown = savedCapture.CountdownSeconds;
            SessionMinutes = savedCapture.SessionMinutes.ToString();
            ReviewSeconds = savedCapture.ReviewSeconds.ToString();
        });
        SelectSlotCommand = new EditorCommand(value => SelectedSlot = value as PhotoSlotViewModel);
        ResetCommand = new EditorCommand(_ => { if (SelectedPreset is not null) Apply(SelectedPreset); });
        AddSlotCommand = new EditorCommand(_ =>
        {
            if (Preview) return;
            if (Slots.Count >= 64 || Slots.Count(s => s.IsPhoto) >= 16) { Warn("Tối đa 16 ô ảnh trên một tờ."); return; }
            PhotoSlotViewModel slot = new() { Number = Slots.Select(s => s.Number).DefaultIfEmpty().Max() + 1, PhotoNumber = Math.Min(99, Slots.Where(s => s.IsPhoto).Select(s => s.PhotoNumber).DefaultIfEmpty().Max() + 1), PageWidth = PageWidth, PageHeight = PageHeight, X = 20, Y = 30 };
            Slots.Add(slot); SelectedSlot = slot;
        });
        RemoveSlotCommand = new EditorCommand(_ => { if (Preview || SelectedSlot?.Locked == true) return; if (SelectedSlot is not null) Slots.Remove(SelectedSlot); SelectedSlot = null; });
        InitializeHistory();
        LoadPrinterSettings();
    }
    private void Apply(LayoutPreset value)
    {
        LoadLayout(value.CreateLayout()); SetArtwork(null, null); Preview = false;
        preset = value; Changed(nameof(SelectedPreset));
    }
}

public sealed record SavedLayoutEntry(string Path, string Name);
