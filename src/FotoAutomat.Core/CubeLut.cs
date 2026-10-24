namespace FotoAutomat.Core;

/// <summary>RGB 3D LUT, red changes fastest. Values are persisted with the preset.</summary>
public sealed record CubeLut(int Size, double[] Values, double[] DomainMin, double[] DomainMax)
{
    public void Validate()
    {
        if (Size < 2 || Size > 65 || Values.Length != Size * Size * Size * 3 || DomainMin.Length != 3 || DomainMax.Length != 3 || Values.Any(v => !double.IsFinite(v)) || DomainMin.Concat(DomainMax).Any(v => !double.IsFinite(v)) || Enumerable.Range(0, 3).Any(c => DomainMax[c] <= DomainMin[c])) throw new ArgumentException("Invalid 3D CUBE table.");
    }
    public (double R, double G, double B) Evaluate(double r, double g, double b)
    {
        double x = Math.Clamp((r - DomainMin[0]) / (DomainMax[0] - DomainMin[0]), 0, 1) * (Size - 1), y = Math.Clamp((g - DomainMin[1]) / (DomainMax[1] - DomainMin[1]), 0, 1) * (Size - 1), z = Math.Clamp((b - DomainMin[2]) / (DomainMax[2] - DomainMin[2]), 0, 1) * (Size - 1);
        int ix = Math.Min((int)x, Size - 2), iy = Math.Min((int)y, Size - 2), iz = Math.Min((int)z, Size - 2);
        double fx = x - ix, fy = y - iy, fz = z - iz;
        double Channel(int c)
        {
            double value = 0;
            for (int dz = 0; dz < 2; dz++) for (int dy = 0; dy < 2; dy++) for (int dx = 0; dx < 2; dx++) value += Values[(((iz + dz) * Size + iy + dy) * Size + ix + dx) * 3 + c] * (dx == 0 ? 1 - fx : fx) * (dy == 0 ? 1 - fy : fy) * (dz == 0 ? 1 - fz : fz);
            return value;
        }
        return (Channel(0), Channel(1), Channel(2));
    }
}
