using HylianGrimoire.Rom;
using HylianGrimoire.TitleText;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class TitleTextTests
{
    [Fact]
    public void TitleTextReadsRetailDefaults()
    {
        RomVersionProfile romProfile = GetProfile("Retail NTSC 1.0");
        Assert.True(TitleTextService.TryGetProfile(romProfile, out TitleTextPatchProfile? profile));
        byte[] rom = CreateRom(profile);

        (TitleTextLine? noController, TitleTextLine pressStart) = TitleTextService.Read(rom, profile);

        Assert.NotNull(noController);
        Assert.Equal("NO CONTROLLER", noController.Text);
        Assert.Equal(99, noController.X);
        Assert.Equal("PRESS START", pressStart.Text);
        Assert.Equal(119, pressStart.X);
    }

    [Fact]
    public void NoControllerCanUsePrefixPaddingForLongerText()
    {
        RomVersionProfile romProfile = GetProfile("Retail NTSC 1.0");
        Assert.True(TitleTextService.TryGetProfile(romProfile, out TitleTextPatchProfile? profile));
        byte[] rom = CreateRom(profile);

        TitleTextService.Write(
            rom,
            profile,
            new TitleTextLine(TitleTextKind.NoController, "KONTROLL SAKNAS", 1, 88, 14),
            new TitleTextLine(TitleTextKind.PressStart, "TRYCK START", 4, 104, 12));

        TitleTextLineProfile noControllerProfile = RequiredNoController(profile);
        Assert.Equal(14, rom[noControllerProfile.LoopCounter1Offset]);
        Assert.Equal(noControllerProfile.DefaultPointer - 2, rom[noControllerProfile.PointerOffset!.Value]);
        Assert.All(noControllerProfile.GapOffsets, offset => Assert.Equal(7, rom[offset]));
        Assert.Equal((byte)('K' - 'A' + noControllerProfile.FontBase), rom[noControllerProfile.StringOffset]);
        Assert.Equal(10, rom[profile.PressStart.LoopCounter1Offset]);
        Assert.All(profile.PressStart.GapOffsets, offset => Assert.Equal(4, rom[offset]));
        Assert.Equal(104, rom[PrimaryXOffset(profile.PressStart)]);

        (TitleTextLine? noController, TitleTextLine pressStart) = TitleTextService.Read(rom, profile);
        Assert.NotNull(noController);
        Assert.Equal("KONTROLL SAKNAS", noController.Text);
        Assert.Equal("TRYCK START", pressStart.Text);
    }

    [Fact]
    public void TitleTextRejectsUnsupportedCharacters()
    {
        RomVersionProfile romProfile = GetProfile("Retail PAL 1.0");
        Assert.True(TitleTextService.TryGetProfile(romProfile, out TitleTextPatchProfile? profile));
        byte[] rom = CreateRom(profile);

        Assert.Throws<InvalidDataException>(() => TitleTextService.Write(
            rom,
            profile,
            new TitleTextLine(TitleTextKind.NoController, "KONTROLL SAKNÅS", 1, 88, 14),
            new TitleTextLine(TitleTextKind.PressStart, "TRYCK START", 4, 104, 12)));
    }

    [Fact]
    public void MajorasMaskTitleTextProfilePatchesPressStartOnly()
    {
        RomVersionProfile romProfile = GetProfile("Majora's Mask NTSC-U");
        Assert.True(TitleTextService.TryGetProfile(romProfile, out TitleTextPatchProfile? profile));
        Assert.Null(profile.NoController);

        byte[] rom = new byte[MaxRequiredOffset(profile.PressStart)];
        TitleTextService.Reset(rom, profile, TitleTextKind.PressStart);

        TitleTextService.Write(
            rom,
            profile,
            noController: null,
            new TitleTextLine(TitleTextKind.PressStart, "TRYCK START", 4, 104, 12));

        Assert.Equal(10, rom[profile.PressStart.LoopCounter1Offset]);
        Assert.Equal(10, rom[profile.PressStart.LoopCounter2Offset]);
        Assert.All(profile.PressStart.GapOffsets, offset => Assert.Equal(4, rom[offset]));
        Assert.Equal(104, rom[profile.PressStart.XOffsets[0].Offset]);
        Assert.Equal(105, rom[profile.PressStart.XOffsets[1].Offset]);
    }

    [Fact]
    public void MajorasMaskGameCubeTitleTextProfilePatchesPressStartOnly()
    {
        RomVersionProfile romProfile = GetProfile("Majora's Mask NTSC-U GameCube");
        Assert.True(TitleTextService.TryGetProfile(romProfile, out TitleTextPatchProfile? profile));
        Assert.Null(profile.NoController);

        byte[] rom = new byte[MaxRequiredOffset(profile.PressStart)];
        TitleTextService.Reset(rom, profile, TitleTextKind.PressStart);

        TitleTextService.Write(
            rom,
            profile,
            noController: null,
            new TitleTextLine(TitleTextKind.PressStart, "TRYCK START", 4, 104, 12));

        Assert.Equal(10, rom[profile.PressStart.LoopCounter1Offset]);
        Assert.Equal(10, rom[profile.PressStart.LoopCounter2Offset]);
        Assert.All(profile.PressStart.GapOffsets, offset => Assert.Equal(4, rom[offset]));
        Assert.Equal(104, rom[profile.PressStart.XOffsets[0].Offset]);
        Assert.Equal(105, rom[profile.PressStart.XOffsets[1].Offset]);
    }

    [Fact]
    public void MajorasMaskEuTitleTextReadsLocalizedDefaults()
    {
        RomVersionProfile romProfile = GetProfile("Majora's Mask EU 1.0");
        Assert.True(TitleTextService.TryGetProfile(romProfile, out TitleTextPatchProfile? profile));
        byte[] rom = CreateLocalizedRom(profile);

        Assert.Equal("PRESS START", TitleTextService.Read(rom, profile, languageIndex: 0).PressStart.Text);
        Assert.Equal("DRÜCKE START", TitleTextService.Read(rom, profile, languageIndex: 1).PressStart.Text);
        Assert.Equal("APPUYEZ SUR START", TitleTextService.Read(rom, profile, languageIndex: 2).PressStart.Text);
        Assert.Equal("PULSA START", TitleTextService.Read(rom, profile, languageIndex: 3).PressStart.Text);
    }

    [Fact]
    public void LocalMajorasMaskEu11TitleTextReadsLocalizedDefaultsWhenAvailable()
    {
        if (!LocalRomFixtures.TryGetMajorasMaskPath("mm_eu_1.1_n64_decompressed.z64", out string path))
        {
            return;
        }

        RomVersionProfile romProfile = GetProfile("Majora's Mask EU 1.1");
        Assert.True(TitleTextService.TryGetProfile(romProfile, out TitleTextPatchProfile? profile));
        byte[] rom = File.ReadAllBytes(path);

        Assert.Equal("PRESS START", TitleTextService.Read(rom, profile, languageIndex: 0).PressStart.Text);
        Assert.Equal("DR\u00DCCKE START", TitleTextService.Read(rom, profile, languageIndex: 1).PressStart.Text);
        Assert.Equal("APPUYEZ SUR START", TitleTextService.Read(rom, profile, languageIndex: 2).PressStart.Text);
        Assert.Equal("PULSA START", TitleTextService.Read(rom, profile, languageIndex: 3).PressStart.Text);
    }

    [Fact]
    public void LocalMajorasMaskEuGameCubeTitleTextReadsLocalizedDefaultsWhenAvailable()
    {
        if (!LocalRomFixtures.TryGetMajorasMaskPath("mm_eu_gc_decompressed.z64", out string path))
        {
            return;
        }

        RomVersionProfile romProfile = GetProfile("Majora's Mask EU GameCube");
        Assert.True(TitleTextService.TryGetProfile(romProfile, out TitleTextPatchProfile? profile));
        byte[] rom = File.ReadAllBytes(path);

        TitleTextLine english = TitleTextService.Read(rom, profile, languageIndex: 0).PressStart;
        TitleTextLine german = TitleTextService.Read(rom, profile, languageIndex: 1).PressStart;
        TitleTextLine french = TitleTextService.Read(rom, profile, languageIndex: 2).PressStart;
        TitleTextLine spanish = TitleTextService.Read(rom, profile, languageIndex: 3).PressStart;

        Assert.Equal("PRESS START", english.Text);
        Assert.Equal("DR\u00DCCKE START", german.Text);
        Assert.Equal("APPUYEZ SUR START", french.Text);
        Assert.Equal("PULSA START", spanish.Text);
        Assert.Equal(119, english.X);
        Assert.Equal(116, german.X);
        Assert.Equal(110, french.X);
        Assert.Equal(119, spanish.X);
        Assert.Equal(10, TitleTextService.GetPressStartMaxCharacters(profile, languageIndex: 0));
        Assert.Equal(11, TitleTextService.GetPressStartMaxCharacters(profile, languageIndex: 1));
        Assert.Equal(15, TitleTextService.GetPressStartMaxCharacters(profile, languageIndex: 2));
        Assert.Equal(10, TitleTextService.GetPressStartMaxCharacters(profile, languageIndex: 3));
    }

    [Fact]
    public void MajorasMaskEu11TitleTextUsesSameLocalizedLimits()
    {
        RomVersionProfile romProfile = GetProfile("Majora's Mask EU 1.1");
        Assert.True(TitleTextService.TryGetProfile(romProfile, out TitleTextPatchProfile? profile));

        Assert.Equal(10, TitleTextService.GetPressStartMaxCharacters(profile, languageIndex: 0));
        Assert.Equal(11, TitleTextService.GetPressStartMaxCharacters(profile, languageIndex: 1));
        Assert.Equal(15, TitleTextService.GetPressStartMaxCharacters(profile, languageIndex: 2));
        Assert.Equal(10, TitleTextService.GetPressStartMaxCharacters(profile, languageIndex: 3));
    }

    [Fact]
    public void MajorasMaskEuTitleTextUsesLanguageSpecificVisibleLimits()
    {
        RomVersionProfile romProfile = GetProfile("Majora's Mask EU 1.0");
        Assert.True(TitleTextService.TryGetProfile(romProfile, out TitleTextPatchProfile? profile));
        byte[] rom = CreateLocalizedRom(profile);

        Assert.Equal(10, TitleTextService.GetPressStartMaxCharacters(profile, languageIndex: 0));
        Assert.Equal(11, TitleTextService.GetPressStartMaxCharacters(profile, languageIndex: 1));
        Assert.Equal(15, TitleTextService.GetPressStartMaxCharacters(profile, languageIndex: 2));
        Assert.Equal(10, TitleTextService.GetPressStartMaxCharacters(profile, languageIndex: 3));

        Assert.Throws<InvalidDataException>(() => TitleTextService.Write(
            rom,
            profile,
            noController: null,
            new TitleTextLine(TitleTextKind.PressStart, "PRESS STARTA", GapAfterIndex: 0, X: 119, MaxCharacters: 10),
            languageIndex: 0));
    }

    [Fact]
    public void MajorasMaskEuTitleTextWritesOnlyActiveLanguage()
    {
        RomVersionProfile romProfile = GetProfile("Majora's Mask EU 1.0");
        Assert.True(TitleTextService.TryGetProfile(romProfile, out TitleTextPatchProfile? profile));
        byte[] rom = CreateLocalizedRom(profile);
        byte[] englishBefore = rom.AsSpan(profile.LocalizedPressStarts[0].StringOffset, 15).ToArray();
        byte[] germanBefore = rom.AsSpan(profile.LocalizedPressStarts[1].StringOffset, 15).ToArray();
        byte[] spanishBefore = rom.AsSpan(profile.LocalizedPressStarts[3].StringOffset, 15).ToArray();

        TitleTextService.Write(
            rom,
            profile,
            noController: null,
            new TitleTextLine(TitleTextKind.PressStart, "APPUYEZ SUR START", GapAfterIndex: 0, X: 112, MaxCharacters: 15),
            languageIndex: 2);

        Assert.Equal(englishBefore, rom.AsSpan(profile.LocalizedPressStarts[0].StringOffset, 15).ToArray());
        Assert.Equal(germanBefore, rom.AsSpan(profile.LocalizedPressStarts[1].StringOffset, 15).ToArray());
        Assert.Equal(spanishBefore, rom.AsSpan(profile.LocalizedPressStarts[3].StringOffset, 15).ToArray());
        Assert.Equal(112, rom[profile.LocalizedPressStarts[2].XOffsets[0].Offset]);
        Assert.Equal(113, rom[profile.LocalizedPressStarts[2].XOffsets[1].Offset]);
    }

    [Fact]
    public void MajorasMaskEuGermanTitleTextStoresUDiaeresisByte()
    {
        RomVersionProfile romProfile = GetProfile("Majora's Mask EU 1.0");
        Assert.True(TitleTextService.TryGetProfile(romProfile, out TitleTextPatchProfile? profile));
        byte[] rom = CreateLocalizedRom(profile);
        TitleTextLocalizedLineProfile german = profile.LocalizedPressStarts[1];

        TitleTextService.Write(
            rom,
            profile,
            noController: null,
            new TitleTextLine(TitleTextKind.PressStart, "DRÜCKE START", GapAfterIndex: 0, X: 116, MaxCharacters: 15),
            languageIndex: 1);

        Assert.Equal(0x57, rom[german.StringOffset + 2]);
        Assert.Equal("DRÜCKE START", TitleTextService.Read(rom, profile, languageIndex: 1).PressStart.Text);
    }

    [Fact]
    public void TitleTextWithoutSpaceMovesGapAfterLastCharacter()
    {
        RomVersionProfile romProfile = GetProfile("Retail NTSC 1.0");
        Assert.True(TitleTextService.TryGetProfile(romProfile, out TitleTextPatchProfile? profile));
        byte[] rom = CreateRom(profile);

        TitleTextService.Write(
            rom,
            profile,
            new TitleTextLine(TitleTextKind.NoController, "KONTROLLSAKNAS", 1, 88, 14),
            new TitleTextLine(TitleTextKind.PressStart, "TRYCKSTART", 4, 104, 12));

        TitleTextLineProfile noControllerProfile = RequiredNoController(profile);
        Assert.All(noControllerProfile.GapOffsets, offset => Assert.Equal(13, rom[offset]));
        Assert.All(profile.PressStart.GapOffsets, offset => Assert.Equal(9, rom[offset]));

        (TitleTextLine? noController, TitleTextLine pressStart) = TitleTextService.Read(rom, profile);
        Assert.NotNull(noController);
        Assert.Equal("KONTROLLSAKNAS", noController.Text);
        Assert.Equal("TRYCKSTART", pressStart.Text);
    }

    private static byte[] CreateRom(TitleTextPatchProfile profile)
    {
        TitleTextLineProfile noController = RequiredNoController(profile);
        int length = new[]
        {
            noController.StringOffset + noController.MaxCharacters,
            noController.LoopCounter1Offset + 1,
            noController.LoopCounter2Offset + 1,
            noController.GapOffsets.Max() + 1,
            MaxXOffset(noController) + 1,
            noController.PointerOffset.GetValueOrDefault() + 1,
            profile.PressStart.StringOffset + profile.PressStart.MaxCharacters,
            profile.PressStart.LoopCounter1Offset + 1,
            profile.PressStart.LoopCounter2Offset + 1,
            profile.PressStart.GapOffsets.Max() + 1,
            MaxXOffset(profile.PressStart) + 1,
        }.Max();

        byte[] rom = new byte[length];
        TitleTextService.Reset(rom, profile, TitleTextKind.NoController);
        TitleTextService.Reset(rom, profile, TitleTextKind.PressStart);
        return rom;
    }

    private static byte[] CreateLocalizedRom(TitleTextPatchProfile profile)
    {
        int length = profile.LocalizedPressStarts.Select(MaxRequiredOffset).Max();
        byte[] rom = new byte[length];
        for (int i = 0; i < profile.LocalizedPressStarts.Count; i++)
        {
            TitleTextService.Reset(rom, profile, TitleTextKind.PressStart, languageIndex: i);
        }

        return rom;
    }

    private static TitleTextLineProfile RequiredNoController(TitleTextPatchProfile profile) =>
        profile.NoController ?? throw new InvalidOperationException("The test profile must define no-controller text.");

    private static int PrimaryXOffset(TitleTextLineProfile profile) => profile.XOffsets[0].Offset;

    private static int MaxXOffset(TitleTextLineProfile profile) => profile.XOffsets.Max(x => x.Offset);

    private static int MaxRequiredOffset(TitleTextLineProfile profile) =>
        new[]
        {
            profile.StringOffset + profile.MaxCharacters,
            profile.LoopCounter1Offset + 1,
            profile.LoopCounter2Offset + 1,
            profile.GapOffsets.Max() + 1,
            MaxXOffset(profile) + 1,
            profile.PointerOffset.GetValueOrDefault() + 1,
        }.Max();

    private static int MaxRequiredOffset(TitleTextLocalizedLineProfile profile) =>
        new[]
        {
            profile.StringOffset + profile.MaxCharacters,
            profile.WidthOffset + profile.MaxCharacters,
            profile.XOffsets.Max(x => x.Offset) + 1,
        }.Max();

    private static RomVersionProfile GetProfile(string name) =>
        Assert.Single(RomVersionDatabase.Profiles, profile => profile.Name == name);
}
