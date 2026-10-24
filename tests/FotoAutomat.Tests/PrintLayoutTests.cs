using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Tests;

public sealed class PrintLayoutTests
{
    [Fact]
    public void Layer_visibility_survives_serialization()
    {
        var layout = new PrintLayout("Layers", false, [new(1, 0, 0, 100, 100, IsVisible: false)]);
        var restored = System.Text.Json.JsonSerializer.Deserialize<PrintLayout>(System.Text.Json.JsonSerializer.Serialize(layout))!;
        Assert.False(restored.Slots[0].IsVisible);
        Assert.True(new PrintLayoutSlot(1, 0, 0, 100, 100).IsVisible);
    }

    [Fact]
    public void Print_date_uses_explicit_job_time_without_mutating_template()
    {
        var template = new PrintLayout("Date", false, [new(1, 0, 0, 100, 100), new(1, 0, 110, 160, 30, Kind: LayoutElementKind.SessionDate)]);
        var result = template.ResolvePrintData(new DateTimeOffset(2026, 10, 2, 0, 5, 0, TimeSpan.FromHours(7)));
        Assert.Equal("02/10/2026", result.Slots[1].Content);
        Assert.Equal(LayoutElementKind.Text, result.Slots[1].Kind);
        Assert.Equal(LayoutElementKind.SessionDate, template.Slots[1].Kind);
        Assert.Empty(template.Slots[1].Content);
    }

    [Fact]
    public void Landscape_validates_against_landscape_bounds()
    {
        var layout = new PrintLayout("Landscape", false, [new(1, 450, 20, 120, 100)], Landscape: true);
        layout.Validate();
        Assert.Equal(600, layout.PageWidth);
        Assert.Equal(400, layout.PageHeight);
        Assert.Throws<ArgumentException>(() => (layout with { Slots = [new(1, 20, 350, 120, 100)] }).Validate());
        Assert.Equal(200, (layout with { SingleStrip = true }).PageHeight);
    }
    [Fact]
    public void Resolution_roundtrips_without_changing_geometry()
    {
        var layout = new PrintLayout("Dpi", false, [new(1, 20, 20, 160, 120)], Dpi: 600);
        var restored = System.Text.Json.JsonSerializer.Deserialize<PrintLayout>(System.Text.Json.JsonSerializer.Serialize(layout))!;
        restored.Validate();
        Assert.Equal(600, restored.Dpi);
        Assert.Equal(layout.Slots, restored.Slots);
        Assert.Throws<ArgumentException>(() => (layout with { Dpi = 450 }).Validate());
    }

    [Fact]
    public void Single_strip_is_not_duplicated_until_printer_planning()
    {
        var layout = new PrintLayout("Dải", false, [new(1, 10, 40, 180, 130)], SingleStrip: true);
        layout.Validate();
        Assert.Equal(200, layout.PageWidth);
        Assert.Single(layout.Slots);
        Assert.Equal(new double[] { 0, 200 }, new PrinterConfiguration().PlacementOffsets(layout));
        Assert.Equal(new double[] { 0 }, new PrinterConfiguration(false).PlacementOffsets(layout));
        Assert.Single(layout.Slots);
        Assert.Throws<ArgumentException>(() => new PrinterConfiguration().PlacementOffsets(layout with { SingleStrip = false }));
        Assert.Throws<ArgumentException>(() => (layout with { Slots = [new(1, 190, 0, 30, 40)] }).Validate());
    }

    [Fact]
    public void Decorations_do_not_require_customer_photo_assignment()
    {
        var layout = new PrintLayout("Chữ", false,
            [new(1, 10, 40, 180, 130), new(1, 10, 200, 180, 30, Kind: LayoutElementKind.Text, Content: "Xin chào")], SingleStrip: true, BackgroundColor: "#FAF0EE");
        var roundtrip = System.Text.Json.JsonSerializer.Deserialize<PrintLayout>(System.Text.Json.JsonSerializer.Serialize(layout))!;
        roundtrip.Validate();
        Assert.True(roundtrip.SingleStrip);
        Assert.Equal(layout.Slots, roundtrip.Slots);
        Assert.Equal(layout.BackgroundColor, roundtrip.BackgroundColor);
        var flow = new CustomerPreviewFlow(new(3, 1, 2), roundtrip);
        flow.Next(); flow.Next(); flow.Next(); flow.ChoosePayment(true); flow.Next(); flow.Next();
        for (int i = 0; i < 60; i++) flow.Tick();
        flow.Next(); flow.SelectPhoto(1); flow.Next();
        Assert.Equal(200, flow.FrameWidth);
        flow.AssignSlot(1);
        Assert.False(flow.CanPrint);
        flow.AssignSlot(0);
        Assert.True(flow.CanPrint);
        Assert.Null(flow.Slots[1].Photo);
    }
    [Fact]
    public void Layout_without_png_preserves_appearance_and_layer_order()
    {
        var slot = new PrintLayoutSlot(1, 80, 100, 120, 160, Rotation: 12, StrokeWidth: 2,
            StrokeColor: "#123456", Shadow: true, ShadowColor: "#345678", ShadowAngle: 140,
            ShadowDistance: 10, ShadowBlur: 12, KeepAspect: true, Locked: true);
        var layout = new PrintLayout("Preset", false, [slot, slot with { PhotoNumber = 2 }]);
        string path = Path.Combine(Path.GetTempPath(), $"layout-{Guid.NewGuid()}.fotolayout");
        try
        {
            var store = new PrintLayoutStore(); store.Save(path, layout);
            var loaded = store.Load(path);
            Assert.Null(loaded.ArtworkPng);
            Assert.Equal(layout.Slots, loaded.Slots);
            var flow = new CustomerPreviewFlow(new(3, 1, 2), loaded);
            flow.Next(); flow.Next(); flow.Next(); flow.ChoosePayment(true); flow.Next(); flow.Next();
            for (int i = 0; i < 60; i++) flow.Tick();
            flow.Next(); flow.SelectPhoto(1); flow.Next();
            Assert.Equal(slot, flow.Slots[0].Appearance);
            flow.AssignSlot(0);
            Assert.Equal(slot, flow.Slots[0].Appearance);
        }
        finally { File.Delete(path); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void Invalid_effects_and_rotated_cut_crossing_are_rejected()
    {
        var layout = Pair();
        Assert.Throws<ArgumentException>(() => (layout with { Slots = [layout.Slots[0] with { StrokeColor = "red" }] }).Validate());
        Assert.Throws<ArgumentException>(() => (layout with { Slots = [layout.Slots[0] with { ShadowBlur = double.NaN }] }).Validate());
        Assert.Throws<ArgumentException>(() => (layout with { Slots = [new(1, 20, 100, 160, 200, Rotation: 45)] }).Validate());
        (layout with { Slots = [new(1, 50, 100, 100, 100, Rotation: 15)] }).Validate();
    }

    [Fact]
    public void Older_layout_files_receive_safe_style_defaults()
    {
        const string json = """{"Name":"Old","CutIntoStrips":false,"Slots":[{"PhotoNumber":1,"X":20,"Y":20,"Width":160,"Height":200}]}""";
        var layout = System.Text.Json.JsonSerializer.Deserialize<PrintLayout>(json)!;
        layout.Validate();
        Assert.Equal("#222222", layout.Slots[0].StrokeColor);
        Assert.False(layout.Slots[0].Shadow);
    }
    private static PrintLayout Pair() => new("Dải đôi", true,
        [new(1, 20, 20, 160, 200), new(1, 220, 20, 160, 200)]);

    [Fact]
    public void Saved_layout_preserves_paper_mode_geometry_and_artwork()
    {
        string path = Path.Combine(Path.GetTempPath(), $"layout-{Guid.NewGuid()}.fotolayout");
        try
        {
            var store = new PrintLayoutStore();
            var value = Pair() with { ArtworkPng = "test embedded payload" };
            store.Save(path, value);
            var loaded = store.Load(path);
            Assert.Equal(value.Name, loaded.Name);
            Assert.True(loaded.CutIntoStrips);
            Assert.Equal(value.Slots, loaded.Slots);
            Assert.Equal(value.ArtworkPng, loaded.ArtworkPng);
            Assert.Throws<ArgumentException>(() => store.Save(path, value with { Slots = [] }));
            Assert.Equal(value.Slots, store.Load(path).Slots);
        }
        finally { File.Delete(path); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void Strips_reject_crossing_the_cut_but_full_sheet_allows_it()
    {
        var crossing = Pair() with { Slots = [new(1, 150, 20, 100, 200)] };
        Assert.Throws<ArgumentException>(() => crossing.Validate());
        (crossing with { CutIntoStrips = false }).Validate();
        Assert.Throws<ArgumentException>(() => (Pair() with { Slots = [new(1, double.NaN, 0, 50, 50)] }).Validate());
        Assert.Throws<ArgumentException>(() => (Pair() with { Slots = [new(1, 390, 0, 50, 50)] }).Validate());
    }

    [Fact]
    public void Customer_assignment_fills_matching_photo_numbers_on_both_strips()
    {
        var flow = new CustomerPreviewFlow(new(3, 1, 2), Pair());
        flow.Next(); flow.Next(); flow.Next(); flow.ChoosePayment(true); flow.Next(); flow.Next();
        for (int i = 0; i < 60; i++) flow.Tick();
        flow.Next(); flow.SelectPhoto(2); flow.Next();
        Assert.False(flow.UsesSampleFrames);
        Assert.Equal(220, flow.Slots[1].X);
        flow.AssignSlot(0);
        Assert.All(flow.Slots, slot => Assert.Equal(2, slot.Photo));
        Assert.True(flow.CanPrint);
    }
}
