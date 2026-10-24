using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;

public class VoucherStoreTests
{
    private static VoucherStore Store() => new(Path.Combine(Path.GetTempPath(), "FotoAutomat-tests", Guid.NewGuid().ToString()));

    [Fact]
    public void Generated_codes_are_unique_six_digit_and_single_use()
    {
        var store = Store();
        var batch = store.Generate(50, 20000, false);
        Assert.Equal(50, batch.Select(v => v.Code).Distinct().Count());
        Assert.All(batch, v => Assert.Matches("^[0-9]{6}$", v.Code));
        var code = batch[0].Code;
        Assert.Equal(20000, store.Check(code).Discount);
        store.Check(code);
        store.Redeem(code);
        Assert.Throws<ArgumentException>(() => store.Check(code));
        Assert.Throws<InvalidOperationException>(() => store.Redeem(code));
        Assert.Equal("Đã dùng", store.All().Single(v => v.Code == code).StatusLabel);
    }

    [Fact]
    public void Staff_code_is_reusable_and_counts_uses()
    {
        var store = Store();
        var staff = store.CreateStaffCode(0, true, "246810");
        for (int i = 0; i < 3; i++) { Assert.True(store.Check("246810").Free); store.Redeem("246810"); }
        Assert.Equal(3, store.All().Single(v => v.Code == staff.Code).Uses);
        Assert.Throws<ArgumentException>(() => store.CreateStaffCode(10000, false, "246810"));
        Assert.Throws<ArgumentException>(() => store.CreateStaffCode(10000, false, "12ab56"));
    }

    [Fact]
    public void Only_allowed_values_and_batch_sizes_are_accepted()
    {
        var store = Store();
        Assert.Throws<ArgumentException>(() => store.Generate(1, 15000, false));
        Assert.Throws<ArgumentException>(() => store.Generate(0, 10000, false));
        Assert.Throws<ArgumentException>(() => store.Generate(VoucherStore.MaxBatch + 1, 10000, false));
        Assert.Single(store.Generate(1, 0, true));
    }

    [Fact]
    public void Revoked_code_cannot_be_used()
    {
        var store = Store();
        var code = store.Generate(1, 50000, false)[0].Code;
        store.Revoke(code);
        Assert.Throws<ArgumentException>(() => store.Check(code));
        Assert.Equal("Đã hủy", store.All()[0].StatusLabel);
    }

    [Fact]
    public void Five_wrong_codes_lock_entry_even_for_a_valid_code()
    {
        var store = Store();
        var code = store.Generate(1, 10000, false)[0].Code;
        string wrong = code == "000000" ? "000001" : "000000";
        for (int i = 0; i < 5; i++) Assert.Throws<ArgumentException>(() => store.Check(wrong));
        var error = Assert.Throws<ArgumentException>(() => store.Check(code));
        Assert.Contains("30 giây", error.Message);
    }

    [Fact]
    public void Mode_card_text_is_trimmed_limited_and_empty_entries_dropped()
    {
        var store = new ModeCardStore(Path.Combine(Path.GetTempPath(), "FotoAutomat-tests", Guid.NewGuid().ToString()));
        store.Save(new Dictionary<string, ModeCard> { ["a"] = new("  Kodak Portra ", " Màu ấm nhẹ "), ["b"] = new("", " ") });
        var loaded = store.Load();
        Assert.Equal(new ModeCard("Kodak Portra", "Màu ấm nhẹ"), loaded["a"]);
        Assert.False(loaded.ContainsKey("b"));
        Assert.Throws<ArgumentException>(() => store.Save(new Dictionary<string, ModeCard> { ["a"] = new(new string('x', 41), "") }));
    }
}
