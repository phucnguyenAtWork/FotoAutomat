namespace FotoAutomat.Core;

public enum SessionStage
{
    AwaitingPayment,
    Paid,
    Capturing,
    Captured,
    Printing,
    Completed,
    NeedsReview
}

public sealed record BoothSession(Guid Id, SessionStage Stage, string? ImagePath, int Revision)
{
    public BoothSession MoveTo(SessionStage next, string? imagePath = null)
    {
        var valid = (Stage, next) switch
        {
            (SessionStage.Paid, SessionStage.Capturing) => true,
            (SessionStage.Capturing, SessionStage.Paid or SessionStage.Captured or SessionStage.NeedsReview) => true,
            (SessionStage.Captured, SessionStage.Printing) => true,
            (SessionStage.Printing, SessionStage.Completed or SessionStage.NeedsReview) => true,
            _ => false
        };
        if (!valid) throw new InvalidOperationException($"Invalid transition: {Stage} to {next}.");
        var path = imagePath ?? ImagePath;
        if (next == SessionStage.Captured && string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("A captured session needs an image path.");
        return this with { Stage = next, ImagePath = path, Revision = Revision + 1 };
    }
}

public sealed record PrintRequest(Guid JobId, string ImagePath);

public enum PrintOutcome { Completed, Unknown }

public sealed class DeviceUnavailableException(string message) : Exception(message);
