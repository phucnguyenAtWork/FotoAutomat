using System.Text.Json;
using FotoAutomat.Core;

namespace FotoAutomat.Infrastructure;

public sealed class CaptureConfigurationStore(string path)
{
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "Operator", "capture.json");
    public CaptureConfiguration Load()
    {
        CaptureConfiguration value = File.Exists(path)
            ? JsonSerializer.Deserialize<CaptureConfiguration>(File.ReadAllText(path)) ?? throw new InvalidDataException("Cấu hình chụp trống.")
            : new();
        value.Validate();
        return value;
    }
    public void Save(CaptureConfiguration value)
    {
        value.Validate();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(value));
        File.Move(path + ".tmp", path, overwrite: true);
    }
}
