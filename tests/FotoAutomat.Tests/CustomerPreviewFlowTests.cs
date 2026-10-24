using FotoAutomat.Core;

namespace FotoAutomat.Tests;

public sealed class CustomerPreviewFlowTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Shot_session_selects_photos_and_beauty_before_frame_and_printing(bool cash)
    {
        var flow = AtCapture(cash);
        flow.Next(); flow.Back();
        Assert.Equal(CustomerScreen.Capture, flow.Screen);
        for (int i = 0; i < 420; i++) flow.Tick();
        Assert.Equal(CustomerScreen.Selection, flow.Screen);
        Assert.Equal(6, flow.CapturedCount);
        flow.Next();
        Assert.Equal(CustomerScreen.Beauty, flow.Screen);
        flow.Next();
        flow.Next();
        Assert.Equal(CustomerScreen.Frame, flow.Screen);
        flow.SelectPhoto(6);
        flow.Next();
        Assert.Equal(CustomerScreen.Frame, flow.Screen);
        flow.SelectLayout(1);
        Assert.Equal(4, flow.Slots.Length);
        for (int i = 0; i < 3; i++) flow.AssignSlot(i);
        flow.Next();
        Assert.Equal(CustomerScreen.Frame, flow.Screen);
        flow.AssignSlot(3); flow.Next();
        Assert.Equal(CustomerScreen.Printing, flow.Screen);
        for (int i = 0; i < 5; i++) flow.Tick();
        flow.Next();
        Assert.Equal(CustomerScreen.Delivery, flow.Screen);
    }

    [Fact]
    public void All_six_photos_are_available_for_four_slots_and_restart_clears_session()
    {
        var flow = AtCapture();
        for (int i = 0; i < 420; i++) flow.Tick();
        Assert.Equal(6, flow.FavoritePhotos.Length);
        flow.Next(); flow.Next();
        flow.SelectLayout(1);
        int[] chosen = [6, 2, 5, 1];
        for (int i = 0; i < chosen.Length; i++) { flow.SelectPhoto(chosen[i]); flow.AssignSlot(i); }
        Assert.Equal(chosen, flow.Slots.Select(s => s.Photo!.Value));
        Assert.True(flow.CanPrint);
        flow.Back();
        Assert.Equal(CustomerScreen.Beauty, flow.Screen);
        flow.Next();
        Assert.Equal(chosen, flow.Slots.Select(s => s.Photo!.Value));
        flow.Restart();
        Assert.Equal(0, flow.CapturedCount);
        Assert.Empty(flow.FavoritePhotos);
        Assert.False(flow.ShowSessionClock);
    }

    [Fact]
    public void Selection_gates_progress_and_beauty_survives_back_until_restart()
    {
        var flow = AtCapture();
        for (int i = 0; i < 420; i++) flow.Tick();
        for (int i = 1; i <= 6; i++) flow.SelectPhoto(i);
        flow.Next();
        Assert.Equal(CustomerScreen.Selection, flow.Screen);
        flow.SelectPhoto(4); flow.Next();
        flow.BeautyLevel = 130;
        Assert.Equal(100, flow.BeautyLevel);
        flow.Back(); flow.Next();
        Assert.Equal(100, flow.BeautyLevel);
        Assert.Equal(4, flow.SelectedPhoto);
        flow.Next();
        Assert.Equal(CustomerScreen.Frame, flow.Screen);
        flow.Restart();
        Assert.Equal(0, flow.BeautyLevel);
    }

    [Fact]
    public void Prepayment_choices_survive_back_but_reset_on_new_session()
    {
        var flow = new CustomerPreviewFlow();
        flow.Next(); flow.SelectFilter(2); flow.Next(); flow.SelectPackage(3);
        flow.Next(); flow.Back();
        Assert.Equal(3, flow.PackageIndex);
        Assert.Equal(2, flow.FilterIndex);
        Assert.Equal("280.000đ", flow.Amount);
        flow.Restart();
        Assert.Equal(0, flow.PackageIndex);
        Assert.Equal(0, flow.FilterIndex);
        flow.Tick(); flow.SelectPhoto(1); flow.AssignSlot(0); flow.ChoosePayment(true);
        Assert.Equal(CustomerScreen.Welcome, flow.Screen);
        Assert.Empty(flow.Slots);
    }

    internal static CustomerPreviewFlow AtCapture(bool cash = true, int countdown = 3)
    {
        var flow = new CustomerPreviewFlow(new(countdown));
        flow.Next(); flow.Next(); flow.Next();
        flow.ChoosePayment(cash); flow.Next(); flow.Next();
        return flow;
    }
}
