namespace FotoAutomat.Core;

public interface ICamera
{
    Task<string> CaptureAsync(Guid sessionId, CancellationToken cancellationToken);
}

public interface IPrinter
{
    Task<PrintOutcome> PrintAsync(PrintRequest request, CancellationToken cancellationToken);
}

public interface IBoothStore
{
    BoothSession Create();
    BoothSession Get(Guid id);
    BoothSession? GetLatest();
    BoothSession ConfirmDemoPayment(Guid sessionId, string receiptId);
    BoothSession Transition(BoothSession expected, SessionStage next, string? imagePath = null);
    int RecoverInterruptedSessions();
}
