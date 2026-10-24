using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;

public sealed class RetouchTests
{
    [Theory]
    [InlineData(false, false, 40)]
    [InlineData(true, false, 40)]
    [InlineData(false, true, 210)]
    [InlineData(true, true, 210)]
    public void Signed_eye_controls_darken_and_brighten_only_selected_eye(bool right, bool sclera, int level)
    {
        var source = Enumerable.Range(0, 10000).SelectMany(_ => new byte[] { (byte)level, (byte)level, (byte)level, 255 }).ToArray();
        RetouchSettings Setting(double v) => sclera ? (right ? new(RightSclera: v) : new(LeftSclera: v)) : (right ? new(RightIris: v) : new(LeftIris: v));
        byte[] Apply(double v) => RetouchProcessor.Apply(source, 100, 100, [new(0, 0, 1, 1, Setting(v))]);
        int pixel = (40 * 100 + (right ? 68 : 32)) * 4, other = (40 * 100 + (right ? 32 : 68)) * 4;
        Assert.True(Apply(-30)[pixel] < level); Assert.True(Apply(30)[pixel] > level);
        Assert.Equal(level, Apply(-30)[other]); Assert.Equal(source, Apply(0));
        Assert.Throws<ArgumentException>(() => Setting(-31).Validate());
        Setting(100).Validate(); // persisted settings from older versions remain readable
    }
    private const int Size = 100;
    private static byte[] SkinImage()
    {
        var pixels = new byte[Size * Size * 4];
        for (int i = 0; i < pixels.Length; i += 4) { pixels[i] = 130; pixels[i + 1] = 165; pixels[i + 2] = 210; pixels[i + 3] = 200; }
        return pixels;
    }
    private static int Pixel(int x, int y) => (y * Size + x) * 4;
    private static FaceRetouch Face(RetouchSettings s) => new(.1, .1, .8, .6, s);
    [Fact]
    public void Neutral_is_exact_and_processing_preserves_original_alpha_and_background()
    {
        var source = SkinImage(); int i = Pixel(50, 45); source[i] += 20; source[i + 1] += 20; source[i + 2] += 20;
        var original = source.ToArray();
        Assert.Equal(source, RetouchProcessor.Apply(source, Size, Size, [Face(new())]));
        var result = RetouchProcessor.Apply(source, Size, Size, [Face(new(EvenSkin: 100))]);
        Assert.True(result[i + 1] < source[i + 1]); Assert.Equal(original, source);
        Assert.Equal(source[Pixel(0, 0)], result[Pixel(0, 0)]);
        for (int a = 3; a < source.Length; a += 4) Assert.Equal(source[a], result[a]);
    }
    [Fact]
    public void Protection_and_erase_control_local_effect_and_roundtrip()
    {
        var source = SkinImage(); var face = Face(new(Contouring: 100)); int i = Pixel(50, 43);
        var plain = RetouchProcessor.Apply(source, Size, Size, [face]); Assert.NotEqual(source[i], plain[i]);
        var mask = new PhotoMask(PhotoMaskKind.Brush, [new(.505, .435, .1)]);
        var protectedFace = face with { Protection = mask };
        Assert.Equal(source[i], RetouchProcessor.Apply(source, Size, Size, [protectedFace])[i]);
        var erased = protectedFace with { Protection = mask with { Dabs = [new(.505, .435, .1), new(.505, .435, .1, Erase: true)] } };
        Assert.Equal(plain[i], RetouchProcessor.Apply(source, Size, Size, [erased])[i]);
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new RetouchWorkspaceStore(path); store.Save(new("sample.jpg", [protectedFace]));
            var loaded = store.Load(); Assert.Equal("sample.jpg", loaded.SamplePath); Assert.Equal(face.Settings, loaded.Faces[0].Settings);
            Assert.Equal(mask.Dabs, loaded.Faces[0].Protection!.Dabs);
            Assert.Throws<ArgumentException>(() => store.Save(new("other.jpg", [face with { Width = 2 }])));
            Assert.Equal("sample.jpg", store.Load().SamplePath);
        }
        finally { File.Delete(path); File.Delete(path + ".tmp"); }
    }
    [Fact]
    public void Neck_is_opt_in_and_eyes_are_independent_of_skin_impact()
    {
        var source = SkinImage(); int neck = Pixel(50, 77); source[neck] += 20; source[neck + 1] += 20; source[neck + 2] += 20;
        var settings = new RetouchSettings(EvenSkin: 100);
        Assert.Equal(source[neck], RetouchProcessor.Apply(source, Size, Size, [Face(settings)])[neck]);
        Assert.True(RetouchProcessor.Apply(source, Size, Size, [Face(settings with { SkinNeck = true })])[neck] < source[neck]);
        int left = Pixel(35, 34), right = Pixel(64, 34);
        for (int c = 0; c < 3; c++) source[left + c] = source[right + c] = 30;
        var eyeResult = RetouchProcessor.Apply(source, Size, Size, [Face(new(Impact: 0, LeftIris: 100))]);
        Assert.True(eyeResult[left] > source[left]); Assert.Equal(source[right], eyeResult[right]);
    }
    [Fact]
    public void Teeth_brightening_is_confined_to_confirmed_mouth_and_requires_light_pixels()
    {
        var source = SkinImage(); int mouth = Pixel(50, 54), outside = Pixel(50, 20);
        for (int c = 0; c < 3; c++) source[mouth + c] = source[outside + c] = 190;
        var output = RetouchProcessor.Apply(source, Size, Size, [Face(new(TeethBrightness: 100))]);
        Assert.True(output[mouth] > source[mouth]); Assert.Equal(source[outside], output[outside]);
        for (int c = 0; c < 3; c++) source[mouth + c] = 30;
        Assert.Equal(source[mouth], RetouchProcessor.Apply(source, Size, Size, [Face(new(TeethBrightness: 100))])[mouth]);
    }
    [Fact]
    public void Invalid_values_bounds_counts_and_cancellation_are_rejected()
    {
        var source = SkinImage();
        Assert.Throws<ArgumentException>(() => RetouchProcessor.Apply(source, Size, Size, [Face(new(Texture: double.NaN))]));
        Assert.Throws<ArgumentException>(() => RetouchProcessor.Apply(source, Size, Size, [Face(new()) with { MouthX = 2 }]));
        Assert.Equal(source, RetouchProcessor.Apply(source, Size, Size, Enumerable.Repeat(Face(new()), 21).ToArray()));
        Assert.Throws<OperationCanceledException>(() => RetouchProcessor.Apply(source, Size, Size, [Face(new())], new CancellationToken(true)));
    }
}
