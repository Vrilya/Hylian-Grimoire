using HylianGrimoire.Games;
using HylianGrimoire.Headers;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using System.Text;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class WorkflowRoundtripTests
{
    [Fact]
    public void TableFilesCanRoundtripIntoHeaderWorkflow()
    {
        using TestDirectory testDir = TestDirectory.Create();
        string tablePath = testDir.PathFor("messages.tbl");
        string binaryPath = testDir.PathFor("messages.bin");
        string headerPath = testDir.PathFor("messages.h");
        List<MessageEntry> entries = CreateEntries("table");
        var tableWorkflow = new TableFileWorkflow();
        var headerWorkflow = new HeaderDocumentWorkflow();

        tableWorkflow.Save(
            tablePath,
            binaryPath,
            entries,
            excludeFontOrderEntry: false,
            GameProfiles.Get(GameKind.OcarinaOfTime));
        MessageFileDocument tableDocument = tableWorkflow.Load(tablePath, binaryPath);

        headerWorkflow.ExportCurrent(
            headerPath,
            tableDocument.Entries,
            tableDocument.GameProfile,
            CHeaderExportFormat.Modern);
        HeaderDocumentLoadResult headerDocument = headerWorkflow.Load(headerPath);

        Assert.Equal(GameKind.OcarinaOfTime, headerDocument.Document.GameProfile.Kind);
        Assert.Equal(0, headerDocument.ActiveLanguageIndex);
        AssertEntriesMatch(entries, headerDocument.Document.Languages[0]);
    }

    [Theory]
    [InlineData(CHeaderExportFormat.Legacy)]
    [InlineData(CHeaderExportFormat.Modern)]
    [InlineData(CHeaderExportFormat.OTRMod)]
    public void ExportedCurrentHeadersCanBeLoadedAgain(CHeaderExportFormat format)
    {
        using TestDirectory testDir = TestDirectory.Create();
        string headerPath = testDir.PathFor($"{format}.h");
        List<MessageEntry> entries = CreateEntries(format.ToString());
        var workflow = new HeaderDocumentWorkflow();

        workflow.ExportCurrent(
            headerPath,
            entries,
            GameProfiles.Get(GameKind.OcarinaOfTime),
            format);
        HeaderDocumentLoadResult result = workflow.Load(headerPath);

        AssertEntriesMatch(entries, result.Document.Languages[0]);
    }

    [Fact]
    public void MultiLanguageHeaderImportPatchesAllWesternRomBanks()
    {
        using TestDirectory testDir = TestDirectory.Create();
        string headerPath = testDir.PathFor("languages.h");
        List<MessageEntry> originalNes = CreateEntries("original NES");
        List<MessageEntry> originalGer = CreateEntries("original GER");
        List<MessageEntry> originalFra = CreateEntries("original FRA");
        List<MessageEntry> importedNes = CreateEntries("imported NES");
        List<MessageEntry> importedGer = CreateEntries("imported GER");
        List<MessageEntry> importedFra = CreateEntries("imported FRA");
        RomMessageData romData = CreateRomData([originalNes, originalGer, originalFra]);
        var workflow = new HeaderDocumentWorkflow();

        workflow.Save(
            headerPath,
            importedNes,
            GameProfiles.Get(GameKind.OcarinaOfTime),
            new Dictionary<int, List<MessageEntry>>
            {
                [0] = importedNes,
                [1] = importedGer,
                [2] = importedFra,
            });
        HeaderRomImportResult result = workflow.ImportIntoRom(
            headerPath,
            romData,
            romData.Entries,
            allWesternLanguages: true,
            selectedSlot: CHeaderMessageSlot.Nes);

        Assert.Equal([0, 1, 2], result.ReplacementBanks.Keys.Order().ToArray());
        AssertEntriesMatch(importedNes, result.RomData.Entries);

        RomMessageData gerBank = RomMessageService.SwitchMessageBank(
            result.RomData,
            result.RomData.Entries,
            1,
            patchCurrentBank: false);
        RomMessageData fraBank = RomMessageService.SwitchMessageBank(
            result.RomData,
            result.RomData.Entries,
            2,
            patchCurrentBank: false);

        AssertEntriesMatch(importedGer, gerBank.Entries);
        AssertEntriesMatch(importedFra, fraBank.Entries);
    }

    [Fact]
    public void SelectedHeaderImportPatchesOnlyActiveRomBank()
    {
        using TestDirectory testDir = TestDirectory.Create();
        string headerPath = testDir.PathFor("selected.h");
        List<MessageEntry> originalNes = CreateEntries("original NES");
        List<MessageEntry> originalGer = CreateEntries("original GER");
        List<MessageEntry> originalFra = CreateEntries("original FRA");
        List<MessageEntry> importedNes = CreateEntries("selected NES");
        List<MessageEntry> importedGer = CreateEntries("selected GER");
        RomMessageData romData = CreateRomData([originalNes, originalGer, originalFra], activeBankIndex: 1);
        var workflow = new HeaderDocumentWorkflow();

        workflow.Save(
            headerPath,
            importedNes,
            GameProfiles.Get(GameKind.OcarinaOfTime),
            new Dictionary<int, List<MessageEntry>>
            {
                [0] = importedNes,
                [1] = importedGer,
            });
        HeaderRomImportResult result = workflow.ImportIntoRom(
            headerPath,
            romData,
            romData.Entries,
            allWesternLanguages: false,
            selectedSlot: CHeaderMessageSlot.Nes);

        Assert.Equal([1], result.ReplacementBanks.Keys.ToArray());
        AssertEntriesMatch(importedNes, result.RomData.Entries);

        RomMessageData nesBank = RomMessageService.SwitchMessageBank(
            result.RomData,
            result.RomData.Entries,
            0,
            patchCurrentBank: false);
        RomMessageData fraBank = RomMessageService.SwitchMessageBank(
            result.RomData,
            result.RomData.Entries,
            2,
            patchCurrentBank: false);

        AssertEntriesMatch(originalNes, nesBank.Entries);
        AssertEntriesMatch(originalFra, fraBank.Entries);
    }

    [Fact]
    public async Task RomWorkflowSaveAndReloadPersistsPatchedMessages()
    {
        using TestDirectory testDir = TestDirectory.Create();
        string romPath = testDir.PathFor("patched.z64");
        List<MessageEntry> originalEntries = CreateEntries("original ROM");
        RomMessageData romData = CreateDetectableRomData(originalEntries);
        List<MessageEntry> patchedEntries = romData.Entries.Select(entry => entry.CreateSnapshot()).ToList();
        patchedEntries[0].Text = "Saved and loaded from ROM";
        patchedEntries[1].Text = "Patched through RomDocumentWorkflow";
        var workflow = new RomDocumentWorkflow();

        RomMessageData reloaded = await workflow.SaveAndReloadAsync(
            romPath,
            romData,
            patchedEntries,
            romData.Profile.GameProfile.EncodingProfile,
            compressOverride: false);

        Assert.True(File.Exists(romPath));
        Assert.False(reloaded.WasCompressed);
        Assert.Equal(romData.Profile.Name, reloaded.Profile.Name);
        Assert.Equal(romData.DecompressedRom.Length, reloaded.DecompressedRom.Length);
        AssertEntriesMatch(patchedEntries, reloaded.Entries);
    }

    [Fact]
    public void HeaderImportTooLargeForRomBankThrowsWithoutMutatingSourceRom()
    {
        using TestDirectory testDir = TestDirectory.Create();
        string headerPath = testDir.PathFor("oversized.h");
        List<MessageEntry> originalEntries = CreateEntries("original bank");
        List<MessageEntry> oversizedEntries = CreateEntries("oversized import");
        oversizedEntries[0].Text = new string('A', 0x1200);
        RomMessageData romData = CreateRomData([originalEntries]);
        RomMessageData beforeImport = romData.CreateSnapshot();
        var workflow = new HeaderDocumentWorkflow();

        workflow.Save(
            headerPath,
            oversizedEntries,
            GameProfiles.Get(GameKind.OcarinaOfTime));
        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => workflow.ImportIntoRom(
            headerPath,
            romData,
            romData.Entries,
            allWesternLanguages: false,
            selectedSlot: CHeaderMessageSlot.Nes));

        Assert.Contains("Encoded message data", exception.Message);
        Assert.Equal(beforeImport.DecompressedRom, romData.DecompressedRom);
        AssertEntriesMatch(beforeImport.Entries, romData.Entries);
    }

    [Fact]
    public async Task RomWorkflowSaveFailureLeavesExistingTargetFileUntouched()
    {
        using TestDirectory testDir = TestDirectory.Create();
        string romPath = testDir.PathFor("existing.z64");
        byte[] existingBytes = [0xde, 0xad, 0xbe, 0xef];
        File.WriteAllBytes(romPath, existingBytes);
        List<MessageEntry> originalEntries = CreateEntries("original ROM");
        RomMessageData romData = CreateDetectableRomData(originalEntries);
        List<MessageEntry> oversizedEntries = romData.Entries.Select(entry => entry.CreateSnapshot()).ToList();
        oversizedEntries[0].Text = "Oversized encoded payload";
        oversizedEntries[0].EncodedBytesOverride = new byte[romData.Profile.DefaultMessageBank.MessageDataSize + 1024];
        var workflow = new RomDocumentWorkflow();

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() => workflow.SaveAndReloadAsync(
            romPath,
            romData,
            oversizedEntries,
            romData.Profile.GameProfile.EncodingProfile,
            compressOverride: false));

        Assert.Contains("Encoded message data", exception.Message);
        Assert.Equal(existingBytes, File.ReadAllBytes(romPath));
        Assert.Empty(FindAtomicTempFiles(testDir.RootPath));
    }

    [Fact]
    public void FailedRomLanguageSwitchDoesNotMutateLoadedRomData()
    {
        List<MessageEntry> originalNes = CreateEntries("original NES");
        List<MessageEntry> originalGer = CreateEntries("original GER");
        RomMessageData romData = CreateRomData([originalNes, originalGer]);
        byte[] beforeSwitchRom = romData.DecompressedRom.ToArray();
        List<MessageEntry> oversizedEntries = romData.Entries.Select(entry => entry.CreateSnapshot()).ToList();
        oversizedEntries[0].Text = "Oversized switch payload";
        oversizedEntries[0].EncodedBytesOverride = new byte[0x2000];

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => RomMessageService.SwitchMessageBank(
            romData,
            oversizedEntries,
            messageBankIndex: 1,
            patchCurrentBank: true));

        Assert.Contains("Encoded message data", exception.Message);
        Assert.Equal(beforeSwitchRom, romData.DecompressedRom);
        AssertEntriesMatch(originalNes, romData.Entries);
    }

    private static List<MessageEntry> CreateEntries(string suffix)
    {
        return
        [
            new MessageEntry(0x6004, 0, 0, 7, 0)
            {
                Text = $"Hello {suffix}",
            },
            new MessageEntry(0x6005, 2, 3, 7, 0)
            {
                Text = $"Talk to Zelda {suffix}!",
            },
        ];
    }

    private static RomMessageData CreateRomData(
        IReadOnlyList<List<MessageEntry>> bankEntries,
        int activeBankIndex = 0)
    {
        IReadOnlyList<MessageBankProfile> banks = CreateMessageBanks(bankEntries.Count);
        var profile = new RomVersionProfile(
            "Workflow Test ROM",
            string.Empty,
            0,
            0,
            0,
            RomCodecKind.Yaz0,
            false,
            0,
            0,
            0,
            0,
            0,
            banks,
            new HashSet<int>());
        byte[] decompressedRom = new byte[0x7000];
        GameProfile gameProfile = GameProfiles.Get(GameKind.OcarinaOfTime);

        for (int i = 0; i < bankEntries.Count; i++)
        {
            WriteBank(decompressedRom, banks[i], bankEntries[i], gameProfile);
        }

        return new RomMessageData(
            bankEntries[activeBankIndex].Select(entry => entry.CreateSnapshot()).ToList(),
            profile,
            false,
            decompressedRom,
            RomFontResources.Empty,
            activeBankIndex,
            RomMessageSection.Messages);
    }

    private static RomMessageData CreateDetectableRomData(List<MessageEntry> entries)
    {
        RomVersionProfile profile = RomVersionDatabase.Profiles.Single(profile => profile.Name == "NTSC iQue");
        byte[] decompressedRom = new byte[GetDetectableRomLength(profile)];
        Encoding.ASCII.GetBytes(profile.BuildDate).CopyTo(decompressedRom, profile.BuildDateOffset);
        WriteDmaEntry(
            decompressedRom,
            profile.DmaTableOffset,
            virtualStart: 0,
            virtualEnd: (uint)decompressedRom.Length,
            physicalStart: 0,
            physicalEnd: 0);
        int messageDataEnd = profile.DefaultMessageBank.MessageDataOffset + profile.DefaultMessageBank.MessageDataSize;
        WriteDmaEntry(
            decompressedRom,
            profile.DmaTableOffset + 16,
            virtualStart: (uint)messageDataEnd,
            virtualEnd: (uint)(messageDataEnd + 0x10),
            physicalStart: (uint)messageDataEnd,
            physicalEnd: 0);
        WriteWidthTablePrefix(decompressedRom, 0x2000);
        WriteBank(decompressedRom, profile.DefaultMessageBank, entries, profile.GameProfile);

        return new RomMessageData(
            entries.Select(entry => entry.CreateSnapshot()).ToList(),
            profile,
            false,
            decompressedRom,
            RomFontResources.Empty,
            0,
            RomMessageSection.Messages);
    }

    private static int GetDetectableRomLength(RomVersionProfile profile)
    {
        const int iQueGlyphDataOffset = 0x8f1000;
        const int widthTableOffset = 0x2000;
        MessageBankProfile bank = profile.DefaultMessageBank;
        return new[]
        {
            profile.BuildDateOffset + profile.BuildDate.Length,
            profile.DmaTableOffset + (profile.DmaEntryCount * 16),
            bank.MessageTableOffset + bank.MessageTableSize,
            bank.MessageDataOffset + bank.MessageDataSize,
            iQueGlyphDataOffset + (RomFontResources.StandardGlyphCount * RomFontResources.GlyphByteSize),
            widthTableOffset + (RomFontResources.StandardWidthCount * sizeof(float)),
        }.Max();
    }

    private static IReadOnlyList<MessageBankProfile> CreateMessageBanks(int count)
    {
        return Enumerable
            .Range(0, count)
            .Select(index => new MessageBankProfile(
                $"Bank {index + 1}",
                0x0100 + (index * 0x2000),
                0x0400,
                0x1000 + (index * 0x2000),
                0x1000))
            .ToList();
    }

    private static void WriteBank(
        byte[] rom,
        MessageBankProfile bank,
        IReadOnlyList<MessageEntry> entries,
        GameProfile gameProfile)
    {
        var (tableBytes, messageBytes) = gameProfile.MessageBankCodec.Build(
            entries.Select(entry => entry.CreateSnapshot()).ToList(),
            gameProfile.EncodingProfile);

        Assert.True(tableBytes.Length <= bank.MessageTableSize);
        Assert.True(messageBytes.Length <= bank.MessageDataSize);
        tableBytes.CopyTo(rom.AsSpan(bank.MessageTableOffset, tableBytes.Length));
        messageBytes.CopyTo(rom.AsSpan(bank.MessageDataOffset, messageBytes.Length));
    }

    private static void WriteDmaEntry(
        byte[] rom,
        int offset,
        uint virtualStart,
        uint virtualEnd,
        uint physicalStart,
        uint physicalEnd)
    {
        WriteUInt32BigEndian(rom, offset, virtualStart);
        WriteUInt32BigEndian(rom, offset + 4, virtualEnd);
        WriteUInt32BigEndian(rom, offset + 8, physicalStart);
        WriteUInt32BigEndian(rom, offset + 12, physicalEnd);
    }

    private static void WriteUInt32BigEndian(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void WriteWidthTablePrefix(byte[] rom, int offset)
    {
        byte[] prefix =
        [
            0x41, 0x00, 0x00, 0x00,
            0x41, 0x00, 0x00, 0x00,
            0x40, 0xc0, 0x00, 0x00,
            0x41, 0x10, 0x00, 0x00,
            0x41, 0x10, 0x00, 0x00,
            0x41, 0x60, 0x00, 0x00,
            0x41, 0x40, 0x00, 0x00,
            0x40, 0x40, 0x00, 0x00,
            0x40, 0xe0, 0x00, 0x00,
            0x40, 0xe0, 0x00, 0x00,
        ];
        prefix.CopyTo(rom.AsSpan(offset, prefix.Length));
    }

    private static IReadOnlyList<string> FindAtomicTempFiles(string path)
        => Directory.GetFiles(path, "*.hylian-write-*", SearchOption.AllDirectories);

    private static void AssertEntriesMatch(
        IReadOnlyList<MessageEntry> expected,
        IReadOnlyList<MessageEntry> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Id, actual[i].Id);
            Assert.Equal(expected[i].Type, actual[i].Type);
            Assert.Equal(expected[i].Position, actual[i].Position);
            Assert.Equal(expected[i].Text, actual[i].Text);
        }
    }

    private sealed class TestDirectory : IDisposable
    {
        private TestDirectory(string root)
        {
            Root = root;
        }

        private string Root { get; }

        public string RootPath => Root;

        public static TestDirectory Create()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "HylianGrimoireTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TestDirectory(root);
        }

        public string PathFor(string fileName) => Path.Combine(Root, fileName);

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
