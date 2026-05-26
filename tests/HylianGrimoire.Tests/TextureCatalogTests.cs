using HylianGrimoire.Rom;
using HylianGrimoire.Textures;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class TextureCatalogTests
{
    [Theory]
    [InlineData("Retail NTSC 1.0", 337)]
    [InlineData("Retail NTSC 1.1", 337)]
    [InlineData("Retail NTSC 1.2", 337)]
    [InlineData("Retail NTSC Master Quest", 336)]
    [InlineData("Retail NTSC GameCube", 330)]
    [InlineData("Retail PAL 1.0", 354)]
    [InlineData("Retail PAL 1.1", 353)]
    [InlineData("Retail PAL Master Quest", 337)]
    [InlineData("Retail PAL GameCube", 336)]
    public void Retail_profiles_have_expected_texture_count(string profileName, int expectedCount)
    {
        RomVersionProfile profile = GetProfile(profileName);

        IReadOnlyList<TextureDefinition> textures = TextureCatalog.GetTextures(profile);

        Assert.Equal(expectedCount, textures.Count);
    }

    [Theory]
    [InlineData("Retail NTSC 1.2", "gTitleTheLegendOfTextTex", 0x17b3700, TextureFormat.IA8, 72, 8)]
    [InlineData("Retail PAL 1.0", "gAttackDoActionENGTex", 0x8a6000, TextureFormat.IA4, 48, 16)]
    [InlineData("Retail PAL GameCube", "gFileSelSwitchENGTex", 0x1a73060, TextureFormat.IA8, 48, 16)]
    public void Known_textures_are_mapped_to_expected_definition(
        string profileName,
        string textureName,
        int expectedAddress,
        TextureFormat expectedFormat,
        int expectedWidth,
        int expectedHeight)
    {
        RomVersionProfile profile = GetProfile(profileName);

        TextureDefinition texture = TextureCatalog.GetTextures(profile).Single(texture => texture.Name == textureName);

        Assert.Equal(expectedAddress, texture.RomAddress);
        Assert.Equal(expectedFormat, texture.Format);
        Assert.Equal(expectedWidth, texture.Width);
        Assert.Equal(expectedHeight, texture.Height);
    }

    [Fact]
    public void Non_retail_profiles_are_not_exposed_in_texture_catalog()
    {
        RomVersionProfile profile = GetProfile("NTSC 1.2");

        Assert.False(TextureCatalog.TryGetTextures(profile, out _));
    }

    private static RomVersionProfile GetProfile(string name)
        => RomVersionDatabase.Profiles.Single(profile => profile.Name == name);
}
