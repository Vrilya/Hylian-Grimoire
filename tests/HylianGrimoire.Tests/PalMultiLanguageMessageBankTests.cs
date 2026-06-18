using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class PalMultiLanguageMessageBankTests
{
    private const int SharedTableOffset = 0x0100;
    private const int SharedTableSize = 0x0400;
    private const int Language1DataOffset = 0x1000;
    private const int Language2DataOffset = 0x2000;
    private const int Language3DataOffset = 0x3000;
    private const int LanguageDataSize = 0x0800;
    private const int Language2PointerTableOffset = 0x4000;
    private const int Language3PointerTableOffset = 0x4100;
    private const int FontOrderRoutineOffset = 0x0020;
    private const uint MessageSegmentStart = 0x0700_0000;
    private const int RomSize = 0x5000;
    private static readonly byte[] FontLoadOrderedFontProlog = [0x27, 0xbd, 0xff, 0xc0, 0xaf, 0xb3, 0x00, 0x24];

    [Fact]
    public void PalLanguageBanksExcludeFontMessageAndUsePointerTables()
    {
        SyntheticPalRom rom = SyntheticPalRom.Create();
        RomMessageData language1 = rom.CreateData(activeBankIndex: 0);

        RomMessageData language2 = RomMessageService.SwitchMessageBank(
            language1,
            language1.Entries,
            messageBankIndex: 1,
            patchCurrentBank: false);
        RomMessageData language3 = RomMessageService.SwitchMessageBank(
            language1,
            language1.Entries,
            messageBankIndex: 2,
            patchCurrentBank: false);

        Assert.Contains(language1.Entries, entry => entry.Id == FontOrderCodec.MessageId);
        Assert.DoesNotContain(language2.Entries, entry => entry.Id == FontOrderCodec.MessageId);
        Assert.DoesNotContain(language3.Entries, entry => entry.Id == FontOrderCodec.MessageId);

        Assert.Equal(["NES short", "NES second message is deliberately much longer"], GetTextsWithoutFontMessage(language1.Entries));
        Assert.Equal(["GER very long first message to force a different second offset", "GER two"], GetTextsWithoutFontMessage(language2.Entries));
        Assert.Equal(["FRA first", "FRA second"], GetTextsWithoutFontMessage(language3.Entries));
    }

    [Fact]
    public void ReplacingOnePointerTableBankPreservesOtherLanguagesAndSharedTable()
    {
        SyntheticPalRom rom = SyntheticPalRom.Create();
        RomMessageData language1 = rom.CreateData(activeBankIndex: 0);
        byte[] before = language1.DecompressedRom.ToArray();
        List<MessageEntry> patchedGerman =
        [
            CreateMessage(0x6004, "GER patched first message"),
            CreateMessage(0x6005, "GER patched second message"),
        ];

        RomMessageData updated = RomMessageService.ReplaceMessageBanks(
            language1,
            language1.Entries,
            new Dictionary<int, List<MessageEntry>>
            {
                [1] = patchedGerman,
            });

        AssertSectionEqual(before, updated.DecompressedRom, SharedTableOffset, SharedTableSize);
        AssertSectionEqual(before, updated.DecompressedRom, Language1DataOffset, LanguageDataSize);
        AssertSectionEqual(before, updated.DecompressedRom, Language3DataOffset, LanguageDataSize);
        AssertSectionEqual(before, updated.DecompressedRom, Language3PointerTableOffset, 12);
        AssertSectionNotEqual(before, updated.DecompressedRom, Language2DataOffset, LanguageDataSize);
        AssertSectionNotEqual(before, updated.DecompressedRom, Language2PointerTableOffset, 12);

        RomMessageData reloadedGerman = RomMessageService.SwitchMessageBank(
            updated,
            updated.Entries,
            messageBankIndex: 1,
            patchCurrentBank: false);
        RomMessageData reloadedFrench = RomMessageService.SwitchMessageBank(
            updated,
            updated.Entries,
            messageBankIndex: 2,
            patchCurrentBank: false);

        Assert.Equal(["GER patched first message", "GER patched second message"], GetTextsWithoutFontMessage(reloadedGerman.Entries));
        Assert.Equal(["FRA first", "FRA second"], GetTextsWithoutFontMessage(reloadedFrench.Entries));
    }

    [Fact]
    public void SavingUnchangedPalLanguageBankPreservesBytes()
    {
        using TestDirectory testDir = TestDirectory.Create();
        SyntheticPalRom rom = SyntheticPalRom.Create();
        RomMessageData language1 = rom.CreateData(activeBankIndex: 0);
        string path = testDir.PathFor("unchanged-pal.z64");

        RomMessageService.SaveMessages(
            path,
            language1,
            language1.Entries,
            compressOverride: false);

        Assert.Equal(rom.Bytes, File.ReadAllBytes(path));
    }

    [Fact]
    public void LanguageOnePatchPlanShowsSharedTableDataAndFontOrderPatches()
    {
        SyntheticPalRom rom = SyntheticPalRom.Create();
        RomMessageData language1 = rom.CreateData(activeBankIndex: 0);

        RomMessagePatchPlan plan = RomMessageService.BuildActiveSectionPatchPlan(
            language1.DecompressedRom,
            language1.Profile,
            messageBankIndex: 0,
            RomMessageSection.Messages,
            language1.Entries);

        Assert.Equal(0, plan.MessageBankIndex);
        Assert.Equal(RomMessageSection.Messages, plan.Section);
        Assert.False(plan.DropsFontOrderEntry);
        Assert.Equal(
            [
                RomMessagePatchOperationKind.SectionWrite,
                RomMessagePatchOperationKind.LuiAddiuAddressWrite,
                RomMessagePatchOperationKind.LuiAddiuAddressWrite,
                RomMessagePatchOperationKind.SectionWrite,
            ],
            plan.Operations.Select(operation => operation.Kind).ToArray());

        var tableWrite = Assert.IsType<RomMessageSectionWriteOperation>(plan.Operations[0]);
        Assert.Equal("message table", tableWrite.Name);
        Assert.Equal(SharedTableOffset, tableWrite.Offset);
        Assert.Equal(SharedTableSize, tableWrite.Length);
        Assert.Contains(FontOrderCodec.MessageId, ReadMessageIdsFromTable(tableWrite.Payload));

        RomMessageLuiAddiuAddressWriteOperation[] fontPointerWrites = plan.Operations
            .OfType<RomMessageLuiAddiuAddressWriteOperation>()
            .ToArray();
        Assert.Collection(
            fontPointerWrites,
            operation =>
            {
                Assert.Equal("font-order message pointer", operation.Name);
                Assert.Equal(FontOrderRoutineOffset + 0x08, operation.LuiOffset);
                Assert.Equal(FontOrderRoutineOffset + 0x0c, operation.AddiuOffset);
            },
            operation =>
            {
                Assert.Equal("font-order message end pointer", operation.Name);
                Assert.Equal(FontOrderRoutineOffset + 0x3c, operation.LuiOffset);
                Assert.Equal(FontOrderRoutineOffset + 0x44, operation.AddiuOffset);
            });

        var dataWrite = Assert.IsType<RomMessageSectionWriteOperation>(plan.Operations[^1]);
        Assert.Equal("message data", dataWrite.Name);
        Assert.Equal(Language1DataOffset, dataWrite.Offset);
        Assert.Equal(LanguageDataSize, dataWrite.Length);
    }

    [Fact]
    public void LanguageTwoPatchPlanShowsPointerTableAndNoSharedTableWrite()
    {
        SyntheticPalRom rom = SyntheticPalRom.Create();
        RomMessageData language1 = rom.CreateData(activeBankIndex: 0);
        RomMessageData language2 = RomMessageService.SwitchMessageBank(
            language1,
            language1.Entries,
            messageBankIndex: 1,
            patchCurrentBank: false);

        RomMessagePatchPlan plan = RomMessageService.BuildActiveSectionPatchPlan(
            language2.DecompressedRom,
            language2.Profile,
            messageBankIndex: 1,
            RomMessageSection.Messages,
            language2.Entries);

        Assert.Equal(1, plan.MessageBankIndex);
        Assert.True(plan.DropsFontOrderEntry);
        Assert.Equal(
            [RomMessagePatchOperationKind.PointerTableWrite, RomMessagePatchOperationKind.SectionWrite],
            plan.Operations.Select(operation => operation.Kind).ToArray());

        var pointerWrite = Assert.IsType<RomMessagePointerTableWriteOperation>(plan.Operations[0]);
        Assert.Equal(Language2PointerTableOffset, pointerWrite.Offset);
        Assert.Equal(3, pointerWrite.MessageOffsets.Count);
        Assert.Equal(4 * sizeof(uint), pointerWrite.Length);

        var dataWrite = Assert.IsType<RomMessageSectionWriteOperation>(plan.Operations[1]);
        Assert.Equal(Language2DataOffset, dataWrite.Offset);
        Assert.Equal(LanguageDataSize, dataWrite.Length);
        Assert.DoesNotContain(plan.Operations, operation => operation.Offset == SharedTableOffset);
    }

    private static IReadOnlyList<string> GetTextsWithoutFontMessage(IReadOnlyList<MessageEntry> entries)
    {
        return entries
            .Where(entry => entry.Id != FontOrderCodec.MessageId)
            .Select(entry => entry.Text)
            .ToArray();
    }

    private static MessageEntry CreateMessage(int id, string text)
    {
        return new MessageEntry(id, type: 0, position: 0, bank: 7, offset: 0)
        {
            Text = text,
        };
    }

    private static MessageEntry CreateFontMessage()
    {
        byte[] bytes = FontOrderCodec.GetStandardBytes();
        string text = FontOrderCodec.ToEditorText(bytes);
        return new MessageEntry(FontOrderCodec.MessageId, type: 0, position: 0, bank: 7, offset: 0)
        {
            Text = text,
            OriginalText = text,
            OriginalEncodedBytes = bytes,
        };
    }

    private static void WriteBank(
        byte[] rom,
        MessageBankProfile bank,
        IReadOnlyList<MessageEntry> entries,
        bool writeTable,
        bool writePointerTable)
    {
        GameProfile gameProfile = GameProfiles.Get(GameKind.OcarinaOfTime);
        var (tableBytes, messageBytes) = gameProfile.MessageBankCodec.Build(
            entries.Select(entry => entry.CreateSnapshot()).ToList(),
            gameProfile.EncodingProfile);

        Assert.True(tableBytes.Length <= bank.MessageTableSize);
        Assert.True(messageBytes.Length <= bank.MessageDataSize);

        if (writeTable)
        {
            tableBytes.CopyTo(rom.AsSpan(bank.MessageTableOffset, tableBytes.Length));
        }

        if (writePointerTable)
        {
            WritePointerTable(rom, bank.PointerTableOffset, tableBytes);
        }

        messageBytes.CopyTo(rom.AsSpan(bank.MessageDataOffset, messageBytes.Length));
    }

    private static void WritePointerTable(byte[] rom, int pointerTableOffset, byte[] tableBytes)
    {
        int[] offsets = ReadMessageOffsetsFromTable(tableBytes);
        for (int i = 0; i < offsets.Length; i++)
        {
            WriteUInt32BigEndian(rom, pointerTableOffset + (i * sizeof(uint)), 0x0700_0000u + (uint)offsets[i]);
        }
    }

    private static int[] ReadMessageOffsetsFromTable(byte[] tableBytes)
    {
        var offsets = new List<int>();
        for (int i = 0; i + 7 < tableBytes.Length; i += 8)
        {
            int id = (tableBytes[i] << 8) | tableBytes[i + 1];
            int offset = (tableBytes[i + 5] << 16) | (tableBytes[i + 6] << 8) | tableBytes[i + 7];

            if (id == 0xffff)
            {
                break;
            }

            offsets.Add(offset);
            if (id == 0xfffd)
            {
                break;
            }
        }

        return offsets.ToArray();
    }

    private static int[] ReadMessageIdsFromTable(IReadOnlyList<byte> tableBytes)
    {
        var ids = new List<int>();
        for (int i = 0; i + 7 < tableBytes.Count; i += 8)
        {
            int id = (tableBytes[i] << 8) | tableBytes[i + 1];
            if (id == 0xffff)
            {
                break;
            }

            ids.Add(id);
            if (id == 0xfffd)
            {
                break;
            }
        }

        return ids.ToArray();
    }

    private static int ReadMessageOffsetFromTable(byte[] rom, int tableOffset, int messageId)
    {
        for (int i = tableOffset; i + 7 < rom.Length; i += 8)
        {
            int id = (rom[i] << 8) | rom[i + 1];
            int offset = (rom[i + 5] << 16) | (rom[i + 6] << 8) | rom[i + 7];
            if (id == messageId)
            {
                return offset;
            }

            if (id is 0xfffd or 0xffff)
            {
                break;
            }
        }

        throw new InvalidDataException($"Message 0x{messageId:X4} was not found in the synthetic table.");
    }

    private static void WriteUInt32BigEndian(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void InstallFontOrderRoutine(byte[] rom, int fontMessageOffset, int fontMessageLength)
    {
        FontLoadOrderedFontProlog.CopyTo(rom.AsSpan(FontOrderRoutineOffset, FontLoadOrderedFontProlog.Length));
        uint fontMessageAddress = MessageSegmentStart + (uint)fontMessageOffset;
        uint fontMessageEndAddress = fontMessageAddress + (uint)Align4(fontMessageLength);

        WriteLuiAddiuAddress(rom, FontOrderRoutineOffset + 0x08, FontOrderRoutineOffset + 0x0c, fontMessageAddress);
        WriteLuiAddiuAddress(rom, FontOrderRoutineOffset + 0x38, FontOrderRoutineOffset + 0x40, MessageSegmentStart);
        WriteLuiAddiuAddress(rom, FontOrderRoutineOffset + 0x3c, FontOrderRoutineOffset + 0x44, fontMessageEndAddress);
    }

    private static void WriteLuiAddiuAddress(byte[] data, int luiOffset, int addiuOffset, uint address)
    {
        ushort lo = (ushort)(address & 0xffff);
        ushort hi = (ushort)((address >> 16) & 0xffff);
        if (lo >= 0x8000)
        {
            hi++;
        }

        WriteUInt16BigEndian(data, luiOffset + 2, hi);
        WriteUInt16BigEndian(data, addiuOffset + 2, lo);
    }

    private static void WriteUInt16BigEndian(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
    }

    private static int Align4(int value) => (value + 3) & ~3;

    private static void AssertSectionEqual(byte[] expected, byte[] actual, int offset, int length)
    {
        Assert.Equal(
            expected.AsSpan(offset, length).ToArray(),
            actual.AsSpan(offset, length).ToArray());
    }

    private static void AssertSectionNotEqual(byte[] expected, byte[] actual, int offset, int length)
    {
        Assert.NotEqual(
            expected.AsSpan(offset, length).ToArray(),
            actual.AsSpan(offset, length).ToArray());
    }

    private sealed record SyntheticPalRom(RomVersionProfile Profile, byte[] Bytes, IReadOnlyList<List<MessageEntry>> Banks)
    {
        public static SyntheticPalRom Create()
        {
            IReadOnlyList<MessageBankProfile> banks =
            [
                new("Language 1", SharedTableOffset, SharedTableSize, Language1DataOffset, LanguageDataSize),
                new(
                    "Language 2",
                    SharedTableOffset,
                    SharedTableSize,
                    Language2DataOffset,
                    LanguageDataSize,
                    MessageBankOffsetMode.Sequential,
                    ExcludesFontMessage: true,
                    PointerTableOffset: Language2PointerTableOffset),
                new(
                    "Language 3",
                    SharedTableOffset,
                    SharedTableSize,
                    Language3DataOffset,
                    LanguageDataSize,
                    MessageBankOffsetMode.Sequential,
                    ExcludesFontMessage: true,
                    PointerTableOffset: Language3PointerTableOffset),
            ];
            var profile = new RomVersionProfile(
                "Synthetic PAL Multi-Language",
                string.Empty,
                BuildDateOffset: 0,
                DmaTableOffset: 0,
                DmaEntryCount: 0,
                RomCodecKind.Yaz0,
                RawDeflateHasNoHeader: false,
                TargetCompressedSizeMiB: 0,
                CreditsTableOffset: 0,
                CreditsTableSize: 0,
                CreditsDataOffset: 0,
                CreditsDataSize: 0,
                banks,
                new HashSet<int>(),
                RomFontBaseline.PalMultiLanguage);
            byte[] rom = new byte[RomSize];
            MessageEntry fontMessage = CreateFontMessage();
            List<MessageEntry> language1 =
            [
                CreateMessage(0x6004, "NES short"),
                fontMessage,
                CreateMessage(0x6005, "NES second message is deliberately much longer"),
            ];
            List<MessageEntry> language2 =
            [
                CreateMessage(0x6004, "GER very long first message to force a different second offset"),
                CreateMessage(0x6005, "GER two"),
            ];
            List<MessageEntry> language3 =
            [
                CreateMessage(0x6004, "FRA first"),
                CreateMessage(0x6005, "FRA second"),
            ];

            WriteBank(rom, banks[0], language1, writeTable: true, writePointerTable: false);
            WriteBank(rom, banks[1], language2, writeTable: false, writePointerTable: true);
            WriteBank(rom, banks[2], language3, writeTable: false, writePointerTable: true);
            InstallFontOrderRoutine(
                rom,
                ReadMessageOffsetFromTable(rom, SharedTableOffset, FontOrderCodec.MessageId),
                fontMessage.OriginalEncodedBytes!.Length);

            return new SyntheticPalRom(profile, rom, [language1, language2, language3]);
        }

        public RomMessageData CreateData(int activeBankIndex)
        {
            return new RomMessageData(
                Banks[activeBankIndex].Select(entry => entry.CreateSnapshot()).ToList(),
                Profile,
                WasCompressed: false,
                DecompressedRom: Bytes.ToArray(),
                RomFontResources.Empty,
                activeBankIndex,
                RomMessageSection.Messages);
        }
    }

    private sealed class TestDirectory : IDisposable
    {
        private TestDirectory(string root)
        {
            Root = root;
        }

        private string Root { get; }

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
