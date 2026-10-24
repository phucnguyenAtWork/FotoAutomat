using FotoAutomat.Core;

namespace FotoAutomat.Tests;

public sealed class MatchLookDraftTests
{
    [Fact]
    public void Only_an_unsaved_draft_of_the_current_match_is_reused()
    {
        var saved = new ColorRecipe("Matched look 1"); var draft = new ColorRecipe("Matched look 2"); var natural = new ColorRecipe();
        ColorRecipe[] drafts = [natural, saved, draft], published = [natural, saved];
        Assert.True(MatchLookDrafts.CanReuse(draft.PresetId, drafts, published));
        Assert.False(MatchLookDrafts.CanReuse(saved.PresetId, drafts, published));    // saved preset: never overwritten
        Assert.False(MatchLookDrafts.CanReuse(null, drafts, published));              // new references: new look
        Assert.False(MatchLookDrafts.CanReuse("deleted", drafts, published));         // draft was removed
    }

    [Fact]
    public void New_matched_looks_get_the_next_free_number()
    {
        Assert.Equal("Matched look 1", MatchLookDrafts.NextName(["Tự nhiên", "Ấm"]));
        Assert.Equal("Matched look 1", MatchLookDrafts.NextName(["Matched look"]));
        Assert.Equal("Matched look 3", MatchLookDrafts.NextName(["Matched look", "Matched look 2", "Matched look 1"]));
        Assert.Equal("Matched look 1", MatchLookDrafts.NextName(["Matched look x", "Matched looks 9"]));
    }
}
