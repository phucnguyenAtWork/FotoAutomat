namespace FotoAutomat.Core;

public sealed record CaptureConfiguration(int CountdownSeconds = 3, int SessionMinutes = 7, int ReviewSeconds = 7)
{
    public int Shots { get; init; } = 6;
    /// <summary>Số lần khách được chụp lại ảnh vừa chụp trong cả phiên.</summary>
    public int RetakeLimit { get; init; } = 2;
    public List<CaptureModeOptions> Modes { get; init; } = CaptureModeOptions.Defaults();
    public int SessionSeconds => SessionMinutes * 60;
    public const int WarningSeconds = 300;
    public void Validate()
    {
        if (Shots is < 4 or > 10) throw new ArgumentException("Số ảnh chụp phải từ 4 đến 10.");
        if (RetakeLimit is < 0 or > 5) throw new ArgumentException("Số lần chụp lại phải từ 0 đến 5.");
        if (Modes is null || Modes.Count != CaptureModeOptions.Ids.Length || Modes.Any(m => m is null) || Modes.Select(m => m.Id).Distinct().Count() != Modes.Count || !Modes.Any(m => m.Enabled))
            throw new ArgumentException("Cần đủ các chế độ và bật ít nhất một chế độ.");
        foreach (var mode in Modes) mode.Validate();
        if (CountdownSeconds is < 1 or > 29)
            throw new ArgumentException("Thời gian đếm ngược phải từ 1 đến 29 giây.");
        if (SessionMinutes is < 1 or > 60)
            throw new ArgumentException("Thời gian phiên phải từ 1 đến 60 phút.");
        if (ReviewSeconds is < 1 or > 60)
            throw new ArgumentException("Thời gian xem ảnh phải từ 1 đến 60 giây.");
    }
}

/// <summary>Elapsed-time policy. A late update never invents a burst of missed captures.</summary>
public sealed class TimedCapture
{
    private readonly CaptureConfiguration configuration;
    private TimeSpan elapsed;
    private TimeSpan phaseEnd;
    private readonly bool requireAcknowledgement;
    public bool AwaitingCapture { get; private set; }
    public string? CaptureError { get; private set; }
    public TimedCapture(CaptureConfiguration configuration, bool requireAcknowledgement = false)
    {
        configuration.Validate();
        this.configuration = configuration;
        this.requireAcknowledgement = requireAcknowledgement;
        phaseEnd = TimeSpan.FromSeconds(configuration.CountdownSeconds);
        RetakesLeft = configuration.RetakeLimit;
    }
    public int CapturedCount { get; private set; }
    public int RetakesLeft { get; private set; }
    public bool CanRetake => Reviewing && !AwaitingCapture && !Finished && CaptureError is null && RetakesLeft > 0;
    /// <summary>Bỏ ảnh đang xem lại và đếm ngược chụp lại ảnh đó. Ảnh gốc trên đĩa không bị xóa.</summary>
    public void Retake()
    {
        if (!CanRetake) throw new InvalidOperationException("Không thể chụp lại lúc này.");
        RetakesLeft--; CapturedCount--; Reviewing = false;
        phaseEnd = elapsed + TimeSpan.FromSeconds(configuration.CountdownSeconds);
    }
    public bool Reviewing { get; private set; }
    public bool Finished { get; private set; }
    public int RemainingSeconds => Math.Max(0, (int)Math.Ceiling(configuration.SessionSeconds - elapsed.TotalSeconds));
    public bool ShowSessionClock => false;
    public int PhaseSeconds => Math.Max(0, (int)Math.Ceiling((phaseEnd - elapsed).TotalSeconds));
    public void Advance(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delta));
        if (Finished || CaptureError is not null) return;
        elapsed += delta;
        if (AwaitingCapture || elapsed < phaseEnd) return;
        if (Reviewing)
        {
            if (CapturedCount >= configuration.Shots) { Finished = true; return; }
            Reviewing = false;
            phaseEnd = elapsed + TimeSpan.FromSeconds(configuration.CountdownSeconds);
        }
        else
        {
            AwaitingCapture = true;
            if (!requireAcknowledgement) ConfirmCapture();
        }
    }
    public void ConfirmCapture()
    {
        if (!AwaitingCapture || CaptureError is not null || Finished) throw new InvalidOperationException("No capture is awaiting completion.");
        AwaitingCapture = false; CapturedCount++; Reviewing = true;
        phaseEnd = elapsed + TimeSpan.FromSeconds(configuration.ReviewSeconds);
    }
    public void FailCapture(string error)
    {
        if (!AwaitingCapture) throw new InvalidOperationException("No capture is awaiting completion.");
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        CaptureError = error;
    }

}
