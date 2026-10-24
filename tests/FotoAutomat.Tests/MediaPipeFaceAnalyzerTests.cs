using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;

public sealed class MediaPipeFaceAnalyzerTests
{
    private static MediaPipeFaceAnalyzer.Candidate Box(float score, float x0, float y0, float size) =>
        new(score, x0, y0, x0 + size, y0 + size, [.. Enumerable.Repeat(x0 + size / 2, 12)]);

    [Fact]
    public void Decode_places_box_and_keypoints_relative_to_the_anchor_and_tile()
    {
        var regressors = new float[2304 * 16]; var scores = Enumerable.Repeat(-10f, 2304).ToArray();
        int anchor = 10 * 48 + 20; // ô (20, 10): tâm (20,5 / 48, 10,5 / 48)
        scores[anchor] = 3;
        regressors[anchor * 16 + 2] = 48; regressors[anchor * 16 + 3] = 96; // w = 0,25, h = 0,5
        regressors[anchor * 16 + 4] = 19.2f; // mắt thứ nhất lệch +0,1 theo x
        var found = MediaPipeFaceAnalyzer.Decode(regressors, scores, new(100, 0, 960)).Single();
        float cx = 100 + 20.5f / 48 * 960, cy = 10.5f / 48 * 960;
        Assert.Equal(cx - 120, found.X0, 2); Assert.Equal(cx + 120, found.X1, 2);
        Assert.Equal(cy - 240, found.Y0, 2); Assert.Equal(cy + 240, found.Y1, 2);
        Assert.Equal(cx + 96, found.Keypoints[0], 2);
        Assert.InRange(found.Score, .95f, .96f);
    }

    [Fact]
    public void Weighted_nms_merges_overlapping_detections_and_keeps_separate_faces()
    {
        var merged = MediaPipeFaceAnalyzer.WeightedNms([Box(.9f, 0, 0, 100), Box(.6f, 10, 0, 100), Box(.8f, 300, 0, 100)]);
        Assert.Equal(2, merged.Count);
        Assert.Equal(.9f, merged[0].Score);
        Assert.Equal(10 * .6f / 1.5f, merged[0].X0, 3); // trung bình theo điểm số
        Assert.Equal(300, merged[1].X0);
    }

    [Fact]
    public void Tiles_cover_the_whole_image_and_add_a_grid_for_large_photos()
    {
        var small = MediaPipeFaceAnalyzer.Tiles(640, 480).ToList();
        Assert.Single(small);
        var large = MediaPipeFaceAnalyzer.Tiles(6000, 4000).ToList();
        Assert.Equal(new MediaPipeFaceAnalyzer.Tile(0, -1000, 6000), large[0]);
        Assert.True(large.Count > 6);
        Assert.All(large.Skip(1), t => Assert.Equal(2000, t.Size));
        for (int y = 0; y < 4000; y += 250) for (int x = 0; x < 6000; x += 250)
            Assert.Contains(large.Skip(1), t => x >= t.X && x < t.X + t.Size && y >= t.Y && y < t.Y + t.Size);
    }

    [Fact]
    public void Bundled_models_load_and_find_no_face_in_a_plain_image()
    {
        Assert.True(MediaPipeFaceAnalyzer.Available());
        using var analyzer = new MediaPipeFaceAnalyzer();
        var gray = Enumerable.Repeat((byte)128, 400 * 300 * 4).ToArray();
        Assert.Empty(analyzer.Detect(gray, 400, 300));
        Assert.Throws<ArgumentException>(() => analyzer.Detect(gray, 10, 10));
    }
}
