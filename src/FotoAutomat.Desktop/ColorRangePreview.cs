using System.Windows.Media;
using System.Windows.Media.Imaging;
using FotoAutomat.Core;

namespace FotoAutomat.Desktop;

public sealed partial class ColorEditorPanel
{
    private bool viewSelectedColorRange;
    private (BitmapSource Preview, ColorRecipe Look, int Layer, bool Skin, int Index, BitmapSource Result)? selectedColorViewCache;
    private (byte[] Input, ColorRecipe Look, int Layer, bool Skin, int Index, byte[] Pixels)? rangeStageCache;
    private BitmapSource? UnmaskedColorPreview => original ? session.Original : session.Previews.ElementAtOrDefault(selected);

    /// <summary>Pixels the given range of the current look (or layer) receives in the real pipeline. An index past the end is a new range.</summary>
    private (byte[] Pixels, int Width, int Height)? RangeStage(bool skin, int index)
    {
        if (session.ColorInput() is not { } input || selected >= session.Workspace.Looks.Length) return null;
        var look = session.Workspace.Looks[selected];
        if (rangeStageCache is { } c && ReferenceEquals(c.Input, input.Pixels) && ReferenceEquals(c.Look, look) && c.Layer == selectedLayer && c.Skin == skin && c.Index == index)
            return (c.Pixels, input.Width, input.Height);
        try
        {
            var pixels = ColorEditorStage.RangeInput(input.Pixels, look, selectedLayer, skin, index, input.Width, input.Height);
            rangeStageCache = (input.Pixels, look, selectedLayer, skin, index, pixels);
            return (pixels, input.Width, input.Height);
        }
        catch (ArgumentException) { return null; }
    }

    private void RefreshSelectedColorView()
    {
        var source = UnmaskedColorPreview;
        if (!viewSelectedColorRange || colorEditorTab == 0 || source is null)
        {
            preview.Source = source;
            selectedColorViewCache = null;
            return;
        }
        bool skin = colorEditorTab == 2; int index = RangeIndex(skin);
        var range = Ranges(CurrentRecipe, skin).ElementAtOrDefault(index) ?? (skin ? new ColorRangeAdjustment(Width: 20, SaturationRadius: .4, Smoothness: 25) : new ColorRangeAdjustment());
        var look = session.Workspace.Looks[selected];
        if (selectedColorViewCache is not { } cache || !ReferenceEquals(cache.Preview, source) || !ReferenceEquals(cache.Look, look) || cache.Layer != selectedLayer || cache.Skin != skin || cache.Index != index)
        {
            var stage = RangeStage(skin, index);
            float[]? coverage = stage is { } s && s.Width == source.PixelWidth && s.Height == source.PixelHeight
                ? ColorEditorStage.Coverage(s.Pixels, range, look, selectedLayer, s.Width, s.Height) : null;
            selectedColorViewCache = cache = (source, look, selectedLayer, skin, index, RenderSelectedColorRange(source, coverage, range));
        }
        preview.Source = cache.Result;
    }

    // Display-only: colour stays where the range acts (on the colours it really receives), the rest turns grey.
    // Without a pipeline input (no sample loaded) it falls back to measuring the preview itself.
    private static BitmapSource RenderSelectedColorRange(BitmapSource source, float[]? coverage, ColorRangeAdjustment range)
    {
        var bgra = source.Format == PixelFormats.Bgra32 ? source : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int stride = bgra.PixelWidth * 4;
        var pixels = new byte[stride * bgra.PixelHeight]; bgra.CopyPixels(pixels, stride, 0);
        for (int i = 0; i < pixels.Length; i += 4)
        {
            double b = pixels[i], g = pixels[i + 1], r = pixels[i + 2];
            double weight;
            if (coverage is not null) weight = coverage[i / 4];
            else { var (h, s, _) = AdjustToolProcessor.ToHsl(r / 255, g / 255, b / 255); weight = range.Weight(h, s); }
            double gray = .2126 * r + .7152 * g + .0722 * b;
            pixels[i] = (byte)Math.Clamp(Math.Round(gray + weight * (b - gray)), 0, 255);
            pixels[i + 1] = (byte)Math.Clamp(Math.Round(gray + weight * (g - gray)), 0, 255);
            pixels[i + 2] = (byte)Math.Clamp(Math.Round(gray + weight * (r - gray)), 0, 255);
        }
        var result = BitmapSource.Create(bgra.PixelWidth, bgra.PixelHeight, bgra.DpiX, bgra.DpiY, PixelFormats.Bgra32, null, pixels, stride);
        result.Freeze(); return result;
    }
}
