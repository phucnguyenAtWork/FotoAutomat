namespace FotoAutomat.Core;

public enum LayoutElementKind { Photo, Text, Image, Rectangle, Ellipse, SessionQr, SessionDate }

public sealed record PrintLayoutSlot(int PhotoNumber, double X, double Y, double Width, double Height,
    double Rotation = 0, double StrokeWidth = 0, string StrokeColor = "#222222",
    bool Shadow = false, string ShadowColor = "#000000", double ShadowAngle = 315,
    double ShadowDistance = 4, double ShadowBlur = 8, bool KeepAspect = false, bool Locked = false, LayoutElementKind Kind = LayoutElementKind.Photo, string Content = "", string FillColor = "#303030", double FontSize = 18, bool IsVisible = true);

/// <summary>400 x 600 layout units represent a portrait 4 x 6 inch sheet.</summary>
public sealed record PrintLayout(string Name, bool CutIntoStrips, PrintLayoutSlot[] Slots, string? ArtworkPng = null, bool ArtworkBehindPhotos = false, bool SingleStrip = false, string BackgroundColor = "#FFFFFF", int Dpi = 300, bool Landscape = false)
{
    public double PageWidth => Landscape ? 600 : SingleStrip ? 200 : 400;
    public double PageHeight => Landscape ? (SingleStrip ? 200 : 400) : 600;
    // Resolve once when preparing a print job; reprints reuse that job's timestamp.
    public PrintLayout ResolvePrintData(DateTimeOffset printTime) => this with
    {
        Slots = Slots.Select(slot => slot.Kind == LayoutElementKind.SessionDate
            ? slot with { Kind = LayoutElementKind.Text, Content = printTime.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture) }
            : slot).ToArray()
    };
    public void Validate()
    {
        if (Dpi is not (300 or 600)) throw new ArgumentException("DPI must be 300 or 600.");
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 80) throw new ArgumentException("Tên mẫu cần từ 1 đến 80 ký tự.");
        if (Slots is null || Slots.Length is < 1 or > 64) throw new ArgumentException("Mẫu cần từ 1 đến 16 ô ảnh.");
        if (Slots.Count(s => s?.Kind == LayoutElementKind.Photo) is < 1 or > 16)
            throw new ArgumentException("Mẫu cần từ 1 đến 16 ô ảnh khách.");
        if (!ValidColor(BackgroundColor)) throw new ArgumentException("Màu nền không hợp lệ.");
        foreach (PrintLayoutSlot slot in Slots)
        {
            if (slot is null || slot.PhotoNumber is < 1 or > 99 ||
                !double.IsFinite(slot.X) || !double.IsFinite(slot.Y) || !double.IsFinite(slot.Width) || !double.IsFinite(slot.Height) ||
                slot.X < 0 || slot.Y < 0 || slot.Width < 20 || slot.Height < 20 || slot.X + slot.Width > PageWidth || slot.Y + slot.Height > PageHeight)
                throw new ArgumentException("Ô ảnh phải nằm trong trang và có kích thước hợp lệ.");
            if (CutIntoStrips && slot.X < 200 && slot.X + slot.Width > 200)
                throw new ArgumentException("Ô ảnh không được nằm ngang đường cắt.");
        }
        foreach (var slot in Slots)
        {
            if (!double.IsFinite(slot.Rotation) || slot.Rotation is < -180 or > 180 ||
                !double.IsFinite(slot.StrokeWidth) || slot.StrokeWidth is < 0 or > 20 ||
                !double.IsFinite(slot.ShadowAngle) || slot.ShadowAngle is < 0 or > 360 ||
                !double.IsFinite(slot.ShadowDistance) || slot.ShadowDistance is < 0 or > 40 ||
                !double.IsFinite(slot.ShadowBlur) || slot.ShadowBlur is < 0 or > 60 ||
                !ValidColor(slot.StrokeColor) || !ValidColor(slot.ShadowColor))
                throw new ArgumentException("Viền, góc xoay hoặc bóng đổ không hợp lệ.");
            double angle = slot.Rotation * Math.PI / 180;
            double halfW = (Math.Abs(Math.Cos(angle)) * slot.Width + Math.Abs(Math.Sin(angle)) * slot.Height) / 2;
            double halfH = (Math.Abs(Math.Sin(angle)) * slot.Width + Math.Abs(Math.Cos(angle)) * slot.Height) / 2;
            double cx = slot.X + slot.Width / 2, cy = slot.Y + slot.Height / 2;
            double left = CutIntoStrips && cx >= 200 ? 200 : 0, right = CutIntoStrips && cx < 200 ? 200 : PageWidth;
            if (cx - halfW < left - 0.001 || cx + halfW > right + 0.001 || cy - halfH < -0.001 || cy + halfH > PageHeight + 0.001)
                throw new ArgumentException("Ô đã xoay vượt mép giấy hoặc đường cắt. Thu nhỏ hoặc di chuyển ô trước khi lưu.");
        }
        foreach (var slot in Slots)
        {
            if (!Enum.IsDefined(slot.Kind) || !ValidColor(slot.FillColor) || !double.IsFinite(slot.FontSize) || slot.FontSize is < 6 or > 100 ||
                slot.Content is null || slot.Content.Length > (slot.Kind == LayoutElementKind.Image ? 8_000_000 : 1000))
                throw new ArgumentException("Nội dung hoặc kiểu phần tử không hợp lệ.");
        }
        if ((long)(ArtworkPng?.Length ?? 0) + Slots.Sum(s => (long)s.Content.Length) > 28_000_000)
            throw new ArgumentException("Tổng ảnh trong mẫu quá lớn. Giảm kích thước ảnh trang trí trước khi lưu.");
        if (ArtworkPng is { Length: > 28_000_000 }) throw new ArgumentException("Ảnh khung quá lớn.");
    }
    private static bool ValidColor(string? value) => value is { Length: 7 } && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);
}
