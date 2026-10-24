namespace FotoAutomat.Core;

public sealed record ColorRecipe(string Name = "Tự nhiên", double Exposure = 0, double Contrast = 0, double Warmth = 0, double Saturation = 0, bool Monochrome = false,
    double Tint = 0, double Brightness = 0, double Highlights = 0, double Shadows = 0, double Whites = 0, double Blacks = 0)
{
    public string PresetId { get; init; } = Guid.NewGuid().ToString("N");
    public MatchLookOrigin? MatchOrigin { get; init; }
    public LevelsAdjustment[]? Levels { get; init; }
    public ToneCurve[]? Curves { get; init; }
    public HslAdjustment[]? Hsl { get; init; }
    public ColorGrade[]? Balance { get; init; }
    public WhiteBalanceSettings? WhiteBalance { get; init; }
    public ColorRangeAdjustment[]? AdvancedColors { get; init; }
    public ColorRangeAdjustment? SkinColor { get; init; }
    public ColorRangeAdjustment[]? SkinColors { get; init; }
    public BlackWhiteSettings? BlackWhite { get; init; }
    public ClarityMethod ClarityMethod { get; init; } = ClarityMethod.Classic;
    public ShadowToneMode DehazeShadowMode { get; init; }
    public double DehazeShadowHue { get; init; }
    public double DehazeShadowSaturation { get; init; }
    public double? VignetteEv { get; init; }
    public VignetteMethod VignetteMethod { get; init; }
    public BlurGlowSettings? BlurGlow { get; init; }
    public double Clarity { get; init; }
    public double Structure { get; init; }
    public double Dehaze { get; init; }
    public PhotoAdjustmentLayer[]? Layers { get; init; }
    public double? Temperature { get; init; }
    public double Vibrance { get; init; }
    public double CurveHighlights { get; init; }
    public double CurveLights { get; init; }
    public double CurveDarks { get; init; }
    public double CurveShadows { get; init; }
    public double GradingBlending { get; init; } = 50;
    public double GradingBalance { get; init; }
    public double Vignette { get; init; }
    public double Grain { get; init; }
    public double Sharpening { get; init; }
    public HslAdjustment[]? Calibration { get; init; }
    public CubeLut? Lut { get; init; }
    public void Validate()
    {
        MatchOrigin?.Validate();
        BlurGlow?.Validate();
        WhiteBalance?.Validate(); BlackWhite?.Validate(); SkinColor?.Validate();
        if (AdvancedColors?.Length > 32) throw new ArgumentException("Tối đa 32 dải màu.");
        foreach (var range in AdvancedColors ?? []) range.Validate();
        if (SkinColors?.Length > 32) throw new ArgumentException("Tối đa 32 dải màu da.");
        foreach (var range in SkinColors ?? []) range.Validate();
        if (!Enum.IsDefined(ClarityMethod) || !Enum.IsDefined(DehazeShadowMode) || !Enum.IsDefined(VignetteMethod)
            || !double.IsFinite(DehazeShadowHue) || DehazeShadowHue < 0 || DehazeShadowHue > 360
            || !double.IsFinite(DehazeShadowSaturation) || DehazeShadowSaturation < 0 || DehazeShadowSaturation > 100
            || VignetteEv is { } ev && (!double.IsFinite(ev) || Math.Abs(ev) > 4)) throw new ArgumentException("Adjust settings không hợp lệ.");
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 80 || !double.IsFinite(Exposure) || Math.Abs(Exposure) > 5 || new[] { Contrast, Warmth, Saturation, Brightness, Highlights, Shadows, Whites, Blacks }.Any(x => !double.IsFinite(x) || Math.Abs(x) > 100) || !double.IsFinite(Tint) || Math.Abs(Tint) > (Temperature is null ? 100 : 150)) throw new ArgumentException("Thông số bộ màu không hợp lệ.");
        if (Temperature is { } t && (!double.IsFinite(t) || t < 2000 || t > 50000) || new[] { Vibrance, CurveHighlights, CurveLights, CurveDarks, CurveShadows, GradingBalance, Vignette }.Any(x => !double.IsFinite(x) || Math.Abs(x) > 100) || new[] { GradingBlending, Grain }.Any(x => !double.IsFinite(x) || x < 0 || x > 100) || !double.IsFinite(Sharpening) || Sharpening < 0 || Sharpening > 150 || Calibration is not null && Calibration.Length != 3) throw new ArgumentException("Preset parameters are out of range.");
        foreach (var value in Calibration ?? []) value.Validate();
        Lut?.Validate();
        if (new[] { Clarity, Structure, Dehaze }.Any(x => !double.IsFinite(x) || Math.Abs(x) > 100) || Levels is not null && Levels.Length != 4 || Curves is not null && Curves.Length != 5 || Hsl is not null && Hsl.Length != 8 || Balance is not null && Balance.Length != 4 || Layers?.Length > 16) throw new ArgumentException("Nhóm chỉnh màu không hợp lệ.");
        foreach (var value in Levels ?? []) value.Validate();
        foreach (var value in Curves ?? []) value.Validate();
        foreach (var value in Hsl ?? []) value.Validate();
        foreach (var value in Balance ?? []) value.Validate();
        foreach (var value in Layers ?? []) value.Validate();
    }
}

/// <summary>Non-destructive BGRA8 preview transform. Input is never modified.</summary>
public static class ColorProcessor
{
    public static byte[] ApplyLiveView(byte[] source, ColorRecipe recipe, int width, int height, CancellationToken cancellationToken = default) => Apply(source, ForLiveView(recipe), width, height, cancellationToken);
    public static ColorRecipe ForLiveView(ColorRecipe r) => r with { BlurGlow = null, Clarity = 0, Structure = 0, Dehaze = 0, Vignette = 0, VignetteEv = null, Grain = 0, Sharpening = 0, Layers = r.Layers?.Select(l => l with { Recipe = ForLiveView(l.Recipe) }).ToArray() };
    public static byte[] Apply(byte[] source, ColorRecipe recipe, int width = 0, int height = 0, CancellationToken cancellationToken = default) =>
        AdvancedColorProcessor.Apply(BaseTone(source, recipe, cancellationToken), recipe, width == 0 ? source.Length / 4 : width, height == 0 ? 1 : height, cancellationToken);

    /// <summary>Pixels exactly as they reach the Color Editor (Advanced/Skin ranges) of this recipe.</summary>
    internal static byte[] BeforeColorEditor(byte[] source, ColorRecipe recipe, int width, int height, CancellationToken cancellationToken = default) =>
        AdvancedColorProcessor.BeforeColorEditor(BaseTone(source, recipe, cancellationToken), recipe, width, height, cancellationToken);

    private static byte[] BaseTone(byte[] source, ColorRecipe recipe, CancellationToken cancellationToken)
    {
        recipe.Validate();
        if (source.Length % 4 != 0) throw new ArgumentException("Dữ liệu ảnh BGRA không hợp lệ.");
        if (recipe.Lut is { } lut)
        {
            source = source.ToArray();
            for (int i = 0; i < source.Length; i += 4)
            {
                if (i % 16384 == 0) cancellationToken.ThrowIfCancellationRequested();
                var c = lut.Evaluate(source[i + 2] / 255d, source[i + 1] / 255d, source[i] / 255d); source[i + 2] = Byte(c.R); source[i + 1] = Byte(c.G); source[i] = Byte(c.B);
            }
        }
        var output = new byte[source.Length];
        double exposure = Math.Pow(2, recipe.Exposure), contrast = 1 + recipe.Contrast / 100, saturation = recipe.Monochrome && recipe.BlackWhite is null ? 0 : 1 + recipe.Saturation / 100;
        double warmth = recipe.Temperature is { } kelvin ? Math.Clamp(100 * Math.Log(kelvin / 6500) / Math.Log(2), -100, 100) : recipe.Warmth;
        double tint = recipe.Tint;
        if (recipe.WhiteBalance is { } wb)
        {
            warmth = wb.Mode == WhiteBalanceMode.Shot ? 0 : 100 * Math.Log(wb.Kelvin / 5000) / Math.Log(2);
            tint = wb.Mode == WhiteBalanceMode.Shot ? 0 : wb.Tint;
        }
        var red = Table(exposure * Math.Pow(2, warmth / 200 + tint / 400), contrast);
        var green = Table(exposure * Math.Pow(2, -tint / 400), contrast);
        var blue = Table(exposure * Math.Pow(2, -warmth / 200 + tint / 400), contrast);
        for (int i = 0; i < source.Length; i += 4)
        {
            if (i % 16384 == 0) cancellationToken.ThrowIfCancellationRequested();
            double r = red[source[i + 2]], g = green[source[i + 1]], b = blue[source[i]];
            double luma = .2126 * r + .7152 * g + .0722 * b;
            double l = Math.Clamp(luma, 0, 1);
            double shift = recipe.Brightness / 100 * .35 * 4 * l * (1 - l)
                + recipe.Shadows / 100 * .3 * Math.Pow(1 - l, 2) * (1 - Smooth(.45, .7, l))
                + recipe.Highlights / 100 * .3 * l * l * Smooth(.3, .6, l)
                + recipe.Blacks / 100 * .2 * (1 - Smooth(0, .3, l))
                + recipe.Whites / 100 * .2 * Smooth(.7, 1, l);
            r += shift; g += shift; b += shift; luma += shift;
            double chroma = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
            double vivid = recipe.Monochrome && recipe.BlackWhite is null ? 0 : saturation * (1 + recipe.Vibrance / 100 * (1 - Math.Clamp(chroma, 0, 1)));
            output[i + 2] = Byte(luma + (r - luma) * vivid);
            output[i + 1] = Byte(luma + (g - luma) * vivid);
            output[i] = Byte(luma + (b - luma) * vivid);
            output[i + 3] = source[i + 3];
        }
        return output;
    }
    private static double Smooth(double lo, double hi, double v) { double t = Math.Clamp((v - lo) / (hi - lo), 0, 1); return t * t * (3 - 2 * t); }
    private static double[] Table(double gain, double contrast) => Enumerable.Range(0, 256).Select(i => (LinearToSrgb(SrgbToLinear(i / 255d) * gain) - .5) * contrast + .5).ToArray();
    private static double SrgbToLinear(double v) => v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4);
    private static double LinearToSrgb(double v) => v <= .0031308 ? 12.92 * v : 1.055 * Math.Pow(v, 1 / 2.4) - .055;
    private static byte Byte(double v) => (byte)Math.Clamp(Math.Round(v * 255), 0, 255);
}
