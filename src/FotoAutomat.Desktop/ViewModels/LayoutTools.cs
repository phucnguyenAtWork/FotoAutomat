using FotoAutomat.Core;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FotoAutomat.Desktop.ViewModels;

public sealed partial class PhotoSlotViewModel
{
    public void ScalePage(double pw, double ph, double sx, double sy) { PageWidth = pw; PageHeight = ph; x *= sx; y *= sy; if (!IsSessionQr) { width *= sx; height *= sy; } NormalizeQrSize(); Changed(nameof(X)); Changed(nameof(Y)); Changed(nameof(Width)); Changed(nameof(Height)); }
    private void NormalizeQrSize()
    {
        if (!IsSessionQr) return;
        width = height = Math.Clamp(Math.Max(width, height), MinimumQrSize, Math.Min(PageWidth, PageHeight));
        x = Math.Clamp(x, 0, PageWidth - width); y = Math.Clamp(y, 0, PageHeight - height);
    }
    private string? testPhoto;
    public bool HasTestPhoto => testPhoto is not null;
    public bool IsSessionDate => Kind == LayoutElementKind.SessionDate;
    public bool CanChooseAspect => Kind is LayoutElementKind.Photo or LayoutElementKind.Image;
    public bool IsSessionQr => Kind == LayoutElementKind.SessionQr;
    public PrintLayoutSlot DisplayAppearance => IsPhoto && testPhoto is not null ? Export() with { Kind = LayoutElementKind.Image, Content = testPhoto } : Appearance;
    public void SetTestPhoto(byte[]? bytes)
    {
        testPhoto = bytes is null ? null : Convert.ToBase64String(bytes);
        Changed(nameof(HasTestPhoto)); Changed(nameof(DisplayAppearance));
    }
    private LayoutElementKind kind;
    private string content = "", fillColor = "#303030";
    private double fontSize = 18;
    public LayoutElementKind Kind { get => kind; set { kind = value; Changed(); Changed(nameof(IsPhoto)); Changed(nameof(IsText)); Changed(nameof(Label)); } }
    public bool IsPhoto => Kind == LayoutElementKind.Photo;
    public bool IsDecoration => !IsPhoto;
    public bool HasFillColor => Kind is LayoutElementKind.SessionDate or LayoutElementKind.Text or LayoutElementKind.Rectangle or LayoutElementKind.Ellipse;
    public bool IsText => Kind == LayoutElementKind.Text;
    public string KindLabel => Kind switch { LayoutElementKind.SessionQr => "QR phiên", LayoutElementKind.Text => "Chữ", LayoutElementKind.Image => "Hình ảnh", LayoutElementKind.Rectangle => "Chữ nhật", LayoutElementKind.Ellipse => "Elip", _ => "Ảnh" };
    public string Content { get => content; set { content = value; Changed(); } }
    public string FillColor { get => fillColor; set { fillColor = Color(value); Changed(); } }
    public double FontSize { get => fontSize; set { fontSize = Limit(value, 6, 100); Changed(); } }
    public PrintLayoutSlot Appearance => Export();
    public void ChangePageWidth(double value)
    {
        double factor = value / PageWidth;
        x *= factor; width = Math.Max(20, width * factor); PageWidth = value;
        x = Math.Clamp(x, 0, PageWidth - width);
        NormalizeQrSize();
        Changed(nameof(X)); Changed(nameof(Y)); Changed(nameof(Width)); Changed(nameof(Height));
    }
}

public sealed partial class LayoutEditorViewModel
{
    public System.Collections.ObjectModel.ObservableCollection<LayoutGalleryItem> Gallery { get; } = [];
    public ICommand AddSessionDateCommand => new EditorCommand(_ =>
    {
        AddDecoration(LayoutElementKind.SessionDate, "dd/MM/yyyy");
        if (SelectedSlot is { IsSessionDate: true } date) PlaceInFreeArea(date);
    });
    public ICommand AddQrCommand => new EditorCommand(_ =>
    {
        if (Slots.Any(s => s.IsSessionQr)) { SelectedSlot = Slots.First(s => s.IsSessionQr); return; }
        AddDecoration(LayoutElementKind.SessionQr, "");
        if (SelectedSlot is { IsSessionQr: true } qr) { qr.Width = 55; qr.Height = 55; qr.KeepAspect = true; PlaceInFreeArea(qr); }
    });
    public ICommand ClearTestPhotoCommand => new EditorCommand(_ => SelectedSlot?.SetTestPhoto(null));
    public int[] DpiOptions { get; } = [300, 600];
    private int dpi = 300;
    public int Dpi { get => dpi; set { if (value is not (300 or 600)) return; dpi = value; Changed(); Changed(nameof(PixelDimensions)); Changed(nameof(PagePixelWidth)); Changed(nameof(PagePixelHeight)); } }
    public string PixelDimensions => $"{PageWidth / 100 * Dpi:0} x {PageHeight / 100 * Dpi} px";
    private bool singleStrip;
    private string backgroundColor = "#FFFFFF";
    public bool SingleStrip
    {
        get => singleStrip;
        set => ApplyPaperFormat(value, landscape);
    }
    private bool landscape;
    public string[] OrientationOptions { get; } = ["Dọc", "Ngang"];
    public int OrientationIndex { get => landscape ? 1 : 0; set => ApplyPaperFormat(singleStrip, value == 1); }
    public string PaperSizeLabel => SingleStrip ? "2 × 6 inch" : "4 × 6 inch";
    public void ApplyPaperFormat(bool strip, bool horizontal)
    {
        if(singleStrip == strip && landscape == horizontal)return;
        double oldWidth=PageWidth,oldHeight=PageHeight;
        singleStrip=strip;landscape=horizontal;
        foreach(var slot in Slots)slot.ScalePage(PageWidth,PageHeight,PageWidth/oldWidth,PageHeight/oldHeight);
        Changed(nameof(SingleStrip));Changed(nameof(OrientationIndex));Changed(nameof(PaperSizeLabel));
        Changed(nameof(PageWidth));Changed(nameof(PageHeight));Changed(nameof(PixelDimensions));
        Changed(nameof(PagePixelWidth));Changed(nameof(PagePixelHeight));Changed(nameof(PaperSummary));
    }
    public double PageHeight => landscape ? (SingleStrip ? 200 : 400) : 600;
    public double PageWidth => landscape ? 600 : SingleStrip ? 200 : 400;
    public double PagePixelWidth => PageWidth / 100 * Dpi;
    public double PagePixelHeight => PageHeight / 100 * Dpi;
    public string BackgroundColor { get => backgroundColor; set { if (value is not { Length: 7 } || value[0] != '#' || !value.Skip(1).All(Uri.IsHexDigit)) throw new ArgumentException("Nhập màu #RRGGBB."); backgroundColor = value; Changed(); } }
    public ICommand AddDecorationCommand => new EditorCommand(parameter =>
    {
        if (Preview || Slots.Count >= 64) return;
        var kind = parameter?.ToString() switch { "Text" => LayoutElementKind.Text, "Ellipse" => LayoutElementKind.Ellipse, _ => LayoutElementKind.Rectangle };
        AddDecoration(kind, kind == LayoutElementKind.Text ? "Nội dung của bạn" : "");
    });
    public void AddDecoration(LayoutElementKind kind, string content)
    {
        if (Preview || Slots.Count >= 64) return;
        var slot = new PhotoSlotViewModel { PageWidth = PageWidth, PageHeight = PageHeight, Number = Slots.Select(s => s.Number).DefaultIfEmpty().Max() + 1,
            PhotoNumber = 1, Kind = kind, Content = content, Width = Math.Min(160, PageWidth - 40), Height = kind is LayoutElementKind.Text or LayoutElementKind.SessionDate ? 50 : 100, X = 20, Y = 30 };
        if (kind == LayoutElementKind.SessionDate) { slot.Width = 80; slot.Height = 20; slot.FontSize = 12; }
        Slots.Add(slot); SelectedSlot = slot;
    }
    private void PlaceInFreeArea(PhotoSlotViewModel target)
    {
        // Work in print-layout units: position must remain proportional in the editor and output.
        var occupied = Slots.Where(s => s != target && s.IsVisible).ToArray();
        BitmapSource? source = Artwork as BitmapSource;
        byte[]? pixels = null;
        int pixelWidth = 0, pixelHeight = 0;
        if (source is not null)
        {
            var bitmap = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            pixelWidth = bitmap.PixelWidth; pixelHeight = bitmap.PixelHeight;
            pixels = new byte[pixelWidth * pixelHeight * 4];
            bitmap.CopyPixels(pixels, pixelWidth * 4, 0);
        }
        double best = double.PositiveInfinity, bestX = 0, bestY = 0;
        for (double y = PageHeight - target.Height - 4; y >= 4; y -= 4)
            for (double x = 4; x + target.Width <= PageWidth - 4; x += 4)
            {
                if (occupied.Any(s => x < s.X + s.Width + 4 && x + target.Width + 4 > s.X && y < s.Y + s.Height + 4 && y + target.Height + 4 > s.Y)) continue;
                double ink = 0;
                if (pixels is not null)
                    for (int gy = 1; gy <= 7; gy++) for (int gx = 1; gx <= 7; gx++)
                    {
                        int px = Math.Clamp((int)((x + target.Width * gx / 8) / PageWidth * pixelWidth), 0, pixelWidth - 1);
                        int py = Math.Clamp((int)((y + target.Height * gy / 8) / PageHeight * pixelHeight), 0, pixelHeight - 1);
                        int offset = (py * pixelWidth + px) * 4;
                        if (pixels[offset + 3] > 200 && Math.Min(pixels[offset], Math.Min(pixels[offset + 1], pixels[offset + 2])) < 220) ink++;
                    }
                double score = ink * 1000 + (PageHeight - y) - x * .1;
                if (score < best) { best = score; bestX = x; bestY = y; }
            }
        if (double.IsPositiveInfinity(best))
        {
            Slots.Remove(target);
            SelectedSlot = null;
            Warn($"Khung {PaperSizeLabel} không còn chỗ trống cho {(target.IsSessionQr ? "QR 0,55 inch" : "dữ liệu phiên")}. Thu nhỏ hoặc di chuyển các ô ảnh trước khi thêm.");
            return;
        }
        target.X = bestX; target.Y = bestY;
    }
}
