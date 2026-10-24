namespace FotoAutomat.Core;

public sealed record LevelsAdjustment(double InputBlack = 0, double InputWhite = 255, double Gamma = 1, double OutputBlack = 0, double OutputWhite = 255)
{
    public void Validate()
    {
        if (new[] { InputBlack, InputWhite, Gamma, OutputBlack, OutputWhite }.Any(x => !double.IsFinite(x)) || InputBlack < 0 || InputWhite > 255 || InputBlack >= InputWhite || Gamma < .1 || Gamma > 5 || OutputBlack < 0 || OutputWhite > 255 || OutputBlack > OutputWhite) throw new ArgumentException("Levels: điểm đen phải nhỏ hơn điểm trắng; gamma từ 0,1 đến 5.");
    }
}
public sealed record CurvePoint(double X, double Y);
public sealed record ToneCurve(CurvePoint[] Points)
{
    public static ToneCurve Identity => new([new(0, 0), new(1, 1)]);
    public void Validate()
    {
        if (Points is null || Points.Length is < 2 or > 32) throw new ArgumentException("Curve cần hai đầu mút và tối đa 32 điểm.");
        for (int i = 0; i < Points.Length; i++) if (!double.IsFinite(Points[i].X) || !double.IsFinite(Points[i].Y) || Points[i].X < 0 || Points[i].X > 1 || Points[i].Y < 0 || Points[i].Y > 1 || i > 0 && Points[i].X <= Points[i - 1].X) throw new ArgumentException("Các điểm Curve phải tăng theo trục vào.");
    }
    public double Evaluate(double x)
    {
        x = Math.Clamp(x, 0, 1);
        if (x <= Points[0].X) return Points[0].Y;
        if (x >= Points[^1].X) return Points[^1].Y;
        int i = 1; while (i < Points.Length - 1 && Points[i].X < x) i++;
        var a = Points[i - 1]; var b = Points[i]; return a.Y + (b.Y - a.Y) * (x - a.X) / (b.X - a.X);
    }
}
public sealed record HslAdjustment(double Hue = 0, double Saturation = 0, double Lightness = 0)
{
    public void Validate() { if (new[] { Hue, Saturation, Lightness }.Any(x => !double.IsFinite(x) || Math.Abs(x) > 100)) throw new ArgumentException("HSL vượt giới hạn."); }
}
public sealed record ColorGrade(double Hue = 0, double Amount = 0, double Lightness = 0)
{
    public void Validate() { if (!double.IsFinite(Hue) || Hue < 0 || Hue > 360 || new[] { Amount, Lightness }.Any(x => !double.IsFinite(x) || Math.Abs(x) > 100) || Amount < 0) throw new ArgumentException("Color Balance vượt giới hạn."); }
}
public enum PhotoMaskKind { Full, Brush, Radial, Linear }
public sealed record MaskDab(double X, double Y, double Radius, double Strength = 1, bool Erase = false);
public sealed record PhotoMask(PhotoMaskKind Kind = PhotoMaskKind.Full, MaskDab[]? Dabs = null, bool Inverted = false,
    double CenterX = .5, double CenterY = .5, double Radius = .3, double Feather = .5, double Angle = 0)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Kind) || new[] { CenterX, CenterY, Radius, Feather, Angle }.Any(x => !double.IsFinite(x)) || CenterX < 0 || CenterX > 1 || CenterY < 0 || CenterY > 1 || Radius < .005 || Radius > 2 || Feather < .01 || Feather > 1 || Math.Abs(Angle) > 360 || Dabs?.Length > 10000) throw new ArgumentException("Mask không hợp lệ.");
        foreach (var dab in Dabs ?? []) if (new[] { dab.X, dab.Y, dab.Radius, dab.Strength }.Any(x => !double.IsFinite(x)) || dab.X < 0 || dab.X > 1 || dab.Y < 0 || dab.Y > 1 || dab.Radius < .005 || dab.Radius > 1 || dab.Strength < 0 || dab.Strength > 1) throw new ArgumentException("Nét bút mask không hợp lệ.");
    }
    public float[] Rasterize(int width, int height, CancellationToken cancellationToken = default)
    {
        Validate();
        if (width <= 0 || height <= 0) throw new ArgumentException("Kích thước mask không hợp lệ.");
        var mask = new float[checked(width * height)];
        if (Kind == PhotoMaskKind.Full) Array.Fill(mask, 1);
        else if (Kind == PhotoMaskKind.Brush)
        {
            foreach (var dab in Dabs ?? [])
            {
                cancellationToken.ThrowIfCancellationRequested();
                double cx = dab.X * width, cy = dab.Y * height, radius = dab.Radius * Math.Min(width, height);
                for (int y = Math.Max(0, (int)(cy - radius)); y < Math.Min(height, cy + radius + 1); y++)
                    for (int x = Math.Max(0, (int)(cx - radius)); x < Math.Min(width, cx + radius + 1); x++)
                    {
                        double distance = Math.Sqrt(Math.Pow(x + .5 - cx, 2) + Math.Pow(y + .5 - cy, 2)) / radius;
                        double strength = Falloff(distance, Feather) * dab.Strength;
                        int i = y * width + x; mask[i] = (float)(dab.Erase ? mask[i] * (1 - strength) : mask[i] + (1 - mask[i]) * strength);
                    }
            }
        }
        else for (int y = 0; y < height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int x = 0; x < width; x++)
            {
                double dx = (x + .5 - CenterX * width) / Math.Min(width, height), dy = (y + .5 - CenterY * height) / Math.Min(width, height);
                mask[y * width + x] = (float)(Kind == PhotoMaskKind.Radial ? Falloff(Math.Sqrt(dx * dx + dy * dy) / Radius, Feather) : Math.Clamp(.5 + (dx * Math.Cos(Angle * Math.PI / 180) + dy * Math.Sin(Angle * Math.PI / 180)) / (Radius * 2 * Feather), 0, 1));
            }
        }
        if (Inverted) for (int i = 0; i < mask.Length; i++) mask[i] = 1 - mask[i];
        return mask;
    }
    private static double Falloff(double distance, double feather) { double t = Math.Clamp((1 - distance) / feather, 0, 1); return t * t * (3 - 2 * t); }
}
public sealed record PhotoAdjustmentLayer(string Name, ColorRecipe Recipe, PhotoMask Mask, double Opacity = 1, bool Enabled = true)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 80 || !double.IsFinite(Opacity) || Opacity < 0 || Opacity > 1 || Recipe.Layers?.Length > 0) throw new ArgumentException("Lớp chỉnh ảnh không hợp lệ hoặc có lớp lồng nhau.");
        Recipe.Validate(); Mask.Validate();
    }
}
