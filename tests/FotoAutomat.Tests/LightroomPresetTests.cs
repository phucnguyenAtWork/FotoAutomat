using FotoAutomat.Core;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Tests;

public sealed class LightroomPresetTests
{
    [Fact]
    public void Xmp_keeps_supported_numbers_and_reports_unsupported_tools()
    {
        var imported = LightroomPresetImporter.Xmp("""
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#" xmlns:crs="http://ns.adobe.com/camera-raw-settings/1.0/">
            <rdf:Description crs:Exposure2012="-0.02" crs:Contrast2012="-19" crs:Temperature="5150" crs:Tint="120" crs:Texture="-5" crs:Vibrance="23" crs:HueAdjustmentOrange="30" crs:ParametricDarks="-17" crs:ColorGradeBlending="64" crs:SplitToningBalance="-27" crs:RedHue="12" crs:Sharpness="85" crs:CameraProfile="Adobe Color">
            <crs:Name><rdf:Alt><rdf:li>Booth Warm</rdf:li></rdf:Alt></crs:Name>
            <crs:ToneCurvePV2012><rdf:Seq><rdf:li>0, 0</rdf:li><rdf:li>128, 150</rdf:li><rdf:li>255, 255</rdf:li></rdf:Seq></crs:ToneCurvePV2012>
            </rdf:Description></rdf:RDF>
            """, "fallback");
        var r = imported.Recipe;
        Assert.Equal("Booth Warm", r.Name); Assert.Equal(-.02, r.Exposure); Assert.Equal(5150, r.Temperature); Assert.Equal(120, r.Tint);
        Assert.Equal(-5, r.Structure); Assert.Equal(30, r.Hsl![1].Hue); Assert.Equal(23, r.Vibrance); Assert.Equal(-17, r.CurveDarks);
        Assert.Equal(64, r.GradingBlending); Assert.Equal(-27, r.GradingBalance); Assert.Equal(12, r.Calibration![0].Hue); Assert.Equal(0, r.Sharpening);
        Assert.Equal(150 / 255d, r.Curves![0].Points[1].Y); Assert.Contains(imported.Warnings, s => s.Contains("CameraProfile"));
    }
    [Fact]
    public void Xmp_rejects_entities_nonfinite_values_and_invalid_curve()
    {
        Assert.ThrowsAny<Exception>(() => LightroomPresetImporter.Xmp("<!DOCTYPE x [<!ENTITY x SYSTEM 'file:///test'>]><x>&x;</x>", "bad"));
        Assert.Throws<InvalidDataException>(() => LightroomPresetImporter.Xmp("<x xmlns:crs='http://ns.adobe.com/camera-raw-settings/1.0/' crs:Exposure2012='NaN'/>", "bad"));
        Assert.Throws<ArgumentException>(() => LightroomPresetImporter.Xmp("<x xmlns:crs='http://ns.adobe.com/camera-raw-settings/1.0/' crs:Exposure2012='12'/>", "bad"));
    }
    private static string IdentityCube() => "TITLE \"Identity\"\nLUT_3D_SIZE 2\n" + string.Join("\n", Enumerable.Range(0, 8).Select(i => $"{i % 2} {(i / 2) % 2} {i / 4}"));
    [Fact]
    public void Cube_preserves_identity_and_interpolates_rgb_in_correct_order()
    {
        var lut = LightroomPresetImporter.Cube(IdentityCube(), "Identity").Recipe.Lut!;
        var c = lut.Evaluate(.2, .4, .8); Assert.Equal(.2, c.R, 9); Assert.Equal(.4, c.G, 9); Assert.Equal(.8, c.B, 9);
        byte[] image = [43, 70, 140, 97]; Assert.Equal(image, ColorProcessor.Apply(image, new() { Lut = lut }));
        var swap = lut with { Values = Enumerable.Range(0, 8).SelectMany(i => new double[] { i / 4, (i / 2) % 2, i % 2 }).ToArray() };
        Assert.Equal(new byte[] { 140, 70, 43, 97 }, ColorProcessor.Apply(image, new() { Lut = swap })); Assert.Equal(43, image[0]);
        Assert.ThrowsAny<Exception>(() => LightroomPresetImporter.Cube("LUT_3D_SIZE 2\n0 0 0", "bad"));
        Assert.ThrowsAny<Exception>(() => LightroomPresetImporter.Cube("LUT_1D_SIZE 16", "bad"));
    }
    [Fact]
    public void Finishing_changes_output_but_live_view_omits_spatial_effects()
    {
        var image = Enumerable.Range(0, 81).SelectMany(i => new byte[] { (byte)(i == 40 ? 140 : 80), 90, 100, 170 }).ToArray();
        var r = new ColorRecipe { Structure = 20, Clarity = 20, Dehaze = 20, Vignette = -60, Grain = 40, Sharpening = 80 };
        var final = ColorProcessor.Apply(image, r, 9, 9); Assert.NotEqual(image, final); Assert.Equal(image, ColorProcessor.ApplyLiveView(image, r, 9, 9));
        Assert.Equal(final, ColorProcessor.Apply(image, r, 9, 9)); Assert.Equal(170, final[3]);
        var calibration = new ColorRecipe { Calibration = [new(40, 20), new(), new()] }; Assert.NotEqual(image, ColorProcessor.Apply(image, calibration, 9, 9));
        Assert.NotEqual(image, ColorProcessor.Apply(image, new() { CurveDarks = 50 }, 9, 9));
    }
    [Fact]
    public void Library_persists_multiple_presets_and_default_selection_beyond_three()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new ColorWorkspaceStore(path); var r = LightroomPresetImporter.Cube(IdentityCube(), "CUBE").Recipe;
            store.Save(new(null, [new(), new(), new(), r]) { DefaultIndex = 3 }); var saved = store.Load();
            Assert.Equal(4, saved.Looks.Length); Assert.Equal(3, saved.DefaultIndex); Assert.Equal(r.Lut!.Values, saved.Looks[3].Lut!.Values);
            var flow = new CustomerPreviewFlow(filterCount: 4, defaultFilter: 3); Assert.Equal(3, flow.FilterIndex); flow.Next(); flow.SelectFilter(2); Assert.Equal(2, flow.FilterIndex); flow.Restart(); Assert.Equal(3, flow.FilterIndex);
        }
        finally { File.Delete(path); }
    }
}
