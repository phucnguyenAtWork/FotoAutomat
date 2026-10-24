using System.Globalization;

namespace FotoAutomat.Core;

/// <summary>Màu nhấn của thương hiệu và các màu suy ra cho giao diện khách.</summary>
/// <param name="Action">Nút chính, chữ nhấn.</param>
/// <param name="Soft">Viền khi rê chuột, chấm tiến trình.</param>
/// <param name="Deep">Chữ nhấn đậm, viền tiêu điểm.</param>
/// <param name="Tint">Nền lựa chọn đang chọn.</param>
/// <param name="Glow">Vầng sáng màn chào.</param>
public sealed record BrandPalette(string Action, string Soft, string Deep, string Tint, string Glow);

/// <summary>Màu thương hiệu dạng #RRGGBB. Kiểm tra độ tương phản để chữ trên giao diện khách vẫn đọc được.</summary>
public static class BrandColors
{
    public const string DefaultAccent = "#A94466", DefaultBackground = "#FAF3E9", Ink = "#3F2D22", White = "#FFFFFF", Black = "#000000";
    public const double MinAccentContrast = 3.0, MinBackgroundContrast = 4.5;

    // Bảng màu gốc của Foto Hunter (CustomerTheme.xaml); thương hiệu mặc định giữ đúng các màu này.
    private static readonly BrandPalette DefaultPalette = new(DefaultAccent, "#C86C8B", "#852640", "#F7E0E7", "#F3D2DF");

    public static bool TryParse(string? hex, out (byte R, byte G, byte B) rgb)
    {
        rgb = default;
        if (hex is not { Length: 7 } || hex[0] != '#' || !int.TryParse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value)) return false;
        rgb = ((byte)(value >> 16), (byte)(value >> 8 & 0xFF), (byte)(value & 0xFF));
        return true;
    }

    public static string Normalize(string hex) => TryParse(hex.Trim(), out var c) ? Format(c) : throw new ArgumentException("Nhập màu dạng #RRGGBB.");

    private static string Format((byte R, byte G, byte B) c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private static (byte R, byte G, byte B) Parse(string hex) => TryParse(hex, out var c) ? c : throw new ArgumentException("Nhập màu dạng #RRGGBB.");

    /// <summary>Trộn <paramref name="hex"/> với <paramref name="other"/>; amount = 0 giữ nguyên, 1 thành other.</summary>
    public static string Mix(string hex, string other, double amount)
    {
        var a = Parse(hex); var b = Parse(other);
        static byte Lerp(byte x, byte y, double t) => (byte)Math.Round(x + (y - x) * t);
        return Format((Lerp(a.R, b.R, amount), Lerp(a.G, b.G, amount), Lerp(a.B, b.B, amount)));
    }

    // Độ tương phản theo WCAG 2.x.
    public static double Contrast(string first, string second)
    {
        static double Channel(byte v) { double s = v / 255.0; return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
        static double Luminance((byte R, byte G, byte B) c) => 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        double l1 = Luminance(Parse(first)), l2 = Luminance(Parse(second));
        return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
    }

    public static void ValidateAccent(string hex)
    {
        if (Contrast(Normalize(hex), White) < MinAccentContrast) throw new ArgumentException("Màu nhấn quá sáng, chữ trắng trên nút sẽ khó đọc.");
    }

    public static void ValidateBackground(string hex)
    {
        if (Contrast(Normalize(hex), Ink) < MinBackgroundContrast) throw new ArgumentException("Màu nền quá tối, chữ trên màn hình khách sẽ khó đọc.");
    }

    public static BrandPalette Palette(string accent)
    {
        string action = Normalize(accent);
        return action == DefaultAccent ? DefaultPalette
            : new(action, Mix(action, White, 0.25), Mix(action, Black, 0.22), Mix(action, White, 0.86), Mix(action, White, 0.75));
    }
}
