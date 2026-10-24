using FotoAutomat.Infrastructure;
namespace FotoAutomat.Tests;

public class GeneralSettingsTests
{
    private static string Folder() => Path.Combine(Path.GetTempPath(), "FotoAutomat-tests", Guid.NewGuid().ToString());
    [Fact]
    public void Pin_requires_current_pin_to_change_and_is_hashed()
    {
        string folder = Folder(); var store = new GeneralSettingsStore(folder);
        Assert.False(store.VerifyPin("123456"));
        Assert.Throws<ArgumentException>(() => store.SetPin("123"));
        store.SetPin("123456");
        Assert.True(new GeneralSettingsStore(folder).VerifyPin("123456"));
        Assert.DoesNotContain("123456", File.ReadAllText(Path.Combine(folder, "admin-pin.json")));
        Assert.Throws<ArgumentException>(() => store.SetPin("654321", "999999"));
        store.SetPin("654321", "123456");
        Assert.True(store.VerifyPin("654321"));
        Assert.False(store.VerifyPin("123456"));
    }
    [Fact]
    public void Settings_roundtrip_and_reject_relative_directories()
    {
        var store = new GeneralSettingsStore(Folder());
        var settings = new GeneralSettings { FullScreen = false, HardwareAcceleration = false, DataDirectory = Folder(), ExportGif = true };
        store.Save(settings);
        Assert.Equal(settings, store.Load());
        Assert.Throws<ArgumentException>(() => store.Save(settings with { DataDirectory = "relative" }));
    }
    private static string LogoSource(string extension, int bytes = 64)
    {
        string path = Path.Combine(Folder(), "logo" + extension);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[bytes]);
        return path;
    }
    [Fact]
    public void Logo_is_copied_on_import_and_replacing_it_deletes_the_old_file_after_save()
    {
        string folder = Folder(); var store = new GeneralSettingsStore(folder);
        var settings = new GeneralSettings { DataDirectory = Folder() };
        string first = store.ImportLogo(LogoSource(".png"));
        store.Save(settings with { LogoFile = first });
        Assert.Equal(Path.Combine(folder, first), store.LogoPath(store.Load()));

        string second = store.ImportLogo(LogoSource(".jpg"));
        Assert.True(File.Exists(Path.Combine(folder, first)));
        store.Save(store.Load() with { LogoFile = second });
        Assert.False(File.Exists(Path.Combine(folder, first)));
        Assert.Equal(second, store.Load().LogoFile);

        store.Save(store.Load() with { LogoFile = "" });
        Assert.Null(store.LogoPath(store.Load()));
        Assert.Empty(Directory.GetFiles(folder, "brand-logo-*"));
    }
    [Fact]
    public void Logo_rejects_other_types_large_files_and_paths()
    {
        string folder = Folder(); var store = new GeneralSettingsStore(folder);
        Assert.Throws<ArgumentException>(() => store.ImportLogo(LogoSource(".gif")));
        Assert.Throws<ArgumentException>(() => store.ImportLogo(LogoSource(".png", (int)GeneralSettingsStore.MaxLogoBytes + 1)));
        var settings = new GeneralSettings { DataDirectory = Folder() };
        Assert.Throws<ArgumentException>(() => store.Save(settings with { LogoFile = @"..\brand-logo-x.png" }));
        Assert.Throws<ArgumentException>(() => store.Save(settings with { LogoFile = "brand-logo-missing.png" }));
        Assert.Null(store.LogoPath(settings with { LogoFile = @"C:\Windows\brand-logo-x.png" }));
    }
    [Fact]
    public void Settings_saved_before_logo_support_still_load()
    {
        string folder = Folder(); Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "general.json"), """{"BrandName":"Studio A","FullScreen":false,"HardwareAcceleration":true,"DataDirectory":"C:\\Data","ExportDirectory":"","ExportOriginals":true,"ExportPrints":true,"ExportGif":false,"ExportTimelapse":false}""");
        var store = new GeneralSettingsStore(folder);
        var loaded = store.Load();
        Assert.Equal("Studio A", loaded.BrandName);
        Assert.Equal("", loaded.LogoFile);
        Assert.Null(store.LogoPath(loaded));
    }
    [Fact]
    public void Brand_colors_are_validated_and_normalized_on_save()
    {
        var store = new GeneralSettingsStore(Folder());
        var settings = new GeneralSettings { DataDirectory = Folder() };
        Assert.Equal("#A94466", settings.AccentColor);
        Assert.Equal("#FAF3E9", settings.BackgroundColor);
        store.Save(settings with { AccentColor = "#0f766e", BackgroundColor = "#ffffff" });
        Assert.Equal("#0F766E", store.Load().AccentColor);
        Assert.Equal("#FFFFFF", store.Load().BackgroundColor);
        Assert.Throws<ArgumentException>(() => store.Save(settings with { AccentColor = "#FDF2F8" }));
        Assert.Throws<ArgumentException>(() => store.Save(settings with { BackgroundColor = "#111827" }));
        Assert.Throws<ArgumentException>(() => store.Save(settings with { AccentColor = "teal" }));
    }
    [Fact]
    public void Background_image_is_imported_separately_from_the_logo()
    {
        string folder = Folder(); var store = new GeneralSettingsStore(folder);
        var settings = new GeneralSettings { DataDirectory = Folder() };
        string logo = store.ImportLogo(LogoSource(".png"));
        string background = store.ImportBackground(LogoSource(".jpg", 6 * 1024 * 1024));
        store.Save(settings with { LogoFile = logo, BackgroundFile = background });
        Assert.Equal(Path.Combine(folder, background), store.BackgroundPath(store.Load()));
        Assert.Throws<ArgumentException>(() => store.Save(settings with { LogoFile = background }));
        Assert.Throws<ArgumentException>(() => store.ImportBackground(LogoSource(".png", (int)GeneralSettingsStore.MaxBackgroundBytes + 1)));
        store.Save(store.Load() with { BackgroundFile = "" });
        Assert.True(File.Exists(Path.Combine(folder, logo)));
        Assert.Empty(Directory.GetFiles(folder, "brand-background-*"));
    }
    [Fact]
    public void Loading_image_is_stored_separately_and_replaced_on_save()
    {
        string folder = Folder(); var store = new GeneralSettingsStore(folder);
        var settings = new GeneralSettings { DataDirectory = Folder() };
        string first = store.ImportLoading(LogoSource(".png"));
        store.Save(settings with { LoadingFile = first });
        Assert.Equal(Path.Combine(folder, first), store.LoadingPath(store.Load()));
        string second = store.ImportLoading(LogoSource(".jpg"));
        store.Save(store.Load() with { LoadingFile = second });
        Assert.False(File.Exists(Path.Combine(folder, first)));
        Assert.Throws<ArgumentException>(() => store.Save(settings with { BackgroundFile = second }));
    }
}
