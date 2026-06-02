using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Rom;

public static partial class RomMessageService
{
    public static bool TryReadActiveFontOrderBytes(RomMessageData source, out byte[] bytes)
    {
        bytes = [];
        if (!UsesActiveFontOrderPointer(source))
        {
            return false;
        }

        MessageBankProfile bank = source.Profile.GameProfile.MessageBankLayout.GetSection(source.Profile, source.ActiveMessageBankIndex, source.ActiveSection);
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

    private static void PatchPalFontMessagePointer(
        byte[] rom,
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section,
        List<MessageEntry> entries,
        byte[] tableBytes,
        MessageEncodingProfile encodingProfile)
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

        int functionOffset = profile.GameProfile.MessageBankLayout.FindFontOrderRoutineOffset(rom);
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
        uint fontMessageEndAddress = checked(fontMessageAddress + (uint)GetPaddedEncodedMessageLength(fontEntry, profile, encodingProfile));

        WriteLuiAddiuAddress(rom, functionOffset + 0x08, functionOffset + 0x0c, fontMessageAddress);
        WriteLuiAddiuAddress(rom, functionOffset + 0x3c, functionOffset + 0x44, fontMessageEndAddress);
    }

    private static bool UsesFontOrderPointer(
        byte[] rom,
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section)
    {
        return profile.GameProfile.MessageBankLayout.UsesFontOrderPointer(rom, profile, messageBankIndex, section);
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

    private static int GetPaddedEncodedMessageLength(
        MessageEntry entry,
        RomVersionProfile profile,
        MessageEncodingProfile encodingProfile)
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

        byte[] encoded = MessageCodec.EncodeMessageTokens(
            MessageTextSyntax.FromEditorText(entry.Text),
            encodingProfile);
        return Align4(encoded.Length);
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
}
