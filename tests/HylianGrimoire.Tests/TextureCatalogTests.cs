using System.Drawing;
using HylianGrimoire.Rom;
using HylianGrimoire.TextTextures;
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
    [InlineData("NTSC 1.0", 467)]
    [InlineData("NTSC 1.1", 467)]
    [InlineData("NTSC 1.2", 467)]
    [InlineData("NTSC Master Quest", 467)]
    [InlineData("NTSC GameCube", 467)]
    [InlineData("PAL 1.0", 489)]
    [InlineData("PAL 1.1", 489)]
    [InlineData("PAL Master Quest", 467)]
    [InlineData("PAL GameCube", 467)]
    [InlineData("NTSC iQue", 467)]
    [InlineData("PAL iQue", 467)]
    [InlineData("NTSC MQ iQue", 467)]
    [InlineData("PAL MQ iQue", 467)]
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
    [InlineData("NTSC 1.2", "gTitleTheLegendOfTextTex", 0x17b4700, TextureFormat.IA8, 72, 8)]
    [InlineData("PAL 1.0", "gAttackDoActionENGTex", 0x8a6000, TextureFormat.IA4, 48, 16)]
    [InlineData("NTSC iQue", "gFileSelSwitchENGTex", 0x1a2da80, TextureFormat.IA8, 48, 16)]
    [InlineData("PAL MQ iQue", "gFileSelSwitchENGTex", 0x1a31a80, TextureFormat.IA8, 48, 16)]
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
    public void Swedish_ique_profiles_share_texture_catalogs_by_variant()
    {
        IReadOnlyList<TextureDefinition> ntscIqueTextures = TextureCatalog.GetTextures(GetProfile("NTSC iQue"));
        IReadOnlyList<TextureDefinition> palIqueTextures = TextureCatalog.GetTextures(GetProfile("PAL iQue"));
        IReadOnlyList<TextureDefinition> ntscMqIqueTextures = TextureCatalog.GetTextures(GetProfile("NTSC MQ iQue"));
        IReadOnlyList<TextureDefinition> palMqIqueTextures = TextureCatalog.GetTextures(GetProfile("PAL MQ iQue"));

        Assert.Equal(ntscIqueTextures, palIqueTextures);
        Assert.Equal(ntscMqIqueTextures, palMqIqueTextures);
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", 112)]
    [InlineData("Retail PAL 1.0", 336)]
    [InlineData("NTSC 1.0", 112)]
    [InlineData("PAL 1.0", 112)]
    public void Ocarina_profiles_expose_item_name_texture_targets(string profileName, int expectedCount)
    {
        RomVersionProfile profile = GetProfile(profileName);

        IReadOnlyList<TextureDefinition> textures = ItemNameTextureCatalog.GetTargets(profile);

        Assert.Equal(expectedCount, textures.Count);
        Assert.All(textures, texture => Assert.True(ItemNameTextureCatalog.IsItemNameTexture(texture)));
        Assert.DoesNotContain(textures, texture => texture.Name.EndsWith("JPNTex", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("gBoleroOfFireItemNameENGTex", "Bolero of Fire")]
    [InlineData("gDinsFireItemNameENGTex", "Din's Fire (MP6)")]
    [InlineData("gFaroresWindItemNameENGTex", "Farore's Wind (MP6)")]
    [InlineData("gMaskofTruthItemNameENGTex", "Mask of Truth")]
    [InlineData("gNayrusLoveItemNameENGTex", "Nayru's Love (MP12)")]
    [InlineData("gBulletBag30ItemNameENGTex", "Bullet Bag (Holds 30)")]
    [InlineData("gQuiver30ItemNameENGTex", "Quiver (Holds 30)")]
    [InlineData("gSOLDOUTItemNameENGTex", "SOLD OUT")]
    public void Item_name_texture_names_can_be_converted_to_display_text(string textureName, string expectedText)
    {
        string displayText = ItemNameTextureCatalog.GetDisplayText(textureName);

        Assert.Equal(expectedText, displayText);
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", 10)]
    [InlineData("Retail PAL 1.0", 30)]
    [InlineData("NTSC 1.0", 10)]
    [InlineData("PAL 1.0", 10)]
    [InlineData("NTSC iQue", 10)]
    public void Ocarina_profiles_expose_pause_prompt_texture_targets(string profileName, int expectedCount)
    {
        RomVersionProfile profile = GetProfile(profileName);

        IReadOnlyList<TextureDefinition> textures = PausePromptTextureCatalog.GetTargets(profile);

        Assert.Equal(expectedCount, textures.Count);
        Assert.All(textures, texture => Assert.True(PausePromptTextureCatalog.IsPausePromptTexture(texture)));
    }

    [Theory]
    [InlineData("Retail PAL 1.0", "gPauseToSelectItemENGTex", "To Select Item", "English")]
    [InlineData("Retail PAL 1.0", "gPauseSavePromptENGTex", "Would you like to save?", "English")]
    [InlineData("Retail PAL 1.0", "gPauseSaveConfirmationENGTex", "Game saved.", "English")]
    [InlineData("Retail PAL 1.0", "gPauseYesENGTex", "Yes", "English")]
    [InlineData("Retail PAL 1.0", "gPauseNoENGTex", "No", "English")]
    [InlineData("Retail PAL 1.0", "gPauseToEquipFRATex", "pour \u00e9quiper", "French")]
    [InlineData("Retail PAL 1.0", "gPauseToEquipmentFRATex", "Equipement", "French")]
    [InlineData("Retail PAL 1.0", "gPauseSavePromptFRATex", "Voulez-vous sauvegarder ?", "French")]
    [InlineData("Retail PAL 1.0", "gPauseSaveConfirmationFRATex", "Jeu sauvegard\u00e9", "French")]
    [InlineData("Retail PAL 1.0", "gPauseYesFRATex", "Oui", "French")]
    [InlineData("Retail PAL 1.0", "gPauseNoFRATex", "Non", "French")]
    [InlineData("Retail PAL 1.0", "gPauseToEquipmentGERTex", "Ausr\u00fcstung", "German")]
    [InlineData("Retail PAL 1.0", "gPauseSavePromptGERTex", "Spielstand sichern ?", "German")]
    [InlineData("Retail PAL 1.0", "gPauseSaveConfirmationGERTex", "Spielstand gesichert.", "German")]
    [InlineData("Retail PAL 1.0", "gPauseYesGERTex", "Ja", "German")]
    [InlineData("Retail PAL 1.0", "gPauseNoGERTex", "Nein", "German")]
    public void Pause_prompt_texture_names_expose_display_text_and_language(
        string profileName,
        string textureName,
        string expectedText,
        string expectedLanguage)
    {
        TextureDefinition texture = PausePromptTextureCatalog.GetTargets(GetProfile(profileName))
            .Single(texture => texture.Name == textureName);

        Assert.Equal(expectedText, PausePromptTextureCatalog.GetDisplayText(texture));
        Assert.Equal(expectedLanguage, PausePromptTextureCatalog.GetLanguage(texture));
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", "gPauseSavePromptENGTex")]
    [InlineData("Retail NTSC 1.0", "gPauseSaveConfirmationENGTex")]
    [InlineData("Retail NTSC 1.0", "gPauseYesENGTex")]
    [InlineData("Retail NTSC 1.0", "gPauseNoENGTex")]
    [InlineData("Retail PAL 1.0", "gPauseSavePromptFRATex")]
    [InlineData("Retail PAL 1.0", "gPauseSaveConfirmationFRATex")]
    [InlineData("Retail PAL 1.0", "gPauseYesFRATex")]
    [InlineData("Retail PAL 1.0", "gPauseNoFRATex")]
    [InlineData("Retail PAL 1.0", "gPauseSavePromptGERTex")]
    [InlineData("Retail PAL 1.0", "gPauseSaveConfirmationGERTex")]
    [InlineData("Retail PAL 1.0", "gPauseYesGERTex")]
    [InlineData("Retail PAL 1.0", "gPauseNoGERTex")]
    public void Pause_prompt_choice_textures_use_choice_rendering_style(string profileName, string textureName)
    {
        TextureDefinition texture = PausePromptTextureCatalog.GetTargets(GetProfile(profileName))
            .Single(texture => texture.Name == textureName);

        Assert.Equal(PausePromptTextureStyle.Choice, PausePromptTextureCatalog.GetStyle(texture));
        Assert.True(PausePromptTextureCatalog.IsChoicePromptTexture(texture));
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", "gPauseSavePromptENGTex", true)]
    [InlineData("Retail NTSC 1.0", "gPauseSaveConfirmationENGTex", false)]
    [InlineData("Retail NTSC 1.0", "gPauseYesENGTex", false)]
    [InlineData("Retail NTSC 1.0", "gPauseNoENGTex", false)]
    [InlineData("Retail PAL 1.0", "gPauseSavePromptFRATex", true)]
    [InlineData("Retail PAL 1.0", "gPauseSaveConfirmationFRATex", false)]
    [InlineData("Retail PAL 1.0", "gPauseSavePromptGERTex", true)]
    [InlineData("Retail PAL 1.0", "gPauseSaveConfirmationGERTex", false)]
    public void Pause_prompt_save_textures_are_identified_separately(
        string profileName,
        string textureName,
        bool expected)
    {
        TextureDefinition texture = PausePromptTextureCatalog.GetTargets(GetProfile(profileName))
            .Single(texture => texture.Name == textureName);

        Assert.Equal(expected, PausePromptTextureCatalog.IsSavePromptTexture(texture));
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", "gPauseSaveConfirmationENGTex", true)]
    [InlineData("Retail NTSC 1.0", "gPauseSavePromptENGTex", false)]
    [InlineData("Retail NTSC 1.0", "gPauseYesENGTex", false)]
    [InlineData("Retail PAL 1.0", "gPauseSaveConfirmationFRATex", true)]
    [InlineData("Retail PAL 1.0", "gPauseSaveConfirmationGERTex", true)]
    public void Pause_prompt_save_confirmation_texture_is_identified_separately(
        string profileName,
        string textureName,
        bool expected)
    {
        TextureDefinition texture = PausePromptTextureCatalog.GetTargets(GetProfile(profileName))
            .Single(texture => texture.Name == textureName);

        Assert.Equal(expected, PausePromptTextureCatalog.IsSaveConfirmationTexture(texture));
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", 9)]
    [InlineData("Retail PAL 1.0", 27)]
    [InlineData("NTSC 1.0", 9)]
    [InlineData("PAL 1.0", 9)]
    [InlineData("NTSC iQue", 9)]
    public void Ocarina_profiles_expose_dungeon_map_name_texture_targets(string profileName, int expectedCount)
    {
        RomVersionProfile profile = GetProfile(profileName);

        IReadOnlyList<TextureDefinition> textures = DungeonMapNameTextureCatalog.GetTargets(profile);

        Assert.Equal(expectedCount, textures.Count);
        Assert.All(textures, texture => Assert.True(DungeonMapNameTextureCatalog.IsDungeonMapNameTexture(texture)));
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", "gPauseBotWTitleENGTex", "Bottom of the Well", "English")]
    [InlineData("Retail NTSC 1.0", "gPauseDekuTitleENGTex", "Inside the Deku Tree", "English")]
    [InlineData("Retail NTSC 1.0", "gPauseDodongoTitleENGTex", "Dodongo's Cavern", "English")]
    [InlineData("Retail NTSC 1.0", "gPauseFireTitleENGTex", "Fire Temple", "English")]
    [InlineData("Retail NTSC 1.0", "gPauseIceCavernTitleENGTex", "Ice Cavern", "English")]
    [InlineData("Retail NTSC 1.0", "gPauseJabuTitleENGTex", "Inside Jabu-Jabu's Belly", "English")]
    [InlineData("Retail NTSC 1.0", "gPauseShadowTitleENGTex", "Shadow Temple", "English")]
    [InlineData("Retail NTSC 1.0", "gPauseSpiritTitleENGTex", "Spirit Temple", "English")]
    [InlineData("Retail NTSC 1.0", "gPauseWaterTitleENGTex", "Water Temple", "English")]
    [InlineData("Retail PAL 1.0", "gPauseBotWTitleFRATex", "Puits", "French")]
    [InlineData("Retail PAL 1.0", "gPauseDekuTitleFRATex", "Arbre Mojo", "French")]
    [InlineData("Retail PAL 1.0", "gPauseDodongoTitleFRATex", "Caverne Dodongo", "French")]
    [InlineData("Retail PAL 1.0", "gPauseFireTitleFRATex", "Temple du Feu", "French")]
    [InlineData("Retail PAL 1.0", "gPauseIceCavernTitleFRATex", "Caverne Polaire", "French")]
    [InlineData("Retail PAL 1.0", "gPauseJabuTitleFRATex", "Ventre de Jabu-Jabu", "French")]
    [InlineData("Retail PAL 1.0", "gPauseShadowTitleFRATex", "Temple de l'Ombre", "French")]
    [InlineData("Retail PAL 1.0", "gPauseSpiritTitleFRATex", "Temple de l'Esprit", "French")]
    [InlineData("Retail PAL 1.0", "gPauseWaterTitleFRATex", "Temple de l'Eau", "French")]
    [InlineData("Retail PAL 1.0", "gPauseBotWTitleGERTex", "Grund des Brunnens", "German")]
    [InlineData("Retail PAL 1.0", "gPauseDekuTitleGERTex", "Im Deku-Baum", "German")]
    [InlineData("Retail PAL 1.0", "gPauseDodongoTitleGERTex", "Dodongos H\u00f6hle", "German")]
    [InlineData("Retail PAL 1.0", "gPauseFireTitleGERTex", "Feuertempel", "German")]
    [InlineData("Retail PAL 1.0", "gPauseIceCavernTitleGERTex", "Eish\u00f6hle", "German")]
    [InlineData("Retail PAL 1.0", "gPauseJabuTitleGERTex", "Jabu-Jabus Bauch", "German")]
    [InlineData("Retail PAL 1.0", "gPauseShadowTitleGERTex", "Schattentempel", "German")]
    [InlineData("Retail PAL 1.0", "gPauseSpiritTitleGERTex", "Geistertempel", "German")]
    [InlineData("Retail PAL 1.0", "gPauseWaterTitleGERTex", "Wassertempel", "German")]
    public void Dungeon_map_name_texture_names_expose_display_text_and_language(
        string profileName,
        string textureName,
        string expectedText,
        string expectedLanguage)
    {
        TextureDefinition texture = DungeonMapNameTextureCatalog.GetTargets(GetProfile(profileName))
            .Single(texture => texture.Name == textureName);

        Assert.Equal(expectedText, DungeonMapNameTextureCatalog.GetDisplayText(texture));
        Assert.Equal(expectedLanguage, DungeonMapNameTextureCatalog.GetLanguage(texture));
        Assert.Equal(96, texture.Width);
        Assert.Equal(16, texture.Height);
        Assert.Equal(TextureFormat.IA8, texture.Format);
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", 17)]
    [InlineData("Retail PAL 1.0", 46)]
    [InlineData("Retail PAL Master Quest", 46)]
    [InlineData("Retail PAL GameCube", 46)]
    [InlineData("NTSC 1.0", 17)]
    [InlineData("PAL 1.0", 17)]
    [InlineData("NTSC iQue", 17)]
    public void Ocarina_profiles_expose_file_select_texture_targets(string profileName, int expectedCount)
    {
        RomVersionProfile profile = GetProfile(profileName);

        IReadOnlyList<TextureDefinition> textures = FileSelectTextureCatalog.GetTargets(profile);

        Assert.Equal(expectedCount, textures.Count);
        Assert.All(textures, texture => Assert.True(FileSelectTextureCatalog.IsFileSelectTexture(texture)));
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", "gFileSelAreYouSureENGTex", "Are you sure?", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelAreYouSure2ENGTex", "Are you sure?", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelCopyWhichFileENGTex", "Copy which file?", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelCopyToWhichFileENGTex", "Copy to which file?", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelEraseWhichFileENGTex", "Erase which file?", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelFileCopiedENGTex", "File copied.", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelFileEmptyENGTex", "This is an empty file.", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelFileErasedENGTex", "File erased.", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelFileInUseENGTex", "This file is in use.", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelNameENGTex", "Name?", "English", 56)]
    [InlineData("Retail NTSC 1.0", "gFileSelNoEmptyFileENGTex", "There is no empty file.", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelNoFileToCopyENGTex", "No file to copy.", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelNoFileToEraseENGTex", "No file to erase.", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelOpenThisFileENGTex", "Open this file?", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelOptionsENGTex", "Options", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelPleaseSelectAFileENGTex", "Please select a file.", "English", 128)]
    [InlineData("Retail NTSC 1.0", "gFileSelControlsENGTex", "A-Decide \u2022 B-Cancel", "English", 144)]
    [InlineData("Retail PAL 1.0", "gFileSelAreYouSure2FRATex", "Etes-vous s\u00fbr?", "French", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelCopyWhichFileFRATex", "Copier quel fichier?", "French", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelCopyToWhichFileFRATex", "Copier sur quel fichier?", "French", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelEraseWhichFileFRATex", "Effacer quel fichier?", "French", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelFileCopiedFRATex", "Fichier copi\u00e9", "French", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelFileEmptyFRATex", "Ce fichier est vide", "French", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelFileErasedFRATex", "Fichier effac\u00e9", "French", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelFileInUseFRATex", "Ce fichier est utilis\u00e9", "French", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelNameFRATex", "Nom?", "French", 56)]
    [InlineData("Retail PAL 1.0", "gFileSelNoEmptyFileFRATex", "Aucun fichier vide", "French", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelNoFileToCopyFRATex", "Aucun fichier \u00e0 copier", "French", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelNoFileToEraseFRATex", "Aucun fichier \u00e0 effacer", "French", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelOpenThisFileFRATex", "Ouvrir ce fichier?", "French", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelPleaseSelectAFileFRATex", "Veuillez choisir un fichier", "French", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelControlsFRATex", "A-Valider \u2022 B-Annuler", "French", 144)]
    [InlineData("Retail PAL 1.0", "gFileSelAreYouSure2GERTex", "Sicher?", "German", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelCopyToWhichFileGERTex", "Wohin kopieren?", "German", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelFileCopiedGERTex", "Datei kopiert.", "German", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelFileEmptyGERTex", "Datei ist leer !", "German", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelFileErasedGERTex", "Datei gel\u00f6scht.", "German", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelFileInUseGERTex", "Datei ist belegt !", "German", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelNameGERTex", "Name?", "German", 56)]
    [InlineData("Retail PAL 1.0", "gFileSelNoEmptyFileGERTex", "Keine leere Datei !", "German", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelNoFileToCopyGERTex", "Keine Datei vorhanden.", "German", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelNoFileToEraseGERTex", "Keine Datei vorhanden.", "German", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelOpenThisFileGERTex", "Datei \u00f6ffnen?", "German", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelOptionsGERTex", "Optionen", "German", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelPleaseSelectAFileGERTex", "Datei w\u00e4hlen.", "German", 128)]
    [InlineData("Retail PAL 1.0", "gFileSelControlsGERTex", "A-Eingabe \u2022 B-Zur\u00fcck", "German", 144)]
    public void File_select_texture_names_expose_display_text_and_language(
        string profileName,
        string textureName,
        string expectedText,
        string expectedLanguage,
        int expectedWidth)
    {
        TextureDefinition texture = FileSelectTextureCatalog.GetTargets(GetProfile(profileName))
            .Single(texture => texture.Name == textureName);

        Assert.Equal(expectedText, FileSelectTextureCatalog.GetDisplayText(texture));
        Assert.Equal(expectedLanguage, FileSelectTextureCatalog.GetLanguage(texture));
        Assert.Equal(expectedWidth, texture.Width);
        Assert.Equal(16, texture.Height);
        Assert.Equal(TextureFormat.IA8, texture.Format);
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", "gFileSelAreYouSureENGTex", FileSelectTexturePreset.ZeroX)]
    [InlineData("Retail NTSC 1.0", "gFileSelFileEmptyENGTex", FileSelectTexturePreset.FileEmpty)]
    [InlineData("Retail NTSC 1.0", "gFileSelNameENGTex", FileSelectTexturePreset.Name)]
    [InlineData("Retail NTSC 1.0", "gFileSelOptionsENGTex", FileSelectTexturePreset.Options)]
    [InlineData("Retail NTSC 1.0", "gFileSelControlsENGTex", FileSelectTexturePreset.Controls)]
    public void File_select_textures_expose_settings_preset(
        string profileName,
        string textureName,
        FileSelectTexturePreset expectedPreset)
    {
        TextureDefinition texture = FileSelectTextureCatalog.GetTargets(GetProfile(profileName))
            .Single(texture => texture.Name == textureName);

        Assert.Equal(expectedPreset, FileSelectTextureCatalog.GetPreset(texture));
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", "gFileSelControlsENGTex", true)]
    [InlineData("Retail PAL 1.0", "gFileSelControlsFRATex", true)]
    [InlineData("Retail PAL 1.0", "gFileSelControlsGERTex", true)]
    [InlineData("Retail PAL 1.0", "gFileSelAreYouSure2FRATex", false)]
    public void File_select_controls_textures_are_identified_separately(
        string profileName,
        string textureName,
        bool expected)
    {
        TextureDefinition texture = FileSelectTextureCatalog.GetTargets(GetProfile(profileName))
            .Single(texture => texture.Name == textureName);

        Assert.Equal(expected, FileSelectTextureCatalog.IsControlsTexture(texture));
    }

    [Theory]
    [InlineData("Retail NTSC 1.0")]
    [InlineData("Retail PAL 1.0")]
    [InlineData("NTSC 1.0")]
    [InlineData("PAL 1.0")]
    [InlineData("NTSC iQue")]
    public void Ocarina_profiles_expose_supported_end_title_texture_targets(string profileName)
    {
        RomVersionProfile profile = GetProfile(profileName);

        IReadOnlyList<TextureDefinition> textures = EndTitleTextureCatalog.GetTargets(profile);

        Assert.Equal(4, textures.Count);
        TextureDefinition ocarina = textures.Single(texture => texture.Name == "sOcarinaOfTimeTex");
        Assert.Equal("\u2013Ocarina of Time\u2122\u2013", EndTitleTextureCatalog.GetDisplayText(ocarina));
        Assert.Equal(112, ocarina.Width);
        Assert.Equal(16, ocarina.Height);
        Assert.True(EndTitleTextureCatalog.IsEndTitleTexture(ocarina));

        TextureDefinition legend = textures.Single(texture => texture.Name == "sTheLegendOfZeldaTex");
        Assert.Equal("The Legend of ZELDA", EndTitleTextureCatalog.GetDisplayText(legend));
        Assert.Equal(120, legend.Width);
        Assert.Equal(24, legend.Height);
        Assert.True(EndTitleTextureCatalog.IsEndTitleTexture(legend));

        TextureDefinition presentedBy = textures.Single(texture => texture.Name == "sPresentedByTex");
        Assert.Equal("PRESENTED BY", EndTitleTextureCatalog.GetDisplayText(presentedBy));
        Assert.Equal(96, presentedBy.Width);
        Assert.Equal(16, presentedBy.Height);
        Assert.True(EndTitleTextureCatalog.IsEndTitleTexture(presentedBy));

        TextureDefinition theEnd = textures.Single(texture => texture.Name == "sTheEndTex");
        Assert.Equal("The End", EndTitleTextureCatalog.GetDisplayText(theEnd));
        Assert.Equal(80, theEnd.Width);
        Assert.Equal(24, theEnd.Height);
        Assert.True(EndTitleTextureCatalog.IsEndTitleTexture(theEnd));
    }

    [Fact]
    public void End_title_texture_catalog_excludes_nintendo_logo_halves()
    {
        IReadOnlyList<TextureDefinition> textures = TextureCatalog.GetTextures(GetProfile("Retail NTSC 1.0"));

        TextureDefinition leftNintendo = textures.Single(texture => texture.Name == "sNintendoLeftTex");
        TextureDefinition rightNintendo = textures.Single(texture => texture.Name == "sNintendoRightTex");

        Assert.False(EndTitleTextureCatalog.IsEndTitleTexture(leftNintendo));
        Assert.False(EndTitleTextureCatalog.IsEndTitleTexture(rightNintendo));
    }

    [Fact]
    public void End_title_ocarina_defaults_match_asset_layout()
    {
        EndTitleTextureRenderSettings settings = new();

        Assert.Equal(14.5, settings.PrefixX);
        Assert.Equal(9.0, settings.PrefixY);
        Assert.Equal(89.0, settings.TmX);
        Assert.Equal(92.4, settings.SuffixX);
        Assert.Equal(9.0, settings.SuffixY);
        Assert.Equal(1.1, settings.OcarinaAssetOutlineWidth);
        Assert.Equal(100, settings.OcarinaAssetOutlinePercent);
        Assert.Equal(100, settings.OcarinaAssetShadowPercent);
    }

    [Theory]
    [InlineData("- Ocarina of Time TM -", "-", "Ocarina of Time", "TM", "-")]
    [InlineData("\u2013 Ocarina of Time\u2122 \u2013", "\u2013", "Ocarina of Time", "TM", "\u2013")]
    [InlineData("Ocarina of Time", "", "Ocarina of Time", "", "")]
    public void End_title_text_parts_parse_ocarina_segments(
        string text,
        string expectedPrefix,
        string expectedTitle,
        string expectedTm,
        string expectedSuffix)
    {
        EndTitleTextParts parts = EndTitleTextParts.Parse(text);

        Assert.Equal(expectedPrefix, parts.Prefix);
        Assert.Equal(expectedTitle, parts.Title);
        Assert.Equal(expectedTm, parts.Tm);
        Assert.Equal(expectedSuffix, parts.Suffix);
    }

    [Fact]
    public void End_title_ocarina_renderer_uses_stamp_assets_for_tm_and_ornaments()
    {
        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (!File.Exists(fontPath))
        {
            return;
        }

        string? tmPath = FindRepositoryFile(Path.Combine("src", "HylianGrimoire", "Assets", "Misc", "TM.png"));
        string? ornamentPath = FindRepositoryFile(Path.Combine("src", "HylianGrimoire", "Assets", "Misc", "-.png"));
        if (tmPath is null || ornamentPath is null)
        {
            return;
        }

        EndTitleTextureSpec spec = EndTitleTextureCatalog.Specs.Single(spec => spec.TextureName == "sOcarinaOfTimeTex");
        EndTitleTextureRenderSettings settings = new();
        EndTitleTextureAssets assets = new(OcarinaTmPath: tmPath, OcarinaOrnamentPath: ornamentPath);
        using Bitmap withAssets = EndTitleTextureRenderer.Render(
            "\u2013",
            "Ocarina of Time",
            "TM",
            "\u2013",
            fontPath,
            spec,
            settings,
            spec.Width,
            spec.Height,
            assets);
        using Bitmap textFallback = EndTitleTextureRenderer.Render(
            "\u2013",
            "Ocarina of Time",
            "TM",
            "\u2013",
            fontPath,
            spec,
            settings,
            spec.Width,
            spec.Height);

        Assert.False(GetAlphaBounds(withAssets).IsEmpty);
        Assert.True(CountNonTransparentPixels(withAssets, new Rectangle(12, 6, 16, 8)) > 0);
        Assert.True(CountNonTransparentPixels(withAssets, new Rectangle(88, 1, 22, 14)) > 0);
        Assert.True(ContainsPixel(withAssets, new Rectangle(12, 6, 16, 8), IsColoredAssetPixel));
        Assert.True(ContainsPixel(withAssets, new Rectangle(88, 1, 22, 14), IsColoredAssetPixel));
        Assert.True(ContainsPixel(withAssets, new Rectangle(12, 6, 16, 8), IsBlackPixel));
        Assert.True(ContainsPixel(withAssets, new Rectangle(88, 1, 22, 14), IsBlackPixel));
        using Bitmap ornament = new(ornamentPath);
        using Bitmap tm = new(tmPath);
        Assert.Equal(
            ornament.GetPixel(0, 0).ToArgb(),
            withAssets.GetPixel((int)Math.Round(settings.PrefixX), (int)Math.Round(settings.PrefixY)).ToArgb());
        Assert.Equal(
            tm.GetPixel(0, 0).ToArgb(),
            withAssets.GetPixel((int)Math.Round(settings.TmX), (int)Math.Round(settings.TmY)).ToArgb());
        Assert.True(BitmapsHaveDifferentPixels(withAssets, textFallback));
    }

    [Fact]
    public void End_title_ocarina_asset_outline_settings_affect_stamp_outline()
    {
        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (!File.Exists(fontPath))
        {
            return;
        }

        string? tmPath = FindRepositoryFile(Path.Combine("src", "HylianGrimoire", "Assets", "Misc", "TM.png"));
        string? ornamentPath = FindRepositoryFile(Path.Combine("src", "HylianGrimoire", "Assets", "Misc", "-.png"));
        if (tmPath is null || ornamentPath is null)
        {
            return;
        }

        EndTitleTextureSpec spec = EndTitleTextureCatalog.Specs.Single(spec => spec.TextureName == "sOcarinaOfTimeTex");
        EndTitleTextureAssets assets = new(OcarinaTmPath: tmPath, OcarinaOrnamentPath: ornamentPath);
        using Bitmap defaultOutline = EndTitleTextureRenderer.Render(
            "\u2013",
            "Ocarina of Time",
            "TM",
            "\u2013",
            fontPath,
            spec,
            new EndTitleTextureRenderSettings(),
            spec.Width,
            spec.Height,
            assets);
        using Bitmap disabledOutline = EndTitleTextureRenderer.Render(
            "\u2013",
            "Ocarina of Time",
            "TM",
            "\u2013",
            fontPath,
            spec,
            new EndTitleTextureRenderSettings(OcarinaAssetOutlinePercent: 0, OcarinaAssetShadowPercent: 0),
            spec.Width,
            spec.Height,
            assets);
        using Bitmap widerOutline = EndTitleTextureRenderer.Render(
            "\u2013",
            "Ocarina of Time",
            "TM",
            "\u2013",
            fontPath,
            spec,
            new EndTitleTextureRenderSettings(OcarinaAssetOutlineWidth: 5.0),
            spec.Width,
            spec.Height,
            assets);

        Rectangle prefixArea = new(12, 6, 16, 8);
        Assert.True(CountPixels(defaultOutline, prefixArea, IsBlackPixel) > CountPixels(disabledOutline, prefixArea, IsBlackPixel));
        Assert.True(CountPixels(widerOutline, prefixArea, IsBlackPixel) > CountPixels(defaultOutline, prefixArea, IsBlackPixel));
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", 56)]
    [InlineData("Retail PAL 1.0", 56)]
    [InlineData("NTSC 1.0", 56)]
    [InlineData("PAL 1.0", 56)]
    [InlineData("NTSC iQue", 56)]
    public void Ocarina_profiles_expose_place_titlecard_texture_targets(string profileName, int expectedCount)
    {
        RomVersionProfile profile = GetProfile(profileName);

        IReadOnlyList<TextureDefinition> textures = PlaceTitleCardTextureCatalog.GetTargets(profile);

        Assert.Equal(expectedCount, textures.Count);
        Assert.All(textures, texture => Assert.True(PlaceTitleCardTextureCatalog.IsPlaceTitleCardTexture(texture)));
    }

    [Theory]
    [InlineData("gForestTempleTitleCardENGTex", "Forest Temple")]
    [InlineData("gDekuTreeTitleCardENGTex", "Inside the Deku Tree")]
    [InlineData("gJabuJabuTitleCardENGTex", "Inside Jabu-Jabu's Belly")]
    [InlineData("gDodongosCavernTitleCardENGTex", "Dodongo's Cavern")]
    [InlineData("gGravekeepersHutTitleCardENGTex", "Gravekeeper's Hut")]
    [InlineData("gZorasDomainTitleCardENGTex", "Zora's Domain")]
    [InlineData("gZorasFountainTitleCardENGTex", "Zora's Fountain")]
    [InlineData("gGanonsCastleTitleCardENGTex", "Ganon's Castle")]
    [InlineData("gGERudoValleyTitleCardENGTex", "Gerudo Valley")]
    [InlineData("gGERudosFortressTitleCardENGTex", "Gerudo's Fortress")]
    [InlineData("gThievesHideoutTitleCardENGTex", "Thieves' Hideout")]
    [InlineData("gQuestionMarkTitleCardENGTex", "?")]
    public void Place_titlecard_texture_names_can_be_converted_to_display_text(string textureName, string expectedText)
    {
        string displayText = PlaceTitleCardTextureCatalog.GetDisplayText(textureName);

        Assert.Equal(expectedText, displayText);
    }

    [Fact]
    public void Place_titlecard_height_scale_compresses_rendered_text_when_font_available()
    {
        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (!File.Exists(fontPath))
        {
            return;
        }

        using Bitmap normal = PlaceTitleCardTextureRenderer.Render(
            "Forest Temple",
            fontPath,
            new PlaceTitleCardTextureRenderSettings(HeightScale: 100.0));
        using Bitmap compressed = PlaceTitleCardTextureRenderer.Render(
            "Forest Temple",
            fontPath,
            new PlaceTitleCardTextureRenderSettings(HeightScale: 70.0));

        Assert.True(GetAlphaBounds(compressed).Height < GetAlphaBounds(normal).Height);
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", 10)]
    [InlineData("Retail PAL 1.0", 10)]
    [InlineData("NTSC 1.0", 10)]
    [InlineData("PAL 1.0", 10)]
    [InlineData("NTSC iQue", 10)]
    public void Ocarina_profiles_expose_boss_titlecard_texture_targets(string profileName, int expectedCount)
    {
        RomVersionProfile profile = GetProfile(profileName);

        IReadOnlyList<TextureDefinition> textures = BossTitleCardTextureCatalog.GetTargets(profile);

        Assert.Equal(expectedCount, textures.Count);
        Assert.All(textures, texture => Assert.True(BossTitleCardTextureCatalog.IsBossTitleCardTexture(texture)));
    }

    [Theory]
    [InlineData("gVolvagiaBossTitleCardENGTex", "Subterranean Lava Dragon", "VOLVAGIA")]
    [InlineData("gPhantomGanonTitleCardENGTex", "Evil Spirit from Beyond", "PHANTOM GANON")]
    [InlineData("gGanondorfTitleCardENGTex", "Great King of Evil", "GANONDORF")]
    [InlineData("gGanonTitleCardENGTex", "", "GANON")]
    [InlineData("gKingDodongoTitleCardENGTex", "Infernal Dinosaur", "KING DODONGO")]
    public void Boss_titlecard_texture_names_can_be_converted_to_display_text(
        string textureName,
        string expectedTopText,
        string expectedBossText)
    {
        BossTitleCardText displayText = BossTitleCardTextureCatalog.GetDisplayText(textureName);

        Assert.Equal(expectedTopText, displayText.TopText);
        Assert.Equal(expectedBossText, displayText.BossText);
    }

    [Fact]
    public void Boss_titlecard_renderer_outputs_texture_when_fonts_available()
    {
        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (!File.Exists(fontPath))
        {
            return;
        }

        using Bitmap rendered = BossTitleCardTextureRenderer.Render(
            "Great King of Evil",
            "GANONDORF",
            fontPath,
            fontPath,
            new BossTitleCardTextureRenderSettings());

        Assert.Equal(BossTitleCardTextureCatalog.Width, rendered.Width);
        Assert.Equal(BossTitleCardTextureCatalog.Height, rendered.Height);
        Assert.False(GetAlphaBounds(rendered).IsEmpty);
    }

    [Theory]
    [InlineData("Retail NTSC 1.0")]
    [InlineData("Retail PAL 1.0")]
    [InlineData("NTSC 1.0")]
    [InlineData("PAL 1.0")]
    [InlineData("NTSC iQue")]
    public void Ocarina_profiles_expose_complete_game_over_texture_target(string profileName)
    {
        RomVersionProfile profile = GetProfile(profileName);

        IReadOnlyList<GameOverTextureTarget> targets = GameOverTextureCatalog.GetTargets(profile);

        Assert.Equal(2, targets.Count);
        GameOverTextureTarget target = targets.Single(target => target.Spec.Kind == GameOverTextureTargetKind.GameOver);
        Assert.Equal("GAME OVER", target.Spec.SampleText);
        Assert.Equal(["gGameOverP1Tex", "gGameOverP2Tex", "gGameOverP3Tex"], target.Textures.Select(texture => texture.Name));
        Assert.All(target.Textures, texture => Assert.True(GameOverTextureCatalog.IsGameOverTripletTexture(texture)));

        GameOverTextureTarget continuePlaying = targets.Single(target => target.Spec.Kind == GameOverTextureTargetKind.ContinuePlaying);
        Assert.Equal("Continue playing ?", continuePlaying.Spec.SampleText);
        Assert.Equal(["gContinuePlayingENGTex"], continuePlaying.Textures.Select(texture => texture.Name));
        Assert.True(GameOverTextureCatalog.IsContinuePlayingTexture(continuePlaying.Texture));
    }

    [Fact]
    public void Continue_playing_renderer_outputs_single_texture_when_font_available()
    {
        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (!File.Exists(fontPath))
        {
            return;
        }

        using Bitmap rendered = ContinuePlayingTextureRenderer.Render("Continue playing ?", fontPath, new ContinuePlayingTextureRenderSettings());

        Assert.Equal(GameOverTextureCatalog.ContinuePlayingWidth, rendered.Width);
        Assert.Equal(GameOverTextureCatalog.ContinuePlayingHeight, rendered.Height);
        Assert.False(GetAlphaBounds(rendered).IsEmpty);
    }

    [Fact]
    public void Continue_playing_center_only_controls_horizontal_position()
    {
        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (!File.Exists(fontPath))
        {
            return;
        }

        using Bitmap high = ContinuePlayingTextureRenderer.Render(
            "Continue playing ?",
            fontPath,
            new ContinuePlayingTextureRenderSettings(Center: true, YNudge: -2));
        using Bitmap low = ContinuePlayingTextureRenderer.Render(
            "Continue playing ?",
            fontPath,
            new ContinuePlayingTextureRenderSettings(Center: true, YNudge: 2));

        Assert.True(GetAlphaBounds(low).Top > GetAlphaBounds(high).Top);
    }

    [Theory]
    [InlineData('A', true)]
    [InlineData('V', true)]
    [InlineData('Z', true)]
    [InlineData('\u00dc', true)]
    [InlineData('-', true)]
    [InlineData('a', true)]
    [InlineData('\u00fc', true)]
    [InlineData(' ', false)]
    [InlineData('\u2022', false)]
    public void File_select_controls_bold_font_rule_applies_to_letters(char character, bool expected)
        => Assert.Equal(expected, FileSelectControlsTextRunBuilder.UsesBoldFont(character));

    [Fact]
    public void File_select_controls_text_runs_use_bold_font_for_every_letter()
    {
        TextTextureFont regular = new("regular.ttf");
        TextTextureFont bold = new("bold.ttf", null, FontStyle.Bold);

        IReadOnlyList<CompactTextTextureTextRun> runs = FileSelectControlsTextRunBuilder.Create(
            "A-Valider \u2022 B-Zur\u00fcck",
            regular,
            bold,
            235,
            5.2);

        Assert.All(
            "A-ValiderBZur\u00fcck".Where(char.IsLetter).Distinct(),
            character => Assert.Contains(runs, run => run.Font == bold && run.Text.Contains(character)));
        Assert.DoesNotContain(runs, run => run.Font == bold && run.Text.Contains('\u2022'));
        Assert.DoesNotContain(runs, run => run.Font == bold && run.Text.Contains(' '));
    }

    [Fact]
    public void Compact_text_renderer_scales_and_offsets_bullet_runs_without_font_glyphs()
    {
        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (!File.Exists(fontPath))
        {
            return;
        }

        TextTextureFont regular = TextTextureFont.FromPath(fontPath);
        TextTextureFont bold = new(fontPath, null, FontStyle.Bold);
        IReadOnlyList<CompactTextTextureTextRun> runs = FileSelectControlsTextRunBuilder.Create(
            "A-Decide \u2022 B-Cancel",
            regular,
            bold,
            235,
            5.2);

        using Bitmap rendered = CompactTextTextureRenderer.Render(
            runs,
            new CompactTextTextureRenderSettings(
                FontSize: 10.4,
                StrokeWidth: 3.40,
                StrokeAlpha: 112,
                StrokeBlurRadius: 1.0,
                StrokeBlurStrength: 126,
                XNudge: 0,
                YOffset: 0,
                HorizontalScale: 86.5,
                FillThreshold: 24,
                WhiteThreshold: 155,
                FillFloor: 17,
                FillBoost: 125,
                Center: true,
                MaxWidth: 144,
                BlendFillAndStrokeEdges: true,
                FillStrokeWidth: 0.50,
                CharacterSpacing: 1.15),
            144,
            16);

        Assert.False(GetAlphaBounds(rendered).IsEmpty);
    }

    [Fact]
    public void Compact_text_renderer_can_blend_fill_and_stroke_edges_when_font_available()
    {
        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (!File.Exists(fontPath))
        {
            return;
        }

        TextTextureFont regular = TextTextureFont.FromPath(fontPath);
        TextTextureFont bold = new(fontPath, null, FontStyle.Bold);
        IReadOnlyList<CompactTextTextureTextRun> runs = FileSelectControlsTextRunBuilder.Create(
            "A-Eingabe \u2022 B-Zur\u00fcck",
            regular,
            bold,
            235,
            5.2);
        var settings = new CompactTextTextureRenderSettings(
            FontSize: 10.8,
            StrokeWidth: 2.9,
            StrokeAlpha: 119,
            XNudge: 11,
            YOffset: 0,
            HorizontalScale: 98.5,
            Center: false,
            MaxWidth: 144);

        using Bitmap unblended = CompactTextTextureRenderer.Render(runs, settings, 144, 16);
        using Bitmap blended = CompactTextTextureRenderer.Render(
            runs,
            settings with { BlendFillAndStrokeEdges = true },
            144,
            16);

        Assert.True(BitmapsHaveDifferentPixels(unblended, blended));
    }

    [Fact]
    public void Compact_text_renderer_can_expand_fill_mask_without_changing_font_when_font_available()
    {
        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (!File.Exists(fontPath))
        {
            return;
        }

        var settings = new CompactTextTextureRenderSettings(
            FontSize: 12,
            StrokeWidth: 0,
            StrokeAlpha: 0,
            XNudge: 2,
            BaselineY: 13,
            FillThreshold: 1,
            WhiteThreshold: 255,
            FillFloor: 0,
            FillBoost: 100,
            Center: false,
            FitToWidth: false,
            MaxWidth: 64);

        using Bitmap normal = CompactTextTextureRenderer.Render("Aa", fontPath, settings, 64, 18);
        using Bitmap expanded = CompactTextTextureRenderer.Render(
            "Aa",
            fontPath,
            settings with { FillStrokeWidth = 0.7 },
            64,
            18);

        Assert.True(CountNonTransparentPixels(expanded) > CountNonTransparentPixels(normal));
    }

    [Fact]
    public void Compact_text_renderer_can_add_character_spacing_when_font_available()
    {
        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (!File.Exists(fontPath))
        {
            return;
        }

        var settings = new CompactTextTextureRenderSettings(
            FontSize: 12,
            StrokeWidth: 0,
            StrokeAlpha: 0,
            XNudge: 2,
            BaselineY: 13,
            FillThreshold: 1,
            WhiteThreshold: 255,
            FillFloor: 0,
            FillBoost: 100,
            Center: false,
            FitToWidth: false,
            MaxWidth: 96);

        using Bitmap normal = CompactTextTextureRenderer.Render("AAAA", fontPath, settings, 96, 18);
        using Bitmap spaced = CompactTextTextureRenderer.Render(
            "AAAA",
            fontPath,
            settings with { CharacterSpacing = 1.5 },
            96,
            18);

        Assert.True(GetAlphaBounds(spaced).Width > GetAlphaBounds(normal).Width);
    }

    [Fact]
    public void Game_over_renderer_outputs_full_canvas_and_split_parts_when_font_available()
    {
        string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (!File.Exists(fontPath))
        {
            return;
        }

        using Bitmap rendered = GameOverTextureRenderer.Render("GAME OVER", fontPath, new GameOverTextureRenderSettings());
        IReadOnlyList<Bitmap> parts = GameOverTextureRenderer.SplitTriplet(rendered);
        try
        {
            Assert.Equal(GameOverTextureCatalog.Width, rendered.Width);
            Assert.Equal(GameOverTextureCatalog.Height, rendered.Height);
            Assert.All(parts, part =>
            {
                Assert.Equal(GameOverTextureCatalog.TileWidth, part.Width);
                Assert.Equal(GameOverTextureCatalog.TileHeight, part.Height);
            });
            Assert.False(GetAlphaBounds(rendered).IsEmpty);
        }
        finally
        {
            foreach (Bitmap part in parts)
            {
                part.Dispose();
            }
        }
    }

    [Fact]
    public void Game_over_render_settings_keep_curated_stroke_defaults_hidden_from_ui()
    {
        GameOverTextureRenderSettings settings = new();

        Assert.True(settings.Center);
        Assert.Equal(5.0, settings.StrokeWidth);
        Assert.Equal(91, settings.StrokeAlpha);
        Assert.Equal(GameOverTextureRenderSettings.CenteredXNudge, settings.XNudge);
        Assert.Equal(GameOverTextureRenderSettings.DefaultY, settings.Y);
    }

    [Fact]
    public void Continue_playing_render_settings_keep_curated_defaults_hidden_from_ui()
    {
        ContinuePlayingTextureRenderSettings settings = new();

        Assert.True(settings.Center);
        Assert.Equal(13.7, settings.FontSize);
        Assert.Equal(5.3, settings.StrokeWidth);
        Assert.Equal(134, settings.StrokeAlpha);
        Assert.Equal(1.5, settings.StrokeGamma);
        Assert.Equal(0.85, settings.BlurRadius);
        Assert.Equal(90, settings.BlurStrength);
        Assert.Equal(0.6, settings.YNudge);
        Assert.Equal(-1.1, settings.Tracking);
        Assert.Equal(0.7, settings.GlyphGap);
        Assert.Equal(92.0, settings.WidthScale);
        Assert.Equal(84.0, settings.HeightScale);
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", 5)]
    [InlineData("Retail PAL 1.0", 5)]
    [InlineData("NTSC 1.0", 3)]
    [InlineData("PAL 1.0", 3)]
    [InlineData("NTSC iQue", 3)]
    public void Ocarina_profiles_expose_complete_pause_header_texture_targets(string profileName, int expectedCount)
    {
        RomVersionProfile profile = GetProfile(profileName);

        IReadOnlyList<PauseHeaderTextureTarget> targets = PauseHeaderTextureCatalog.GetTargets(profile);

        Assert.Equal(expectedCount, targets.Count);
        Assert.All(targets, target =>
        {
            Assert.Equal(3, target.Textures.Count);
            Assert.All(target.Textures, texture => Assert.True(PauseHeaderTextureCatalog.IsPauseHeaderTexture(texture)));
        });
    }

    [Fact]
    public void Pause_header_texture_targets_use_expected_triplets()
    {
        RomVersionProfile profile = GetProfile("Retail NTSC 1.0");

        PauseHeaderTextureTarget equipment = PauseHeaderTextureCatalog.GetTargets(profile)
            .Single(target => target.Spec.Key == "Equipment");

        Assert.Equal("EQUIPMENT", equipment.Spec.SampleText);
        Assert.Equal(
            ["gPauseEquipment00Tex", "gPauseEquipment10ENGTex", "gPauseEquipment20Tex"],
            equipment.Textures.Select(texture => texture.Name));
        Assert.Equal(new PauseHeaderPageColor(90, 100, 60), equipment.Spec.OriginalColorRamp.Column1);
    }

    [Fact]
    public void Pause_header_original_color_preview_modulates_texture_intensity()
    {
        PauseHeaderTextureSpec spec = PauseHeaderTextureCatalog.Specs.Single(spec => spec.Key == "SelectItem");
        using Bitmap source = new(PauseHeaderTextureCatalog.Width, PauseHeaderTextureCatalog.Height);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                source.SetPixel(x, y, Color.White);
            }
        }

        using Bitmap colorized = PauseHeaderTextureRenderer.ApplyOriginalColors(source, spec);

        Assert.Equal(Color.FromArgb(255, 10, 50, 80).ToArgb(), colorized.GetPixel(0, 0).ToArgb());
        Assert.Equal(Color.FromArgb(255, 70, 100, 130).ToArgb(), colorized.GetPixel(79, 0).ToArgb());
        Assert.Equal(Color.FromArgb(255, 70, 100, 130).ToArgb(), colorized.GetPixel(80, 0).ToArgb());
        Assert.Equal(Color.FromArgb(255, 10, 50, 80).ToArgb(), colorized.GetPixel(239, 0).ToArgb());
    }

    private static RomVersionProfile GetProfile(string name)
        => RomVersionDatabase.Profiles.Single(profile => profile.Name == name);

    private static Rectangle GetAlphaBounds(Bitmap bitmap)
    {
        int left = bitmap.Width;
        int top = bitmap.Height;
        int right = -1;
        int bottom = -1;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).A == 0)
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return right < left || bottom < top
            ? Rectangle.Empty
            : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static bool BitmapsHaveDifferentPixels(Bitmap left, Bitmap right)
    {
        if (left.Size != right.Size)
        {
            return true;
        }

        for (int y = 0; y < left.Height; y++)
        {
            for (int x = 0; x < left.Width; x++)
            {
                if (left.GetPixel(x, y).ToArgb() != right.GetPixel(x, y).ToArgb())
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int CountNonTransparentPixels(Bitmap bitmap)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).A > 0)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountNonTransparentPixels(Bitmap bitmap, Rectangle area)
    {
        int count = 0;
        int left = Math.Clamp(area.Left, 0, bitmap.Width);
        int top = Math.Clamp(area.Top, 0, bitmap.Height);
        int right = Math.Clamp(area.Right, 0, bitmap.Width);
        int bottom = Math.Clamp(area.Bottom, 0, bitmap.Height);
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                if (bitmap.GetPixel(x, y).A > 0)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool ContainsPixel(Bitmap bitmap, Predicate<Color> predicate)
    {
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (predicate(bitmap.GetPixel(x, y)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ContainsPixel(Bitmap bitmap, Rectangle area, Predicate<Color> predicate)
    {
        int left = Math.Clamp(area.Left, 0, bitmap.Width);
        int top = Math.Clamp(area.Top, 0, bitmap.Height);
        int right = Math.Clamp(area.Right, 0, bitmap.Width);
        int bottom = Math.Clamp(area.Bottom, 0, bitmap.Height);
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                if (predicate(bitmap.GetPixel(x, y)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int CountPixels(Bitmap bitmap, Rectangle area, Predicate<Color> predicate)
    {
        int count = 0;
        int left = Math.Clamp(area.Left, 0, bitmap.Width);
        int top = Math.Clamp(area.Top, 0, bitmap.Height);
        int right = Math.Clamp(area.Right, 0, bitmap.Width);
        int bottom = Math.Clamp(area.Bottom, 0, bitmap.Height);
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                if (predicate(bitmap.GetPixel(x, y)))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool IsColoredAssetPixel(Color color)
        => color.A > 0
            && color.R == color.G
            && color.G == color.B
            && color.R > 0
            && color.R < 255;

    private static bool IsBlackPixel(Color color)
        => color.A > 0 && color.R == 0 && color.G == 0 && color.B == 0;

    private static string? FindRepositoryFile(string relativePath)
    {
        string? current = AppContext.BaseDirectory;
        for (int i = 0; i < 8 && current is not null; i++)
        {
            string path = Path.Combine(current, relativePath);
            if (File.Exists(path))
            {
                return path;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        return null;
    }

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
