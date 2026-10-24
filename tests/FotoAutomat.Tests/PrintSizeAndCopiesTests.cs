using FotoAutomat.Core;
using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;

public class PrintSizeAndCopiesTests
{
    private static PrintLayout Strip(string name) => new(name, false, [new(1, 10, 40, 180, 130)], SingleStrip: true);
    private static PrintLayout Postcard(string name) => new(name, false, [new(1, 20, 20, 160, 120)]);
    private static PreviewPackage[] Sizes() => [new(2, 90000, "Khổ nhỏ") { SingleStrip = true }, new(2, 120000, "Khổ lớn") { SingleStrip = false }];
    private static CustomerPreviewFlow AtSizeScreen(params PrintLayout[] frames)
    {
        var flow = new CustomerPreviewFlow(packages: Sizes(), frames: frames);
        flow.Next(); flow.Next();
        Assert.Equal(CustomerScreen.Layout, flow.Screen);
        return flow;
    }

    [Fact]
    public void Strip_size_allows_only_even_copies_and_rounds_up_when_switching()
    {
        var flow = AtSizeScreen(Strip("Dải"), Postcard("Bưu thiếp"));
        flow.SelectPackage(1); flow.SelectCopies(3);
        Assert.Equal(3, flow.Copies);
        Assert.Equal([2, 3, 4, 5, 6, 7, 8], flow.CopyOptions);
        flow.SelectPackage(0);
        Assert.Equal([2, 4, 6, 8], flow.CopyOptions);
        Assert.Equal(4, flow.Copies);
        flow.SelectCopies(5);
        Assert.Equal(4, flow.Copies);
        Assert.Contains("4 bản in", flow.OrderSummary);
        Assert.Equal("Khổ nhỏ 2 × 6 · 90.000đ", flow.PackageOptions[0].Label);
    }

    [Fact]
    public void Size_without_published_frame_cannot_be_chosen()
    {
        var flow = AtSizeScreen(Postcard("Bưu thiếp"));
        Assert.Equal([false, true], flow.PackageAvailability);
        Assert.Equal(1, flow.PackageIndex);
        flow.SelectPackage(0);
        Assert.Equal(1, flow.PackageIndex);
        flow.Next();
        Assert.Equal(CustomerScreen.Payment, flow.Screen);
    }

    [Fact]
    public void Frame_choices_follow_the_chosen_size()
    {
        var flow = AtSizeScreen(Strip("Dải A"), Postcard("Bưu thiếp"), Strip("Dải B"));
        flow.SelectPackage(0);
        Assert.Equal(["Dải A", "Dải B"], flow.FrameChoices.Select(f => f.Name));
        Assert.True(flow.HasFrameChoice);
        Assert.Equal("Dải A", flow.CurrentFrame?.Name);
        flow.SelectPackage(1);
        Assert.False(flow.HasFrameChoice);
        Assert.Equal("Bưu thiếp", flow.CurrentFrame?.Name);
        Assert.False(flow.UsesSampleFrames);
    }

    [Fact]
    public void Without_published_frames_both_sizes_use_sample_frames_and_restart_resets_copies()
    {
        var flow = AtSizeScreen();
        Assert.Equal([true, true], flow.PackageAvailability);
        Assert.True(flow.UsesSampleFrames);
        flow.SelectCopies(6);
        flow.Restart();
        Assert.Equal(2, flow.Copies);
    }

    [Fact]
    public void Store_publishes_copies_and_migrates_the_single_active_frame_once()
    {
        string root = Path.Combine(Path.GetTempPath(), "fa-publish-" + Guid.NewGuid());
        try
        {
            var store = new PrintLayoutStore(root);
            string library = Path.Combine(root, "Layouts");
            string a = Path.Combine(library, "a.fotolayout"), b = Path.Combine(library, "b.fotolayout");
            store.Save(a, Strip("Dải"));
            store.Save(Path.Combine(root, "active.fotolayout"), Strip("Dải"));
            store.Save(b, Postcard("Bưu thiếp"));

            Assert.Equal(["Dải"], store.LoadPublished().Select(f => f.Name));
            Assert.True(File.Exists(store.PublishedPath(a)));
            Assert.False(File.Exists(Path.Combine(root, "active.fotolayout")));

            store.Publish(b, store.Load(b));
            store.Save(b, Postcard("Bưu thiếp mới"));
            Assert.Equal(["Dải", "Bưu thiếp"], store.LoadPublished().Select(f => f.Name));

            store.Unpublish(a);
            Assert.Equal(["Bưu thiếp"], store.LoadPublished().Select(f => f.Name));
            Assert.Equal(2, Directory.GetFiles(library).Length);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
