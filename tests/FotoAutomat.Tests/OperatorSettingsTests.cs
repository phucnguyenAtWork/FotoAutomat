using System.Text.Json;
using FotoAutomat.Infrastructure;

namespace FotoAutomat.Tests;

public sealed class OperatorSettingsTests
{
    [Fact]
    public void Settings_survive_reopen_and_invalid_save_preserves_previous_value()
    {
        var path = Path.Combine(Path.GetTempPath(), $"foto-settings-{Guid.NewGuid()}.json");
        try
        {
            var store = new OperatorSettingsStore(path);
            var expected = new OperatorSettings { BoothName = "Booth thử", CameraNotes = "Camera dự kiến" };
            store.Save(expected);
            Assert.Equal(expected, new OperatorSettingsStore(path).Load());
            Assert.Throws<ArgumentException>(() => store.Save(expected with { BoothName = " " }));
            Assert.Equal(expected, store.Load());
            File.WriteAllText(path, "invalid json");
            Assert.Throws<JsonException>(() => store.Load());
            Assert.Equal("invalid json", File.ReadAllText(path));
        }
        finally { File.Delete(path); File.Delete(path + ".tmp"); }
    }
}
