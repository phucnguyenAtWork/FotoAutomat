namespace FotoAutomat.Core;

public enum WhiteBalanceMode { Shot, Custom }
public enum ClarityMethod { Classic, Natural, Punch, Neutral }
public enum ShadowToneMode { Auto, Manual }
public enum VignetteMethod { EllipticOnCrop, CircularOnCrop, Circular }

public sealed record WhiteBalanceSettings(WhiteBalanceMode Mode = WhiteBalanceMode.Shot, double Kelvin = 5000, double Tint = 0)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Mode) || !double.IsFinite(Kelvin) || Kelvin < 800 || Kelvin > 140000 || !double.IsFinite(Tint) || Math.Abs(Tint) > 50) throw new ArgumentException("White Balance không hợp lệ.");
    }
}

/// <summary>A reusable selection in colour space, independent of image coordinates.</summary>
public sealed record ColorRangeAdjustment(double CenterHue = 25, double CenterSaturation = .35,
    double Width = 30, double SaturationRadius = 1, double Smoothness = 20,
    double Hue = 0, double Saturation = 0, double Lightness = 0, bool Enabled = true,
    double UniformHue = 0, double UniformSaturation = 0, double UniformLightness = 0, double TargetLightness = .65)
{
    public void Validate()
    {
        if (new[] { CenterHue, CenterSaturation, Width, SaturationRadius, Smoothness, Hue, Saturation, Lightness, UniformHue, UniformSaturation, UniformLightness, TargetLightness }.Any(v => !double.IsFinite(v))
            || CenterHue < 0 || CenterHue > 360 || CenterSaturation < 0 || CenterSaturation > 1 || Width < 1 || Width > 180 || SaturationRadius < .01 || SaturationRadius > 1
            || Smoothness < 1 || Smoothness > 30 || Math.Abs(Hue) > 30 || Saturation < -100 || Saturation > 200 || Math.Abs(Lightness) > 100
            || new[] { UniformHue, UniformSaturation, UniformLightness }.Any(v => v < 0 || v > 100) || TargetLightness < 0 || TargetLightness > 1) throw new ArgumentException("Color range không hợp lệ.");
    }
    public double Weight(double hue, double saturation)
    {
        double d = Math.Abs(AdjustToolProcessor.HueDelta(hue, CenterHue));
        double feather = Smoothness;
        double h = Math.Clamp((Width + feather - d) / feather, 0, 1);
        double s = Math.Clamp((SaturationRadius + .1 - Math.Abs(saturation - CenterSaturation)) / .1, 0, 1);
        return h * h * (3 - 2 * h) * s * s * (3 - 2 * s);
    }
}

public sealed record BlackWhiteSettings
{
    public double[] Sensitivity { get; init; } = new double[6];
    public double HighlightHue { get; init; }
    public double HighlightSaturation { get; init; }
    public double ShadowHue { get; init; }
    public double ShadowSaturation { get; init; }
    public void Validate()
    {
        if (Sensitivity is null || Sensitivity.Length != 6 || Sensitivity.Any(v => !double.IsFinite(v) || Math.Abs(v) > 100)
            || new[] { HighlightHue, ShadowHue }.Any(v => !double.IsFinite(v) || v < 0 || v > 360)
            || new[] { HighlightSaturation, ShadowSaturation }.Any(v => !double.IsFinite(v) || v < 0 || v > 100)) throw new ArgumentException("Black & White không hợp lệ.");
    }
}
