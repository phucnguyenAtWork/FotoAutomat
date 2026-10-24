using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Tests;

public sealed class SessionPriceTests
{
    private static BoothManagementStore NewStore() => new(Path.Combine(Path.GetTempPath(), "FotoAutomat-tests", Guid.NewGuid().ToString()));

    [Fact]
    public void Session_prices_default_to_two_rounded_values()
    {
        var prices = NewStore().SessionPrices();
        Assert.Equal(90000, prices.Small);
        Assert.Equal(120000, prices.Large);
    }

    [Fact]
    public void Session_prices_persist_and_allow_equal_sizes()
    {
        var store = NewStore();
        store.SaveSessionPrices(new(100000, 100000));
        Assert.Equal(new BoothSessionPrices(100000, 100000), store.SessionPrices());
    }

    [Theory]
    [InlineData(-1, 100000)]
    [InlineData(100000, 100000001)]
    public void Session_prices_reject_out_of_range_values(int small, int large) =>
        Assert.Throws<ArgumentException>(() => NewStore().SaveSessionPrices(new(small, large)));

    [Fact]
    public void Customer_flow_uses_the_two_configured_sizes()
    {
        var flow = new CustomerPreviewFlow(packages: [new(2, 90000, "Khổ nhỏ"), new(2, 120000, "Khổ lớn")]);
        flow.Next(); flow.Next();
        Assert.Equal(2, flow.PackageOptions.Count);
        Assert.Equal("Khổ nhỏ · 90.000đ", flow.PackageOptions[0].Label);
        flow.SelectPackage(1);
        Assert.Equal("120.000đ", flow.Amount);
        flow.SelectPackage(2);
        Assert.Equal(1, flow.PackageIndex);
    }
}
