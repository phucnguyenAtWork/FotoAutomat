namespace FotoAutomat.Core;

public sealed record BlurGlowSettings
{
    public double MistAmount { get; init; }
    public double MistSpread { get; init; } = 35;
    public double Haze { get; init; } = 20;
    public double PreserveBlacks { get; init; } = 70;
    public double GlowAmount { get; init; }
    public double GlowRadius { get; init; } = 35;
    public double Threshold { get; init; } = 70;
    public double Softness { get; init; } = 40;
    public double Warmth { get; init; }
    public double GaussianRadius { get; init; } = 20;
    public double GaussianMix { get; init; }
    public bool IsActive => MistAmount > 0 || GlowAmount > 0 || GaussianMix > 0 && GaussianRadius > 0;

    public void Validate()
    {
        if (new[] { MistAmount, MistSpread, Haze, PreserveBlacks, GlowAmount, GlowRadius, Threshold, Softness, GaussianRadius, GaussianMix }
            .Any(v => !double.IsFinite(v) || v < 0 || v > 100) || !double.IsFinite(Warmth) || Math.Abs(Warmth) > 100)
            throw new ArgumentException("Blur & Glow settings are out of range.");
    }
}
