using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Compression;
using HylianGrimoire.Headers;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class RomCompressionTests
{
    [Fact]
    public void FontOrderStandardTextUsesCanonicalOrder()
    {
        Assert.Equal(
            """
            0123456789
            ABCDEFGHIJKLMN
            OPQRSTUVWXYZ
            abcdefghijklmn
            opqrstuvwxyz
             -.
            """.Replace("\r\n", "\n"),
            FontOrderCodec.GetStandardEditorText());
    }

    [Fact]
    public void RomProfilesKeepSwedishIqueButDisableRetailIque()
    {
        Assert.DoesNotContain(RomVersionDatabase.Profiles, profile => profile.Name == "Retail iQue");
        Assert.Contains(RomVersionDatabase.Profiles, profile => profile.Name == "NTSC iQue");
        Assert.Contains(RomVersionDatabase.Profiles, profile => profile.Name == "PAL iQue");
        Assert.Contains(RomVersionDatabase.Profiles, profile => profile.Name == "NTSC MQ iQue");
        Assert.Contains(RomVersionDatabase.Profiles, profile => profile.Name == "PAL MQ iQue");
    }

    [Fact]
    public void Yaz0RoundtripsRepeatedData()
    {
        byte[] source = Encoding.ASCII.GetBytes(new string('A', 64) + "0123456789" + new string('A', 64));

        byte[] encoded = Yaz0Codec.Encode(source);
        byte[] decoded = Yaz0Codec.Decode(encoded);

        Assert.Equal(source, decoded);
        Assert.True(Yaz0Codec.IsYaz0(encoded));
    }

    [Fact]
    public void RawDeflateRoundtripsRepeatedData()
    {
        byte[] source = Encoding.ASCII.GetBytes("Raw deflate test " + new string('Z', 512));

        byte[] encoded = RawDeflateCodec.Encode(source);
        byte[] decoded = RawDeflateCodec.Decode(encoded, source.Length);

        Assert.Equal(source, decoded);
    }

    [Fact]
    public void RawDeflateMatchesReferenceEncoderVectors()
    {
        AssertRawDeflateVector([], "0300");
        AssertRawDeflateVector(
            Encoding.ASCII.GetBytes("Raw deflate exactness."),
            "0B4A2C5748494DCB492C495548AD484C2EC94B2D2ED60300");
        AssertRawDeflateVector(
            Encoding.ASCII.GetBytes(new string('A', 64) + "0123456789" + new string('A', 64)),
            "7374A40C18181A199B989A995B585268902300");
    }

    [Fact]
    public void DecompressRomExpandsCompressedDmaEntries()
    {
        byte[] file1 = Encoding.ASCII.GetBytes("compressed-" + new string('Q', 128));
        byte[] compressedFile1 = Yaz0Codec.Encode(file1);
        byte[] rom = CreateSyntheticNtsc12Rom(compressedFile1, compressedSecondEntry: true);

        RomCompressionResult result = RomCompressionService.DecompressRom(rom);

        Assert.Equal("NTSC 1.2", result.Profile.Name);
        Assert.Equal(file1, result.Data.AsSpan(0xe000, file1.Length).ToArray());
        Assert.Equal(0u, ReadUInt32BigEndian(result.Data, 0x7960 + 8));
        Assert.Equal(0u, ReadUInt32BigEndian(result.Data, 0x7960 + 12));
        Assert.Equal(0xe000u, ReadUInt32BigEndian(result.Data, 0x7960 + 30 * 16 + 8));
        Assert.Equal(0u, ReadUInt32BigEndian(result.Data, 0x7960 + 30 * 16 + 12));
    }

    [Fact]
    public void CompressRomProducesLoadableCompressedRom()
    {
        byte[] file1 = Encoding.ASCII.GetBytes("compress-me-" + new string('R', 256));
        byte[] rom = CreateSyntheticNtsc12Rom(file1, compressedSecondEntry: false);
        var progressReports = new List<RomCompressionProgress>();

        RomCompressionResult compressed = RomCompressionService.CompressRom(
            rom,
            targetSizeMiB: 1,
            new CaptureProgress(progressReports.Add));
        RomCompressionResult decompressed = RomCompressionService.DecompressRom(compressed.Data);

        Assert.Equal(file1, decompressed.Data.AsSpan(0xe000, file1.Length).ToArray());
        uint physicalStart = FindDmaPhysicalStart(compressed.Data, 0x7960, 1526, 0xe000);
        Assert.True(Yaz0Codec.IsYaz0(compressed.Data.AsSpan(checked((int)physicalStart))));
        Assert.Equal(100, progressReports.Last().Percent);
    }

    [Fact]
    public void LocalRomFixturesDecompressByteForByteWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertDecompressesToFixture(
            FixturePath(root, "compressed", "Tidens_okarina-NTSC10.z64"),
            FixturePath(root, "decompressed", "Tidens_okarina-NTSC10.z64"));
        AssertDecompressesToFixture(
            FixturePath(root, "compressed", "Tidens_okarina-NTSC11.z64"),
            FixturePath(root, "decompressed", "Tidens_okarina-NTSC11.z64"));
        AssertDecompressesToFixture(
            FixturePath(root, "compressed", "Tidens_okarina-NTSC12.z64"),
            FixturePath(root, "decompressed", "Tidens_okarina-NTSC12.z64"));
        AssertDecompressesToFixture(
            FixturePath(root, "compressed", "Tidens_okarina-NTSCGC.z64"),
            FixturePath(root, "decompressed", "Tidens_okarina-NTSCGC.z64"));
        AssertDecompressesToFixture(
            FixturePath(root, "compressed", "Tidens_okarina-NTSCMQ.z64"),
            FixturePath(root, "decompressed", "Tidens_okarina-NTSCMQ.z64"));
        AssertDecompressesToFixture(
            FixturePath(root, "compressed", "Tidens_okarina-IQUENTSC.z64"),
            FixturePath(root, "decompressed", "Tidens_okarina-IQUENTSC.z64"));
    }

    [Fact]
    public void LocalRetailRomFixturesDecompressByteForByteWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertRetailDecompressesToFixture(root, "ntsc10_orig.z64");
        AssertRetailDecompressesToFixture(root, "ntsc11_orig.z64");
        AssertRetailDecompressesToFixture(root, "ntsc12_orig.z64");
        AssertRetailDecompressesToFixture(root, "ntscgc_orig.z64");
        AssertRetailDecompressesToFixture(root, "ntscmq_orig.z64");
        AssertRetailDecompressesToFixture(root, "pal10_orig.z64");
        AssertRetailDecompressesToFixture(root, "pal11_orig.z64");
        AssertRetailDecompressesToFixture(root, "palgc_orig.z64");
        AssertRetailDecompressesToFixture(root, "palmq_orig.z64");
    }

    [Fact]
    public void LocalRomFixturesLoadNormalMessagesWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertLoadsMessagesFromFixture(
            FixturePath(root, "compressed", "Tidens_okarina-NTSC10.z64"),
            "NTSC 1.0");
        AssertLoadsMessagesFromFixture(
            FixturePath(root, "compressed", "Tidens_okarina-NTSC11.z64"),
            "NTSC 1.1");
        AssertLoadsMessagesFromFixture(
            FixturePath(root, "compressed", "Tidens_okarina-NTSC12.z64"),
            "NTSC 1.2");
        AssertLoadsMessagesFromFixture(
            FixturePath(root, "compressed", "Tidens_okarina-NTSCGC.z64"),
            "NTSC GameCube");
        AssertLoadsMessagesFromFixture(
            FixturePath(root, "compressed", "Tidens_okarina-NTSCMQ.z64"),
            "NTSC Master Quest");
        AssertLoadsMessagesFromFixture(
            FixturePath(root, "compressed", "Tidens_okarina-IQUENTSC.z64"),
            "NTSC iQue");
    }

    [Fact]
    public void LocalRomFixturesLocateRomFontResourcesWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-NTSC10.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-NTSC11.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-NTSC12.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-NTSCGC.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-NTSCMQ.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-PAL10.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-PAL11.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-IQUENTSC.z64"));
    }

    [Fact]
    public void LocalRetailRomFixturesLoadNormalMessagesWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertLoadsMessagesFromFixture(FixturePath(root, "retailcompressed", "ntsc10_orig.z64"), "Retail NTSC 1.0", 2114);
        AssertLoadsMessagesFromFixture(FixturePath(root, "retailcompressed", "ntsc11_orig.z64"), "Retail NTSC 1.1", 2114);
        AssertLoadsMessagesFromFixture(FixturePath(root, "retailcompressed", "ntsc12_orig.z64"), "Retail NTSC 1.2", 2114);
        AssertLoadsMessagesFromFixture(FixturePath(root, "retailcompressed", "ntscgc_orig.z64"), "Retail NTSC GameCube", 2114);
        AssertLoadsMessagesFromFixture(FixturePath(root, "retailcompressed", "ntscmq_orig.z64"), "Retail NTSC Master Quest", 2114);
        AssertRetailNtscGameCubeReadsFromCorrectMessageBank(FixturePath(root, "retailcompressed", "ntscgc_orig.z64"));
    }

    [Fact]
    public void LocalPalRomFixturesLoadLanguageBanksWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertLoadsPalLanguageBanks(FixturePath(root, "compressed", "Tidens_okarina-PAL10.z64"), "PAL 1.0");
        AssertLoadsPalLanguageBanks(FixturePath(root, "compressed", "Tidens_okarina-PAL11.z64"), "PAL 1.1");
        AssertLoadsPalLanguageBanks(FixturePath(root, "compressed", "Tidens_okarina-PALGC.z64"), "PAL GameCube");
        AssertLoadsPalLanguageBanks(FixturePath(root, "compressed", "Tidens_okarina-PALMQ.z64"), "PAL Master Quest");
    }

    [Fact]
    public void LocalRetailPalRomFixturesLoadLanguageBanksWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertLoadsPalLanguageBanks(FixturePath(root, "retailcompressed", "pal10_orig.z64"), "Retail PAL 1.0", 2115);
        AssertLoadsPalLanguageBanks(FixturePath(root, "retailcompressed", "pal11_orig.z64"), "Retail PAL 1.1", 2115);
        AssertLoadsPalLanguageBanks(FixturePath(root, "retailcompressed", "palgc_orig.z64"), "Retail PAL GameCube", 2115);
        AssertLoadsPalLanguageBanks(FixturePath(root, "retailcompressed", "palmq_orig.z64"), "Retail PAL Master Quest", 2115);
    }

    [Fact]
    public void LocalRetailPalRomFixturesPreserveFontMessageWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertLoadsFontMessage(FixturePath(root, "retailcompressed", "pal10_orig.z64"));
        AssertLoadsFontMessage(FixturePath(root, "retailcompressed", "pal11_orig.z64"));
        AssertLoadsFontMessage(FixturePath(root, "retailcompressed", "palgc_orig.z64"));
        AssertLoadsFontMessage(FixturePath(root, "retailcompressed", "palmq_orig.z64"));
    }

    [Fact]
    public void LocalRetailPalRomFixtureReadsCompleteFontOrderBytesWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        string romPath = FixturePath(root, "retailcompressed", "pal10_orig.z64");
        if (!File.Exists(romPath))
        {
            return;
        }

        RomMessageData data = RomMessageService.LoadMessages(romPath);
        Assert.True(RomMessageService.TryReadActiveFontOrderBytes(data, out byte[] raw));
        Assert.Equal(
            """
            0123456789
            ABCDEFGHIJKLMN
            OPQRSTUVWXYZ
            abcdefghijklmn
            opqrstuvwxyz
             -.
            """.Replace("\r\n", "\n"),
            FontOrderCodec.ToEditorText(raw));
        Assert.Equal(raw, FontOrderCodec.FromEditorText(FontOrderCodec.ToEditorText(raw)));
    }

    [Fact]
    public void LocalRetailPalRomFixtureModernHeaderExportImportPreservesBytesWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        string romPath = FixturePath(root, "retaildecompressed", "pal10_orig.z64");
        if (!File.Exists(romPath))
        {
            return;
        }

        string headerPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.h");
        string tempRomPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.z64");
        try
        {
            RomMessageData data = RomMessageService.LoadMessages(romPath);
            var banks = RomMessageService.LoadModernExportBanks(data, data.Entries);
            File.WriteAllText(headerPath, CHeaderExporter.ExportModernLanguages(banks.Jpn, banks.Nes, banks.Ger, banks.Fra));

            IReadOnlyDictionary<int, List<MessageEntry>> replacements =
                HeaderDocumentService.BuildAllWesternRomImports(headerPath, data, data.Entries);
            RomMessageData replaced = RomMessageService.ReplaceMessageBanks(data, data.Entries, replacements);
            RomMessageService.SaveMessages(tempRomPath, replaced, replaced.Entries, compressOverride: false);

            Assert.Equal(File.ReadAllBytes(romPath), File.ReadAllBytes(tempRomPath));
        }
        finally
        {
            File.Delete(headerPath);
            File.Delete(tempRomPath);
        }
    }

    [Fact]
    public void LocalModernJapaneseAndEnglishHeaderOffersOnlyWesternSlotsWhenAvailable()
    {
        string path = @"D:\test30\testntsc.h";
        if (!File.Exists(path))
        {
            return;
        }

        List<CHeaderMessageSlot> slots = HeaderDocumentService.GetAvailableWesternSlots(path);

        Assert.Equal([CHeaderMessageSlot.Nes], slots);
    }

    [Fact]
    public void LocalRetailPalRomFixturesReadSharedTableLanguageMessagesWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertReadsSharedTableLanguageMessages(FixturePath(root, "retailcompressed", "pal10_orig.z64"));
        AssertReadsSharedTableLanguageMessages(FixturePath(root, "retailcompressed", "pal11_orig.z64"));
        AssertReadsSharedTableLanguageMessages(FixturePath(root, "retailcompressed", "palgc_orig.z64"));
        AssertReadsSharedTableLanguageMessages(FixturePath(root, "retailcompressed", "palmq_orig.z64"));
    }

    [Fact]
    public void LocalRomFixturesLoadJapaneseModernExportBankWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertLoadsJapaneseModernExportBank(FixturePath(root, "retailcompressed", "ntsc12_orig.z64"));
        AssertLoadsJapaneseModernExportBank(FixturePath(root, "compressed", "Tidens_okarina-IQUENTSC.z64"));
    }

    [Fact]
    public void PalRomProfilesUseExpectedFontBaselines()
    {
        Assert.Equal(RomFontBaseline.PalMultiLanguage, GetProfile("Retail PAL 1.0").FontBaseline);
        Assert.Equal(RomFontBaseline.PalMultiLanguage, GetProfile("Retail PAL 1.1").FontBaseline);
        Assert.Equal(RomFontBaseline.PalMultiLanguage, GetProfile("PAL 1.0").FontBaseline);
        Assert.Equal(RomFontBaseline.PalMultiLanguage, GetProfile("PAL 1.1").FontBaseline);

        Assert.Equal(RomFontBaseline.PalGameCube, GetProfile("Retail PAL GameCube").FontBaseline);
        Assert.Equal(RomFontBaseline.PalGameCube, GetProfile("Retail PAL Master Quest").FontBaseline);
        Assert.Equal(RomFontBaseline.PalGameCube, GetProfile("PAL GameCube").FontBaseline);
        Assert.Equal(RomFontBaseline.PalGameCube, GetProfile("PAL Master Quest").FontBaseline);
        Assert.Equal(RomFontBaseline.Standard, GetProfile("NTSC 1.2").FontBaseline);
    }

    [Fact]
    public void RomFontBaselinesHaveCorrectIWithCircumflexWidth()
    {
        Assert.Equal(12.0, RomFontBaselineMetrics.GetDefaultAdvance(RomFontBaseline.Standard, 0x81));
        Assert.Equal(6.0, RomFontBaselineMetrics.GetDefaultAdvance(RomFontBaseline.PalGameCube, 0x81));
        Assert.Equal(6.0, RomFontBaselineMetrics.GetDefaultAdvance(RomFontBaseline.PalMultiLanguage, 0x81));
        Assert.Equal(8.0, RomFontBaselineMetrics.GetDefaultAdvance(RomFontBaseline.Standard, 0x20));
        Assert.Equal(8.0, RomFontBaselineMetrics.GetDefaultAdvance(RomFontBaseline.PalGameCube, 0x20));
        Assert.Equal(8.0, RomFontBaselineMetrics.GetDefaultAdvance(RomFontBaseline.PalMultiLanguage, 0x20));
    }

    [Fact]
    public void LocalRomFixturesLoadCreditsWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertLoadsCreditsFromFixture(FixturePath(root, "compressed", "Tidens_okarina-NTSC12.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "compressed", "Tidens_okarina-NTSCGC.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "compressed", "Tidens_okarina-NTSCMQ.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "compressed", "Tidens_okarina-IQUENTSC.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "compressed", "Tidens_okarina-PAL10.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "compressed", "Tidens_okarina-PALGC.z64"));
    }

    [Fact]
    public void LocalRetailRomFixturesLoadCreditsWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertLoadsCreditsFromFixture(FixturePath(root, "retailcompressed", "ntsc10_orig.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "retailcompressed", "ntscgc_orig.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "retailcompressed", "pal10_orig.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "retailcompressed", "palgc_orig.z64"));
    }

    [Fact]
    public void LocalRomFixturesSaveMessagesRoundtripWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertSavesMessagesRoundtrip(
            FixturePath(root, "compressed", "Tidens_okarina-NTSC12.z64"),
            FixturePath(root, "decompressed", "Tidens_okarina-NTSC12.z64"));
        AssertSavesMessagesRoundtrip(
            FixturePath(root, "compressed", "Tidens_okarina-IQUENTSC.z64"),
            FixturePath(root, "decompressed", "Tidens_okarina-IQUENTSC.z64"));
    }

    [Fact]
    public void LocalRomFixturesSaveCreditsRoundtripWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        AssertSavesCreditsRoundtrip(
            FixturePath(root, "compressed", "Tidens_okarina-NTSC12.z64"),
            FixturePath(root, "decompressed", "Tidens_okarina-NTSC12.z64"));
    }

    [Fact]
    public void LocalPalRomFixtureSaveUpdatesFontMessagePointerWhenAvailable()
    {
        string? root = Environment.GetEnvironmentVariable("HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        string romPath = FixturePath(root, "compressed", "Tidens_okarina-PAL10.z64");
        if (!File.Exists(romPath))
        {
            return;
        }

        string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.z64");
        try
        {
            RomMessageData data = RomMessageService.LoadMessages(romPath);
            MessageEntry firstMessage = data.Entries.First(entry => entry.Id != 0xfffc);
            firstMessage.Text += ".";

            RomMessageService.SaveMessages(tempPath, data, data.Entries);
            RomCompressionResult saved = RomCompressionService.DecompressRom(File.ReadAllBytes(tempPath));

            int fontMessageRomOffset = ReadFontLoadOrderedFontMessageOffset(saved.Data);
            Assert.Equal(Encoding.ASCII.GetBytes("0123456789"), saved.Data.AsSpan(fontMessageRomOffset, 10).ToArray());
            Assert.Equal(Encoding.ASCII.GetBytes("ABCDEFGHIJKLMN"), saved.Data.AsSpan(fontMessageRomOffset + 11, 14).ToArray());
        }
        finally
        {
            File.Delete(tempPath);
        }
    }


    private static byte[] CreateSyntheticNtsc12Rom(byte[] secondFilePayload, bool compressedSecondEntry)
    {
        var rom = new byte[0x120000];
        Encoding.ASCII.GetBytes("26-05-18 10:00:06").CopyTo(rom, 0x793c);

        WriteDmaEntry(rom, 0, 0, 0xd8c0, 0, 0);
        WriteDmaEntry(
            rom,
            30,
            0xe000,
            (uint)(0xe000 + (compressedSecondEntry ? Yaz0Codec.Decode(secondFilePayload).Length : secondFilePayload.Length)),
            compressedSecondEntry ? 0x10000u : 0xe000u,
            (uint)(compressedSecondEntry ? 0x10000 + secondFilePayload.Length : 0));

        secondFilePayload.CopyTo(rom, compressedSecondEntry ? 0x10000 : 0xe000);
        return rom;
    }

    private static void AssertDecompressesToFixture(string compressedPath, string decompressedPath)
    {
        if (!File.Exists(compressedPath) || !File.Exists(decompressedPath))
        {
            return;
        }

        byte[] compressed = File.ReadAllBytes(compressedPath);
        byte[] expected = File.ReadAllBytes(decompressedPath);
        RomCompressionResult actual = RomCompressionService.DecompressRom(compressed);
        Assert.Equal(expected, actual.Data);
    }

    private static void AssertRetailDecompressesToFixture(string root, string fileName)
    {
        AssertDecompressesToFixture(
            FixturePath(root, "retailcompressed", fileName),
            FixturePath(root, "retaildecompressed", fileName));
    }

    private static void AssertLoadsMessagesFromFixture(string romPath, string expectedProfileName, int expectedCount = 2105)
    {
        if (!File.Exists(romPath))
        {
            return;
        }

        RomMessageData data = RomMessageService.LoadMessages(romPath);

        Assert.Equal(expectedProfileName, data.Profile.Name);
        Assert.Equal(expectedCount, data.Entries.Count);
        Assert.True(data.WasCompressed);
    }

    private static void AssertRetailNtscGameCubeReadsFromCorrectMessageBank(string romPath)
    {
        if (!File.Exists(romPath))
        {
            return;
        }

        RomMessageData data = RomMessageService.LoadMessages(romPath);
        MessageEntry pocketEgg = Assert.Single(data.Entries, entry => entry.Id == 0x0001);

        Assert.Equal("Retail NTSC GameCube", data.Profile.Name);
        Assert.Contains("You borrowed a", pocketEgg.Text);
        Assert.Contains("[item:2d]", pocketEgg.Text);
        Assert.DoesNotContain("We all look alike", pocketEgg.Text);
    }

    private static void AssertLocatesRomFontResources(string romPath)
    {
        if (!File.Exists(romPath))
        {
            return;
        }

        RomCompressionResult decompressed = RomCompressionService.DecompressRom(File.ReadAllBytes(romPath));
        RomFontResources resources = RomFontService.Locate(decompressed.Data, decompressed.Profile);

        Assert.True(resources.GlyphDataOffset > 0);
        Assert.True(resources.WidthTableOffset > 0);
        Assert.Equal(RomFontResources.GlyphByteSize, RomFontService.ReadGlyph(decompressed.Data, resources, 0x41).Length);
        Assert.Equal(8.0f, RomFontService.ReadWidth(decompressed.Data, resources, 0x20));
        Assert.Equal(8.0f, RomFontService.ReadWidth(decompressed.Data, resources, 0x21));
        Assert.Equal(14.0f, RomFontService.ReadWidth(decompressed.Data, resources, 0x25));

        var glyphSource = new RomGlyphSource(decompressed.Data, resources);
        Assert.True(File.Exists(glyphSource.GetGlyphPath(0x41)));
        Assert.Equal(RomFontService.ReadWidth(decompressed.Data, resources, 0x41), (float)glyphSource.GetAdvance(0x41));
    }

    private static void AssertLoadsPalLanguageBanks(string romPath, string expectedProfileName, int expectedCount = 2105)
    {
        if (!File.Exists(romPath))
        {
            return;
        }

        RomMessageData language1 = RomMessageService.LoadMessages(romPath, 0);
        RomMessageData language2 = RomMessageService.LoadMessages(romPath, 1);
        RomMessageData language3 = RomMessageService.LoadMessages(romPath, 2);

        Assert.Equal(expectedProfileName, language1.Profile.Name);
        Assert.Equal(0, language1.ActiveMessageBankIndex);
        Assert.Equal(1, language2.ActiveMessageBankIndex);
        Assert.Equal(2, language3.ActiveMessageBankIndex);
        Assert.Equal(expectedCount, language1.Entries.Count);
        Assert.Equal(language1.Entries.Count - 1, language2.Entries.Count);
        Assert.Equal(language1.Entries.Count - 1, language3.Entries.Count);
        Assert.Contains(language1.Entries, entry => entry.Id == 0xfffc);
        Assert.DoesNotContain(language2.Entries, entry => entry.Id == 0xfffc);
        Assert.DoesNotContain(language3.Entries, entry => entry.Id == 0xfffc);
        Assert.Contains(
            language1.Entries.Zip(language2.Entries),
            pair => pair.First.Text != pair.Second.Text);
        Assert.Contains(
            language1.Entries.Zip(language3.Entries),
            pair => pair.First.Text != pair.Second.Text);
    }

    private static void AssertLoadsFontMessage(string romPath)
    {
        if (!File.Exists(romPath))
        {
            return;
        }

        RomMessageData data = RomMessageService.LoadMessages(romPath);
        MessageEntry fontMessage = Assert.Single(data.Entries, entry => entry.Id == 0xfffc);

        Assert.Contains("0123456789", fontMessage.Text);
        Assert.Contains("ABCDEFGHIJKLMN", fontMessage.Text);
        Assert.Contains("OPQRSTUVWXYZ", fontMessage.Text);
        Assert.Contains("abcdefghijklmn", fontMessage.Text);
        Assert.Contains("opqrstuvwxyz", fontMessage.Text);
    }

    private static void AssertReadsSharedTableLanguageMessages(string romPath)
    {
        if (!File.Exists(romPath))
        {
            return;
        }

        RomMessageData german = RomMessageService.LoadMessages(romPath, 1);
        RomMessageData french = RomMessageService.LoadMessages(romPath, 2);

        MessageEntry germanCojiro = Assert.Single(german.Entries, entry => entry.Id == 0x0002);
        Assert.StartsWith("[unskippable][item:2f][quicktexton]Du hast Kiki", germanCojiro.Text);
        Assert.Contains("Henni", germanCojiro.Text);

        MessageEntry germanGhostShop = Assert.Single(german.Entries, entry => entry.Id == 0x70f5);
        Assert.Contains("Nachtschwärmer", germanGhostShop.Text);
        Assert.Contains("[poe]", germanGhostShop.Text);

        MessageEntry germanZelda = Assert.Single(german.Entries, entry => entry.Id == 0x2079);
        Assert.StartsWith("[unskippable][textspeed:01]Ich weiß...[textspeed:00]", germanZelda.Text);
        Assert.Contains("Natürlich erzählte ich meinem Vater", germanZelda.Text);

        MessageEntry frenchZelda = Assert.Single(french.Entries, entry => entry.Id == 0x2079);
        Assert.StartsWith("[unskippable][textspeed:01]........[textspeed:00]", frenchZelda.Text);
        Assert.Contains("Quoi qu'il en soit", frenchZelda.Text);
    }

    private static void AssertLoadsJapaneseModernExportBank(string romPath)
    {
        if (!File.Exists(romPath))
        {
            return;
        }

        RomMessageData data = RomMessageService.LoadMessages(romPath);
        Assert.NotNull(data.Profile.JapaneseMessageBank);

        var banks = RomMessageService.LoadModernExportBanks(data, data.Entries);
        Assert.NotNull(banks.Jpn);
        Assert.NotNull(banks.Nes);
        Assert.NotEmpty(banks.Jpn);
        Assert.All(banks.Jpn, entry => Assert.Equal(0x08, entry.Bank));

        MessageEntry firstJapanese = Assert.Single(banks.Jpn, entry => entry.Id == 0x0001);
        Assert.NotNull(firstJapanese.OriginalEncodedBytes);
        Assert.True(firstJapanese.OriginalEncodedBytes!.AsSpan(0, 2).SequenceEqual([(byte)0x81, (byte)0x99]));

        string exported = CHeaderExporter.ExportModernLanguages(banks.Jpn, banks.Nes, banks.Ger, banks.Fra);
        Assert.Contains("DEFINE_MESSAGE(0x0001", exported);
        Assert.Contains("MSG(\nUNSKIPPABLE ITEM_ICON(ITEM_POCKET_EGG)", exported);
        Assert.Contains("ポケットタマゴ", exported);
    }

    private static RomVersionProfile GetProfile(string name)
    {
        return Assert.Single(RomVersionDatabase.Profiles, profile => profile.Name == name);
    }

    private static void AssertRawDeflateVector(byte[] source, string expectedHex)
    {
        byte[] encoded = RawDeflateCodec.Encode(source);
        Assert.Equal(Convert.FromHexString(expectedHex), encoded);
        Assert.Equal(source, RawDeflateCodec.Decode(encoded, source.Length));
    }

    private static void AssertLoadsCreditsFromFixture(string romPath)
    {
        if (!File.Exists(romPath))
        {
            return;
        }

        RomMessageData credits = RomMessageService.LoadMessages(
            romPath,
            section: RomMessageSection.Credits);

        Assert.Equal(RomMessageSection.Credits, credits.ActiveSection);
        Assert.NotEmpty(credits.Entries);
        Assert.All(credits.Entries, entry => Assert.Equal(11, entry.Type));
    }

    private static void AssertSavesMessagesRoundtrip(string romPath, string expectedDecompressedPath)
    {
        AssertSavesRomSectionRoundtrip(romPath, expectedDecompressedPath, RomMessageSection.Messages);
    }

    private static void AssertSavesCreditsRoundtrip(string romPath, string expectedDecompressedPath)
    {
        AssertSavesRomSectionRoundtrip(romPath, expectedDecompressedPath, RomMessageSection.Credits);
    }

    private static void AssertSavesRomSectionRoundtrip(
        string romPath,
        string expectedDecompressedPath,
        RomMessageSection section)
    {
        if (!File.Exists(romPath) || !File.Exists(expectedDecompressedPath))
        {
            return;
        }

        string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.z64");
        try
        {
            RomMessageData data = RomMessageService.LoadMessages(romPath, section: section);
            RomMessageService.SaveMessages(tempPath, data, data.Entries);

            byte[] expected = File.ReadAllBytes(expectedDecompressedPath);
            RomCompressionResult actual = RomCompressionService.DecompressRom(File.ReadAllBytes(tempPath));
            RomMessageData reloaded = RomMessageService.LoadMessages(
                tempPath,
                data.ActiveMessageBankIndex,
                data.ActiveSection);

            AssertEntriesEqual(data.Entries, reloaded.Entries);
            MessageBankProfile changedBank = section == RomMessageSection.Credits
                ? data.Profile.CreditsBank
                : data.Profile.MessageBanks[data.ActiveMessageBankIndex];
            AssertRomEqualExceptMessageBank(expected, actual.Data, changedBank);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private static string FixturePath(string root, string kind, string fileName) =>
        Path.Combine(root, kind, fileName);

    private static void AssertEntriesEqual(IReadOnlyList<MessageEntry> expected, IReadOnlyList<MessageEntry> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Id, actual[i].Id);
            Assert.Equal(expected[i].Type, actual[i].Type);
            Assert.Equal(expected[i].Position, actual[i].Position);
            Assert.Equal(expected[i].Bank, actual[i].Bank);
            Assert.Equal(expected[i].TableEndMarkerId, actual[i].TableEndMarkerId);
            Assert.Equal(expected[i].TableHasFinalEndMarker, actual[i].TableHasFinalEndMarker);
            Assert.Equal(expected[i].PreserveOffsetWithoutMessageData, actual[i].PreserveOffsetWithoutMessageData);
            Assert.Equal(expected[i].Text, actual[i].Text);
        }
    }

    private static void AssertRomEqualExceptMessageBank(byte[] expected, byte[] actual, MessageBankProfile bank)
    {
        Assert.Equal(expected.Length, actual.Length);

        var excludedRanges = new[]
        {
            (Start: bank.MessageTableOffset, End: bank.MessageTableOffset + bank.MessageTableSize),
            (Start: bank.MessageDataOffset, End: bank.MessageDataOffset + bank.MessageDataSize),
        }
            .OrderBy(range => range.Start)
            .ToArray();

        int offset = 0;
        foreach (var range in excludedRanges)
        {
            if (range.Start > offset)
            {
                AssertRangesEqual(expected, actual, offset, range.Start - offset);
            }

            offset = Math.Max(offset, range.End);
        }

        if (offset < expected.Length)
        {
            AssertRangesEqual(expected, actual, offset, expected.Length - offset);
        }
    }

    private static void AssertRangesEqual(byte[] expected, byte[] actual, int offset, int length)
    {
        byte[] expectedRange = expected.AsSpan(offset, length).ToArray();
        byte[] actualRange = actual.AsSpan(offset, length).ToArray();
        Assert.Equal(expectedRange, actualRange);
    }


    private static void WriteDmaEntry(byte[] rom, int index, uint virtualStart, uint virtualEnd, uint physicalStart, uint physicalEnd)
    {
        int offset = 0x7960 + (index * 16);
        WriteUInt32BigEndian(rom, offset, virtualStart);
        WriteUInt32BigEndian(rom, offset + 4, virtualEnd);
        WriteUInt32BigEndian(rom, offset + 8, physicalStart);
        WriteUInt32BigEndian(rom, offset + 12, physicalEnd);
    }

    private static uint ReadUInt32BigEndian(ReadOnlySpan<byte> data, int offset) =>
        ((uint)data[offset] << 24)
        | ((uint)data[offset + 1] << 16)
        | ((uint)data[offset + 2] << 8)
        | data[offset + 3];

    private static uint FindDmaPhysicalStart(byte[] rom, int tableOffset, int entryCount, uint virtualStart)
    {
        for (int i = 0; i < entryCount; i++)
        {
            int offset = tableOffset + (i * 16);
            if (ReadUInt32BigEndian(rom, offset) == virtualStart)
            {
                return ReadUInt32BigEndian(rom, offset + 8);
            }
        }

        throw new InvalidDataException($"DMA entry 0x{virtualStart:x8} was not found.");
    }

    private static int ReadFontLoadOrderedFontMessageOffset(byte[] rom)
    {
        byte[] prolog = [0x27, 0xbd, 0xff, 0xc0, 0xaf, 0xb3, 0x00, 0x24];
        int functionOffset = FindBytes(rom, prolog);
        Assert.True(functionOffset >= 0);

        uint fontMessageVirt = ReadLuiAddiuAddress(rom, functionOffset + 0x08, functionOffset + 0x0c);
        uint segmentVirt = ReadLuiAddiuAddress(rom, functionOffset + 0x38, functionOffset + 0x40);
        uint segmentRom = ReadLuiAddiuAddress(rom, functionOffset + 0x48, functionOffset + 0x54);
        return checked((int)(segmentRom + (fontMessageVirt - segmentVirt)));
    }

    private static uint ReadLuiAddiuAddress(byte[] rom, int luiOffset, int addiuOffset)
    {
        ushort hi = ReadUInt16BigEndian(rom, luiOffset + 2);
        ushort lo = ReadUInt16BigEndian(rom, addiuOffset + 2);
        return unchecked((uint)((hi << 16) + (lo >= 0x8000 ? lo - 0x10000 : lo)));
    }

    private static ushort ReadUInt16BigEndian(byte[] data, int offset) =>
        (ushort)((data[offset] << 8) | data[offset + 1]);

    private static int FindBytes(byte[] data, byte[] pattern)
    {
        for (int i = 0; i <= data.Length - pattern.Length; i++)
        {
            if (data.AsSpan(i, pattern.Length).SequenceEqual(pattern))
            {
                return i;
            }
        }

        return -1;
    }

    private static void WriteUInt32BigEndian(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private sealed class CaptureProgress(Action<RomCompressionProgress> onReport) : IProgress<RomCompressionProgress>
    {
        public void Report(RomCompressionProgress value) => onReport(value);
    }
}
