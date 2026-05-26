using System.Buffers.Binary;
using HylianGrimoire.Rom;
using HylianGrimoire.Tweaks;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class RomTweakTests
{
    private static readonly uint[] OriginalBootLogoStub =
    [
        0x240E0001,
        0xA08E01E1,
        0x03E00008,
        0x00000000,
    ];

    [Fact]
    public void GcBootLogoTweakTogglesRetailGameCubeRom()
    {
        RomVersionProfile profile = GetProfile("Retail NTSC GameCube");
        byte[] rom = CreateRetailNtscGameCubeRomWithOriginalBootLogoStub();

        Assert.Equal(RomTweakState.Off, GcBootLogoTweak.GetStatus(rom, profile).State);

        GcBootLogoTweak.SetEnabled(rom, profile, enabled: true);
        Assert.Equal(RomTweakState.On, GcBootLogoTweak.GetStatus(rom, profile).State);

        GcBootLogoTweak.SetEnabled(rom, profile, enabled: false);
        Assert.Equal(RomTweakState.Off, GcBootLogoTweak.GetStatus(rom, profile).State);
    }

    [Fact]
    public void GcBootLogoTweakReportsUnsupportedForNonGameCubeRom()
    {
        RomVersionProfile profile = GetProfile("Retail NTSC 1.2");
        byte[] rom = new byte[0x1000];

        Assert.Equal(RomTweakState.Unsupported, GcBootLogoTweak.GetStatus(rom, profile).State);
    }

    [Fact]
    public void GcBootLogoTweakDoesNotPatchUnknownState()
    {
        RomVersionProfile profile = GetProfile("Retail NTSC GameCube");
        byte[] rom = CreateRetailNtscGameCubeRomWithOriginalBootLogoStub();
        rom[0x00B5A68C] = 1;

        RomTweakStatus status = GcBootLogoTweak.GetStatus(rom, profile);
        Assert.Equal(RomTweakState.Unknown, status.State);
        Assert.Throws<InvalidOperationException>(() => GcBootLogoTweak.SetEnabled(rom, profile, enabled: true));
    }

    [Fact]
    public void GcColorTweakTogglesRetailGameCubeRom()
    {
        RomVersionProfile profile = GetProfile("Retail NTSC GameCube");
        byte[] rom = File.ReadAllBytes(GetRequiredRetailDecompressedFixture("ntscgc_orig.z64"));

        Assert.Equal(RomTweakState.Off, GcColorTweak.GetStatus(rom, profile).State);

        GcColorTweak.SetEnabled(rom, profile, enabled: true);
        Assert.Equal(RomTweakState.On, GcColorTweak.GetStatus(rom, profile).State);

        GcColorTweak.SetEnabled(rom, profile, enabled: false);
        Assert.Equal(RomTweakState.Off, GcColorTweak.GetStatus(rom, profile).State);
    }

    [Fact]
    public void ViPalTweakTogglesRetailPalGameCubeRom()
    {
        RomVersionProfile profile = GetProfile("Retail PAL GameCube");
        byte[] rom = File.ReadAllBytes(GetRequiredRetailDecompressedFixture("palgc_orig.z64"));

        Assert.Equal(RomTweakState.Off, ViPalTweak.GetStatus(rom, profile).State);

        ViPalTweak.SetEnabled(rom, profile, enabled: true);
        Assert.Equal(RomTweakState.On, ViPalTweak.GetStatus(rom, profile).State);

        ViPalTweak.SetEnabled(rom, profile, enabled: false);
        Assert.Equal(RomTweakState.Off, ViPalTweak.GetStatus(rom, profile).State);
    }

    [Fact]
    public void ViPalTweakIsBuildDateRestricted()
    {
        RomVersionProfile profile = GetProfile("PAL GameCube");
        byte[] rom = File.ReadAllBytes(GetRequiredRetailDecompressedFixture("palgc_orig.z64"));

        Assert.Equal(RomTweakState.Unsupported, ViPalTweak.GetStatus(rom, profile).State);
    }

    [Theory]
    [InlineData("Retail PAL 1.0", "pal10_orig.z64")]
    [InlineData("Retail PAL 1.1", "pal11_orig.z64")]
    public void ViSelectorTweakTogglesPalRetailRom(string profileName, string fixtureName)
    {
        RomVersionProfile profile = GetProfile(profileName);
        byte[] rom = File.ReadAllBytes(GetRequiredRetailDecompressedFixture(fixtureName));
        byte[] original = (byte[])rom.Clone();

        Assert.Equal(RomTweakState.Off, ViSelectorTweak.GetStatus(rom, profile).State);

        ViSelectorTweak.SetEnabled(rom, profile, enabled: true);
        Assert.Equal(RomTweakState.On, ViSelectorTweak.GetStatus(rom, profile).State);

        ViSelectorTweak.SetEnabled(rom, profile, enabled: false);
        Assert.Equal(RomTweakState.Off, ViSelectorTweak.GetStatus(rom, profile).State);
        Assert.Equal(original, rom);
    }

    [Fact]
    public void ViSelectorTweakRejectsNonPalRetailRom()
    {
        RomVersionProfile profile = GetProfile("Retail NTSC 1.2");
        byte[] rom = File.ReadAllBytes(GetRequiredRetailDecompressedFixture("ntsc12_orig.z64"));

        Assert.Equal(RomTweakState.Unsupported, ViSelectorTweak.GetStatus(rom, profile).State);
    }

    [Fact]
    public void GcNoControllerTweakTogglesRetailGameCubeRom()
    {
        RomVersionProfile profile = GetProfile("Retail NTSC GameCube");
        byte[] rom = File.ReadAllBytes(GetRequiredRetailDecompressedFixture("ntscgc_orig.z64"));

        Assert.Equal(RomTweakState.Off, GcNoControllerTweak.GetStatus(rom, profile).State);

        GcNoControllerTweak.SetEnabled(rom, profile, enabled: true);
        Assert.Equal(RomTweakState.On, GcNoControllerTweak.GetStatus(rom, profile).State);

        GcNoControllerTweak.SetEnabled(rom, profile, enabled: false);
        Assert.Equal(RomTweakState.Off, GcNoControllerTweak.GetStatus(rom, profile).State);
    }

    [Fact]
    public void GcCreditsTweakTogglesRetailGameCubeRom()
    {
        RomVersionProfile profile = GetProfile("Retail NTSC GameCube");
        byte[] rom = File.ReadAllBytes(GetRequiredRetailDecompressedFixture("ntscgc_orig.z64"));

        Assert.Equal(RomTweakState.Off, GcCreditsTweak.GetStatus(rom, profile).State);

        GcCreditsTweak.SetEnabled(rom, profile, enabled: true);
        Assert.Equal(RomTweakState.On, GcCreditsTweak.GetStatus(rom, profile).State);

        GcCreditsTweak.SetEnabled(rom, profile, enabled: false);
        Assert.Equal(RomTweakState.Off, GcCreditsTweak.GetStatus(rom, profile).State);
    }

    [Theory]
    [InlineData("Retail NTSC 1.0", "ntsc10_orig.z64")]
    [InlineData("Retail NTSC 1.1", "ntsc11_orig.z64")]
    [InlineData("Retail NTSC 1.2", "ntsc12_orig.z64")]
    [InlineData("Retail PAL 1.0", "pal10_orig.z64")]
    [InlineData("Retail PAL 1.1", "pal11_orig.z64")]
    public void AntiPiracyTweakTogglesRetailN64Rom(string profileName, string fixtureName)
    {
        RomVersionProfile profile = GetProfile(profileName);
        byte[] rom = File.ReadAllBytes(GetRequiredRetailDecompressedFixture(fixtureName));
        byte[] original = (byte[])rom.Clone();

        Assert.Equal(RomTweakState.Off, AntiPiracyTweak.GetStatus(rom, profile).State);

        AntiPiracyTweak.SetEnabled(rom, profile, enabled: true);
        Assert.Equal(RomTweakState.On, AntiPiracyTweak.GetStatus(rom, profile).State);
        Assert.Equal(4, CountDifferences(original, rom));

        AntiPiracyTweak.SetEnabled(rom, profile, enabled: false);
        Assert.Equal(RomTweakState.Off, AntiPiracyTweak.GetStatus(rom, profile).State);
        Assert.Equal(original, rom);
    }

    [Fact]
    public void AntiPiracyTweakRejectsGameCubeRom()
    {
        RomVersionProfile profile = GetProfile("Retail NTSC GameCube");
        byte[] rom = File.ReadAllBytes(GetRequiredRetailDecompressedFixture("ntscgc_orig.z64"));

        Assert.Equal(RomTweakState.Unsupported, AntiPiracyTweak.GetStatus(rom, profile).State);
    }

    private static byte[] CreateRetailNtscGameCubeRomWithOriginalBootLogoStub()
    {
        byte[] rom = new byte[0x00B8B000];
        for (int i = 0; i < OriginalBootLogoStub.Length; i++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(
                rom.AsSpan(0x00B8AA60 + i * sizeof(uint), sizeof(uint)),
                OriginalBootLogoStub[i]);
        }

        return rom;
    }

    private static RomVersionProfile GetProfile(string name)
    {
        return Assert.Single(RomVersionDatabase.Profiles, profile => profile.Name == name);
    }

    private static string GetRequiredRetailDecompressedFixture(string fileName)
    {
        string path = Path.Combine(@"D:\test30\retaildecompressed", fileName);
        Assert.True(File.Exists(path), $"Missing local ROM fixture: {path}");
        return path;
    }

    private static int CountDifferences(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        Assert.Equal(left.Length, right.Length);

        int count = 0;
        for (int i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
            {
                count++;
            }
        }

        return count;
    }
}
