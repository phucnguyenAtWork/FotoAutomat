using System.Text.Json;
using FotoAutomat.Core;

namespace FotoAutomat.Infrastructure;

public sealed class PrinterConfigurationStore
{
    public static string DefaultPath => Path.Combine(Path.GetDirectoryName(PrintLayoutStore.ActivePath)!, "printer.json");
    public PrinterConfiguration Load() { var value = File.Exists(DefaultPath) ? JsonSerializer.Deserialize<PrinterConfiguration>(File.ReadAllText(DefaultPath)) ?? new() : new(); value.Validate(); return value; }
    public void Save(PrinterConfiguration value)
    {
        value.Validate(); Directory.CreateDirectory(Path.GetDirectoryName(DefaultPath)!);
        File.WriteAllText(DefaultPath + ".tmp", JsonSerializer.Serialize(value)); File.Move(DefaultPath + ".tmp", DefaultPath, true);
    }
}
