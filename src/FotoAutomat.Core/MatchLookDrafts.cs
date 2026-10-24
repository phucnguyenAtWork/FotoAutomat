namespace FotoAutomat.Core;

/// <summary>Where a Match Look result goes. A result only overwrites the unsaved draft of the current match; once that
/// draft is saved (published), or the references change, the next Apply creates a new "Matched look N" so existing
/// presets are never modified by matching.</summary>
public static class MatchLookDrafts
{
    public const string BaseName = "Matched look";

    public static bool CanReuse(string? draftId, IEnumerable<ColorRecipe> drafts, IEnumerable<ColorRecipe> published) =>
        draftId is not null && drafts.Any(r => r.PresetId == draftId) && !published.Any(r => r.PresetId == draftId);

    /// <summary>"Matched look 1", "Matched look 2", … — one more than the highest number already used.</summary>
    public static string NextName(IEnumerable<string> names)
    {
        int highest = 0;
        foreach (var name in names)
        {
            if (name.StartsWith(BaseName + " ", StringComparison.Ordinal) && int.TryParse(name[(BaseName.Length + 1)..], out int n) && n > 0) highest = Math.Max(highest, n);
        }
        return $"{BaseName} {highest + 1}";
    }
}
