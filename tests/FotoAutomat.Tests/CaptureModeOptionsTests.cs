using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;
public class CaptureModeOptionsTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(10)]
    public void StopsAfterConfiguredShotsAndFinalReviewRegardlessOfSessionMinutes(int shots)
    {
        var capture = new TimedCapture(new(29, 1, 2) { Shots = shots });
        for (int i=0;i<shots;i++)
        {
            capture.Advance(TimeSpan.FromSeconds(29));
            Assert.Equal(i+1,capture.CapturedCount);
            Assert.False(capture.Finished);
            capture.Advance(TimeSpan.FromSeconds(2));
        }
        Assert.True(capture.Finished);
        capture.Advance(TimeSpan.FromHours(1));
        Assert.Equal(shots,capture.CapturedCount);
    }
    [Theory]
    [InlineData(3)]
    [InlineData(11)]
    public void RejectsInvalidShotCount(int shots) => Assert.Throws<ArgumentException>(() => new CaptureConfiguration {Shots=shots}.Validate());
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(17)]
    [InlineData(29)]
    public void PhotoCountdownAcceptsEachSecondBelowThirty(int seconds)
    {
        var timer = new TimedCapture(new(seconds));
        timer.Advance(TimeSpan.FromSeconds(seconds - 1));
        Assert.Equal(0, timer.CapturedCount);
        timer.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(1, timer.CapturedCount);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public void RejectsCountdownOutsidePhotoRange(int seconds) => Assert.Throws<ArgumentException>(() => new CaptureConfiguration(seconds).Validate());
    [Fact]
    public void ModeSettingsPersistAndInvalidSaveKeepsPreviousFile()
    {
        string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".json");
        try
        {
            var store=new CaptureConfigurationStore(path); var configuration=new CaptureConfiguration();
            var slow=configuration.Modes.Single(m=>m.Id=="slowmotion");
            slow.Enabled=true; slow.Reverse=true; slow.Segments=[new(){Duration=2,Speed=0.5},new(){Duration=3,Speed=1}];
            slow.OverlayPath="overlay.png"; store.Save(configuration);
            Assert.Equivalent(configuration,store.Load()); Assert.Equal(14,slow.OutputSeconds);
            slow.Segments[0].Speed=0;
            Assert.Throws<ArgumentException>(()=>store.Save(configuration));
            Assert.Equal(0.5,store.Load().Modes.Single(m=>m.Id=="slowmotion").Segments[0].Speed);
        }
        finally {File.Delete(path);File.Delete(path+".tmp");}
    }
    [Fact]
    public void RequiresOneEnabledModeAndUniqueKnownModes()
    {
        var configuration=new CaptureConfiguration(); foreach(var mode in configuration.Modes)mode.Enabled=false;
        Assert.Throws<ArgumentException>(configuration.Validate);
        configuration.Modes[0].Enabled=true;configuration.Modes[1].Id="photo";
        Assert.Throws<ArgumentException>(configuration.Validate);
        Assert.Throws<ArgumentException>(()=>new CaptureModeOptions {Segments=[new(){Duration=double.NaN}]}.Validate());
    }
}
