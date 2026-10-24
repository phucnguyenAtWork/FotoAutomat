using System.IO;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Desktop;

/// <summary>Camera for the customer screens. Create builds a fresh connection each time, because a faulted
/// BoothCameraPipeline cannot reconnect. Problem explains why the booth runs as a preview without a camera.</summary>
internal sealed record CustomerCamera(Func<BoothCameraPipeline>? Create, string? Problem)
{
    /// <summary>JPEG served as live view and captures (--simulated-camera). Development only, never the default.</summary>
    public static string? SimulatedSource { get; set; }

    public static CustomerCamera Resolve()
    {
        string root;
        try { root = Path.Combine(new GeneralSettingsStore().Load().DataDirectory, "Sessions"); }
        catch (Exception error) { return new(null, "Không đọc được cài đặt chung: " + error.Message); }
        if (SimulatedSource is { } sample) return new(() => new BoothCameraPipeline(new SimulatedCameraConnection(sample), root), null);

        var store = new CameraSetupStore();
        if (!store.Exists) return new(null, "Chưa cấu hình máy ảnh");
        CameraSetup setup;
        try { setup = store.Load(); }
        catch (Exception error) { return new(null, "Cấu hình máy ảnh lỗi: " + error.Message); }
        if (setup.Brand != "Fujifilm") return new(null, $"Chưa có adapter {setup.Brand}");
        string? sdk = string.IsNullOrWhiteSpace(setup.SdkDirectory) ? FujifilmSdkDiscovery.FindInstalled() : setup.SdkDirectory;
        if (string.IsNullOrWhiteSpace(sdk)) return new(null, "Chưa tìm thấy Fujifilm SDK");
        return new(() => new BoothCameraPipeline(new FujifilmCameraConnection(sdk), root), null);
    }
}
