using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Services;

namespace HylianGrimoire.Rom;

public static partial class RomMessageService
{
    public static void SaveMessages(
        string path,
        RomMessageData source,
        List<Models.MessageEntry> entries,
        IProgress<RomFileOperationProgress>? progress = null,
        bool? compressOverride = null,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= source.Profile.GameProfile.EncodingProfile;
        byte[] decompressed = source.DecompressedRom.ToArray();
        PatchActiveSection(decompressed, source.Profile, source.ActiveMessageBankIndex, source.ActiveSection, entries, encodingProfile);

        bool compress = compressOverride ?? source.WasCompressed;
        byte[] output = compress
            ? RomCompressionService.CompressRom(decompressed, progress: progress).Data
            : decompressed;
        if (!compress)
        {
            progress?.Report(new RomFileOperationProgress(1, 1));
        }

        AtomicFileWriter.WriteAllBytes(path, output);
    }

    public static RomMessageData ReplaceMessageBanks(
        RomMessageData source,
        List<Models.MessageEntry> currentEntries,
        IReadOnlyDictionary<int, List<Models.MessageEntry>> replacementBanks,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= source.Profile.GameProfile.EncodingProfile;
        byte[] decompressed = source.DecompressedRom.ToArray();
        PatchActiveSection(
            decompressed,
            source.Profile,
            source.ActiveMessageBankIndex,
            source.ActiveSection,
            currentEntries,
            encodingProfile);

        foreach ((int bankIndex, List<Models.MessageEntry> entries) in replacementBanks)
        {
            IReadOnlyList<MessageBankProfile> editableBanks = source.Profile.GameProfile.MessageBankLayout.GetEditableBanks(source.Profile);
            if (bankIndex < 0 || bankIndex >= editableBanks.Count)
            {
                throw new InvalidDataException($"ROM message bank {bankIndex + 1} is not available.");
            }

            PatchActiveSection(decompressed, source.Profile, bankIndex, RomMessageSection.Messages, entries, encodingProfile);
        }

        List<Models.MessageEntry> activeEntries = LoadSectionEntries(
            decompressed,
            source.Profile,
            source.ActiveMessageBankIndex,
            source.ActiveSection,
            encodingProfile);

        return source with
        {
            Entries = activeEntries,
            DecompressedRom = decompressed,
        };
    }

    private static void PatchActiveSection(
        byte[] decompressedRom,
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section,
        List<Models.MessageEntry> entries,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= profile.GameProfile.EncodingProfile;
        MessageBankProfile bank = profile.GameProfile.MessageBankLayout.GetSection(profile, messageBankIndex, section);
        bool dropOotFontOrderEntry = profile.GameProfile.Kind == GameKind.OcarinaOfTime
            && !UsesFontOrderPointer(decompressedRom, profile, messageBankIndex, section);
        List<Models.MessageEntry> entriesToPatch = dropOotFontOrderEntry
            ? entries.Where(entry => entry.Id != 0xfffc).ToList()
            : entries;
        var (tableBytes, messageBytes) = profile.GameProfile.MessageBankCodec.Build(entriesToPatch, encodingProfile);

        if (bank.OffsetMode == MessageBankOffsetMode.Table)
        {
            WriteSection(decompressedRom, bank.MessageTableOffset, bank.MessageTableSize, tableBytes, "message table");
            PatchPalFontMessagePointer(decompressedRom, profile, messageBankIndex, section, entriesToPatch, tableBytes, encodingProfile);
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
}
