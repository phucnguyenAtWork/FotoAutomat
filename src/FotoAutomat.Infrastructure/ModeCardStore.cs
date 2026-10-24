using System.Text.Json;
using FotoAutomat.Core;

namespace FotoAutomat.Infrastructure;

/// <summary>Chữ trên thẻ chế độ chụp của giao diện khách và tính năng được bật, gắn với PresetId của bộ lọc.
/// Features null: ModeFeatures.Default.</summary>
public sealed record ModeCard(string Title, string Description, ModeFeatures? Features = null)
{
    public ModeFeatures EffectiveFeatures => Features ?? ModeFeatures.Default;
}

public sealed class ModeCardStore(string? directory = null)
{
    public const int MaxTitle = 40, MaxDescription = 120;
    private readonly string root = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "Operator");
    private string FilePath => Path.Combine(root, "mode-cards.json");

    public Dictionary<string, ModeCard> Load() => File.Exists(FilePath)
        ? JsonSerializer.Deserialize<Dictionary<string, ModeCard>>(File.ReadAllText(FilePath)) ?? []
        : [];

    public void Save(IReadOnlyDictionary<string, ModeCard> cards)
    {
        var cleaned = new Dictionary<string, ModeCard>();
        foreach (var (presetId, card) in cards)
        {
            string title = card.Title.Trim(), description = card.Description.Trim();
            if (title.Length > MaxTitle || description.Length > MaxDescription) throw new ArgumentException($"Tiêu đề tối đa {MaxTitle} ký tự, mô tả tối đa {MaxDescription} ký tự.");
            var features = card.Features is null || card.Features == ModeFeatures.Default ? null : card.Features;
            if (title.Length > 0 || description.Length > 0 || features is not null) cleaned[presetId] = new(title, description, features);
        }
        Directory.CreateDirectory(root);
        File.WriteAllText(FilePath + ".tmp", JsonSerializer.Serialize(cleaned));
        File.Move(FilePath + ".tmp", FilePath, true);
    }
}
