using FotoAutomat.Core;
using System.Windows.Media;

namespace FotoAutomat.Desktop.ViewModels;

public sealed partial class PhotoSlotViewModel
{
    private double rotation, strokeWidth, shadowAngle = 315, shadowDistance = 4, shadowBlur = 8;
    private string strokeColor = "#222222", shadowColor = "#000000";
    private bool isVisible = true;
    public bool IsVisible { get => isVisible; set { isVisible = value; Changed(); } }
    private bool shadow, keepAspect, locked;
    public double Rotation { get => rotation; set { rotation = Limit(value, -180, 180); Changed(); } }
    public double StrokeWidth { get => strokeWidth; set { strokeWidth = Limit(value, 0, 20); Changed(); Changed(nameof(PixelStroke)); Changed(nameof(StrokeThickness)); } }
    public System.Windows.Thickness StrokeThickness => new(StrokeWidth);
    public double PixelStroke { get => StrokeWidth * 3; set => StrokeWidth = value / 3; }
    public string StrokeColor { get => strokeColor; set { strokeColor = Color(value); Changed(); } }
    public string ShadowColor { get => shadowColor; set { shadowColor = Color(value); Changed(); } }
    public bool Shadow { get => shadow; set { shadow = value; Changed(); Changed(nameof(ShadowOpacity)); } }
    public double ShadowOpacity => Shadow ? 0.45 : 0;
    public double ShadowAngle { get => shadowAngle; set { shadowAngle = Limit(value, 0, 360); Changed(); } }
    public double ShadowDistance { get => shadowDistance; set { shadowDistance = Limit(value, 0, 40); Changed(); } }
    public double ShadowBlur { get => shadowBlur; set { shadowBlur = Limit(value, 0, 60); Changed(); } }
    public double PixelShadowDistance { get => ShadowDistance * 3; set { ShadowDistance = value / 3; Changed(); } }
    public double PixelShadowBlur { get => ShadowBlur * 3; set { ShadowBlur = value / 3; Changed(); } }
    public bool KeepAspect { get => keepAspect; set { keepAspect = value; Changed(); } }
    public bool Locked { get => locked; set { locked = value; Changed(); Changed(nameof(CanEdit)); Changed(nameof(LayerLabel)); } }
    public bool CanEdit => !Locked;
    public string LayerLabel => Label + (Locked ? " · Đã khóa" : "");
    public string PlaceholderBrush => (PhotoNumber % 6) switch
    {
        1 => "#D9E4E5", 2 => "#E9DECF", 3 => "#DDDFEB", 4 => "#DCE5D7", 5 => "#E9DADD", _ => "#DFDFDA"
    };
    private static double Limit(double value, double min, double max)
    {
        if (!double.IsFinite(value) || value < min || value > max) throw new ArgumentException($"Nhập số từ {min} đến {max}.");
        return value;
    }
    private static string Color(string value)
    {
        if (value is not { Length: 7 } || value[0] != '#' || !value.Skip(1).All(Uri.IsHexDigit))
            throw new ArgumentException("Nhập màu dạng #RRGGBB.");
        return value.ToUpperInvariant();
    }
    public PrintLayoutSlot Export() => new(PhotoNumber, X, Y, Width, Height, Rotation, StrokeWidth, StrokeColor,
        Shadow, ShadowColor, ShadowAngle, ShadowDistance, ShadowBlur, KeepAspect, Locked, Kind, Content, FillColor, FontSize, IsVisible);
    public static PhotoSlotViewModel From(PrintLayoutSlot slot, int number, bool strips, double pageWidth = 400, double pageHeight = 600) => new()
    {
        PageWidth = pageWidth, PageHeight = pageHeight, Number = number, PhotoNumber = slot.PhotoNumber, Kind = slot.Kind, Width = slot.Width, Height = slot.Height,
        X = slot.X, Y = slot.Y, Strips = strips, Rotation = slot.Rotation, StrokeWidth = slot.StrokeWidth,
        StrokeColor = slot.StrokeColor, Shadow = slot.Shadow, ShadowColor = slot.ShadowColor,
        ShadowAngle = slot.ShadowAngle, ShadowDistance = slot.ShadowDistance, ShadowBlur = slot.ShadowBlur,
        KeepAspect = slot.KeepAspect, Locked = slot.Locked, Content = slot.Content, FillColor = slot.FillColor, FontSize = slot.FontSize, IsVisible = slot.IsVisible
    };
}
