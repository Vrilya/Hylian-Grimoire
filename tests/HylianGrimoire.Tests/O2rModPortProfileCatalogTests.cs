using HylianGrimoire.Games;
using HylianGrimoire.O2r;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class O2rModPortProfileCatalogTests
{
    [Fact]
    public void PortProfilesUseGenericO2rToolTitle()
    {
        RomVersionProfile ocarinaRomProfile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "Retail NTSC 1.0");
        RomVersionProfile majorasMaskRomProfile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "Majora's Mask NTSC-U");

        Assert.True(O2rModPortProfileCatalog.TryGetProfile(
            GameProfiles.Get(GameKind.OcarinaOfTime),
            ocarinaRomProfile,
            out O2rModPortProfile ocarinaProfile));
        Assert.True(O2rModPortProfileCatalog.TryGetProfile(
            GameProfiles.Get(GameKind.MajorasMask),
            majorasMaskRomProfile,
            out O2rModPortProfile majorasMaskProfile));

        Assert.Equal("O2R Mod Maker", ocarinaProfile.ToolTitle);
        Assert.Equal("O2R Mod Maker", majorasMaskProfile.ToolTitle);
    }

    [Fact]
    public void RomProfilesRestrictO2rModMakerToOcarinaTextureCatalogsAndMajorasMaskUs()
    {
        RomVersionProfile ocarinaRetailRomProfile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "Retail NTSC 1.0");
        RomVersionProfile ocarinaSwedishRomProfile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "NTSC 1.2");
        RomVersionProfile majorasMaskUsRomProfile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "Majora's Mask NTSC-U");

        Assert.True(O2rModPortProfileCatalog.TryGetProfile(
            GameProfiles.Get(GameKind.OcarinaOfTime),
            ocarinaRetailRomProfile,
            out _));
        Assert.True(O2rModPortProfileCatalog.TryGetProfile(
            GameProfiles.Get(GameKind.OcarinaOfTime),
            ocarinaSwedishRomProfile,
            out _));
        Assert.True(O2rModPortProfileCatalog.TryGetProfile(
            GameProfiles.Get(GameKind.MajorasMask),
            majorasMaskUsRomProfile,
            out _));

        List<RomVersionProfile> unsupportedMajorasMaskProfiles = RomVersionDatabase.Profiles
            .Where(profile => profile.Game == GameKind.MajorasMask && profile.Name != "Majora's Mask NTSC-U")
            .ToList();

        Assert.NotEmpty(unsupportedMajorasMaskProfiles);

        foreach (RomVersionProfile profile in RomVersionDatabase.Profiles.Where(profile => profile.Game == GameKind.OcarinaOfTime))
        {
            Assert.True(O2rModPortProfileCatalog.TryGetProfile(profile.GameProfile, profile, out _));
        }

        foreach (RomVersionProfile profile in unsupportedMajorasMaskProfiles)
        {
            Assert.False(O2rModPortProfileCatalog.TryGetProfile(profile.GameProfile, profile, out _));
        }
    }

    [Fact]
    public void TwoShipTwoHarkinianProfileUses2s2hTextureResourcePaths()
    {
        RomVersionProfile romProfile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "Majora's Mask NTSC-U");
        Assert.True(O2rModPortProfileCatalog.TryGetProfile(
            GameProfiles.Get(GameKind.MajorasMask),
            romProfile,
            out O2rModPortProfile profile));

        Assert.Equal(
            "nes_font_static/gMsgChar82LatinCapitalLetterAWithCircumflexTex",
            profile.GetTextureResourcePath(CreateTexture("interface/nes_font_static", "gMsgChar82LatinCapitalLetterAWithCircumflexTex")));
        Assert.Equal(
            "icon_item_static_yar/gItemIconOcarinaOfTimeTex",
            profile.GetTextureResourcePath(CreateTexture("archives/icon_item_static_yar", "gItemIconOcarinaOfTimeTex")));
        Assert.Equal(
            "objects/object_boss07/gMajorasMaskWithNormalEyesTex",
            profile.GetTextureResourcePath(CreateTexture("objects/object_boss07", "gMajorasMaskWithNormalEyesTex")));
        Assert.Equal(
            "code/fbdemo_circle/gCircleTex",
            profile.GetTextureResourcePath(CreateTexture("code", "sCircleTex")));
        Assert.Equal(
            "code/debug_display/sDebugDisplayBallTex",
            profile.GetTextureResourcePath(CreateTexture("code", "sDebugDisplayBallTex")));
        Assert.Equal(
            "scenes/nonmq/Z2_SECOM/gSakonsHidoutConveyorBeltTex",
            profile.GetTextureResourcePath(CreateTexture("scenes/Z2_SECOM", "gSakonsHidoutConveyorBeltTex")));
    }

    [Fact]
    public void TwoShipTwoHarkinianProfileExcludesTexturesThatAreNot2s2hOtrResources()
    {
        RomVersionProfile romProfile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "Majora's Mask NTSC-U");
        Assert.True(O2rModPortProfileCatalog.TryGetProfile(
            GameProfiles.Get(GameKind.MajorasMask),
            romProfile,
            out O2rModPortProfile profile));

        Assert.False(profile.SupportsTextureResource(CreateTexture("interface/kanji", "gMsgKanji8140SpaceTex")));
        Assert.False(profile.SupportsTextureResource(CreateTexture("code", "sWhiteSquareTex")));
        Assert.True(profile.SupportsTextureResource(CreateTexture("interface/nes_font_static", "gMsgChar82LatinCapitalLetterAWithCircumflexTex")));
        Assert.True(profile.SupportsTextureResource(CreateTexture("code", "sCircleTex")));
    }

    private static TextureDefinition CreateTexture(string group, string name) =>
        new(group, name, RomAddress: 0, Width: 16, Height: 16, TextureFormat.I4);
}
