using HylianGrimoire.Codecs;
using HylianGrimoire.Games;

namespace HylianGrimoire.Rom;

public static partial class RomMessageService
{
    internal static RomMessagePatchPlan BuildActiveSectionPatchPlan(
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
        var operations = new List<RomMessagePatchOperation>();

        if (bank.OffsetMode == MessageBankOffsetMode.Table)
        {
            operations.Add(RomMessagePatchOperation.WriteSection(
                decompressedRom.Length,
                bank.MessageTableOffset,
                bank.MessageTableSize,
                tableBytes,
                "message table"));
            AddPalFontMessagePointerPatches(
                operations,
                decompressedRom,
                profile,
                messageBankIndex,
                section,
                entriesToPatch,
                tableBytes,
                encodingProfile);
        }
        else if (bank.PointerTableOffset > 0)
        {
            AddMessagePointerTablePatch(operations, decompressedRom, bank.PointerTableOffset, tableBytes);
        }

        AddMessageDataSectionPatch(operations, decompressedRom, profile, bank, messageBytes);

        return new RomMessagePatchPlan(
            messageBankIndex,
            section,
            bank,
            dropOotFontOrderEntry,
            tableBytes.Length,
            messageBytes.Length,
            operations);
    }

    private static void AddMessagePointerTablePatch(
        List<RomMessagePatchOperation> operations,
        byte[] rom,
        int pointerTableOffset,
        byte[] tableBytes)
    {
        List<int> offsets = ReadMessageOffsetsFromTable(tableBytes);
        operations.Add(RomMessagePatchOperation.WritePointerTable(rom.Length, pointerTableOffset, offsets));
    }

    private static void AddMessageDataSectionPatch(
        List<RomMessagePatchOperation> operations,
        byte[] rom,
        RomVersionProfile profile,
        MessageBankProfile bank,
        byte[] payload)
    {
        DmaEntry? entry = FindDmaEntry(rom, profile, bank.MessageDataOffset);
        if (entry is null)
        {
            operations.Add(RomMessagePatchOperation.WriteSection(
                rom.Length,
                bank.MessageDataOffset,
                bank.MessageDataSize,
                payload,
                "message data"));
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

        operations.Add(RomMessagePatchOperation.WriteSection(
            rom.Length,
            bank.MessageDataOffset,
            requiredCapacity,
            payload,
            "message data"));

        uint requiredVirtualEnd = checked((uint)(bank.MessageDataOffset + requiredCapacity));
        if (requiredVirtualEnd > entry.VirtualEnd)
        {
            int dmaOffset = profile.DmaTableOffset + (entry.Index * 16);
            operations.Add(RomMessagePatchOperation.WriteUInt32(
                rom.Length,
                dmaOffset + 4,
                requiredVirtualEnd,
                "message data DMA virtual end"));
        }
    }
}
