using FotoAutomat.Infrastructure;

namespace FotoAutomat.Tests;

public sealed class CreativeDraftTests
{
    [Fact]
    public void Draft_roundtrips_and_invalid_update_preserves_last_saved_value()
    {
        string root = Path.Combine(Path.GetTempPath(), $"creative-{Guid.NewGuid()}");
        try
        {
            var store = new CreativeDraftStore(Path.Combine(root, "draft.json"));
            var draft = new CreativeDraft("Fujifilm X-H2", "Ấm", 0.4, 15, 20, -4, 30, 10);
            store.Save(draft);
            Assert.Equal(draft, store.Load());
            Assert.Throws<ArgumentException>(() => store.Save(draft with { Exposure = double.NaN }));
            Assert.Throws<ArgumentException>(() => store.Save(draft with { LookName = "" }));
            Assert.Equal(draft, store.Load());
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
