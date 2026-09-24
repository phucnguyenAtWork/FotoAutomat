namespace FotoAutomat.Core;

/// <summary>Simulator foundation. Production payment and fulfillment policies are not implemented.</summary>
public sealed class BoothWorkflow(IBoothStore store, ICamera camera, IPrinter printer)
{
    public BoothSession Start() => store.Create();

    public BoothSession ConfirmDemoPayment(Guid sessionId, string receiptId) =>
        store.ConfirmDemoPayment(sessionId, receiptId);

    public async Task<BoothSession> CaptureAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = RequireStage(sessionId, SessionStage.Paid);
        var capturing = store.Transition(session, SessionStage.Capturing);
        try
        {
            var path = await camera.CaptureAsync(sessionId, cancellationToken);
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            return store.Transition(capturing, SessionStage.Captured, path);
        }
        catch (DeviceUnavailableException)
        {
            // Adapter guarantees no capture started for this specific error.
            store.Transition(capturing, SessionStage.Paid);
            throw;
        }
        catch
        {
            store.Transition(capturing, SessionStage.NeedsReview);
            throw;
        }
    }

    public async Task<BoothSession> PrintAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = RequireStage(sessionId, SessionStage.Captured);
        if (string.IsNullOrWhiteSpace(session.ImagePath))
            throw new InvalidOperationException("No captured image is recorded.");

        // Record intent before calling the device. A crash now leaves a reviewable job.
        var printing = store.Transition(session, SessionStage.Printing);
        try
        {
            var outcome = await printer.PrintAsync(new PrintRequest(session.Id, session.ImagePath), cancellationToken);
            return store.Transition(printing,
                outcome == PrintOutcome.Completed ? SessionStage.Completed : SessionStage.NeedsReview);
        }
        catch
        {
            // A timeout or cancellation cannot prove that a physical print did not happen.
            store.Transition(printing, SessionStage.NeedsReview);
            throw;
        }
    }

    private BoothSession RequireStage(Guid id, SessionStage expected)
    {
        var session = store.Get(id);
        if (session.Stage != expected)
            throw new InvalidOperationException($"Expected {expected}, found {session.Stage}.");
        return session;
    }
}
