namespace FotoAutomat.Core;

[Flags]
public enum MatchComponents
{
    None = 0, Contrast = 1, Highlights = 2, Shadows = 4, Whites = 8, Blacks = 16, RgbCurve = 32,
    Saturation = 64, HslHue = 128, HslSaturation = 256, HslLightness = 512,
    ShadowBalance = 1024, MidtoneBalance = 2048, HighlightBalance = 4096,
    RedCurve = 8192, GreenCurve = 16384, BlueCurve = 32768,
    Light = Contrast | Highlights | Shadows | Whites | Blacks | RgbCurve,
    Color = Saturation | HslHue | HslSaturation | HslLightness,
    Tone = ShadowBalance | MidtoneBalance | HighlightBalance | RedCurve | GreenCurve | BlueCurve,
    All = Light | Color | Tone
}
