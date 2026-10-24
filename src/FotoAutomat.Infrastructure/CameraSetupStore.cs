using System.Text.Json;

namespace FotoAutomat.Infrastructure;

public sealed record CameraSetup
{
    public string Brand { get; init; } = "Canon";
    public string Model { get; init; } = "Canon EOS R50";
    public bool MirrorLiveView { get; init; } = true;
    public int Rotation { get; init; }
    public int PreviewWidth { get; init; } = 640;
    public string SdkDirectory { get; init; } = "";
    public Dictionary<string, string> ParameterValues { get; init; } = new();
    public void Validate()
    {
        if (Brand is not ("Canon" or "Fujifilm" or "Nikon") || string.IsNullOrWhiteSpace(Model) || Model.Length > 100)
            throw new ArgumentException("Máy ảnh mục tiêu không hợp lệ.");
        if (Rotation is not (0 or 90 or 180 or 270) || PreviewWidth is not (640 or 960 or 1280))
            throw new ArgumentException("Thiết lập hiển thị không hợp lệ.");
    }
}

public sealed class CameraSetupStore(string? path = null)
{
    private readonly string path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "Operator", "camera-setup.json");
    public bool Exists => File.Exists(path);
    public CameraSetup Load()
    {
        var value = File.Exists(path) ? JsonSerializer.Deserialize<CameraSetup>(File.ReadAllText(path)) ?? throw new InvalidDataException("Cấu hình máy ảnh trống.") : new CameraSetup();
        value.Validate(); return value;
    }
    public void Save(CameraSetup value)
    {
        value.Validate(); Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, path, overwrite: true);
    }
}

