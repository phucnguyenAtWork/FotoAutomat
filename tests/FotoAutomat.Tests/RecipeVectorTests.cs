using FotoAutomat.Core;

namespace FotoAutomat.Tests;

public sealed class RecipeVectorTests
{
    [Fact]
    public void Layout_is_fixed_and_identity_encodes_to_zero()
    {
        Assert.Equal(52, RecipeVector.Length);
        Assert.Equal(RecipeVector.Length, RecipeVector.Parameters.Select(p => p.Name).Distinct().Count());
        Assert.All(RecipeVector.Encode(new ColorRecipe()), v => Assert.Equal(0, v, 9));
        Assert.All(RecipeVector.ToUnit(RecipeVector.Encode(new ColorRecipe())), v => Assert.Equal(0, v, 9));
    }

    [Fact]
    public void Decode_then_encode_round_trips_and_renders_identically()
    {
        var random = new Random(5);
        for (int n = 0; n < 50; n++)
        {
            var unit = Enumerable.Range(0, RecipeVector.Length).Select(_ => random.NextDouble() * 1.6 - .8).ToArray();
            var raw = RecipeVector.FromUnit(unit);
            // Curve knots may be made monotone by Decode; re-encoding a decoded recipe must be stable.
            var recipe = RecipeVector.Decode(raw); var again = RecipeVector.Decode(RecipeVector.Encode(recipe));
            Assert.All(RecipeVector.Encode(recipe).Zip(RecipeVector.Encode(again)), p => Assert.Equal(p.First, p.Second, 6));
            var pixels = Enumerable.Range(0, 64 * 4).Select(i => (byte)(i % 4 == 3 ? 255 : random.Next(256))).ToArray();
            Assert.Equal(ColorProcessor.Apply(pixels, recipe), ColorProcessor.Apply(pixels, again));
        }
    }

    [Fact]
    public void Values_are_clamped_and_unit_round_trip_holds()
    {
        var raw = RecipeVector.Encode(new ColorRecipe(Exposure: 4, Warmth: -100, Saturation: 90));
        Assert.Equal(1.5, raw[0]); Assert.Equal(-60, raw[1]); Assert.Equal(60, raw[8]);
        var unit = RecipeVector.ToUnit(raw); Assert.Equal(1, unit[0], 9); Assert.Equal(-1, unit[1], 9);
        Assert.All(RecipeVector.FromUnit(unit).Zip(raw), p => Assert.Equal(p.Second, p.First, 9));
        Assert.Throws<ArgumentException>(() => RecipeVector.Decode(new double[3]));
        RecipeVector.Decode(Enumerable.Repeat(double.NaN, RecipeVector.Length).ToArray()).Validate();
    }
}
