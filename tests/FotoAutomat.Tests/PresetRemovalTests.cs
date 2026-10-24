using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Tests;

public sealed class PresetRemovalTests
{
    private static ColorRecipe Lut() => new("Imported") { Lut = new(2, new double[24], [0, 0, 0], [1, 1, 1]) };
    [Fact]
    public void Removal_preserves_default_identity_and_unrelated_edits()
    {
        var lut = Lut(); var edited = new ColorRecipe("Edited", Exposure: .7);
        var workspace = new ColorWorkspace(null, [lut, edited]) { DefaultIndex = 1 };
        var removed = workspace.RemovePreset(lut.PresetId);
        Assert.Same(edited, Assert.Single(removed.Looks)); Assert.Equal(0, removed.DefaultIndex);
        Assert.Equal(2, workspace.Looks.Length);
    }
    [Fact]
    public void Removing_default_or_last_preset_keeps_valid_library()
    {
        var lut = Lut(); var other = new ColorRecipe();
        var removed = new ColorWorkspace(null, [other, lut]) { DefaultIndex = 1 }.RemovePreset(lut.PresetId);
        Assert.Equal(0, removed.DefaultIndex); Assert.Same(other, removed.Looks[0]);
        var last = new ColorWorkspace(null, [lut]).RemovePreset(lut.PresetId);
        Assert.Null(Assert.Single(last.Looks).Lut); Assert.Equal(0, last.DefaultIndex); last.Looks[0].Validate();
    }
    [Fact]
    public void Regular_and_matched_presets_can_be_removed_and_missing_id_is_noop()
    {
        var natural = new ColorRecipe(); var matched = new ColorRecipe("Matched look 1", Warmth: 12); var warm = new ColorRecipe("Ấm", Warmth: 25);
        var workspace = new ColorWorkspace(null, [natural, matched, warm]) { DefaultIndex = 2 };
        Assert.Same(workspace, workspace.RemovePreset("absent"));
        var removed = workspace.RemovePreset(matched.PresetId);
        Assert.Equal([natural, warm], removed.Looks); Assert.Equal(1, removed.DefaultIndex); Assert.Same(warm, removed.Looks[removed.DefaultIndex]);
    }
}
