using HylianGrimoire.Rom;
using HylianGrimoire.Textures;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class TextureCatalogTests
{
    [Theory]
    [InlineData("Retail NTSC 1.0", 12513)]
    [InlineData("Retail NTSC 1.1", 12513)]
    [InlineData("Retail NTSC 1.2", 12513)]
    [InlineData("Retail NTSC Master Quest", 12525)]
    [InlineData("Retail NTSC GameCube", 12515)]
    [InlineData("Retail PAL 1.0", 8862)]
    [InlineData("Retail PAL 1.1", 8866)]
    [InlineData("Retail PAL Master Quest", 8861)]
    [InlineData("Retail PAL GameCube", 8852)]
    public void Retail_profiles_have_expected_texture_count(string profileName, int expectedCount)
    {
        RomVersionProfile profile = GetProfile(profileName);

        IReadOnlyList<TextureDefinition> textures = TextureCatalog.GetTextures(profile);

        Assert.Equal(expectedCount, textures.Count);
    }

    [Theory]
    [InlineData("Retail NTSC 1.2", "gTitleTheLegendOfTextTex", 0x17b3700, TextureFormat.I8, 72, 8)]
    [InlineData("Retail PAL 1.0", "gAttackDoActionENGTex", 0x8a6000, TextureFormat.IA4, 48, 16)]
    [InlineData("Retail PAL GameCube", "gFileSelSwitchENGTex", 0x1a73000, TextureFormat.IA8, 48, 16)]
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
    public void Color_indexed_textures_include_tlut_metadata()
    {
        RomVersionProfile profile = GetProfile("Retail NTSC 1.2");

        TextureDefinition texture = TextureCatalog.GetTextures(profile).Single(texture => texture.Name == "gLinkChildNoseTex");

        Assert.Equal(TextureFormat.CI8, texture.Format);
        Assert.True(texture.UsesTlut);
        Assert.Equal(0xfc3500, texture.TlutRomAddress);
        Assert.Equal(256, texture.EffectiveTlutColorCount);
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", "ntsc10_orig.z64")]
    [InlineData("Retail NTSC 1.1", "ntsc11_orig.z64")]
    [InlineData("Retail NTSC 1.2", "ntsc12_orig.z64")]
    [InlineData("Retail NTSC Master Quest", "ntscmq_orig.z64")]
    [InlineData("Retail NTSC GameCube", "ntscgc_orig.z64")]
    [InlineData("Retail PAL 1.0", "pal10_orig.z64")]
    [InlineData("Retail PAL 1.1", "pal11_orig.z64")]
    [InlineData("Retail PAL Master Quest", "palmq_orig.z64")]
    [InlineData("Retail PAL GameCube", "palgc_orig.z64")]
    public void Color_indexed_texture_palettes_cover_used_indices(string profileName, string romFileName)
    {
        string romPath = Path.Combine(@"D:\test30\retaildecompressed", romFileName);
        if (!File.Exists(romPath))
        {
            return;
        }

        byte[] rom = File.ReadAllBytes(romPath);
        RomVersionProfile profile = GetProfile(profileName);

        foreach (TextureDefinition texture in TextureCatalog.GetTextures(profile).Where(texture => texture.UsesTlut))
        {
            int textureLength = TextureCodec.GetByteLength(texture.Width, texture.Height, texture.Format);
            int maxIndex = GetMaxPaletteIndex(rom.AsSpan(texture.RomAddress, textureLength), texture.Format);

            Assert.True(
                texture.EffectiveTlutColorCount > maxIndex,
                $"{profileName} {texture.Group}/{texture.Name} uses palette index {maxIndex}, but catalog only exposes {texture.EffectiveTlutColorCount} colors.");
        }
    }

    [Fact]
    public void Non_retail_profiles_are_not_exposed_in_texture_catalog()
    {
        RomVersionProfile profile = GetProfile("NTSC 1.2");

        Assert.False(TextureCatalog.TryGetTextures(profile, out _));
    }

    private static RomVersionProfile GetProfile(string name)
        => RomVersionDatabase.Profiles.Single(profile => profile.Name == name);

    private static int GetMaxPaletteIndex(ReadOnlySpan<byte> data, TextureFormat format)
    {
        int maxIndex = 0;
        foreach (byte value in data)
        {
            if (format == TextureFormat.CI4)
            {
                maxIndex = Math.Max(maxIndex, Math.Max(value >> 4, value & 0x0f));
            }
            else
            {
                maxIndex = Math.Max(maxIndex, value);
            }
        }

        return maxIndex;
    }
}
