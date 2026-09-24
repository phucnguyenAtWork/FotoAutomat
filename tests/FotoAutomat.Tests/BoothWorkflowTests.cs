using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Tests;

public sealed class BoothWorkflowTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "FotoAutomat.Tests", Guid.NewGuid().ToString("N"));
    private string Database => Path.Combine(directory, "booth.db");
    private SqliteBoothStore Store() => new(Database);
    private SimulatedCamera Camera(bool fail = false) => new(Path.Combine(directory, "captures"), fail);

    [Fact]
    public async Task Offline_session_survives_database_reopen()
    {
        var store = Store();
        var printer = new SimulatedPrinter();
        var workflow = new BoothWorkflow(store, Camera(), printer);
        var session = workflow.Start();
        workflow.ConfirmDemoPayment(session.Id, "receipt-1");
        session = await workflow.CaptureAsync(session.Id);
        Assert.True(File.Exists(session.ImagePath));
        session = await workflow.PrintAsync(session.Id);
        Assert.Equal(SessionStage.Completed, Store().Get(session.Id).Stage);
        Assert.Equal(1, printer.SubmissionCount);
    }

    [Fact]
    public void Receipt_replay_is_idempotent_after_reopen()
    {
        var store = Store();
        var session = store.Create();
        var paid = store.ConfirmDemoPayment(session.Id, "receipt-1");
        var replay = Store().ConfirmDemoPayment(session.Id, "receipt-1");
        Assert.Equal(paid, replay);
    }

    [Fact]
    public void Receipt_cannot_pay_another_session()
    {
        var store = Store();
        var first = store.Create();
        var second = store.Create();
        store.ConfirmDemoPayment(first.Id, "receipt-1");
        Assert.Throws<InvalidOperationException>(() => store.ConfirmDemoPayment(second.Id, "receipt-1"));
        Assert.Equal(SessionStage.AwaitingPayment, store.Get(second.Id).Stage);
    }

    [Fact]
    public void Failed_payment_does_not_consume_a_new_receipt()
    {
        var store = Store();
        var first = store.Create();
        store.ConfirmDemoPayment(first.Id, "receipt-1");
        Assert.Throws<InvalidOperationException>(() => store.ConfirmDemoPayment(first.Id, "receipt-2"));
        var second = store.Create();
        Assert.Equal(SessionStage.Paid, store.ConfirmDemoPayment(second.Id, "receipt-2").Stage);
    }

    [Fact]
    public async Task Capture_before_payment_is_rejected()
    {
        var store = Store();
        var session = store.Create();
        var workflow = new BoothWorkflow(store, Camera(), new SimulatedPrinter());
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.CaptureAsync(session.Id));
        Assert.False(Directory.Exists(Path.Combine(directory, "captures")));
    }

    [Fact]
    public async Task Known_camera_disconnect_preserves_paid_session_for_retry()
    {
        var store = Store();
        var session = store.ConfirmDemoPayment(store.Create().Id, "receipt-1");
        var workflow = new BoothWorkflow(store, Camera(fail: true), new SimulatedPrinter());
        await Assert.ThrowsAsync<DeviceUnavailableException>(() => workflow.CaptureAsync(session.Id));
        Assert.Equal(SessionStage.Paid, store.Get(session.Id).Stage);
        var retry = new BoothWorkflow(store, Camera(), new SimulatedPrinter());
        Assert.Equal(SessionStage.Captured, (await retry.CaptureAsync(session.Id)).Stage);
    }

    [Fact]
    public async Task Unknown_print_outcome_requires_review_and_cannot_be_retried()
    {
        var store = Store();
        var printer = new SimulatedPrinter(PrintOutcome.Unknown);
        var workflow = new BoothWorkflow(store, Camera(), printer);
        var session = store.ConfirmDemoPayment(store.Create().Id, "receipt-1");
        await workflow.CaptureAsync(session.Id);
        Assert.Equal(SessionStage.NeedsReview, (await workflow.PrintAsync(session.Id)).Stage);
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.PrintAsync(session.Id));
        Assert.Equal(1, printer.SubmissionCount);
    }

    [Fact]
    public async Task Completed_print_cannot_be_submitted_again()
    {
        var store = Store();
        var printer = new SimulatedPrinter();
        var workflow = new BoothWorkflow(store, Camera(), printer);
        var session = store.ConfirmDemoPayment(store.Create().Id, "receipt-1");
        await workflow.CaptureAsync(session.Id);
        await workflow.PrintAsync(session.Id);
        var reopened = new BoothWorkflow(Store(), Camera(), printer);
        await Assert.ThrowsAsync<InvalidOperationException>(() => reopened.PrintAsync(session.Id));
        Assert.Equal(1, printer.SubmissionCount);
    }

    [Theory]
    [InlineData(SessionStage.Capturing)]
    [InlineData(SessionStage.Printing)]
    public void Startup_holds_interrupted_operations_without_replay(SessionStage stage)
    {
        var store = Store();
        var session = store.ConfirmDemoPayment(store.Create().Id, "receipt-1");
        session = store.Transition(session, SessionStage.Capturing);
        if (stage == SessionStage.Printing)
        {
            session = store.Transition(session, SessionStage.Captured, "synthetic.bmp");
            store.Transition(session, SessionStage.Printing);
        }
        var reopened = Store();
        Assert.Equal(1, reopened.RecoverInterruptedSessions());
        Assert.Equal(SessionStage.NeedsReview, reopened.Get(session.Id).Stage);
        Assert.Equal(0, reopened.RecoverInterruptedSessions());
    }

    [Fact]
    public void Stale_session_cannot_override_a_newer_transition()
    {
        var store = Store();
        var session = store.ConfirmDemoPayment(store.Create().Id, "receipt-1");
        store.Transition(session, SessionStage.Capturing);
        Assert.Throws<InvalidOperationException>(() => Store().Transition(session, SessionStage.Capturing));
    }

    [Fact]
    public async Task Two_print_requests_only_call_the_device_once()
    {
        var store = Store();
        var printer = new BlockingPrinter();
        var workflow = new BoothWorkflow(store, Camera(), printer);
        var session = store.ConfirmDemoPayment(store.Create().Id, "receipt-1");
        await workflow.CaptureAsync(session.Id);
        var first = workflow.PrintAsync(session.Id);
        await printer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            var otherWorkflow = new BoothWorkflow(Store(), Camera(), printer);
            await Assert.ThrowsAsync<InvalidOperationException>(() => otherWorkflow.PrintAsync(session.Id));
        }
        finally
        {
            printer.Release.TrySetResult();
        }
        await first;
        Assert.Equal(1, printer.Count);
    }

    [Fact]
    public async Task Print_timeout_does_not_assume_no_physical_output()
    {
        var store = Store();
        var workflow = new BoothWorkflow(store, Camera(), new FailingPrinter());
        var session = store.ConfirmDemoPayment(store.Create().Id, "receipt-1");
        await workflow.CaptureAsync(session.Id);
        await Assert.ThrowsAsync<TimeoutException>(() => workflow.PrintAsync(session.Id));
        Assert.Equal(SessionStage.NeedsReview, Store().Get(session.Id).Stage);
    }

    [Fact]
    public async Task Cancellation_before_print_has_no_side_effect()
    {
        var store = Store();
        var printer = new SimulatedPrinter();
        var workflow = new BoothWorkflow(store, Camera(), printer);
        var session = store.ConfirmDemoPayment(store.Create().Id, "receipt-1");
        await workflow.CaptureAsync(session.Id);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => workflow.PrintAsync(session.Id, new CancellationToken(true)));
        Assert.Equal(SessionStage.Captured, store.Get(session.Id).Stage);
        Assert.Equal(0, printer.SubmissionCount);
    }

    public void Dispose()
    {
        // Only this test instance's generated directory, never a configured booth path.
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private sealed class FailingPrinter : IPrinter
    {
        public Task<PrintOutcome> PrintAsync(PrintRequest request, CancellationToken cancellationToken) =>
            throw new TimeoutException("Simulated printer timeout after submission.");
    }

    private sealed class BlockingPrinter : IPrinter
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Count { get; private set; }

        public async Task<PrintOutcome> PrintAsync(PrintRequest request, CancellationToken cancellationToken)
        {
            Count++;
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return PrintOutcome.Completed;
        }
    }
}
