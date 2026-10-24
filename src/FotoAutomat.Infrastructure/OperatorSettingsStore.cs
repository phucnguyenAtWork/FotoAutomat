using System.Text.Json;

namespace FotoAutomat.Infrastructure;

/// <summary>Draft operator preferences, deliberately not connected to the booth runtime.</summary>
public sealed record OperatorSettings
{
    public string BoothName { get; init; } = "Foto Hunter";
    public string CameraNotes { get; init; } = "";
    public string PaymentNotes { get; init; } = "";
    public string PrinterNotes { get; init; } = "";
    public string BrandNotes { get; init; } = "";
}

public sealed class OperatorSettingsStore(string path)
{
    public OperatorSettings Load() => File.Exists(path)
        ? JsonSerializer.Deserialize<OperatorSettings>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Settings file is empty.")
        : new();

    public void Save(OperatorSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.BoothName) || settings.BoothName.Length > 80)
            throw new ArgumentException("Tên booth cần từ 1 đến 80 ký tự.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, path, overwrite: true);
    }
}
