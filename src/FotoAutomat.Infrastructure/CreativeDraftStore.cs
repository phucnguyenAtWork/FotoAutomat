using System.Text.Json;

namespace FotoAutomat.Infrastructure;

/// <summary>Operator drafts only. No camera SDK or image processing is invoked.</summary>
public sealed record CreativeDraft(string Camera = "Canon EOS R50", string LookName = "Màu mới", double Exposure = 0, double Contrast = 0, double Warmth = 0, double Saturation = 0, double Beauty = 0, double Sharpness = 0);

public sealed class CreativeDraftStore(string? path = null)
{
    private readonly string target = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "Operator", "creative-draft.json");
    public CreativeDraft Load() => File.Exists(target) ? JsonSerializer.Deserialize<CreativeDraft>(File.ReadAllText(target)) ?? throw new InvalidDataException("Không đọc được bản nháp màu.") : new();
    public void Save(CreativeDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.LookName) || draft.LookName.Length > 80) throw new ArgumentException("Tên bộ màu cần từ 1 đến 80 ký tự.");
        if (draft.Camera is not ("Canon EOS R50" or "Canon EOS R100" or "Fujifilm X-H2")) throw new ArgumentException("Máy ảnh không hợp lệ.");
        if (!double.IsFinite(draft.Exposure) || Math.Abs(draft.Exposure) > 3 || new[] { draft.Contrast, draft.Warmth, draft.Saturation }.Any(v => !double.IsFinite(v) || Math.Abs(v) > 100) || new[] { draft.Beauty, draft.Sharpness }.Any(v => !double.IsFinite(v) || v < 0 || v > 100)) throw new ArgumentException("Thông số màu vượt giới hạn.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target))!);
        File.WriteAllText(target + ".tmp", JsonSerializer.Serialize(draft));
        File.Move(target + ".tmp", target, true);
    }
}
