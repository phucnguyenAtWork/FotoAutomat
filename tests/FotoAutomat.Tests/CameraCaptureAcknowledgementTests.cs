using FotoAutomat.Core;

namespace FotoAutomat.Tests;

public sealed class CameraCaptureAcknowledgementTests
{
    [Fact]
    public void Countdown_waits_for_camera_and_review_starts_after_receipt()
    {
        var capture = new TimedCapture(new(3, 7, 7), requireAcknowledgement: true);
        capture.Advance(TimeSpan.FromSeconds(3));
        Assert.True(capture.AwaitingCapture); Assert.Equal(0, capture.CapturedCount); Assert.False(capture.Reviewing);
        capture.Advance(TimeSpan.FromSeconds(30));
        Assert.Equal(0, capture.CapturedCount);
        capture.ConfirmCapture();
        Assert.Equal(1, capture.CapturedCount); Assert.True(capture.Reviewing); Assert.Equal(7, capture.PhaseSeconds);
        Assert.Throws<InvalidOperationException>(() => capture.ConfirmCapture());
        capture.Advance(TimeSpan.FromSeconds(7)); Assert.False(capture.Reviewing); Assert.Equal(3, capture.PhaseSeconds);
    }
    [Fact]
    public void Camera_failure_does_not_count_photo_or_automatically_retry()
    {
        var capture = new TimedCapture(new(), requireAcknowledgement: true);
        capture.Advance(TimeSpan.FromSeconds(3)); capture.FailCapture("Disconnected");
        capture.Advance(TimeSpan.FromHours(1));
        Assert.Equal(0, capture.CapturedCount); Assert.False(capture.Finished); Assert.Equal("Disconnected", capture.CaptureError);
        Assert.Throws<InvalidOperationException>(() => capture.ConfirmCapture());
    }
    [Fact]
    public void Customer_flow_keeps_each_received_original_and_rejects_duplicate_ack()
    {
        var flow = new CustomerPreviewFlow(requireCameraAcknowledgement: true);
        flow.Next(); flow.Next(); flow.Next(); flow.ChoosePayment(false); flow.Next(); flow.Next();
        flow.Tick(TimeSpan.FromSeconds(3)); Assert.True(flow.CaptureRequested);
        flow.CompleteCameraCapture("original-1.jpg"); Assert.Equal("original-1.jpg", Assert.Single(flow.CapturedPaths));
        Assert.False(flow.CaptureRequested); Assert.True(flow.IsReviewingCapture);
        Assert.Throws<InvalidOperationException>(() => flow.CompleteCameraCapture("original-1.jpg"));
        flow.Restart(); Assert.Empty(flow.CapturedPaths);
    }
}
