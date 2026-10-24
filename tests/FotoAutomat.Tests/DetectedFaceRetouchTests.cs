using FotoAutomat.Core;
namespace FotoAutomat.Tests;

public sealed class DetectedFaceRetouchTests
{
    private const int Size = 200;
    private static int Pixel(int x, int y) => (y * Size + x) * 4;
    private static byte[] Skin()
    {
        var pixels = new byte[Size * Size * 4];
        for (int i = 0; i < pixels.Length; i += 4) { pixels[i] = 130; pixels[i + 1] = 165; pixels[i + 2] = 210; pixels[i + 3] = 255; }
        return pixels;
    }
    private static void Paint(byte[] pixels, int x, int y, byte value) { int i = Pixel(x, y); pixels[i] = pixels[i + 1] = pixels[i + 2] = value; }
    // Mặt 100 × 120 px ở (50, 40); mắt ở 32%/68% bề rộng, 40% chiều cao; miệng 74%.
    private static DetectedFace Box() => new(.25, .2, .5, .6);
    private static DetectedFace Landmarks() => Box() with { LeftEye = new(.41, .44), RightEye = new(.59, .44), Mouth = new(.5, .644) };

    [Fact]
    public void Box_only_face_smooths_skin_but_never_touches_eyes_or_teeth()
    {
        var source = Skin(); Paint(source, 100, 90, 240); Paint(source, 82, 88, 30); Paint(source, 100, 117, 190);
        var settings = new RetouchSettings(EvenSkin: 100, LeftIris: 30, TeethBrightness: 100);
        var plan = RetouchProcessor.Plan([Box()], Size, Size);
        Assert.Equal(new RetouchFacePlan(0, true, false, false), plan[0]);
        var result = RetouchProcessor.Apply(source, Size, Size, [Box()], settings);
        Assert.True(result[Pixel(100, 90) + 1] < source[Pixel(100, 90) + 1]);
        var eyesOnly = RetouchProcessor.Apply(source, Size, Size, [Box()], new RetouchSettings(Impact: 0, LeftIris: 30, TeethBrightness: 100));
        Assert.Equal(source, eyesOnly);
    }

    [Fact]
    public void Valid_landmarks_enable_eye_tools_at_the_detected_position_and_bad_landmarks_do_not()
    {
        var source = Skin(); Paint(source, 82, 88, 30);
        var settings = new RetouchSettings(Impact: 0, LeftIris: 30);
        Assert.True(RetouchProcessor.Plan([Landmarks()], Size, Size)[0].Features);
        Assert.True(RetouchProcessor.Apply(source, Size, Size, [Landmarks()], settings)[Pixel(82, 88)] > 30);

        var tilted = Landmarks() with { RightEye = new(.55, .60) };
        var close = Landmarks() with { RightEye = new(.43, .44) };
        var outside = Landmarks() with { Mouth = new(.5, .95) };
        Assert.All(RetouchProcessor.Plan([tilted, close, outside], Size, Size), p => Assert.False(p.Features));
        Assert.Equal(source, RetouchProcessor.Apply(source, Size, Size, [tilted], settings));
    }

    [Fact]
    public void Teeth_need_the_inner_lip_outline()
    {
        var source = Skin(); Paint(source, 100, 117, 190);
        var settings = new RetouchSettings(Impact: 0, TeethBrightness: 100);
        Assert.Equal(source, RetouchProcessor.Apply(source, Size, Size, [Landmarks()], settings));
        var mesh = Landmarks() with { InnerLips = [new(.47, .58), new(.53, .58), new(.53, .6), new(.47, .6)] };
        Assert.Equal(FaceEvidence.Mesh, mesh.Evidence);
        Assert.True(RetouchProcessor.Plan([mesh], Size, Size)[0].Teeth);
        Assert.True(RetouchProcessor.Apply(source, Size, Size, [mesh], settings)[Pixel(100, 117)] > 190);
    }

    [Fact]
    public void Overlapping_faces_are_not_applied_twice_and_order_does_not_matter()
    {
        var source = Skin(); Paint(source, 100, 90, 240);
        var settings = new RetouchSettings(EvenSkin: 100);
        var once = RetouchProcessor.Apply(source, Size, Size, [Box()], settings);
        Assert.Equal(once, RetouchProcessor.Apply(source, Size, Size, [Box(), Box()], settings));
        var shifted = new DetectedFace(.3, .22, .45, .55);
        Assert.Equal(RetouchProcessor.Apply(source, Size, Size, [Box(), shifted], settings), RetouchProcessor.Apply(source, Size, Size, [shifted, Box()], settings));
    }

    [Fact]
    public void Small_invalid_and_extra_faces_are_skipped_without_failing()
    {
        var faces = new List<DetectedFace> { new(.1, .1, .02, .02), new(double.NaN, 0, .2, .2), new(.7, .7, .5, .5) };
        faces.AddRange(Enumerable.Range(0, 21).Select(i => new DetectedFace(i % 5 * .18, i / 5 * .18, .1 + i * .001, .1)));
        var plan = RetouchProcessor.Plan(faces, Size, Size);
        Assert.Equal("Mặt quá nhỏ", plan[0].Skipped);
        Assert.Equal("Khung mặt không hợp lệ", plan[1].Skipped);
        Assert.Null(plan[2].Skipped); // cắt theo mép ảnh, còn đủ lớn
        Assert.Equal(RetouchProcessor.MaxFaces, plan.Count(p => p.Skipped is null));
        Assert.Equal("Quá 20 mặt", plan[3].Skipped); // mặt nhỏ nhất trong nhóm 22 mặt hợp lệ
        Assert.Equal(Skin(), RetouchProcessor.Apply(Skin(), Size, Size, faces, new RetouchSettings()));
    }
}
