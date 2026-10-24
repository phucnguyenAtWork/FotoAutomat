namespace FotoAutomat.Core;

public sealed record CaptureModeOptions
{
    public string Id { get; set; } = "photo";
    public bool Enabled { get; set; }
    public int Countdown { get; set; } = 3;
    public int FrameCount { get; set; } = 4;
    public int FrameDelayMs { get; set; } = 500;
    public int DurationSeconds { get; set; } = 10;
    public int IntervalSeconds { get; set; } = 2;
    public string Size { get; set; } = "1280 × 720";
    public int Rotation { get; set; }
    public int Quality { get; set; } = 85;
    public bool Reverse { get; set; }
    public bool Audio { get; set; } = true;
    public bool AlsoGif { get; set; }
    public bool Beep { get; set; } = true;
    public bool Autofocus { get; set; } = true;
    public string OverlayPath { get; set; } = "";
    public string SoundtrackPath { get; set; } = "";
    public List<SpeedSegment> Segments { get; set; } = [new()];
    public static string[] Ids { get; } = ["photo", "gif", "boomerang", "video", "slowmotion", "timelapse", "greeting", "idphoto"];
    public static List<CaptureModeOptions> Defaults() => Ids.Select(id => new CaptureModeOptions { Id = id, Enabled = id == "photo", Reverse = id == "boomerang" }).ToList();
    public double OutputSeconds => Id == "slowmotion" ? Segments.Sum(s => s.Duration / s.Speed) * (Reverse ? 2 : 1) : Id is "gif" or "boomerang" ? FrameCount * FrameDelayMs / 1000d * (Reverse ? 2 : 1) : DurationSeconds;
    public void Validate()
    {
        if (!Ids.Contains(Id) || Countdown is < 0 or > 30 || FrameCount is < 2 or > 60 || FrameDelayMs is < 50 or > 2000 || DurationSeconds is < 1 or > 120 || IntervalSeconds is < 1 or > 60 || Quality is < 1 or > 100 || Rotation is not (0 or 90 or 180 or 270) || Size is not ("1280 × 720" or "1920 × 1080" or "1080 × 1080" or "1080 × 1920"))
            throw new ArgumentException("Thông số chế độ chụp không hợp lệ.");
        if (Segments is null || Segments.Count is < 1 or > 12 || Segments.Any(s => s is null || !double.IsFinite(s.Duration) || s.Duration is < 1 or > 30 || s.Speed is not (0.25 or 0.5 or 1 or 2)) || Segments.Sum(s => s.Duration) > 120)
            throw new ArgumentException("Dòng thời gian cần 1–12 đoạn, mỗi đoạn 1–30 giây, tổng tối đa 120 giây.");
    }
}
public sealed record SpeedSegment
{
    public double Duration { get; set; } = 3;
    public double Speed { get; set; } = 1;
}
