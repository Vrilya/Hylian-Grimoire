using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Rom;

public static class RomMessageService
{
    private static readonly byte[] FontLoadOrderedFontProlog = [0x27, 0xbd, 0xff, 0xc0, 0xaf, 0xb3, 0x00, 0x24];

    public static RomMessageData LoadMessages(
        string path,
        int messageBankIndex = 0,
        RomMessageSection section = RomMessageSection.Messages)
    {
        byte[] rom = File.ReadAllBytes(path);
        bool wasCompressed = HasCompressedEntries(rom);
        RomCompressionResult decompressed = RomCompressionService.DecompressRom(rom);
        RomVersionProfile profile = decompressed.Profile;
        RomFontResources fontResources = RomFontService.Locate(decompressed.Data, profile);

        List<Models.MessageEntry> entries = LoadSectionEntries(decompressed.Data, profile, messageBankIndex, section);

        return new RomMessageData(entries, profile, wasCompressed, decompressed.Data, fontResources, messageBankIndex, section);
    }

    public static RomMessageData SwitchMessageBank(
        RomMessageData source,
        List<Models.MessageEntry> currentEntries,
        int messageBankIndex,
        bool patchCurrentBank)
    {
        if (patchCurrentBank)
        {
            PatchActiveSection(source.DecompressedRom, source.Profile, source.ActiveMessageBankIndex, source.ActiveSection, currentEntries);
        }

        List<Models.MessageEntry> entries = LoadSectionEntries(
            source.DecompressedRom,
            source.Profile,
            messageBankIndex,
            RomMessageSection.Messages);
        return source with
        {
            Entries = entries,
            ActiveMessageBankIndex = messageBankIndex,
            ActiveSection = RomMessageSection.Messages,
        };
    }

    public static RomMessageData SwitchMessageSection(
        RomMessageData source,
        List<Models.MessageEntry> currentEntries,
        RomMessageSection section,
        bool patchCurrentSection)
    {
        if (patchCurrentSection)
        {
            PatchActiveSection(source.DecompressedRom, source.Profile, source.ActiveMessageBankIndex, source.ActiveSection, currentEntries);
        }

        List<Models.MessageEntry> entries = LoadSectionEntries(
            source.DecompressedRom,
            source.Profile,
            source.ActiveMessageBankIndex,
            section);
        return source with
        {
            Entries = entries,
            ActiveSection = section,
        };
    }

    public static void SaveMessages(
        string path,
        RomMessageData source,
        List<Models.MessageEntry> entries,
        IProgress<RomCompressionProgress>? progress = null,
        bool? compressOverride = null)
    {
        byte[] decompressed = source.DecompressedRom.ToArray();
        PatchActiveSection(decompressed, source.Profile, source.ActiveMessageBankIndex, source.ActiveSection, entries);

        bool compress = compressOverride ?? source.WasCompressed;
        byte[] output = compress
            ? RomCompressionService.CompressRom(decompressed, progress: progress).Data
            : decompressed;
        if (!compress)
        {
            progress?.Report(new RomCompressionProgress(1, 1));
        }

        File.WriteAllBytes(path, output);
    }

    public static IReadOnlyList<List<Models.MessageEntry>> LoadAllMessageBanks(
        RomMessageData source,
        List<Models.MessageEntry> currentEntries)
    {
        byte[] decompressed = source.DecompressedRom.ToArray();
        PatchActiveSection(
            decompressed,
            source.Profile,
            source.ActiveMessageBankIndex,
            source.ActiveSection,
            currentEntries);

        var banks = new List<List<Models.MessageEntry>>(source.Profile.MessageBanks.Count);
        for (int i = 0; i < source.Profile.MessageBanks.Count; i++)
        {
            banks.Add(LoadSectionEntries(decompressed, source.Profile, i, RomMessageSection.Messages));
        }

        return banks;
    }

    public static RomMessageData ReplaceMessageBanks(
        RomMessageData source,
        List<Models.MessageEntry> currentEntries,
        IReadOnlyDictionary<int, List<Models.MessageEntry>> replacementBanks)
    {
        byte[] decompressed = source.DecompressedRom.ToArray();
        PatchActiveSection(
            decompressed,
            source.Profile,
            source.ActiveMessageBankIndex,
            source.ActiveSection,
            currentEntries);

        foreach ((int bankIndex, List<Models.MessageEntry> entries) in replacementBanks)
        {
            if (bankIndex < 0 || bankIndex >= source.Profile.MessageBanks.Count)
            {
                throw new InvalidDataException($"ROM message bank {bankIndex + 1} is not available.");
            }

            PatchActiveSection(decompressed, source.Profile, bankIndex, RomMessageSection.Messages, entries);
        }

        List<Models.MessageEntry> activeEntries = LoadSectionEntries(
            decompressed,
            source.Profile,
            source.ActiveMessageBankIndex,
            source.ActiveSection);

        return source with
        {
            Entries = activeEntries,
            DecompressedRom = decompressed,
        };
    }

    public static (List<Models.MessageEntry>? Jpn, List<Models.MessageEntry>? Nes, List<Models.MessageEntry>? Ger, List<Models.MessageEntry>? Fra)
        LoadModernExportBanks(RomMessageData source, List<Models.MessageEntry> currentEntries)
    {
        byte[] decompressed = source.DecompressedRom.ToArray();
        PatchActiveSection(
            decompressed,
            source.Profile,
            source.ActiveMessageBankIndex,
            source.ActiveSection,
            currentEntries);

        List<Models.MessageEntry>? jpn = source.Profile.JapaneseMessageBank is MessageBankProfile japaneseBank
            ? LoadBankEntries(decompressed, source.Profile, japaneseBank, decodeMessages: false)
            : null;
        List<Models.MessageEntry>? nes = source.Profile.MessageBanks.Count > 0
            ? LoadBankEntries(decompressed, source.Profile, source.Profile.MessageBanks[0])
            : null;
        List<Models.MessageEntry>? ger = source.Profile.MessageBanks.Count > 1
            ? LoadBankEntries(decompressed, source.Profile, source.Profile.MessageBanks[1])
            : null;
        List<Models.MessageEntry>? fra = source.Profile.MessageBanks.Count > 2
            ? LoadBankEntries(decompressed, source.Profile, source.Profile.MessageBanks[2])
            : null;

        return (jpn, nes, ger, fra);
    }

    public static List<Models.MessageEntry> LoadCreditsBank(
        RomMessageData source,
        List<Models.MessageEntry> currentEntries)
    {
        byte[] decompressed = source.DecompressedRom.ToArray();
        PatchActiveSection(
            decompressed,
            source.Profile,
            source.ActiveMessageBankIndex,
            source.ActiveSection,
            currentEntries);

        return LoadBankEntries(decompressed, source.Profile, source.Profile.CreditsBank);
    }

    public static List<Models.MessageEntry>? LoadJapaneseBank(
        RomMessageData source,
        List<Models.MessageEntry> currentEntries)
    {
        if (source.Profile.JapaneseMessageBank is not MessageBankProfile japaneseBank)
        {
            return null;
        }

        byte[] decompressed = source.DecompressedRom.ToArray();
        PatchActiveSection(
            decompressed,
            source.Profile,
            source.ActiveMessageBankIndex,
            source.ActiveSection,
            currentEntries);

        return LoadBankEntries(decompressed, source.Profile, japaneseBank, decodeMessages: false);
    }

    public static bool TryReadActiveFontOrderBytes(RomMessageData source, out byte[] bytes)
    {
        bytes = [];
        if (!UsesActiveFontOrderPointer(source))
        {
            return false;
        }

        MessageBankProfile bank = GetSection(source.Profile, source.ActiveMessageBankIndex, source.ActiveSection);
        byte[] tableBytes = Slice(source.DecompressedRom, bank.MessageTableOffset, bank.MessageTableSize);
        if (!TryFindMessageBounds(tableBytes, 0xfffc, out int startOffset, out int endOffset))
        {
            return false;
        }

        int messageDataSize = GetMessageDataSize(source.DecompressedRom, source.Profile, bank);
        if (startOffset < 0 || endOffset < startOffset || endOffset > messageDataSize)
        {
            return false;
        }

        bytes = source.DecompressedRom
            .AsSpan(bank.MessageDataOffset + startOffset, endOffset - startOffset)
            .ToArray();
        return true;
    }

    public static bool UsesActiveFontOrderPointer(RomMessageData source)
        => UsesFontOrderPointer(
            source.DecompressedRom,
            source.Profile,
            source.ActiveMessageBankIndex,
            source.ActiveSection);

    private static bool TryFindMessageBounds(byte[] tableBytes, int messageId, out int startOffset, out int endOffset)
    {
        startOffset = 0;
        endOffset = 0;

        int i = FindMessageTableStart(tableBytes);
        while (i + 15 < tableBytes.Length)
        {
            int id = (tableBytes[i] << 8) | tableBytes[i + 1];
            int offset = (tableBytes[i + 5] << 16) | (tableBytes[i + 6] << 8) | tableBytes[i + 7];

            if (id is 0xfffd or 0xffff)
            {
                return false;
            }

            int nextId = (tableBytes[i + 8] << 8) | tableBytes[i + 9];
            int nextOffset = (tableBytes[i + 13] << 16) | (tableBytes[i + 14] << 8) | tableBytes[i + 15];
            if (id == messageId)
            {
                startOffset = offset;
                endOffset = nextOffset;
                return true;
            }

            i += 8;
        }

        return false;
    }

    private static List<Models.MessageEntry> LoadSectionEntries(
        byte[] decompressedRom,
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section)
    {
        MessageBankProfile bank = GetSection(profile, messageBankIndex, section);
        byte[] tableBytes = Slice(decompressedRom, bank.MessageTableOffset, bank.MessageTableSize);
        int messageDataSize = GetMessageDataSize(decompressedRom, profile, bank);
        byte[] messageBytes = Slice(decompressedRom, bank.MessageDataOffset, messageDataSize);
        IReadOnlyList<int>? pointerBounds = bank.PointerTableOffset > 0
            ? ReadMessagePointerBounds(decompressedRom, bank.PointerTableOffset, CountMessageEntries(tableBytes, bank.ExcludesFontMessage) + 1)
            : null;

        return MessageTableCodec.ParseTable(
            tableBytes,
            messageBytes,
            bank.OffsetMode == MessageBankOffsetMode.Sequential,
            bank.ExcludesFontMessage,
            pointerBounds,
            MessageEncodingProfile.Default,
            bank.TableSegment);
    }

    private static List<Models.MessageEntry> LoadBankEntries(
        byte[] decompressedRom,
        RomVersionProfile profile,
        MessageBankProfile bank,
        bool decodeMessages = true)
    {
        byte[] tableBytes = Slice(decompressedRom, bank.MessageTableOffset, bank.MessageTableSize);
        int messageDataSize = GetMessageDataSize(decompressedRom, profile, bank);
        byte[] messageBytes = Slice(decompressedRom, bank.MessageDataOffset, messageDataSize);
        IReadOnlyList<int>? pointerBounds = bank.PointerTableOffset > 0
            ? ReadMessagePointerBounds(decompressedRom, bank.PointerTableOffset, CountMessageEntries(tableBytes, bank.ExcludesFontMessage) + 1)
            : null;

        return MessageTableCodec.ParseTable(
            tableBytes,
            messageBytes,
            bank.OffsetMode == MessageBankOffsetMode.Sequential,
            bank.ExcludesFontMessage,
            pointerBounds,
            MessageEncodingProfile.Default,
            bank.TableSegment,
            decodeMessages);
    }

    private static void PatchActiveSection(
        byte[] decompressedRom,
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section,
        List<Models.MessageEntry> entries)
    {
        MessageBankProfile bank = GetSection(profile, messageBankIndex, section);
        List<Models.MessageEntry> entriesToPatch = UsesFontOrderPointer(decompressedRom, profile, messageBankIndex, section)
            ? entries
            : entries.Where(entry => entry.Id != 0xfffc).ToList();
        var (tableBytes, messageBytes) = MessageTableCodec.BuildFiles(entriesToPatch, MessageEncodingProfile.Default);

        if (bank.OffsetMode == MessageBankOffsetMode.Table)
        {
            WriteSection(decompressedRom, bank.MessageTableOffset, bank.MessageTableSize, tableBytes, "message table");
            PatchPalFontMessagePointer(decompressedRom, profile, messageBankIndex, section, entriesToPatch, tableBytes);
        }
        else if (bank.PointerTableOffset > 0)
        {
            WriteMessagePointerTable(decompressedRom, bank.PointerTableOffset, tableBytes);
        }

        WriteMessageDataSection(decompressedRom, profile, bank, messageBytes);
    }

    private static List<int> ReadMessagePointerBounds(byte[] rom, int pointerTableOffset, int pointerCount)
    {
        if (pointerTableOffset < 0 || pointerCount < 0 || pointerTableOffset + (pointerCount * sizeof(uint)) > rom.Length)
        {
            throw new InvalidDataException("ROM message pointer table is outside the decompressed ROM buffer.");
        }

        var offsets = new List<int>(pointerCount);
        for (int i = 0; i < pointerCount; i++)
        {
            uint pointer = DmaTable.ReadUInt32BigEndian(rom, pointerTableOffset + (i * sizeof(uint)));
            offsets.Add((int)(pointer & 0x00ff_ffff));
        }

        return offsets;
    }

    private static void WriteMessagePointerTable(byte[] rom, int pointerTableOffset, byte[] tableBytes)
    {
        List<int> offsets = ReadMessageOffsetsFromTable(tableBytes);
        int byteCount = checked((offsets.Count + 1) * sizeof(uint));
        if (pointerTableOffset < 0 || pointerTableOffset + byteCount > rom.Length)
        {
            throw new InvalidDataException("ROM message pointer table is outside the decompressed ROM buffer.");
        }

        for (int i = 0; i < offsets.Count; i++)
        {
            DmaTable.WriteUInt32BigEndian(rom, pointerTableOffset + (i * sizeof(uint)), 0x0700_0000u + (uint)offsets[i]);
        }

        DmaTable.WriteUInt32BigEndian(rom, pointerTableOffset + (offsets.Count * sizeof(uint)), 0);
    }

    private static int CountMessageEntries(byte[] tableBytes, bool excludeFontMessage)
    {
        int count = 0;
        foreach (int id in EnumerateMessageIds(tableBytes))
        {
            if (excludeFontMessage && id == 0xfffc)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private static List<int> ReadMessageOffsetsFromTable(byte[] tableBytes)
    {
        var offsets = new List<int>();
        int i = FindMessageTableStart(tableBytes);
        while (i + 7 < tableBytes.Length)
        {
            int id = (tableBytes[i] << 8) | tableBytes[i + 1];
            int offset = (tableBytes[i + 5] << 16) | (tableBytes[i + 6] << 8) | tableBytes[i + 7];
            i += 8;

            if (id == 0xfffd)
            {
                offsets.Add(offset);
                break;
            }

            if (id == 0xffff)
            {
                break;
            }

            offsets.Add(offset);
        }

        return offsets;
    }

    private static IEnumerable<int> EnumerateMessageIds(byte[] tableBytes)
    {
        int i = FindMessageTableStart(tableBytes);
        while (i + 7 < tableBytes.Length)
        {
            int id = (tableBytes[i] << 8) | tableBytes[i + 1];
            i += 8;

            if (id is 0xfffd or 0xffff)
            {
                yield break;
            }

            yield return id;
        }
    }

    private static int FindMessageTableStart(byte[] tableBytes)
    {
        int i = 0;
        while (i + 7 < tableBytes.Length && tableBytes[i + 4] != 0x07)
        {
            i += 8;
        }

        if (i + 7 >= tableBytes.Length)
        {
            throw new InvalidDataException("Could not find a valid OoT message table section.");
        }

        return i;
    }

    private static MessageBankProfile GetSection(
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section)
    {
        return section == RomMessageSection.Credits
            ? profile.CreditsBank
            : GetMessageBank(profile, messageBankIndex);
    }

    private static MessageBankProfile GetMessageBank(RomVersionProfile profile, int messageBankIndex)
    {
        if (messageBankIndex < 0 || messageBankIndex >= profile.MessageBanks.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(messageBankIndex),
                $"Message bank {messageBankIndex + 1} is outside the ROM profile's message bank list.");
        }

        return profile.MessageBanks[messageBankIndex];
    }

    private static bool HasCompressedEntries(ReadOnlySpan<byte> rom)
    {
        RomVersionProfile profile = RomVersionDatabase.Detect(rom);
        List<DmaEntry> entries = DmaTable.Parse(rom, profile);
        return entries.Any(entry => !entry.IsDeleted && !entry.IsEmpty && entry.IsCompressed);
    }

    private static int GetMessageDataSize(byte[] rom, RomVersionProfile profile, MessageBankProfile bank)
    {
        DmaEntry? entry = FindDmaEntry(rom, profile, bank.MessageDataOffset);
        if (entry is not null && entry.VirtualEnd > bank.MessageDataOffset)
        {
            return checked((int)(entry.VirtualEnd - bank.MessageDataOffset));
        }

        return bank.MessageDataSize;
    }

    private static byte[] Slice(byte[] data, int offset, int size)
    {
        if (offset < 0 || size < 0 || offset + size > data.Length)
        {
            throw new InvalidDataException("ROM message section is outside the decompressed ROM buffer.");
        }

        return data.AsSpan(offset, size).ToArray();
    }

    private static void WriteSection(byte[] rom, int offset, int capacity, byte[] payload, string name)
    {
        if (payload.Length > capacity)
        {
            throw new InvalidDataException(
                $"Encoded {name} is {payload.Length} bytes, but this ROM profile has room for {capacity} bytes.");
        }

        if (offset < 0 || capacity < 0 || offset + capacity > rom.Length)
        {
            throw new InvalidDataException($"ROM {name} section is outside the decompressed ROM buffer.");
        }

        rom.AsSpan(offset, capacity).Clear();
        payload.CopyTo(rom.AsSpan(offset, payload.Length));
    }

    private static void WriteMessageDataSection(
        byte[] rom,
        RomVersionProfile profile,
        MessageBankProfile bank,
        byte[] payload)
    {
        DmaEntry? entry = FindDmaEntry(rom, profile, bank.MessageDataOffset);
        if (entry is null)
        {
            WriteSection(rom, bank.MessageDataOffset, bank.MessageDataSize, payload, "message data");
            return;
        }

        uint nextVirtualStart = FindNextVirtualStart(rom, profile, entry);
        int maxCapacity = checked((int)(nextVirtualStart - (uint)bank.MessageDataOffset));
        if (payload.Length > maxCapacity)
        {
            throw new InvalidDataException(
                $"Encoded message data is {payload.Length} bytes, but this ROM profile has room for {maxCapacity} bytes before the next DMA file.");
        }

        int requiredCapacity = payload.Length > bank.MessageDataSize
            ? Align16(payload.Length)
            : bank.MessageDataSize;
        if (bank.MessageDataOffset < 0 || bank.MessageDataOffset + requiredCapacity > rom.Length)
        {
            throw new InvalidDataException("ROM message data section is outside the decompressed ROM buffer.");
        }

        rom.AsSpan(bank.MessageDataOffset, requiredCapacity).Clear();
        payload.CopyTo(rom.AsSpan(bank.MessageDataOffset, payload.Length));

        uint requiredVirtualEnd = checked((uint)(bank.MessageDataOffset + requiredCapacity));
        if (requiredVirtualEnd > entry.VirtualEnd)
        {
            int dmaOffset = profile.DmaTableOffset + (entry.Index * 16);
            DmaTable.WriteUInt32BigEndian(rom, dmaOffset + 4, requiredVirtualEnd);
        }
    }

    private static DmaEntry? FindDmaEntry(byte[] rom, RomVersionProfile profile, int virtualOffset)
    {
        return DmaTable.Parse(rom, profile)
            .FirstOrDefault(entry =>
                !entry.IsDeleted
                && !entry.IsEmpty
                && entry.VirtualStart <= virtualOffset
                && virtualOffset < entry.VirtualEnd);
    }

    private static uint FindNextVirtualStart(byte[] rom, RomVersionProfile profile, DmaEntry currentEntry)
    {
        return DmaTable.Parse(rom, profile)
            .Where(entry => !entry.IsDeleted && !entry.IsEmpty && entry.VirtualStart > currentEntry.VirtualStart)
            .Select(entry => entry.VirtualStart)
            .DefaultIfEmpty((uint)rom.Length)
            .Min();
    }

    private static int Align16(int value) => (value + 15) & ~15;

    private static void PatchPalFontMessagePointer(
        byte[] rom,
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section,
        List<MessageEntry> entries,
        byte[] tableBytes)
    {
        if (!UsesFontOrderPointer(rom, profile, messageBankIndex, section))
        {
            return;
        }

        MessageEntry? fontEntry = entries.FirstOrDefault(entry => entry.Id == 0xfffc);
        if (fontEntry is null)
        {
            return;
        }

        int functionOffset = FindBytes(rom, FontLoadOrderedFontProlog);
        if (functionOffset < 0)
        {
            return;
        }

        if (!TryFindTableOffset(tableBytes, 0xfffc, out int fontMessageOffset))
        {
            return;
        }

        uint segmentStart = ReadLuiAddiuAddress(rom, functionOffset + 0x38, functionOffset + 0x40);
        uint fontMessageAddress = checked(segmentStart + (uint)fontMessageOffset);
        uint fontMessageEndAddress = checked(fontMessageAddress + (uint)GetPaddedEncodedMessageLength(fontEntry));

        WriteLuiAddiuAddress(rom, functionOffset + 0x08, functionOffset + 0x0c, fontMessageAddress);
        WriteLuiAddiuAddress(rom, functionOffset + 0x3c, functionOffset + 0x44, fontMessageEndAddress);
    }

    private static bool UsesFontOrderPointer(
        byte[] rom,
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section)
    {
        return section == RomMessageSection.Messages
            && messageBankIndex == 0
            && profile.MessageBanks.Count > 1
            && FindBytes(rom, FontLoadOrderedFontProlog) >= 0;
    }

    private static bool TryFindTableOffset(byte[] tableBytes, int messageId, out int messageOffset)
    {
        for (int i = 0; i + 7 < tableBytes.Length; i += 8)
        {
            int id = (tableBytes[i] << 8) | tableBytes[i + 1];
            if (id == messageId)
            {
                messageOffset = (tableBytes[i + 5] << 16) | (tableBytes[i + 6] << 8) | tableBytes[i + 7];
                return true;
            }
        }

        messageOffset = 0;
        return false;
    }

    private static int GetPaddedEncodedMessageLength(MessageEntry entry)
    {
        if (entry.EncodedBytesOverride is not null)
        {
            return entry.EncodedBytesOverride.Length;
        }

        if (entry.Id == 0xfffc && entry.OriginalEncodedBytes is not null)
        {
            return entry.OriginalEncodedBytes.Length;
        }

        if (entry.HasUnchangedEncodedBytes())
        {
            return entry.OriginalEncodedBytes!.Length;
        }

        byte[] encoded = MessageCodec.EncodeMessageTokens(MessageTextSyntax.FromEditorText(entry.Text), MessageEncodingProfile.Default);
        return Align4(encoded.Length);
    }

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

    private static uint ReadLuiAddiuAddress(byte[] data, int luiOffset, int addiuOffset)
    {
        ushort hi = ReadUInt16BigEndian(data, luiOffset + 2);
        ushort lo = ReadUInt16BigEndian(data, addiuOffset + 2);
        return unchecked((uint)((hi << 16) + SignExtend16(lo)));
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

    private static int SignExtend16(ushort value) => value >= 0x8000 ? value - 0x10000 : value;

    private static ushort ReadUInt16BigEndian(byte[] data, int offset) =>
        (ushort)((data[offset] << 8) | data[offset + 1]);

    private static void WriteUInt16BigEndian(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
    }

    private static int Align4(int value) => (value + 3) & ~3;
}
