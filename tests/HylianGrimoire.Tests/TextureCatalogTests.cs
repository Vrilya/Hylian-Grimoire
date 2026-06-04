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
    [InlineData("Majora's Mask NTSC-U", 8759)]
    public void Supported_profiles_have_expected_texture_count(string profileName, int expectedCount)
    {
        RomVersionProfile profile = GetProfile(profileName);

        IReadOnlyList<TextureDefinition> textures = TextureCatalog.GetTextures(profile);

        Assert.Equal(expectedCount, textures.Count);
    }

    [Fact]
    public void Texture_groups_do_not_repeat_adjacent_path_segments()
    {
        foreach (RomVersionProfile profile in RomVersionDatabase.Profiles)
        {
            if (!TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? textures))
            {
                continue;
            }

            foreach (TextureDefinition texture in textures)
            {
                string[] parts = texture.Group.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < parts.Length; i++)
                {
                    Assert.False(
                        string.Equals(parts[i - 1], parts[i], StringComparison.OrdinalIgnoreCase),
                        $"{profile.Name} {texture.Group}/{texture.Name} repeats adjacent texture tree segments.");
                }
            }
        }
    }

    [Theory]
    [InlineData("Retail NTSC 1.2", "gTitleTheLegendOfTextTex", 0x17b3700, TextureFormat.I8, 72, 8)]
    [InlineData("Retail PAL 1.0", "gAttackDoActionENGTex", 0x8a6000, TextureFormat.IA4, 48, 16)]
    [InlineData("Retail PAL GameCube", "gFileSelSwitchENGTex", 0x1a73000, TextureFormat.IA8, 48, 16)]
    [InlineData("Majora's Mask NTSC-U", "gQuestIconHeartContainerTex", 0x900, TextureFormat.Rgba32, 24, 24)]
    [InlineData("Majora's Mask NTSC-U", "gLinkHumanEyesOpenTex", 0x115b000, TextureFormat.CI8, 64, 32)]
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
    public void MajorasMask_archive_textures_are_mapped_to_cmpdma_archive()
    {
        RomVersionProfile profile = GetProfile("Majora's Mask NTSC-U");

        TextureDefinition texture = TextureCatalog.GetTextures(profile).Single(texture => texture.Name == "gQuestIconHeartContainerTex");

        Assert.Equal(TextureStorageKind.CmpDmaArchive, texture.StorageKind);
        Assert.Equal(0x900, texture.RomAddress);
        Assert.Equal(0xa7bee0, texture.ArchiveRomAddress);
        Assert.Equal(0x48c0, texture.ArchiveLength);
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

    [RetailDecompressedRomFixtureTheory]
    [InlineData("Retail NTSC 1.0", "oot_retail_ntsc_1.0_decompressed.z64")]
    [InlineData("Retail NTSC 1.1", "oot_retail_ntsc_1.1_decompressed.z64")]
    [InlineData("Retail NTSC 1.2", "oot_retail_ntsc_1.2_decompressed.z64")]
    [InlineData("Retail NTSC Master Quest", "oot_retail_ntsc_mq_decompressed.z64")]
    [InlineData("Retail NTSC GameCube", "oot_retail_ntsc_gc_decompressed.z64")]
    [InlineData("Retail PAL 1.0", "oot_retail_pal_1.0_decompressed.z64")]
    [InlineData("Retail PAL 1.1", "oot_retail_pal_1.1_decompressed.z64")]
    [InlineData("Retail PAL Master Quest", "oot_retail_pal_mq_decompressed.z64")]
    [InlineData("Retail PAL GameCube", "oot_retail_pal_gc_decompressed.z64")]
    public void Color_indexed_texture_palettes_cover_used_indices(string profileName, string romFileName)
    {
        string romPath = LocalRomFixtures.GetRequiredRetailDecompressedPath(romFileName);

        byte[] rom = File.ReadAllBytes(romPath);
        RomVersionProfile profile = GetProfile(profileName);

        foreach (TextureDefinition texture in TextureCatalog.GetTextures(profile).Where(texture => texture.UsesTlut))
        {
            int maxIndex = GetMaxPaletteIndex(TextureRomService.ReadRaw(rom, texture), texture.Format);

            Assert.True(
                texture.EffectiveTlutColorCount > maxIndex,
                $"{profileName} {texture.Group}/{texture.Name} uses palette index {maxIndex}, but catalog only exposes {texture.EffectiveTlutColorCount} colors.");
        }
    }

    [Fact]
    public void MajorasMask_color_indexed_textures_include_tlut_metadata()
    {
        RomVersionProfile profile = GetProfile("Majora's Mask NTSC-U");

        TextureDefinition texture = TextureCatalog.GetTextures(profile).Single(texture => texture.Name == "gLinkHumanEyesOpenTex");

        Assert.Equal(TextureFormat.CI8, texture.Format);
        Assert.True(texture.UsesTlut);
        Assert.Equal(0x1160000, texture.TlutRomAddress);
        Assert.Equal(256, texture.EffectiveTlutColorCount);
    }

    [MajorasMaskRomFixtureFact("mm_us_n64_decompressed.z64")]
    public void MajorasMask_color_indexed_texture_palettes_cover_used_indices()
    {
        string romPath = LocalRomFixtures.GetRequiredMajorasMaskPath("mm_us_n64_decompressed.z64");

        byte[] rom = File.ReadAllBytes(romPath);
        RomVersionProfile profile = GetProfile("Majora's Mask NTSC-U");

        foreach (TextureDefinition texture in TextureCatalog.GetTextures(profile).Where(texture => texture.UsesTlut))
        {
            int maxIndex = GetMaxPaletteIndex(TextureRomService.ReadRaw(rom, texture), texture.Format);

            Assert.True(
                texture.EffectiveTlutColorCount > maxIndex,
                $"{profile.Name} {texture.Group}/{texture.Name} uses palette index {maxIndex}, but catalog only exposes {texture.EffectiveTlutColorCount} colors.");
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
