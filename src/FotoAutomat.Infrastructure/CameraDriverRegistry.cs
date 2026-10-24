using FotoAutomat.Core;

namespace FotoAutomat.Infrastructure;

/// <summary>Explicit registration: missing vendor SDK never falls back to simulated capture.</summary>
public sealed class CameraDriverRegistry(IEnumerable<ICameraDriver> drivers)
{
    private readonly Dictionary<CameraBrand, ICameraDriver> drivers = drivers.ToDictionary(d => d.Brand);
    public IReadOnlyList<CameraBrand> InstalledBrands => drivers.Keys.ToArray();
    public ICameraDriver Require(CameraBrand brand) => drivers.TryGetValue(brand, out var driver)
        ? driver : throw new InvalidOperationException($"Camera driver for {brand} is not installed. Install and validate the vendor SDK adapter first.");
}
