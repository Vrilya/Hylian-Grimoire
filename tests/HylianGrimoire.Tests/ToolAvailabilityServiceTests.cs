using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class ToolAvailabilityServiceTests
{
    [Fact]
    public void EmptySessionDisablesTools()
    {
        ToolAvailability availability = ToolAvailabilityService.Build(
            activeGameProfile: null,
            DocumentKind.None,
            [],
            romData: null);

        Assert.False(availability.HasActiveProject);
        Assert.False(availability.CanSaveDocument);
        Assert.False(availability.CanSaveLoadedRom);
        Assert.False(availability.CanUseCHeaders);
        Assert.False(availability.CanUseGlyphTools);
        Assert.False(availability.CanUseMessagePreview);
        Assert.False(availability.CanUseTextTextureEditor);
        Assert.False(availability.CanUseTweaks);
    }

    [Fact]
    public void OcarinaProjectWithoutRomKeepsDocumentToolsButDisablesRomScopedTools()
    {
        GameProfile gameProfile = GameProfiles.Get(GameKind.OcarinaOfTime);
        List<MessageEntry> entries = [CreateEntry(0x0001)];

        ToolAvailability availability = ToolAvailabilityService.Build(
            gameProfile,
            DocumentKind.Project,
            entries,
            romData: null);

        Assert.True(availability.HasActiveProject);
        Assert.True(availability.CanSaveDocument);
        Assert.True(availability.CanUseCHeaders);
        Assert.True(availability.CanExportHeader);
        Assert.True(availability.CanUseGlyphTools);
        Assert.True(availability.CanRemapGlyphBytes);
        Assert.True(availability.CanUseMessagePreview);
        Assert.True(availability.CanUseO2rModMaker);
        Assert.False(availability.CanSaveLoadedRom);
        Assert.False(availability.CanImportHeaderIntoRom);
        Assert.False(availability.CanUseTweaks);
        Assert.False(availability.CanUseTitleText);
        Assert.False(availability.CanUsePromptEditor);
        Assert.False(availability.CanUseTextureManager);
        Assert.False(availability.CanUseTextTextureEditor);
    }

    [Fact]
    public void OcarinaRetailRomEnablesRomScopedOcarinaTools()
    {
        RomVersionProfile profile = GetProfile("Retail NTSC 1.0");
        List<MessageEntry> entries = [CreateEntry(0x0001)];
        RomMessageData romData = CreateRomData(profile, entries);

        ToolAvailability availability = ToolAvailabilityService.Build(
            profile.GameProfile,
            DocumentKind.Rom,
            entries,
            romData);

        Assert.True(availability.CanSaveDocument);
        Assert.True(availability.CanSaveLoadedRom);
        Assert.True(availability.CanImportHeaderIntoRom);
        Assert.True(availability.CanUseRomGlyphEditor);
        Assert.True(availability.CanUseTweaks);
        Assert.True(availability.CanUseTitleText);
        Assert.True(availability.CanUsePromptEditor);
        Assert.True(availability.CanUseTextureManager);
        Assert.True(availability.CanUseTextTextureEditor);
        Assert.True(availability.CanUseO2rModMaker);
    }

    [Fact]
    public void OcarinaNonRetailRomsEnableTextureManagerAndO2rModMakerButDisableTweaks()
    {
        List<RomVersionProfile> profiles = RomVersionDatabase.Profiles
            .Where(profile => profile.Game == GameKind.OcarinaOfTime && !profile.IsRetail)
            .ToList();

        Assert.NotEmpty(profiles);

        foreach (RomVersionProfile profile in profiles)
        {
            List<MessageEntry> entries = [CreateEntry(0x0001)];
            RomMessageData romData = CreateRomData(profile, entries);

            ToolAvailability availability = ToolAvailabilityService.Build(
                profile.GameProfile,
                DocumentKind.Rom,
                entries,
                romData);

            Assert.True(availability.CanUseTextureManager);
            Assert.True(availability.CanUseTextTextureEditor);
            Assert.True(availability.CanUseO2rModMaker);
            Assert.False(availability.CanUseTweaks);
        }
    }

    [Fact]
    public void MajorasMaskUsRomEnables2S2hO2rModMaker()
    {
        RomVersionProfile profile = GetProfile("Majora's Mask NTSC-U");
        List<MessageEntry> entries = [CreateEntry(0x0001)];
        RomMessageData romData = CreateRomData(profile, entries);

        ToolAvailability availability = ToolAvailabilityService.Build(
            profile.GameProfile,
            DocumentKind.Rom,
            entries,
            romData);

        Assert.True(availability.CanSaveDocument);
        Assert.True(availability.CanSaveLoadedRom);
        Assert.True(availability.CanUseCHeaders);
        Assert.True(availability.CanImportHeaderIntoRom);
        Assert.True(availability.CanUseGlyphTools);
        Assert.True(availability.CanUseRomGlyphEditor);
        Assert.True(availability.CanUseMessagePreview);
        Assert.True(availability.CanUseTweaks);
        Assert.True(availability.CanUseTitleText);
        Assert.True(availability.CanUsePromptEditor);
        Assert.True(availability.CanUseTextureManager);
        Assert.False(availability.CanUseTextTextureEditor);
        Assert.True(availability.CanUseO2rModMaker);
        Assert.False(availability.CanUseFontOrder);
    }

    [Theory]
    [InlineData("Majora's Mask NTSC-U GameCube")]
    [InlineData("Majora's Mask EU 1.0")]
    [InlineData("Majora's Mask EU 1.1")]
    [InlineData("Majora's Mask EU GameCube")]
    public void MajorasMaskNonNtscUN64RomsDisableTweaksAndO2rModMaker(string profileName)
    {
        RomVersionProfile profile = GetProfile(profileName);
        List<MessageEntry> entries = [CreateEntry(0x0001)];
        RomMessageData romData = CreateRomData(profile, entries);

        ToolAvailability availability = ToolAvailabilityService.Build(
            profile.GameProfile,
            DocumentKind.Rom,
            entries,
            romData);

        Assert.False(availability.CanUseTweaks);
        Assert.False(availability.CanUseO2rModMaker);
    }

    [Fact]
    public void MajorasMaskUsGameCubeRomDisablesO2rModMaker()
    {
        RomVersionProfile profile = GetProfile("Majora's Mask NTSC-U GameCube");
        List<MessageEntry> entries = [CreateEntry(0x0001)];
        RomMessageData romData = CreateRomData(profile, entries);

        ToolAvailability availability = ToolAvailabilityService.Build(
            profile.GameProfile,
            DocumentKind.Rom,
            entries,
            romData);

        Assert.False(availability.CanUseO2rModMaker);
        Assert.False(availability.CanUseTextureManager);
        Assert.False(availability.CanUseTextTextureEditor);
    }

    [Fact]
    public void MajorasMaskEuRomDoesNotEnable2S2hO2rModMaker()
    {
        RomVersionProfile profile = GetProfile("Majora's Mask EU 1.0");
        List<MessageEntry> entries = [CreateEntry(0x0001)];
        RomMessageData romData = CreateRomData(profile, entries);

        ToolAvailability availability = ToolAvailabilityService.Build(
            profile.GameProfile,
            DocumentKind.Rom,
            entries,
            romData);

        Assert.False(availability.CanUseO2rModMaker);
    }

    private static MessageEntry CreateEntry(int id) =>
        new(id, type: 0, position: 0, bank: 0, offset: 0)
        {
            Text = "Test",
        };

    private static RomMessageData CreateRomData(RomVersionProfile profile, List<MessageEntry> entries) =>
        new(entries, profile, WasCompressed: false, [], RomFontResources.Empty, ActiveMessageBankIndex: 0, RomMessageSection.Messages);

    private static RomVersionProfile GetProfile(string name) =>
        RomVersionDatabase.Profiles.Single(profile => profile.Name == name);
}
