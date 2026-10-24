using System.Text.Json;
using FotoAutomat.Core;
namespace FotoAutomat.Tests;

public class RetakeAndVoucherFlowTests
{
    private static CustomerPreviewFlow AtPayment(int price = 90000)
    {
        var flow = new CustomerPreviewFlow(new CaptureConfiguration(CountdownSeconds: 1, ReviewSeconds: 2) { Shots = 4, RetakeLimit = 1 }, packages: [new PreviewPackage(1, price, "Khổ nhỏ"), new PreviewPackage(1, 120000, "Khổ lớn")]);
        flow.Next(); flow.Next(); flow.Next();
        Assert.Equal(CustomerScreen.Payment, flow.Screen);
        return flow;
    }

    [Fact]
    public void Retake_discards_the_reviewed_shot_and_counts_down_again_until_the_limit()
    {
        var flow = AtPayment();
        flow.ChoosePayment(cash: true); flow.Next(); flow.Next();
        Assert.Equal(CustomerScreen.Capture, flow.Screen);
        flow.Tick(TimeSpan.FromSeconds(1));
        Assert.Equal(1, flow.CapturedCount);
        Assert.True(flow.CanRetake);
        flow.RetakeLast();
        Assert.Equal(0, flow.CapturedCount);
        Assert.Equal(0, flow.RetakesLeft);
        Assert.False(flow.CanRetake);
        flow.Tick(TimeSpan.FromSeconds(1));
        Assert.Equal(1, flow.CapturedCount);
        Assert.False(flow.CanRetake);
        flow.RetakeLast();
        Assert.Equal(1, flow.CapturedCount);
    }

    [Fact]
    public void Retake_works_while_reviewing_the_last_shot()
    {
        var flow = AtPayment();
        flow.ChoosePayment(cash: true); flow.Next(); flow.Next();
        for (int shot = 0; shot < 4; shot++) { flow.Tick(TimeSpan.FromSeconds(1)); if (shot < 3) flow.Tick(TimeSpan.FromSeconds(2)); }
        Assert.Equal(4, flow.CapturedCount);
        Assert.True(flow.CanRetake);
        flow.RetakeLast();
        Assert.Equal(CustomerScreen.Capture, flow.Screen);
        flow.Tick(TimeSpan.FromSeconds(1)); flow.Tick(TimeSpan.FromSeconds(2));
        Assert.Equal(CustomerScreen.Selection, flow.Screen);
        Assert.Equal(4, flow.CapturedCount);
    }

    [Fact]
    public void Voucher_discount_is_capped_and_follows_the_selected_size()
    {
        var flow = AtPayment();
        flow.ApplyVoucher(new AppliedVoucher("123456", 50000, false));
        Assert.Equal(40000, flow.AmountDue);
        Assert.Equal("40.000đ", flow.Amount);
        flow.Back(); flow.SelectPackage(1); flow.Next();
        Assert.Equal(70000, flow.AmountDue);
        flow.ApplyVoucher(new AppliedVoucher("654321", 500000, false));
        Assert.Equal(0, flow.AmountDue);
        Assert.True(flow.IsFree);
    }

    [Fact]
    public void Free_voucher_skips_cash_and_qr_and_restart_clears_it()
    {
        var flow = AtPayment();
        flow.StartFree();
        Assert.Equal(CustomerScreen.Payment, flow.Screen);
        flow.ApplyVoucher(new AppliedVoucher("111111", 0, true));
        flow.StartFree();
        Assert.Equal(CustomerScreen.Instructions, flow.Screen);
        flow.Back();
        Assert.Equal(CustomerScreen.Payment, flow.Screen);
        flow.Restart();
        Assert.Null(flow.Voucher);
        Assert.Equal(90000, AtPayment().AmountDue);
    }

    [Fact]
    public void Capture_settings_saved_before_retakes_still_load_with_the_default()
    {
        var value = JsonSerializer.Deserialize<CaptureConfiguration>("""{"CountdownSeconds":3,"SessionMinutes":7,"ReviewSeconds":5,"Shots":8}""")!;
        value.Validate();
        Assert.Equal(2, value.RetakeLimit);
        Assert.Throws<ArgumentException>(() => (value with { RetakeLimit = 6 }).Validate());
    }
}
