using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;

public class ManagementTests
{
    [Fact]
    public void Password_is_required_and_not_stored_in_plaintext()
    {
        string folder = Path.Combine(Path.GetTempPath(), "FotoAutomat-tests", Guid.NewGuid().ToString());
        var store = new BoothManagementStore(folder);
        Assert.False(store.VerifyPassword("test-password"));
        store.SetPassword("test-password");
        Assert.True(store.VerifyPassword("test-password"));
        Assert.False(store.VerifyPassword("wrong-password"));
        Assert.DoesNotContain("test-password", File.ReadAllText(Path.Combine(folder, "revenue-password.json")));
        Assert.Throws<InvalidOperationException>(() => store.SetPassword("replacement"));
    }
    [Fact]
    public void Prices_persist_and_reject_decreasing_totals()
    {
        var store = new BoothManagementStore(Path.Combine(Path.GetTempPath(), "FotoAutomat-tests", Guid.NewGuid().ToString()));
        var prices = store.Prices(); Assert.Equal(8, prices.Count);
        foreach (var row in prices) row.Total = 50000 + (row.Copies - 2) * 10000;
        store.SavePrices(prices);
        Assert.Equal(80000, store.Prices().Last().Total);
        prices.Last().Total = 100;
        Assert.Throws<ArgumentException>(() => store.SavePrices(prices));
    }
}
