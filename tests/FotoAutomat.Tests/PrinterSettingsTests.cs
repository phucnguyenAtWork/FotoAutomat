using System.Text.Json;
using FotoAutomat.Core;

namespace FotoAutomat.Tests;

public class PrinterSettingsTests
{
    [Theory]
    [InlineData(300, false, 1200, 1800)]
    [InlineData(600, false, 2400, 3600)]
    [InlineData(300, true, 1800, 1200)]
    [InlineData(600, true, 3600, 2400)]
    public void TwoStripsFillOneSheetWithoutChangingTemplate(int dpi, bool landscape, int width, int height)
    {
        var layout = new PrintLayout("Strip", false, [new(1, 20, 20, 100, 100)], SingleStrip: true, Dpi: dpi, Landscape: landscape);
        var plan = new PrinterConfiguration(true, 2).CreateSheetPlan(layout);
        Assert.Equal(width, plan.Width); Assert.Equal(height, plan.Height);
        Assert.Equal(2, plan.Copies); Assert.Equal(2, plan.Placements.Length);
        Assert.Equal(width * height, plan.Placements.Sum(p => p.Width * p.Height));
        Assert.Equal(landscape ? 0 : width / 2, plan.Placements[1].X);
        Assert.Equal(landscape ? height / 2 : 0, plan.Placements[1].Y);
        Assert.True(layout.SingleStrip); Assert.Single(layout.Slots);
        Assert.Single(new PrinterConfiguration(false).CreateSheetPlan(layout).Placements);
        Assert.Single(new PrinterConfiguration(true).CreateSheetPlan(layout with { SingleStrip = false }).Placements);
    }
    [Fact]
    public void LegacyConfigurationKeepsDefaults()
    {
        var settings = JsonSerializer.Deserialize<PrinterConfiguration>("{\"PairAndCut\":false,\"Copies\":2}")!;
        settings.Validate();
        Assert.Equal(100, settings.Main.Scale);
        Assert.False(settings.PairAndCut);
        Assert.Equal(2, settings.Copies);
    }
    [Fact]
    public void ProfilesAndOptionsSurviveRoundTrip()
    {
        var settings = new PrinterConfiguration(false, 3) { Main = new() { QueueName = "A", Scale = 98, OffsetX = 2 }, Secondary = new() { QueueName = "B", OffsetY = -3 }, AlternatePrinters = true, LimitsEnabled = true, SessionLimit = 4 };
        var copy = JsonSerializer.Deserialize<PrinterConfiguration>(JsonSerializer.Serialize(settings))!;
        copy.Validate();
        Assert.Equal(98, copy.Main.Scale);
        Assert.Equal(-3, copy.Secondary.OffsetY);
        Assert.Equal(4, copy.SessionLimit);
        Assert.True(copy.AlternatePrinters);
    }
    [Fact]
    public void RejectsInvalidAlignmentAndDuplicatePrinters()
    {
        Assert.Throws<ArgumentException>(() => new PrinterProfile { Scale = double.NaN }.Validate());
        Assert.Throws<ArgumentException>(() => new PrinterProfile { OffsetX = 21 }.Validate());
        Assert.Throws<ArgumentException>(() => new PrinterConfiguration { AlternatePrinters = true, Main = new() { QueueName = "A" }, Secondary = new() { QueueName = "A" } }.Validate());
    }
}
