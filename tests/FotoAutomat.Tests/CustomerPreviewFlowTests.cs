using FotoAutomat.Core;

namespace FotoAutomat.Tests;

public sealed class CustomerPreviewFlowTests
{
    [Theory]
    [InlineData(true, CustomerScreen.CashPayment)]
    [InlineData(false, CustomerScreen.QrPayment)]
    public void Both_payment_branches_complete_the_customer_journey(bool cash, CustomerScreen expected)
    {
        var flow = AtPayment();
        flow.ChoosePayment(cash);
        Assert.Equal(expected, flow.Screen);
        flow.Next();
        Assert.Equal(CustomerScreen.Instructions, flow.Screen);
        flow.Back();
        Assert.Equal(expected, flow.Screen);
        flow.Next();
        flow.Next();
        CompleteCapture(flow);
        flow.Next();
        for (var i = 0; i < 4; i++) { flow.SelectPhoto(i + 1); flow.AssignSlot(i); }
        flow.Next();
        Assert.Equal(CustomerScreen.Printing, flow.Screen);
        flow.Next();
        Assert.Equal(CustomerScreen.Printing, flow.Screen);
        for (var i = 0; i < 5; i++) flow.Tick();
        flow.Next();
        Assert.Equal(CustomerScreen.Delivery, flow.Screen);
    }

    [Fact]
    public void Capture_and_frame_gates_require_six_photos_and_four_filled_slots()
    {
        var flow = AtCapture();
        flow.Next();
        Assert.Equal(CustomerScreen.Capture, flow.Screen);
        for (var i = 0; i < 17; i++) flow.Tick();
        Assert.Equal(5, flow.CapturedCount);
        Assert.False(flow.CanSelectPhotos);
        flow.Tick();
        flow.Next();
        for (var i = 0; i < 3; i++) flow.AssignSlot(i);
        flow.Next();
        Assert.Equal(CustomerScreen.Selection, flow.Screen);
        flow.AssignSlot(3);
        Assert.True(flow.CanPrint);
    }

    [Fact]
    public void Retakes_clear_slots_and_stop_at_the_reference_limit()
    {
        var flow = AtCapture();
        for (var i = 0; i < 4; i++)
        {
            CompleteCapture(flow);
            flow.Next();
            flow.AssignSlot(0);
            flow.Retake();
            Assert.Equal(CustomerScreen.Capture, flow.Screen);
            Assert.All(flow.Slots, slot => Assert.Null(slot.Photo));
            Assert.Equal(0, flow.CapturedCount);
        }
        CompleteCapture(flow);
        flow.Next();
        flow.Retake();
        Assert.Equal(CustomerScreen.Selection, flow.Screen);
        Assert.False(flow.CanRetake);
        Assert.Equal(4, flow.RetakesUsed);
    }

    [Fact]
    public void Back_keeps_choices_and_new_session_clears_them()
    {
        var flow = new CustomerPreviewFlow();
        flow.Next();
        flow.SelectFilter(2);
        flow.Next();
        flow.SelectLayout(1);
        flow.SelectPackage(3);
        flow.Next();
        flow.Back();
        Assert.Equal(1, flow.LayoutIndex);
        Assert.Equal(3, flow.PackageIndex);
        Assert.Equal(2, flow.FilterIndex);
        Assert.Equal("280.000đ", flow.Amount);
        flow.Restart();
        Assert.Equal(CustomerScreen.Welcome, flow.Screen);
        Assert.Equal(0, flow.PackageIndex);
        Assert.Equal(0, flow.FilterIndex);
        Assert.Equal(0, flow.LayoutIndex);
        Assert.All(flow.Slots, slot => Assert.Null(slot.Photo));
    }

    [Fact]
    public void Inactive_ticks_and_out_of_stage_actions_do_not_advance_preview()
    {
        var flow = new CustomerPreviewFlow();
        flow.Tick();
        flow.AssignSlot(0);
        flow.ChoosePayment(true);
        flow.Retake();
        flow.SelectPhoto(6);
        Assert.Equal(CustomerScreen.Welcome, flow.Screen);
        Assert.Equal(0, flow.CapturedCount);
        Assert.All(flow.Slots, slot => Assert.Null(slot.Photo));
        Assert.Equal(1, flow.SelectedPhoto);
    }

    private static CustomerPreviewFlow AtPayment()
    {
        var flow = new CustomerPreviewFlow();
        flow.Next(); flow.Next(); flow.Next();
        return flow;
    }

    private static CustomerPreviewFlow AtCapture()
    {
        var flow = AtPayment();
        flow.ChoosePayment(true); flow.Next(); flow.Next();
        return flow;
    }

    private static void CompleteCapture(CustomerPreviewFlow flow)
    {
        for (var i = 0; i < 18; i++) flow.Tick();
    }
}
