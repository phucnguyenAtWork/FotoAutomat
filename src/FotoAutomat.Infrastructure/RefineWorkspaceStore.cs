using System.Text.Json;
using FotoAutomat.Core;
namespace FotoAutomat.Infrastructure;
public sealed record RefineWorkspace(string? SamplePath, RefineSettings Settings);
public sealed class RefineWorkspaceStore(string? path = null)
{
    private readonly string target = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "Operator", "refine-workspace.json");
    public RefineWorkspace Load()
    {
        var value = File.Exists(target) ? JsonSerializer.Deserialize<RefineWorkspace>(File.ReadAllText(target)) ?? throw new InvalidDataException("Cannot read Refine settings.") : new(null, new());
        value.Settings.Validate(); return value;
    }
    public void Save(RefineWorkspace value)
    {
        value.Settings.Validate(); Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target))!);
        File.WriteAllText(target + ".tmp", JsonSerializer.Serialize(value)); File.Move(target + ".tmp", target, true);
    }
}
