using System.Text.Json;
using FotoAutomat.Core;

namespace FotoAutomat.Infrastructure;
public sealed record ColorWorkspace(string? SamplePath, ColorRecipe[] Looks)
{
    public int DefaultIndex { get; init; }
    /// <summary>Removes any preset (adjusted, matched or imported LUT). The library never becomes empty, and the
    /// default moves to the first remaining preset when the default itself is removed.</summary>
    public ColorWorkspace RemovePreset(string presetId)
    {
        int index = Array.FindIndex(Looks, r => r.PresetId == presetId);
        if (index < 0) return this;
        var remaining = Looks.Where((_, i) => i != index).ToArray();
        if (remaining.Length == 0) remaining = [new ColorRecipe()];
        int nextDefault = index == DefaultIndex ? 0 : DefaultIndex - (index < DefaultIndex ? 1 : 0);
        return this with { Looks = remaining, DefaultIndex = nextDefault };
    }
}
public sealed class ColorWorkspaceStore(string? path = null)
{
    private readonly string target = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "Operator", "color-workspace.json");
    public ColorWorkspace Load() => File.Exists(target) ? JsonSerializer.Deserialize<ColorWorkspace>(File.ReadAllText(target)) ?? throw new InvalidDataException("Không đọc được bộ màu.") : new(null, [new(), new("Ấm", Warmth: 25, Saturation: -10), new("Trắng đen", Monochrome: true)]);
    public void Save(ColorWorkspace value)
    {
        if (value.Looks.Length is < 1 or > 100 || value.DefaultIndex < 0 || value.DefaultIndex >= value.Looks.Length) throw new ArgumentException("Preset library không hợp lệ (1–100 preset).");
        foreach (var look in value.Looks) look.Validate();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target))!);
        File.WriteAllText(target + ".tmp", JsonSerializer.Serialize(value)); File.Move(target + ".tmp", target, true);
    }
}
