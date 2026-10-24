using FotoAutomat.Core;

namespace FotoAutomat.Tests;

public sealed class CurveEndpointTests
{
    [Fact]
    public void Independent_input_output_endpoints_hold_values_outside_range()
    {
        var curve = new ToneCurve([new(.2, .1), new(.8, .9)]); curve.Validate();
        Assert.Equal(.1, curve.Evaluate(0)); Assert.Equal(.9, curve.Evaluate(1));
        Assert.Equal(.5, curve.Evaluate(.5), 10);
        var recipe = new ColorRecipe { Curves = [curve, ToneCurve.Identity, ToneCurve.Identity, ToneCurve.Identity, ToneCurve.Identity] };
        var pixels = new byte[] { 0, 0, 0, 255, 255, 255, 255, 255 };
        var rendered = ColorProcessor.Apply(pixels, recipe);
        Assert.InRange(rendered[0], (byte)25, (byte)26);
        Assert.InRange(rendered[4], (byte)229, (byte)230);
    }
    [Fact]
    public void Crossed_or_duplicate_input_points_remain_invalid()
    {
        Assert.Throws<ArgumentException>(() => new ToneCurve([new(.8, 0), new(.2, 1)]).Validate());
        Assert.Throws<ArgumentException>(() => new ToneCurve([new(.2, 0), new(.2, 1)]).Validate());
        Assert.Equal(.37, ToneCurve.Identity.Evaluate(.37), 10);
    }
}
