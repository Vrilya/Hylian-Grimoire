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

        (TitleTextLine noController, TitleTextLine pressStart) = TitleTextService.Read(rom, profile);

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

        Assert.Equal(14, rom[profile.NoController.LoopCounter1Offset]);
        Assert.Equal(profile.NoController.DefaultPointer - 2, rom[profile.NoController.PointerOffset!.Value]);
        Assert.All(profile.NoController.GapOffsets, offset => Assert.Equal(7, rom[offset]));
        Assert.Equal((byte)('K' - 'A' + profile.NoController.FontBase), rom[profile.NoController.StringOffset]);
        Assert.Equal(10, rom[profile.PressStart.LoopCounter1Offset]);
        Assert.All(profile.PressStart.GapOffsets, offset => Assert.Equal(4, rom[offset]));
        Assert.Equal(104, rom[profile.PressStart.XOffset]);

        (TitleTextLine noController, TitleTextLine pressStart) = TitleTextService.Read(rom, profile);
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

        Assert.All(profile.NoController.GapOffsets, offset => Assert.Equal(13, rom[offset]));
        Assert.All(profile.PressStart.GapOffsets, offset => Assert.Equal(9, rom[offset]));

        (TitleTextLine noController, TitleTextLine pressStart) = TitleTextService.Read(rom, profile);
        Assert.Equal("KONTROLLSAKNAS", noController.Text);
        Assert.Equal("TRYCKSTART", pressStart.Text);
    }

    private static byte[] CreateRom(TitleTextPatchProfile profile)
    {
        int length = new[]
        {
            profile.NoController.StringOffset + profile.NoController.MaxCharacters,
            profile.NoController.LoopCounter1Offset + 1,
            profile.NoController.LoopCounter2Offset + 1,
            profile.NoController.GapOffsets.Max() + 1,
            profile.NoController.XOffset + 1,
            profile.NoController.PointerOffset.GetValueOrDefault() + 1,
            profile.PressStart.StringOffset + profile.PressStart.MaxCharacters,
            profile.PressStart.LoopCounter1Offset + 1,
            profile.PressStart.LoopCounter2Offset + 1,
            profile.PressStart.GapOffsets.Max() + 1,
            profile.PressStart.XOffset + 1,
        }.Max();

        byte[] rom = new byte[length];
        TitleTextService.Reset(rom, profile, TitleTextKind.NoController);
        TitleTextService.Reset(rom, profile, TitleTextKind.PressStart);
        return rom;
    }

    private static RomVersionProfile GetProfile(string name) =>
        Assert.Single(RomVersionDatabase.Profiles, profile => profile.Name == name);
}
