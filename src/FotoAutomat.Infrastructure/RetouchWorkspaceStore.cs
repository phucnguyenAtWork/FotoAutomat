using System.Text.Json;
using FotoAutomat.Core;
namespace FotoAutomat.Infrastructure;
public sealed record RetouchWorkspace(string? SamplePath, FaceRetouch[] Faces);
public sealed class RetouchWorkspaceStore(string? path = null)
{
    private readonly string target = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "Operator", "retouch-workspace.json");
    public RetouchWorkspace Load()
    {
        var value = File.Exists(target) ? JsonSerializer.Deserialize<RetouchWorkspace>(File.ReadAllText(target)) ?? throw new InvalidDataException("Cannot read retouch settings.") : new(null, []);
        Validate(value); return value;
    }
    private static void Validate(RetouchWorkspace value) { if (value.Faces.Length > 20) throw new ArgumentException("Maximum 20 faces."); foreach (var f in value.Faces) f.Validate(); }
    public void Save(RetouchWorkspace value)
    {
        Validate(value); Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target))!);
        File.WriteAllText(target + ".tmp", JsonSerializer.Serialize(value)); File.Move(target + ".tmp", target, true);
    }
}
