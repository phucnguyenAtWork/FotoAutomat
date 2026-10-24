using System.Text.Json;
using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
using Microsoft.Data.Sqlite;
namespace FotoAutomat.Tests;

public class ShootingModeTests
{
    private static byte[] Noisy() => [.. Enumerable.Range(0, 64).SelectMany(i => new byte[] { (byte)(60 + i % 7 * 20), (byte)(90 + i % 3 * 30), (byte)(120 + i % 5 * 15), 255 })];

    [Fact]
    public void Legacy_grain_and_sharpening_move_to_refine_once()
    {
        var look = new ColorRecipe { Grain = 40, Sharpening = 60, Vignette = -20 };
        var mode = ShootingMode.Compose(look, null, null);
        Assert.Equal(0, mode.Color.Grain);
        Assert.Equal(0, mode.Color.Sharpening);
        Assert.Equal(-20, mode.Color.Vignette);
        Assert.Equal(30, mode.Refine.GrainImpact);
        Assert.Equal(80, mode.Refine.SharpenAmount);
        Assert.Equal(2, mode.Notes.Length);
        // Recipe màu không còn tự áp grain/sharpen, nên không bị cộng hai lần.
        Assert.Equal(ColorProcessor.Apply(Noisy(), look with { Grain = 0, Sharpening = 0 }, 8, 8), ColorProcessor.Apply(Noisy(), look, 8, 8));
    }

    [Fact]
    public void Existing_refine_values_win_and_sample_dust_spots_are_dropped()
    {
        var refine = new RefineSettings(SharpenAmount: 120, GrainImpact: 10, DustSpots: [new(.5, .5)]);
        var mode = ShootingMode.Compose(new ColorRecipe { Grain = 40, Sharpening = 60 }, new RetouchSettings(EvenSkin: 30), refine);
        Assert.Equal(120, mode.Refine.SharpenAmount);
        Assert.Equal(10, mode.Refine.GrainImpact);
        Assert.Null(mode.Refine.DustSpots);
        Assert.Equal(30, mode.Retouch.EvenSkin);
        Assert.All(mode.Notes, note => Assert.StartsWith("Bỏ", note));
    }

    [Fact]
    public void Retouch_of_sample_takes_the_first_edited_face_without_its_coordinates()
    {
        FaceRetouch[] faces = [new(.1, .1, .2, .2, new()), new(.5, .1, .2, .2, new(Blemishes: 40)), new(.7, .1, .2, .2, new(Blemishes: 90))];
        Assert.Equal(40, ShootingMode.RetouchOf(faces).Blemishes);
        Assert.Equal(new RetouchSettings(), ShootingMode.RetouchOf([]));
    }

    [Fact]
    public void Disabled_features_are_neutral()
    {
        var lut = new CubeLut(2, [.. Enumerable.Range(0, 8).SelectMany(i => new double[] { (i & 1) / 2d, i >> 1 & 1, i >> 2 & 1 })], [0, 0, 0], [1, 1, 1]);
        var look = new ColorRecipe(Exposure: .5) { Lut = lut, BlurGlow = new BlurGlowSettings { GlowAmount = 40 }, Grain = 40 };
        var retouch = new RetouchSettings(EvenSkin: 30);
        var refine = new RefineSettings(SharpenAmount: 90);

        var lutOnly = ShootingMode.Compose(look, retouch, refine, new(true, false, false, false, false));
        Assert.Same(lut, lutOnly.Color.Lut);
        Assert.Equal(0, lutOnly.Color.Exposure);
        Assert.Null(lutOnly.Color.BlurGlow);
        Assert.Equal(look.PresetId, lutOnly.Color.PresetId);
        Assert.Equal(new RetouchSettings(), lutOnly.Retouch);
        Assert.Equal(new RefineSettings(), lutOnly.Refine);

        var adjustRetouch = ShootingMode.Compose(look, retouch, refine, new(false, true, false, true, false));
        Assert.Null(adjustRetouch.Color.Lut);
        Assert.Equal(.5, adjustRetouch.Color.Exposure);
        Assert.Equal(30, adjustRetouch.Retouch.EvenSkin);
        Assert.Equal(0, adjustRetouch.Refine.SharpenAmount);
        Assert.Equal(30, adjustRetouch.Refine.GrainImpact);

        var all = ShootingMode.Compose(look, retouch, refine, ModeFeatures.All);
        Assert.NotNull(all.Color.BlurGlow);
        Assert.Equal(90, all.Refine.SharpenAmount);
    }

    [Fact]
    public void Mode_card_store_keeps_features_and_omits_the_default()
    {
        string folder = Path.Combine(Path.GetTempPath(), "FotoAutomat-tests", Guid.NewGuid().ToString());
        var store = new ModeCardStore(folder);
        var custom = new ModeFeatures(true, false, false, true, false);
        store.Save(new Dictionary<string, ModeCard> { ["a"] = new("", "", custom), ["b"] = new("", "", ModeFeatures.Default), ["c"] = new("Tên", "") });
        var loaded = store.Load();
        Assert.Equal(custom, loaded["a"].EffectiveFeatures);
        Assert.False(loaded.ContainsKey("b"));
        Assert.Equal(ModeFeatures.Default, loaded["c"].EffectiveFeatures);
    }

    [Fact]
    public void Paid_session_keeps_mode_snapshot_and_gets_a_unique_share_token()
    {
        string folder = Path.Combine(Path.GetTempPath(), "FotoAutomat-tests", Guid.NewGuid().ToString());
        var store = new SqliteCustomerSessionStore(folder);
        var snapshot = new SessionSnapshot("Khổ nhỏ", 4, 4, 2, "p", "Tự nhiên", "{}", null, JsonSerializer.Serialize(new CaptureConfiguration()),
            new RetouchSettings(EvenSkin: 25), new RefineSettings(SharpenAmount: 50), ["note"]);
        var first = store.StartPaid(snapshot, new SessionPayment(90000, 0, 90000, "cash"));
        var second = store.StartPaid(snapshot, new SessionPayment(90000, 0, 90000, "cash"));
        var reopened = new SqliteCustomerSessionStore(folder).Get(first.Id);
        Assert.Equal(25, reopened.Snapshot.Retouch!.EvenSkin);
        Assert.Equal(50, reopened.Snapshot.Refine!.SharpenAmount);
        Assert.Equal(first.ShareToken, reopened.ShareToken);
        Assert.Equal(22, reopened.ShareToken!.Length);
        Assert.NotEqual(first.ShareToken, second.ShareToken);
        Assert.Equal(2, store.List().Count(s => s.Session.ShareToken is not null));
    }

    [Fact]
    public void Existing_database_without_share_token_column_is_upgraded()
    {
        string folder = Path.Combine(Path.GetTempPath(), "FotoAutomat-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        using (var old = new SqliteConnection($"Data Source={Path.Combine(folder, "booth.db")};Pooling=False"))
        {
            old.Open();
            using var create = old.CreateCommand();
            create.CommandText = "CREATE TABLE customer_sessions (id TEXT PRIMARY KEY, started_at TEXT NOT NULL, stage TEXT NOT NULL, revision INTEGER NOT NULL, completed_at TEXT, compensates TEXT, snapshot TEXT NOT NULL)";
            create.ExecuteNonQuery();
        }
        var session = new SqliteCustomerSessionStore(folder).StartPaid(
            new SessionSnapshot("Khổ lớn", 2, 4, 0, "p", "n", "{}", null, "{}"), new SessionPayment(120000, 0, 120000, "qr"));
        Assert.NotNull(new SqliteCustomerSessionStore(folder).Get(session.Id).ShareToken);
    }
}
