using FotoAutomat.Core;
namespace FotoAutomat.Tests;

public class BrandColorsTests
{
    [Fact]
    public void Default_accent_keeps_the_original_customer_palette()
    {
        var palette = BrandColors.Palette("#a94466");
        Assert.Equal(new BrandPalette("#A94466", "#C86C8B", "#852640", "#F7E0E7", "#F3D2DF"), palette);
    }
    [Fact]
    public void Other_accents_derive_lighter_and_darker_shades()
    {
        var palette = BrandColors.Palette("#0F766E");
        Assert.Equal("#0F766E", palette.Action);
        Assert.True(BrandColors.Contrast(palette.Tint, BrandColors.White) < BrandColors.Contrast(palette.Action, BrandColors.White));
        Assert.True(BrandColors.Contrast(palette.Deep, BrandColors.White) > BrandColors.Contrast(palette.Action, BrandColors.White));
    }
    [Theory]
    [InlineData("A94466")]
    [InlineData("#A9446")]
    [InlineData("#GG4466")]
    [InlineData("")]
    public void Rejects_values_that_are_not_rrggbb(string value) => Assert.Throws<ArgumentException>(() => BrandColors.Normalize(value));
    [Fact]
    public void Rejects_accents_too_light_for_white_text_and_backgrounds_too_dark_for_ink()
    {
        BrandColors.ValidateAccent(BrandColors.DefaultAccent);
        BrandColors.ValidateBackground(BrandColors.DefaultBackground);
        Assert.Throws<ArgumentException>(() => BrandColors.ValidateAccent("#F7E0E7"));
        Assert.Throws<ArgumentException>(() => BrandColors.ValidateBackground("#3F2D22"));
        Assert.Equal(21, BrandColors.Contrast(BrandColors.White, BrandColors.Black), 3);
    }
}
