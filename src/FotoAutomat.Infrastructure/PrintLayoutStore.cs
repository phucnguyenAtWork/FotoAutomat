using System.Text.Json;
using FotoAutomat.Core;

namespace FotoAutomat.Infrastructure;

/// <summary>Mẫu bố cục: thư viện (Layouts), bản đang sửa và các khung đã phát hành cho khách (Published).</summary>
public sealed class PrintLayoutStore(string? operatorDirectory = null)
{
    private readonly string directory = operatorDirectory ?? Path.GetDirectoryName(ActivePath)!;
    /// <summary>Bản sao cố định của mẫu trong thư viện, cùng tên file. Sửa mẫu không đổi khung khách cho tới khi phát hành lại.</summary>
    public string PublishedDirectory => Path.Combine(directory, "Published");
    public string PublishedPath(string libraryPath) => Path.Combine(PublishedDirectory, Path.GetFileName(libraryPath));
    public void Publish(string libraryPath, PrintLayout layout) => Save(PublishedPath(libraryPath), layout);
    public void Unpublish(string libraryPath) { if (File.Exists(PublishedPath(libraryPath))) File.Delete(PublishedPath(libraryPath)); }

    /// <summary>Khung đã phát hành, theo thứ tự phát hành. Mẫu hỏng bị bỏ qua để màn khách vẫn chạy.</summary>
    public IReadOnlyList<PrintLayout> LoadPublished()
    {
        MigrateActive();
        if (!Directory.Exists(PublishedDirectory)) return [];
        var result = new List<PrintLayout>();
        foreach (string path in Directory.EnumerateFiles(PublishedDirectory, "*.fotolayout").OrderBy(File.GetLastWriteTimeUtc))
        {
            try { result.Add(Load(path)); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidDataException) { }
        }
        return result;
    }

    // Trước đây chỉ có một khung "Dùng cho khách" (active.fotolayout): chuyển thành khung phát hành một lần.
    private void MigrateActive()
    {
        string active = Path.Combine(directory, "active.fotolayout");
        if (Directory.Exists(PublishedDirectory) || !File.Exists(active)) return;
        PrintLayout layout = Load(active);
        string json = JsonSerializer.Serialize(layout), library = Path.Combine(directory, "Layouts");
        string? match = !Directory.Exists(library) ? null : Directory.EnumerateFiles(library, "*.fotolayout").FirstOrDefault(path =>
        {
            try { return JsonSerializer.Serialize(Load(path)) == json; }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidDataException) { return false; }
        });
        if (match is null) { match = Path.Combine(library, Guid.NewGuid() + ".fotolayout"); Save(match, layout); }
        Publish(match, layout);
        File.Move(active, active + ".migrated", overwrite: true);
    }
    public static string LibraryDirectory => Path.Combine(Path.GetDirectoryName(ActivePath)!, "Layouts");
    public static string EditorPath => Path.Combine(Path.GetDirectoryName(ActivePath)!, "editor.fotolayout");
    public static string ActivePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "Operator", "active.fotolayout");
    public PrintLayout? LoadActive() => File.Exists(ActivePath) ? Load(ActivePath) : null;
    public PrintLayout Load(string path)
    {
        if (new FileInfo(path).Length > 30_000_000) throw new InvalidDataException("Tệp mẫu quá lớn.");
        PrintLayout value = JsonSerializer.Deserialize<PrintLayout>(File.ReadAllText(path)) ?? throw new InvalidDataException("Tệp mẫu trống.");
        value.Validate();
        return value;
    }
    public void Save(string path, PrintLayout value)
    {
        value.Validate();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(path + ".tmp", path, overwrite: true);
    }
    public byte[] ReadArtwork(string path)
    {
        if (new FileInfo(path).Length > 20_000_000) throw new InvalidDataException("Chọn ảnh PNG nhỏ hơn 20 MB.");
        return File.ReadAllBytes(path);
    }
}
