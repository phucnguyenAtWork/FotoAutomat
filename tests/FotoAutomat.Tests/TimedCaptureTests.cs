using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Tests;

public sealed class TimedCaptureTests
{
    [Fact]
    public void Configured_duration_and_review_drive_capture_and_customer_copy()
    {
        var settings = new CaptureConfiguration(3, 1, 2);
        var capture = new TimedCapture(settings);
        Assert.False(capture.ShowSessionClock);
        capture.Advance(TimeSpan.FromSeconds(3));
        Assert.True(capture.Reviewing);
        capture.Advance(TimeSpan.FromSeconds(2));
        Assert.False(capture.Reviewing);
        for (int i = 5; i < 60; i++) capture.Advance(TimeSpan.FromSeconds(1));
        Assert.True(capture.Finished);
        Assert.Equal(6, capture.CapturedCount);
        var flow = new CustomerPreviewFlow(settings);
        Assert.Contains("6 ảnh", flow.OrderSummary);
        Assert.Contains("2 giây", flow.CaptureInstructions);
    }

    [Fact]
    public void Old_config_defaults_and_all_new_values_round_trip()
    {
        string path = Path.Combine(Path.GetTempPath(), $"capture-{Guid.NewGuid()}.json");
        try
        {
            File.WriteAllText(path, "{\"CountdownSeconds\":5}");
            var store = new CaptureConfigurationStore(path);
            Assert.Equivalent(new CaptureConfiguration(5, 7, 7), store.Load());
            store.Save(new(10, 15, 4));
            Assert.Equivalent(new CaptureConfiguration(10, 15, 4), store.Load());
            Assert.Throws<ArgumentException>(() => store.Save(new(3, 0, 7)));
            Assert.Throws<ArgumentException>(() => store.Save(new(3, 7, 0)));
            Assert.Throws<ArgumentException>(() => store.Save(new(3, 61, 7)));
            Assert.Throws<ArgumentException>(() => store.Save(new(3, 7, 61)));
            Assert.Equivalent(new CaptureConfiguration(10, 15, 4), store.Load());
        }
        finally { File.Delete(path); File.Delete(path + ".tmp"); }
    }
    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void Countdown_and_seven_second_review_repeat(int countdown)
    {
        var capture = new TimedCapture(new(countdown));
        capture.Advance(TimeSpan.FromSeconds(countdown - 1));
        Assert.Equal(0, capture.CapturedCount);
        capture.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(1, capture.CapturedCount);
        Assert.True(capture.Reviewing);
        capture.Advance(TimeSpan.FromSeconds(6));
        Assert.True(capture.Reviewing);
        capture.Advance(TimeSpan.FromSeconds(1));
        Assert.False(capture.Reviewing);
        Assert.Equal(countdown, capture.PhaseSeconds);
    }




    [Fact]
    public void Delayed_ui_updates_do_not_invent_photos_or_end_before_shot_limit()
    {
        var capture = new TimedCapture(new(3));
        capture.Advance(TimeSpan.FromSeconds(3));
        capture.Advance(TimeSpan.FromMinutes(8));
        Assert.False(capture.Finished);
        Assert.Equal(1, capture.CapturedCount);
        Assert.Equal(0, capture.RemainingSeconds);
    }

    [Fact]
    public void Invalid_settings_are_rejected_and_saved_settings_survive_reopen()
    {
        string path = Path.Combine(Path.GetTempPath(), $"capture-{Guid.NewGuid()}.json");
        try
        {
            var store = new CaptureConfigurationStore(path);
            store.Save(new(10));
            Assert.Equal(10, new CaptureConfigurationStore(path).Load().CountdownSeconds);
            store.Save(new(4));
            Assert.Equal(4, new CaptureConfigurationStore(path).Load().CountdownSeconds);
            Assert.Throws<ArgumentException>(() => store.Save(new(30)));
            Assert.Equal(4, store.Load().CountdownSeconds);
        }
        finally { File.Delete(path); File.Delete(path + ".tmp"); }
    }
}
