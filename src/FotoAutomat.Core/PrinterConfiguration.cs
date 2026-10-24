namespace FotoAutomat.Core;

public sealed record PrinterConfiguration(bool PairAndCut = true, int Copies = 1)
{
    public PrinterProfile Main { get; set; } = new();
    public PrinterProfile Secondary { get; set; } = new();
    public bool AutomaticPrint { get; set; } = true;
    public bool ShowPrintButton { get; set; } = true;
    public bool AlternatePrinters { get; set; }
    public bool LimitsEnabled { get; set; }
    public int EventLimit { get; set; } = 5000;
    public int SessionLimit { get; set; } = 5;
    public string PrintLabel { get; set; } = "In ảnh";
    public string PrintingLabel { get; set; } = "Đang in…";
    public string CopiesLabel { get; set; } = "Số bản in";
    public void Validate()
    {
        if (Copies is < 1 or > 10) throw new ArgumentException("Số bản in từ 1 đến 10.");
        if (Main is null || Secondary is null) throw new ArgumentException("Thiếu cấu hình máy in.");
        Main.Validate(); Secondary.Validate();
        if (EventLimit < 1 || SessionLimit < 1) throw new ArgumentException("Giới hạn in phải lớn hơn 0.");
        if (AlternatePrinters && (string.IsNullOrWhiteSpace(Main.QueueName) || string.IsNullOrWhiteSpace(Secondary.QueueName) || Main.QueueName == Secondary.QueueName)) throw new ArgumentException("Chọn hai máy in khác nhau để in luân phiên.");
        if (string.IsNullOrWhiteSpace(PrintLabel) || string.IsNullOrWhiteSpace(PrintingLabel) || string.IsNullOrWhiteSpace(CopiesLabel)) throw new ArgumentException("Nhập đầy đủ nhãn hiển thị.");
    }
    public double[] PlacementOffsets(PrintLayout layout)
    {
        Validate(); layout.Validate();
        if (PairAndCut && !layout.SingleStrip) throw new ArgumentException("Chế độ hai dải cần mẫu 2 × 6. Chọn in nguyên tờ cho mẫu 4 × 6.");
        return PairAndCut ? [0, 200] : [0];
    }
    public PrintSheetPlan CreateSheetPlan(PrintLayout layout)
    {
        Validate(); layout.Validate();
        bool paired = PairAndCut && layout.SingleStrip;
        int stripWidth = (int)Math.Round(layout.PageWidth * layout.Dpi / 100);
        int stripHeight = (int)Math.Round(layout.PageHeight * layout.Dpi / 100);
        return new PrintSheetPlan(
            paired && !layout.Landscape ? stripWidth * 2 : stripWidth,
            paired && layout.Landscape ? stripHeight * 2 : stripHeight,
            layout.Dpi, Copies,
            paired ? [new(0, 0, stripWidth, stripHeight), new(layout.Landscape ? 0 : stripWidth, layout.Landscape ? stripHeight : 0, stripWidth, stripHeight)] : [new(0, 0, stripWidth, stripHeight)]);
    }
}

public sealed record PrintPlacement(int X, int Y, int Width, int Height);
public sealed record PrintSheetPlan(int Width, int Height, int Dpi, int Copies, PrintPlacement[] Placements);

public sealed record PrinterProfile : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    public string QueueName { get; set; } = "";
    public string TicketXml { get; set; } = "";
    private double scale = 100, offsetX, offsetY;
    public double Scale { get => scale; set { scale = value; PropertyChanged?.Invoke(this, new(nameof(Scale))); } }
    public double OffsetX { get => offsetX; set { offsetX = value; PropertyChanged?.Invoke(this, new(nameof(OffsetX))); } }
    public double OffsetY { get => offsetY; set { offsetY = value; PropertyChanged?.Invoke(this, new(nameof(OffsetY))); } }
    public void Validate()
    {
        if (!double.IsFinite(Scale) || Scale is < 50 or > 150 || !double.IsFinite(OffsetX) || Math.Abs(OffsetX) > 20 || !double.IsFinite(OffsetY) || Math.Abs(OffsetY) > 20)
            throw new ArgumentException("Tỉ lệ từ 50–150%; độ lệch từ −20 đến 20 mm.");
    }
}
