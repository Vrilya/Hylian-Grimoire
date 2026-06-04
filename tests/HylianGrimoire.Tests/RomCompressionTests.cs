using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Compression;
using HylianGrimoire.Glyphs;
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
        var progressReports = new List<RomFileOperationProgress>();

        RomCompressionResult result = RomCompressionService.DecompressRom(rom, new CaptureProgress(progressReports.Add));

        Assert.Equal("NTSC 1.2", result.Profile.Name);
        Assert.Equal(file1, result.Data.AsSpan(0xe000, file1.Length).ToArray());
        Assert.Equal(0u, ReadUInt32BigEndian(result.Data, 0x7960 + 8));
        Assert.Equal(0u, ReadUInt32BigEndian(result.Data, 0x7960 + 12));
        Assert.Equal(0xe000u, ReadUInt32BigEndian(result.Data, 0x7960 + 30 * 16 + 8));
        Assert.Equal(0u, ReadUInt32BigEndian(result.Data, 0x7960 + 30 * 16 + 12));
        Assert.Equal(0, progressReports.First().Percent);
        Assert.Equal(100, progressReports.Last().Percent);
    }

    [Fact]
    public void CompressRomProducesLoadableCompressedRom()
    {
        byte[] file1 = Encoding.ASCII.GetBytes("compress-me-" + new string('R', 256));
        byte[] rom = CreateSyntheticNtsc12Rom(file1, compressedSecondEntry: false);
        var progressReports = new List<RomFileOperationProgress>();

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

#if HYLIAN_GRIMOIRE_LEGACY_OOT_FIXTURE_TESTS
    [LegacyOotRomFixtureFact]
    public void LocalRomFixturesDecompressByteForByteWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

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

    [LegacyOotRomFixtureFact]
    public void LocalRetailRomFixturesDecompressByteForByteWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

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
#endif

    [MajorasMaskRomFixturePairFact("mm_us_n64_compressed.z64", "mm_us_n64_decompressed.z64")]
    public void LocalMajorasMaskFixtureDecompressesByteForByteWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_us_n64_compressed.z64",
            "mm_us_n64_decompressed.z64");

        RomCompressionResult decompressed = RomCompressionService.DecompressRom(File.ReadAllBytes(compressedPath));

        Assert.Equal("Majora's Mask NTSC-U", decompressed.Profile.Name);
        Assert.True(decompressed.Profile.SupportsMessageEditing);
        Assert.Equal(File.ReadAllBytes(decompressedPath), decompressed.Data);
    }

    [MajorasMaskRomFixturePairFact("mm_us_gc_compressed.z64", "mm_us_gc_decompressed.z64")]
    public void LocalMajorasMaskUsGameCubeFixtureDecompressesByteForByteWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_us_gc_compressed.z64",
            "mm_us_gc_decompressed.z64");

        RomCompressionResult decompressed = RomCompressionService.DecompressRom(File.ReadAllBytes(compressedPath));

        Assert.Equal("Majora's Mask NTSC-U GameCube", decompressed.Profile.Name);
        Assert.True(decompressed.Profile.SupportsMessageEditing);
        Assert.Equal(File.ReadAllBytes(decompressedPath), decompressed.Data);
    }

    [MajorasMaskRomFixturePairFact("mm_eu_1.0_n64_compressed.z64", "mm_eu_1.0_n64_decompressed.z64")]
    public void LocalMajorasMaskEuFixtureDecompressesByteForByteWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_eu_1.0_n64_compressed.z64",
            "mm_eu_1.0_n64_decompressed.z64");

        RomCompressionResult decompressed = RomCompressionService.DecompressRom(File.ReadAllBytes(compressedPath));

        Assert.Equal("Majora's Mask EU 1.0", decompressed.Profile.Name);
        Assert.True(decompressed.Profile.SupportsMessageEditing);
        Assert.True(decompressed.Profile.Capabilities.SupportsMultipleMessageBanks);
        Assert.True(decompressed.Profile.Capabilities.SupportsCreditsEditing);
        Assert.Equal(File.ReadAllBytes(decompressedPath), decompressed.Data);
    }

    [MajorasMaskRomFixturePairFact("mm_eu_1.1_n64_compressed.z64", "mm_eu_1.1_n64_decompressed.z64")]
    public void LocalMajorasMaskEu11FixtureDecompressesByteForByteWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_eu_1.1_n64_compressed.z64",
            "mm_eu_1.1_n64_decompressed.z64");

        RomCompressionResult decompressed = RomCompressionService.DecompressRom(File.ReadAllBytes(compressedPath));

        Assert.Equal("Majora's Mask EU 1.1", decompressed.Profile.Name);
        Assert.True(decompressed.Profile.SupportsMessageEditing);
        Assert.True(decompressed.Profile.Capabilities.SupportsMultipleMessageBanks);
        Assert.True(decompressed.Profile.Capabilities.SupportsCreditsEditing);
        Assert.Equal(File.ReadAllBytes(decompressedPath), decompressed.Data);
    }

    [MajorasMaskRomFixturePairFact("mm_eu_gc_compressed.z64", "mm_eu_gc_decompressed.z64")]
    public void LocalMajorasMaskEuGameCubeFixtureDecompressesByteForByteWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_eu_gc_compressed.z64",
            "mm_eu_gc_decompressed.z64");

        RomCompressionResult decompressed = RomCompressionService.DecompressRom(File.ReadAllBytes(compressedPath));

        Assert.Equal("Majora's Mask EU GameCube", decompressed.Profile.Name);
        Assert.True(decompressed.Profile.SupportsMessageEditing);
        Assert.True(decompressed.Profile.Capabilities.SupportsMultipleMessageBanks);
        Assert.True(decompressed.Profile.Capabilities.SupportsCreditsEditing);
        Assert.Equal(File.ReadAllBytes(decompressedPath), decompressed.Data);
    }

    [MajorasMaskRomFixturePairFact("mm_us_n64_compressed.z64", "mm_us_n64_decompressed.z64")]
    public void LocalMajorasMaskProjectFixtureLoadsRomMessagesWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_us_n64_compressed.z64",
            "mm_us_n64_decompressed.z64");

        AssertLoadsMajorasMaskMessagesFromRom(compressedPath, wasCompressed: true);
        AssertLoadsMajorasMaskMessagesFromRom(decompressedPath, wasCompressed: false);
        AssertLoadsMajorasMaskCreditsFromRom(compressedPath, wasCompressed: true);
        AssertLoadsMajorasMaskCreditsFromRom(decompressedPath, wasCompressed: false);
    }

    [MajorasMaskRomFixturePairFact("mm_us_gc_compressed.z64", "mm_us_gc_decompressed.z64")]
    public void LocalMajorasMaskUsGameCubeProjectFixtureLoadsRomMessagesWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_us_gc_compressed.z64",
            "mm_us_gc_decompressed.z64");

        AssertLoadsMajorasMaskMessagesFromRom(
            compressedPath,
            wasCompressed: true,
            expectedProfileName: "Majora's Mask NTSC-U GameCube",
            expectedGlyphDataOffset: 0xadb000,
            expectedWidthTableOffset: 0xc73f10,
            expectedFontBaseline: RomFontBaseline.MajorasMaskUsGameCube);
        AssertLoadsMajorasMaskMessagesFromRom(
            decompressedPath,
            wasCompressed: false,
            expectedProfileName: "Majora's Mask NTSC-U GameCube",
            expectedGlyphDataOffset: 0xadb000,
            expectedWidthTableOffset: 0xc73f10,
            expectedFontBaseline: RomFontBaseline.MajorasMaskUsGameCube);
        AssertLoadsMajorasMaskCreditsFromRom(
            compressedPath,
            wasCompressed: true,
            expectedProfileName: "Majora's Mask NTSC-U GameCube");
        AssertLoadsMajorasMaskCreditsFromRom(
            decompressedPath,
            wasCompressed: false,
            expectedProfileName: "Majora's Mask NTSC-U GameCube");
    }

    [MajorasMaskRomFixturePairFact("mm_eu_1.0_n64_compressed.z64", "mm_eu_1.0_n64_decompressed.z64")]
    public void LocalMajorasMaskEuProjectFixtureLoadsRomMessagesWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_eu_1.0_n64_compressed.z64",
            "mm_eu_1.0_n64_decompressed.z64");

        AssertLoadsMajorasMaskEuMessagesFromRom(compressedPath, wasCompressed: true);
        AssertLoadsMajorasMaskEuMessagesFromRom(decompressedPath, wasCompressed: false);
        AssertLoadsMajorasMaskEuCreditsFromRom(compressedPath, wasCompressed: true);
        AssertLoadsMajorasMaskEuCreditsFromRom(decompressedPath, wasCompressed: false);
    }

    [MajorasMaskRomFixturePairFact("mm_eu_1.1_n64_compressed.z64", "mm_eu_1.1_n64_decompressed.z64")]
    public void LocalMajorasMaskEu11ProjectFixtureLoadsRomMessagesWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_eu_1.1_n64_compressed.z64",
            "mm_eu_1.1_n64_decompressed.z64");

        AssertLoadsMajorasMaskEuMessagesFromRom(
            compressedPath,
            wasCompressed: true,
            expectedProfileName: "Majora's Mask EU 1.1",
            expectedWidthTableOffset: 0xdac9d0);
        AssertLoadsMajorasMaskEuMessagesFromRom(
            decompressedPath,
            wasCompressed: false,
            expectedProfileName: "Majora's Mask EU 1.1",
            expectedWidthTableOffset: 0xdac9d0);
        AssertLoadsMajorasMaskEuCreditsFromRom(
            compressedPath,
            wasCompressed: true,
            expectedProfileName: "Majora's Mask EU 1.1");
        AssertLoadsMajorasMaskEuCreditsFromRom(
            decompressedPath,
            wasCompressed: false,
            expectedProfileName: "Majora's Mask EU 1.1");
    }

    [MajorasMaskRomFixturePairFact("mm_eu_gc_compressed.z64", "mm_eu_gc_decompressed.z64")]
    public void LocalMajorasMaskEuGameCubeProjectFixtureLoadsRomMessagesWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_eu_gc_compressed.z64",
            "mm_eu_gc_decompressed.z64");

        AssertLoadsMajorasMaskEuMessagesFromRom(
            compressedPath,
            wasCompressed: true,
            expectedProfileName: "Majora's Mask EU GameCube",
            expectedGlyphDataOffset: 0xaaf000,
            expectedWidthTableOffset: 0xdb99d0);
        AssertLoadsMajorasMaskEuMessagesFromRom(
            decompressedPath,
            wasCompressed: false,
            expectedProfileName: "Majora's Mask EU GameCube",
            expectedGlyphDataOffset: 0xaaf000,
            expectedWidthTableOffset: 0xdb99d0);
        AssertLoadsMajorasMaskEuCreditsFromRom(
            compressedPath,
            wasCompressed: true,
            expectedProfileName: "Majora's Mask EU GameCube");
        AssertLoadsMajorasMaskEuCreditsFromRom(
            decompressedPath,
            wasCompressed: false,
            expectedProfileName: "Majora's Mask EU GameCube");
    }

    [MajorasMaskRomFixtureFact("mm_us_n64_decompressed.z64")]
    public void LocalMajorasMaskProjectFixtureSavesDecompressedMessagesByteForByteWhenAvailable()
    {
        string decompressedPath = LocalRomFixtures.GetRequiredMajorasMaskPath("mm_us_n64_decompressed.z64");

        string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.z64");
        try
        {
            RomMessageData data = RomMessageService.LoadMessages(decompressedPath);
            RomMessageService.SaveMessages(tempPath, data, data.Entries, compressOverride: false);

            Assert.Equal(File.ReadAllBytes(decompressedPath), File.ReadAllBytes(tempPath));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [MajorasMaskRomFixtureFact("mm_eu_1.0_n64_decompressed.z64")]
    public void LocalMajorasMaskEuProjectFixtureSavesDecompressedMessagesByteForByteWhenAvailable()
    {
        string decompressedPath = LocalRomFixtures.GetRequiredMajorasMaskPath("mm_eu_1.0_n64_decompressed.z64");

        for (int bankIndex = 0; bankIndex < 4; bankIndex++)
        {
            AssertSavesMajorasMaskSectionByteForByte(decompressedPath, bankIndex, RomMessageSection.Messages);
        }

        AssertSavesMajorasMaskSectionByteForByte(decompressedPath, messageBankIndex: 0, RomMessageSection.Credits);
    }

    [MajorasMaskRomFixtureFact("mm_eu_1.1_n64_decompressed.z64")]
    public void LocalMajorasMaskEu11ProjectFixtureSavesDecompressedMessagesByteForByteWhenAvailable()
    {
        string decompressedPath = LocalRomFixtures.GetRequiredMajorasMaskPath("mm_eu_1.1_n64_decompressed.z64");

        for (int bankIndex = 0; bankIndex < 4; bankIndex++)
        {
            AssertSavesMajorasMaskSectionByteForByte(decompressedPath, bankIndex, RomMessageSection.Messages);
        }

        AssertSavesMajorasMaskSectionByteForByte(decompressedPath, messageBankIndex: 0, RomMessageSection.Credits);
    }

    [MajorasMaskRomFixtureFact("mm_eu_gc_decompressed.z64")]
    public void LocalMajorasMaskEuGameCubeProjectFixtureSavesDecompressedMessagesByteForByteWhenAvailable()
    {
        string decompressedPath = LocalRomFixtures.GetRequiredMajorasMaskPath("mm_eu_gc_decompressed.z64");

        for (int bankIndex = 0; bankIndex < 4; bankIndex++)
        {
            AssertSavesMajorasMaskSectionByteForByte(decompressedPath, bankIndex, RomMessageSection.Messages);
        }

        AssertSavesMajorasMaskSectionByteForByte(decompressedPath, messageBankIndex: 0, RomMessageSection.Credits);
    }

    [MajorasMaskRomFixtureFact("mm_us_gc_decompressed.z64")]
    public void LocalMajorasMaskUsGameCubeProjectFixtureSavesDecompressedMessagesByteForByteWhenAvailable()
    {
        string decompressedPath = LocalRomFixtures.GetRequiredMajorasMaskPath("mm_us_gc_decompressed.z64");

        AssertSavesMajorasMaskSectionByteForByte(decompressedPath, messageBankIndex: 0, RomMessageSection.Messages);
        AssertSavesMajorasMaskSectionByteForByte(decompressedPath, messageBankIndex: 0, RomMessageSection.Credits);
    }

    [MajorasMaskRomFixtureFact("mm_us_n64_decompressed.z64")]
    public void LocalMajorasMaskProjectFixtureReloadsChangedRomFontWidthWhenAvailable()
    {
        string decompressedPath = LocalRomFixtures.GetRequiredMajorasMaskPath("mm_us_n64_decompressed.z64");

        string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.z64");
        try
        {
            RomMessageData data = RomMessageService.LoadMessages(decompressedPath);
            RomFontService.WriteWidth(data.DecompressedRom, data.FontResources, 0x7b, 12.0f);

            RomMessageService.SaveMessages(tempPath, data, data.Entries, compressOverride: false);
            RomMessageData reloaded = RomMessageService.LoadMessages(tempPath);

            Assert.Equal(12.0f, RomFontService.ReadWidth(reloaded.DecompressedRom, reloaded.FontResources, 0x7b));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [MajorasMaskRomFixtureFact("mm_us_n64_compressed.z64")]
    public void LocalMajorasMaskProjectFixtureSavesCompressedTextAndGlyphChangesWhenAvailable()
    {
        string compressedPath = LocalRomFixtures.GetRequiredMajorasMaskPath("mm_us_n64_compressed.z64");

        string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.z64");
        try
        {
            RomMessageData data = RomMessageService.LoadMessages(compressedPath);
            MessageEntry strayFairy = Assert.Single(data.Entries, entry => entry.Id == 0x0011);
            strayFairy.Text = strayFairy.Text.Replace("Stray Fairy", "Lost Fairy", StringComparison.Ordinal);

            byte[] replacementGlyph = RomFontService.ReadGlyph(data.DecompressedRom, data.FontResources, 0x7d);
            RomFontService.WriteGlyph(data.DecompressedRom, data.FontResources, 0x7b, replacementGlyph);
            RomFontService.WriteWidth(data.DecompressedRom, data.FontResources, 0x7b, 12.0f);

            RomMessageService.SaveMessages(tempPath, data, data.Entries, compressOverride: true);
            RomMessageData reloaded = RomMessageService.LoadMessages(tempPath);

            Assert.True(reloaded.WasCompressed);
            MessageEntry reloadedStrayFairy = Assert.Single(reloaded.Entries, entry => entry.Id == 0x0011);
            Assert.Contains("Lost Fairy", reloadedStrayFairy.Text);
            Assert.Equal(
                replacementGlyph,
                RomFontService.ReadGlyph(reloaded.DecompressedRom, reloaded.FontResources, 0x7b));
            Assert.Equal(12.0f, RomFontService.ReadWidth(reloaded.DecompressedRom, reloaded.FontResources, 0x7b));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [MajorasMaskRomFixtureFact("mm_us_n64_decompressed.z64")]
    public void LocalMajorasMaskProjectFixtureExportsDataFilesByteForByteWhenAvailable()
    {
        string decompressedPath = LocalRomFixtures.GetRequiredMajorasMaskPath("mm_us_n64_decompressed.z64");

        string tempDir = Path.Combine(Path.GetTempPath(), "HylianGrimoireTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string tblPath = Path.Combine(tempDir, "nes_message_data_static.tbl");
        string binPath = Path.Combine(tempDir, "nes_message_data_static.bin");
        try
        {
            RomMessageData data = RomMessageService.LoadMessages(decompressedPath);
            List<MessageEntry> entries = MessageExportService.GetTableFileSaveEntries(
                data.Entries,
                excludeFontOrderEntry: true,
                data.Profile.GameProfile);
            MessageFileService.SaveTableFiles(entries, tblPath, binPath, data.Profile.GameProfile);

            MessageBankProfile bank = data.Profile.DefaultMessageBank;
            byte[] expectedTable = data.DecompressedRom.AsSpan(bank.MessageTableOffset, bank.MessageTableSize).ToArray();
            byte[] expectedMessages = data.DecompressedRom.AsSpan(bank.MessageDataOffset, bank.MessageDataSize).ToArray();

            Assert.Equal(expectedTable, File.ReadAllBytes(tblPath));
            Assert.Equal(expectedMessages, File.ReadAllBytes(binPath));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [MajorasMaskRomFixtureFact("mm_us_n64_decompressed.z64")]
    public void LocalMajorasMaskFixtureCompressesToLoadableRomWhenAvailable()
    {
        string decompressedPath = LocalRomFixtures.GetRequiredMajorasMaskPath("mm_us_n64_decompressed.z64");

        RomCompressionResult compressed = RomCompressionService.CompressRom(File.ReadAllBytes(decompressedPath));
        RomCompressionResult decompressed = RomCompressionService.DecompressRom(compressed.Data);

        Assert.Equal("Majora's Mask NTSC-U", compressed.Profile.Name);
        Assert.Equal(32 * 0x100000, compressed.Data.Length);
        AssertRomEqualExceptChecksum(File.ReadAllBytes(decompressedPath), decompressed.Data);
    }

    [MajorasMaskRomFixturePairFact("mm_us_n64_compressed.z64", "mm_us_n64_decompressed.z64")]
    public void LocalMajorasMaskFixtureCompressesByteForByteWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_us_n64_compressed.z64",
            "mm_us_n64_decompressed.z64");

        RomCompressionResult compressed = RomCompressionService.CompressRom(File.ReadAllBytes(decompressedPath));

        Assert.Equal("Majora's Mask NTSC-U", compressed.Profile.Name);
        Assert.Equal(File.ReadAllBytes(compressedPath), compressed.Data);
    }

    [MajorasMaskRomFixturePairFact("mm_us_gc_compressed.z64", "mm_us_gc_decompressed.z64")]
    public void LocalMajorasMaskUsGameCubeFixtureCompressesByteForByteWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_us_gc_compressed.z64",
            "mm_us_gc_decompressed.z64");

        RomCompressionResult compressed = RomCompressionService.CompressRom(File.ReadAllBytes(decompressedPath));

        Assert.Equal("Majora's Mask NTSC-U GameCube", compressed.Profile.Name);
        Assert.Equal(File.ReadAllBytes(compressedPath), compressed.Data);
    }

    [MajorasMaskRomFixturePairFact("mm_eu_1.0_n64_compressed.z64", "mm_eu_1.0_n64_decompressed.z64")]
    public void LocalMajorasMaskEuFixtureCompressesByteForByteWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_eu_1.0_n64_compressed.z64",
            "mm_eu_1.0_n64_decompressed.z64");

        RomCompressionResult compressed = RomCompressionService.CompressRom(File.ReadAllBytes(decompressedPath));

        Assert.Equal("Majora's Mask EU 1.0", compressed.Profile.Name);
        Assert.Equal(File.ReadAllBytes(compressedPath), compressed.Data);
    }

    [MajorasMaskRomFixturePairFact("mm_eu_1.1_n64_compressed.z64", "mm_eu_1.1_n64_decompressed.z64")]
    public void LocalMajorasMaskEu11FixtureCompressesByteForByteWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_eu_1.1_n64_compressed.z64",
            "mm_eu_1.1_n64_decompressed.z64");

        RomCompressionResult compressed = RomCompressionService.CompressRom(File.ReadAllBytes(decompressedPath));

        Assert.Equal("Majora's Mask EU 1.1", compressed.Profile.Name);
        Assert.Equal(File.ReadAllBytes(compressedPath), compressed.Data);
    }

    [MajorasMaskRomFixturePairFact("mm_eu_gc_compressed.z64", "mm_eu_gc_decompressed.z64")]
    public void LocalMajorasMaskEuGameCubeFixtureCompressesByteForByteWhenAvailable()
    {
        var (compressedPath, decompressedPath) = LocalRomFixtures.GetRequiredMajorasMaskPair(
            "mm_eu_gc_compressed.z64",
            "mm_eu_gc_decompressed.z64");

        RomCompressionResult compressed = RomCompressionService.CompressRom(File.ReadAllBytes(decompressedPath));

        Assert.Equal("Majora's Mask EU GameCube", compressed.Profile.Name);
        Assert.Equal(File.ReadAllBytes(compressedPath), compressed.Data);
    }

#if HYLIAN_GRIMOIRE_LEGACY_OOT_FIXTURE_TESTS
    [LegacyOotRomFixtureFact]
    public void LocalRomFixturesLoadNormalMessagesWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

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

    [LegacyOotRomFixtureFact]
    public void LocalRomFixturesLocateRomFontResourcesWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-NTSC10.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-NTSC11.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-NTSC12.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-NTSCGC.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-NTSCMQ.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-PAL10.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-PAL11.z64"));
        AssertLocatesRomFontResources(FixturePath(root, "compressed", "Tidens_okarina-IQUENTSC.z64"));
    }

    [LegacyOotRomFixtureFact]
    public void LocalRetailRomFixturesLoadNormalMessagesWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        AssertLoadsMessagesFromFixture(FixturePath(root, "retailcompressed", "ntsc10_orig.z64"), "Retail NTSC 1.0", 2114);
        AssertLoadsMessagesFromFixture(FixturePath(root, "retailcompressed", "ntsc11_orig.z64"), "Retail NTSC 1.1", 2114);
        AssertLoadsMessagesFromFixture(FixturePath(root, "retailcompressed", "ntsc12_orig.z64"), "Retail NTSC 1.2", 2114);
        AssertLoadsMessagesFromFixture(FixturePath(root, "retailcompressed", "ntscgc_orig.z64"), "Retail NTSC GameCube", 2114);
        AssertLoadsMessagesFromFixture(FixturePath(root, "retailcompressed", "ntscmq_orig.z64"), "Retail NTSC Master Quest", 2114);
        AssertRetailNtscGameCubeReadsFromCorrectMessageBank(FixturePath(root, "retailcompressed", "ntscgc_orig.z64"));
    }

    [LegacyOotRomFixtureFact]
    public void LocalPalRomFixturesLoadLanguageBanksWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        AssertLoadsPalLanguageBanks(FixturePath(root, "compressed", "Tidens_okarina-PAL10.z64"), "PAL 1.0");
        AssertLoadsPalLanguageBanks(FixturePath(root, "compressed", "Tidens_okarina-PAL11.z64"), "PAL 1.1");
        AssertLoadsPalLanguageBanks(FixturePath(root, "compressed", "Tidens_okarina-PALGC.z64"), "PAL GameCube");
        AssertLoadsPalLanguageBanks(FixturePath(root, "compressed", "Tidens_okarina-PALMQ.z64"), "PAL Master Quest");
    }

    [LegacyOotRomFixtureFact]
    public void LocalRetailPalRomFixturesLoadLanguageBanksWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        AssertLoadsPalLanguageBanks(FixturePath(root, "retailcompressed", "pal10_orig.z64"), "Retail PAL 1.0", 2115);
        AssertLoadsPalLanguageBanks(FixturePath(root, "retailcompressed", "pal11_orig.z64"), "Retail PAL 1.1", 2115);
        AssertLoadsPalLanguageBanks(FixturePath(root, "retailcompressed", "palgc_orig.z64"), "Retail PAL GameCube", 2115);
        AssertLoadsPalLanguageBanks(FixturePath(root, "retailcompressed", "palmq_orig.z64"), "Retail PAL Master Quest", 2115);
    }

    [LegacyOotRomFixtureFact]
    public void LocalRetailPalRomFixturesPreserveFontMessageWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        AssertLoadsFontMessage(FixturePath(root, "retailcompressed", "pal10_orig.z64"));
        AssertLoadsFontMessage(FixturePath(root, "retailcompressed", "pal11_orig.z64"));
        AssertLoadsFontMessage(FixturePath(root, "retailcompressed", "palgc_orig.z64"));
        AssertLoadsFontMessage(FixturePath(root, "retailcompressed", "palmq_orig.z64"));
    }

    [LegacyOotRomFixtureFact]
    public void LocalRetailPalRomFixtureReadsCompleteFontOrderBytesWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        string romPath = FixturePath(root, "retailcompressed", "pal10_orig.z64");
        LocalRomFixtures.RequirePath(romPath);

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

    [LegacyOotRomFixtureFact]
    public void LocalRetailPalRomFixtureModernHeaderExportImportPreservesBytesWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        string romPath = FixturePath(root, "retaildecompressed", "pal10_orig.z64");
        LocalRomFixtures.RequirePath(romPath);

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
#endif

    [Fact]
    public void LocalModernJapaneseAndEnglishHeaderOffersOnlyWesternSlotsWhenAvailable()
    {
        string path = Path.Combine(Path.GetTempPath(), $"modern-header-{Guid.NewGuid():N}.h");
        File.WriteAllText(path, """
        DEFINE_MESSAGE(0x0001, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        MSG("JPN")
        ,
        MSG("NES")
        ,
        MSG(/* MISSING */)
        ,
        MSG(/* MISSING */)
        )
        """);

        try
        {
            List<CHeaderMessageSlot> slots = HeaderDocumentService.GetAvailableWesternSlots(path);

            Assert.Equal([CHeaderMessageSlot.Nes], slots);
        }
        finally
        {
            File.Delete(path);
        }
    }

#if HYLIAN_GRIMOIRE_LEGACY_OOT_FIXTURE_TESTS
    [LegacyOotRomFixtureFact]
    public void LocalRetailPalRomFixturesReadSharedTableLanguageMessagesWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        AssertReadsSharedTableLanguageMessages(FixturePath(root, "retailcompressed", "pal10_orig.z64"));
        AssertReadsSharedTableLanguageMessages(FixturePath(root, "retailcompressed", "pal11_orig.z64"));
        AssertReadsSharedTableLanguageMessages(FixturePath(root, "retailcompressed", "palgc_orig.z64"));
        AssertReadsSharedTableLanguageMessages(FixturePath(root, "retailcompressed", "palmq_orig.z64"));
    }

    [LegacyOotRomFixtureFact]
    public void LocalRomFixturesLoadJapaneseModernExportBankWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        AssertLoadsJapaneseModernExportBank(FixturePath(root, "retailcompressed", "ntsc12_orig.z64"));
        AssertLoadsJapaneseModernExportBank(FixturePath(root, "compressed", "Tidens_okarina-IQUENTSC.z64"));
    }
#endif

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
        Assert.Equal(RomFontBaseline.MajorasMask, GetProfile("Majora's Mask NTSC-U").FontBaseline);
        Assert.Equal(RomFontBaseline.MajorasMaskUsGameCube, GetProfile("Majora's Mask NTSC-U GameCube").FontBaseline);
        Assert.Equal(RomFontBaseline.MajorasMaskEu, GetProfile("Majora's Mask EU 1.0").FontBaseline);
        Assert.Equal(RomFontBaseline.MajorasMaskEu, GetProfile("Majora's Mask EU 1.1").FontBaseline);
        Assert.Equal(RomFontBaseline.MajorasMaskEu, GetProfile("Majora's Mask EU GameCube").FontBaseline);
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
        Assert.Equal(MmGlyphMetrics.GetDefaultAdvance(0x9e), RomFontBaselineMetrics.GetDefaultAdvance(RomFontBaseline.MajorasMask, 0x9e));
        Assert.Equal(MmGlyphMetrics.GetDefaultAdvance(0x9e), RomFontBaselineMetrics.GetDefaultAdvance(RomFontBaseline.MajorasMaskEu, 0x9e));
    }

#if HYLIAN_GRIMOIRE_LEGACY_OOT_FIXTURE_TESTS
    [LegacyOotRomFixtureFact]
    public void LocalRomFixturesLoadCreditsWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        AssertLoadsCreditsFromFixture(FixturePath(root, "compressed", "Tidens_okarina-NTSC12.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "compressed", "Tidens_okarina-NTSCGC.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "compressed", "Tidens_okarina-NTSCMQ.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "compressed", "Tidens_okarina-IQUENTSC.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "compressed", "Tidens_okarina-PAL10.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "compressed", "Tidens_okarina-PALGC.z64"));
    }

    [LegacyOotRomFixtureFact]
    public void LocalRetailRomFixturesLoadCreditsWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        AssertLoadsCreditsFromFixture(FixturePath(root, "retailcompressed", "ntsc10_orig.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "retailcompressed", "ntscgc_orig.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "retailcompressed", "pal10_orig.z64"));
        AssertLoadsCreditsFromFixture(FixturePath(root, "retailcompressed", "palgc_orig.z64"));
    }

    [LegacyOotRomFixtureFact]
    public void LocalRomFixturesSaveMessagesRoundtripWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        AssertSavesMessagesRoundtrip(
            FixturePath(root, "compressed", "Tidens_okarina-NTSC12.z64"),
            FixturePath(root, "decompressed", "Tidens_okarina-NTSC12.z64"));
        AssertSavesMessagesRoundtrip(
            FixturePath(root, "compressed", "Tidens_okarina-IQUENTSC.z64"),
            FixturePath(root, "decompressed", "Tidens_okarina-IQUENTSC.z64"));
    }

    [LegacyOotRomFixtureFact]
    public void LocalRomFixturesSaveCreditsRoundtripWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        AssertSavesCreditsRoundtrip(
            FixturePath(root, "compressed", "Tidens_okarina-NTSC12.z64"),
            FixturePath(root, "decompressed", "Tidens_okarina-NTSC12.z64"));
    }

    [LegacyOotRomFixtureFact]
    public void LocalPalRomFixtureSaveUpdatesFontMessagePointerWhenAvailable()
    {
        string root = LocalRomFixtures.GetRequiredRoot();

        string romPath = FixturePath(root, "compressed", "Tidens_okarina-PAL10.z64");
        LocalRomFixtures.RequirePath(romPath);

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
#endif


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
        LocalRomFixtures.RequirePath(compressedPath);
        LocalRomFixtures.RequirePath(decompressedPath);

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
        LocalRomFixtures.RequirePath(romPath);

        RomMessageData data = RomMessageService.LoadMessages(romPath);

        Assert.Equal(expectedProfileName, data.Profile.Name);
        Assert.Equal(expectedCount, data.Entries.Count);
        Assert.True(data.WasCompressed);
    }

    private static void AssertRetailNtscGameCubeReadsFromCorrectMessageBank(string romPath)
    {
        LocalRomFixtures.RequirePath(romPath);

        RomMessageData data = RomMessageService.LoadMessages(romPath);
        MessageEntry pocketEgg = Assert.Single(data.Entries, entry => entry.Id == 0x0001);

        Assert.Equal("Retail NTSC GameCube", data.Profile.Name);
        Assert.Contains("You borrowed a", pocketEgg.Text);
        Assert.Contains("[item:2d]", pocketEgg.Text);
        Assert.DoesNotContain("We all look alike", pocketEgg.Text);
    }

    private static void AssertLocatesRomFontResources(string romPath)
    {
        LocalRomFixtures.RequirePath(romPath);

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
        LocalRomFixtures.RequirePath(romPath);

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
        LocalRomFixtures.RequirePath(romPath);

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
        LocalRomFixtures.RequirePath(romPath);

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
        LocalRomFixtures.RequirePath(romPath);

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
        LocalRomFixtures.RequirePath(romPath);

        RomMessageData credits = RomMessageService.LoadMessages(
            romPath,
            section: RomMessageSection.Credits);

        Assert.Equal(RomMessageSection.Credits, credits.ActiveSection);
        Assert.NotEmpty(credits.Entries);
        Assert.All(credits.Entries, entry => Assert.Equal(11, entry.Type));
    }

    private static void AssertLoadsMajorasMaskMessagesFromRom(
        string romPath,
        bool wasCompressed,
        string expectedProfileName = "Majora's Mask NTSC-U",
        int expectedGlyphDataOffset = 0xacc000,
        int expectedWidthTableOffset = 0xc669b0,
        RomFontBaseline expectedFontBaseline = RomFontBaseline.MajorasMask)
    {
        RomMessageData data = RomMessageService.LoadMessages(romPath);

        Assert.Equal(expectedProfileName, data.Profile.Name);
        Assert.Equal(expectedFontBaseline, data.Profile.FontBaseline);
        Assert.Equal(0, data.ActiveMessageBankIndex);
        Assert.Equal(RomMessageSection.Messages, data.ActiveSection);
        Assert.Equal(wasCompressed, data.WasCompressed);
        Assert.Equal(4589, data.Entries.Count);
        Assert.Equal(0x0000, data.Entries[0].Id);
        Assert.Equal(0xfffd, data.Entries[^1].Id);
        Assert.Contains(data.Entries, entry => entry.Id == 0xfffc);
        Assert.NotEqual(RomFontResources.Empty, data.FontResources);
        Assert.Equal(expectedGlyphDataOffset, data.FontResources.GlyphDataOffset);
        Assert.Equal(expectedWidthTableOffset, data.FontResources.WidthTableOffset);
        Assert.Equal(156, data.FontResources.GlyphCount);
        Assert.Equal(MmGlyphMetrics.DefaultWidths.Length, data.FontResources.WidthCount);
        Assert.Equal(9.0f, RomFontService.ReadWidth(data.DecompressedRom, data.FontResources, 0x9e));
        Assert.Equal(
            MmGlyphCatalog.GetOriginalGlyphBytes(0x9e),
            RomFontService.ReadGlyph(data.DecompressedRom, data.FontResources, 0x9e));
        Assert.Equal(
            MmGlyphCatalog.GetOriginalGlyphBytes(0x2c, expectedFontBaseline),
            RomFontService.ReadGlyph(data.DecompressedRom, data.FontResources, 0x2c));
        if (expectedFontBaseline == RomFontBaseline.MajorasMask)
        {
            Assert.False(MmGlyphCatalog.GetOriginalGlyphBytes(0x2c, RomFontBaseline.MajorasMaskEu)
                .SequenceEqual(RomFontService.ReadGlyph(data.DecompressedRom, data.FontResources, 0x2c)));
        }
        Assert.NotEqual(
            RomFontService.ReadGlyph(data.DecompressedRom, data.FontResources, 0x9d),
            RomFontService.ReadGlyph(data.DecompressedRom, data.FontResources, 0x9e));

        MessageEntry strayFairy = Assert.Single(data.Entries, entry => entry.Id == 0x0011);
        var metadata = Assert.IsType<MajorasMaskMessageMetadata>(strayFairy.CodecMetadata);
        Assert.Equal(0x11, metadata.IconId);
        Assert.Contains("Stray Fairy", strayFairy.Text);
        Assert.Contains("[delay:000a]", strayFairy.Text);
    }

    private static void AssertLoadsMajorasMaskEuMessagesFromRom(
        string romPath,
        bool wasCompressed,
        string expectedProfileName = "Majora's Mask EU 1.0",
        int expectedGlyphDataOffset = 0xaa0000,
        int expectedWidthTableOffset = 0xdac8b0)
    {
        string[] expectedTexts =
        [
            "Blue Rupee",
            "Blauer Rubin",
            "Rubis bleu",
            "Rupia Azul",
        ];

        for (int bankIndex = 0; bankIndex < expectedTexts.Length; bankIndex++)
        {
            RomMessageData data = RomMessageService.LoadMessages(romPath, messageBankIndex: bankIndex);

            Assert.Equal(expectedProfileName, data.Profile.Name);
            Assert.Equal(RomFontBaseline.MajorasMaskEu, data.Profile.FontBaseline);
            Assert.Equal(4, data.Profile.MessageBanks.Count);
            Assert.True(data.Profile.Capabilities.SupportsMultipleMessageBanks);
            Assert.Equal(bankIndex, data.ActiveMessageBankIndex);
            Assert.Equal(RomMessageSection.Messages, data.ActiveSection);
            Assert.Equal(wasCompressed, data.WasCompressed);
            Assert.NotEmpty(data.Entries);
            Assert.Equal(0x0000, data.Entries[0].Id);
            Assert.Equal(0xfffd, data.Entries[^1].Id);
            Assert.Contains(data.Entries, entry => entry.Id == 0xfffc);
            Assert.NotEqual(RomFontResources.Empty, data.FontResources);
            Assert.Equal(expectedGlyphDataOffset, data.FontResources.GlyphDataOffset);
            Assert.Equal(expectedWidthTableOffset, data.FontResources.WidthTableOffset);
            Assert.Equal(156, data.FontResources.GlyphCount);
            Assert.Equal(MmGlyphMetrics.DefaultWidths.Length, data.FontResources.WidthCount);
            Assert.Equal(
                MmGlyphCatalog.GetOriginalGlyphBytes(0x2c, RomFontBaseline.MajorasMaskEu),
                RomFontService.ReadGlyph(data.DecompressedRom, data.FontResources, 0x2c));
            Assert.False(MmGlyphCatalog.GetOriginalGlyphBytes(0x2c, RomFontBaseline.MajorasMask)
                .SequenceEqual(RomFontService.ReadGlyph(data.DecompressedRom, data.FontResources, 0x2c)));

            MessageEntry blueRupee = Assert.Single(data.Entries, entry => entry.Id == 0x0002);
            Assert.Contains(expectedTexts[bankIndex], blueRupee.Text);
            Assert.IsType<MajorasMaskMessageMetadata>(blueRupee.CodecMetadata);
        }
    }

    private static void AssertLoadsMajorasMaskCreditsFromRom(
        string romPath,
        bool wasCompressed,
        string expectedProfileName = "Majora's Mask NTSC-U")
    {
        RomMessageData credits = RomMessageService.LoadMessages(
            romPath,
            section: RomMessageSection.Credits);

        Assert.Equal(expectedProfileName, credits.Profile.Name);
        Assert.Equal(RomMessageSection.Credits, credits.ActiveSection);
        Assert.Equal(wasCompressed, credits.WasCompressed);
        Assert.Equal(45, credits.Entries.Count);
        Assert.Equal(0x4e20, credits.Entries[0].Id);
        Assert.Equal(0x4e4c, credits.Entries[^1].Id);
        Assert.All(credits.Entries, entry => Assert.Equal(11, entry.Type));
    }

    private static void AssertLoadsMajorasMaskEuCreditsFromRom(
        string romPath,
        bool wasCompressed,
        string expectedProfileName = "Majora's Mask EU 1.0")
    {
        RomMessageData credits = RomMessageService.LoadMessages(
            romPath,
            section: RomMessageSection.Credits);

        Assert.Equal(expectedProfileName, credits.Profile.Name);
        Assert.Equal(RomMessageSection.Credits, credits.ActiveSection);
        Assert.Equal(wasCompressed, credits.WasCompressed);
        Assert.Equal(45, credits.Entries.Count);
        Assert.Equal(0x4e20, credits.Entries[0].Id);
        Assert.Equal(0x4e4c, credits.Entries[^1].Id);
        Assert.All(credits.Entries, entry => Assert.Equal(11, entry.Type));
    }

    private static void AssertSavesMajorasMaskSectionByteForByte(
        string decompressedPath,
        int messageBankIndex,
        RomMessageSection section)
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.z64");
        try
        {
            RomMessageData data = RomMessageService.LoadMessages(
                decompressedPath,
                messageBankIndex,
                section);
            RomMessageService.SaveMessages(tempPath, data, data.Entries, compressOverride: false);

            Assert.Equal(File.ReadAllBytes(decompressedPath), File.ReadAllBytes(tempPath));
        }
        finally
        {
            File.Delete(tempPath);
        }
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
        LocalRomFixtures.RequirePath(romPath);
        LocalRomFixtures.RequirePath(expectedDecompressedPath);

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
        LocalRomFixtures.GetRequiredPath(Path.Combine(root, kind, fileName));

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

    private static void AssertRomEqualExceptChecksum(byte[] expected, byte[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        AssertRangesEqual(expected, actual, 0, 0x10);
        AssertRangesEqual(expected, actual, 0x18, expected.Length - 0x18);
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

    private sealed class CaptureProgress(Action<RomFileOperationProgress> onReport) : IProgress<RomFileOperationProgress>
    {
        public void Report(RomFileOperationProgress value) => onReport(value);
    }
}
