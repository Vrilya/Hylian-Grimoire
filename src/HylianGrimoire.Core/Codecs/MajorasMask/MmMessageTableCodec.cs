using HylianGrimoire.Models;

namespace HylianGrimoire.Codecs.MajorasMask;

public static class MmMessageTableCodec
{
    public const int DebuggerEndMessageId = 0xfffd;

    private const int EntrySize = 8;
    private const int TableSegment = 0x08;
    private const int HeaderSize = 11;
    private const int EndMarkerId = 0xffff;
    private const int LastMainMessageId = 0x354c;
    private static readonly byte[] MainMessageDataTail =
    [
        0x00, 0x00, 0xfe, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xbf,
        0x00, 0x00, 0xfe, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
        0x65, 0x6e, 0x64, 0x21, 0xbf,
    ];

    public static List<MessageEntry> ParseTable(
        byte[] tableBytes,
        byte[] messageBytes,
        MessageEncodingProfile? encodingProfile = null,
        bool decodeMessages = true,
        int? tableSegment = null)
    {
        encodingProfile ??= MessageEncodingProfile.MajorasMask;
        int expectedTableSegment = tableSegment ?? TableSegment;

        int tableOffset = FindTableStart(tableBytes, expectedTableSegment);
        if (tableOffset < 0)
        {
            throw new InvalidDataException("Could not find a valid Majora's Mask message table section.");
        }

        var entries = new List<MessageEntry>();
        int i = tableOffset;
        int finalEndMarkerBank = 0;
        int finalEndMarkerOffset = 0;
        while (i + EntrySize <= tableBytes.Length)
        {
            int id = ReadU16(tableBytes, i);
            byte typePosition = tableBytes[i + 2];
            int pointer = ReadU32(tableBytes, i + 4);
            i += EntrySize;

            if (id == EndMarkerId)
            {
                finalEndMarkerBank = (pointer >> 24) & 0xff;
                finalEndMarkerOffset = pointer & 0x00ffffff;
                break;
            }

            int bank = (pointer >> 24) & 0xff;
            int offset = pointer & 0x00ffffff;
            if (bank != expectedTableSegment || offset < 0 || offset >= messageBytes.Length)
            {
                throw new InvalidDataException($"Message 0x{id:x4} has an invalid Majora's Mask message pointer.");
            }

            MajorasMaskMessageMetadata metadata = ReadMetadata(messageBytes, offset, typePosition);
            var entry = new MessageEntry(id, metadata.Type, metadata.Position, bank, offset)
            {
                CodecMetadata = metadata,
                OriginalCodecMetadata = metadata,
                TableEndMarkerId = EndMarkerId,
                TableHasFinalEndMarker = true,
            };
            entries.Add(entry);
        }

        if (entries.Count == 0)
        {
            throw new InvalidDataException("The Majora's Mask message table did not contain any messages.");
        }

        int[] sortedOffsets = entries
            .Select(entry => entry.Offset)
            .Distinct()
            .Order()
            .ToArray();

        for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
        {
            entries[entryIndex].OriginalFinalTableEndMarkerBank = finalEndMarkerBank;
            entries[entryIndex].OriginalFinalTableEndMarkerOffset = finalEndMarkerOffset;

            MessageEntry entry = entries[entryIndex];
            int nextOffset = FindNextMessageOffset(sortedOffsets, entry.Offset, messageBytes.Length);
            if (nextOffset <= entry.Offset || nextOffset > messageBytes.Length)
            {
                throw new InvalidDataException($"Message 0x{entry.Id:x4} has an invalid message data range.");
            }

            byte[] encodedBytes = messageBytes[entry.Offset..nextOffset];
            if (decodeMessages)
            {
                entry.Text = MmMessageTextCodec.Decode(
                    encodedBytes,
                    HeaderSize,
                    encodedBytes.Length - HeaderSize,
                    encodingProfile);
            }

            entry.OriginalText = entry.Text;
            entry.OriginalEncodedBytes = encodedBytes;
        }

        return entries;
    }

    public static (byte[] TableBytes, byte[] MessageBytes) BuildFiles(
        List<MessageEntry> entries,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.MajorasMask;

        if (CanRebuildOriginalLayout(entries))
        {
            return BuildOriginalLayoutFiles(entries);
        }

        var messageOutput = new List<byte>();
        var offsets = new List<int>(entries.Count);

        foreach (MessageEntry entry in entries)
        {
            offsets.Add(messageOutput.Count);
            if (CanReuseOriginalEncodedBytes(entry))
            {
                messageOutput.AddRange(entry.OriginalEncodedBytes!);
            }
            else
            {
                if (entry.CodecMetadata is not MajorasMaskMessageMetadata metadata)
                {
                    throw new InvalidDataException($"Message 0x{entry.Id:x4} is missing Majora's Mask message metadata.");
                }

                try
                {
                    messageOutput.AddRange(metadata.BuildHeader(entry.Type, entry.Position));
                    messageOutput.AddRange(MmMessageTextCodec.Encode(entry.Text, encodingProfile));
                }
                catch (InvalidDataException ex)
                {
                    throw new InvalidDataException($"Message 0x{entry.Id:x4}: {ex.Message}", ex);
                }
            }

            PadToFourByteBoundary(messageOutput);
        }

        AppendMainMessageDataTail(entries, messageOutput);

        var tableOutput = new List<byte>();
        for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
        {
            MessageEntry entry = entries[entryIndex];
            if (entry.CodecMetadata is not MajorasMaskMessageMetadata metadata)
            {
                throw new InvalidDataException($"Message 0x{entry.Id:x4} is missing Majora's Mask message metadata.");
            }

            WriteEntry(tableOutput, entry.Id, metadata.TableTypePosition, GetEntryTableSegment(entry, entries), offsets[entryIndex]);
        }

        (int finalBank, int finalOffset) = GetFinalEndMarkerPointer(entries, offsets, messageOutput.Count);
        WriteEntry(tableOutput, EndMarkerId, 0x00, finalBank, finalOffset);
        return (tableOutput.ToArray(), messageOutput.ToArray());
    }

    private static bool CanRebuildOriginalLayout(List<MessageEntry> entries)
    {
        return entries.Count > 0
            && entries.All(entry => CanReuseOriginalEncodedBytes(entry) && entry.Offset >= 0);
    }

    private static (byte[] TableBytes, byte[] MessageBytes) BuildOriginalLayoutFiles(List<MessageEntry> entries)
    {
        int messageLength = entries.Max(entry => entry.Offset + entry.OriginalEncodedBytes!.Length);
        byte[] messageOutput = new byte[messageLength];
        var written = new bool[messageLength];

        foreach (MessageEntry entry in entries)
        {
            byte[] encodedBytes = entry.OriginalEncodedBytes!;
            for (int i = 0; i < encodedBytes.Length; i++)
            {
                int target = entry.Offset + i;
                if (written[target] && messageOutput[target] != encodedBytes[i])
                {
                    throw new InvalidDataException($"Message 0x{entry.Id:x4} overlaps another Majora's Mask message with different bytes.");
                }

                messageOutput[target] = encodedBytes[i];
                written[target] = true;
            }
        }

        var tableOutput = new List<byte>();
        foreach (MessageEntry entry in entries)
        {
            if (entry.CodecMetadata is not MajorasMaskMessageMetadata metadata)
            {
                throw new InvalidDataException($"Message 0x{entry.Id:x4} is missing Majora's Mask message metadata.");
            }

            WriteEntry(tableOutput, entry.Id, metadata.TableTypePosition, GetEntryTableSegment(entry, entries), entry.Offset);
        }

        (int finalBank, int finalOffset) = GetFinalEndMarkerPointer(
            entries,
            entries.Select(entry => entry.Offset).ToList(),
            messageOutput.Length);
        WriteEntry(tableOutput, EndMarkerId, 0x00, finalBank, finalOffset);
        var messageList = messageOutput.ToList();
        AppendMainMessageDataTail(entries, messageList);
        return (tableOutput.ToArray(), messageList.ToArray());
    }

    private static bool CanReuseOriginalEncodedBytes(MessageEntry entry)
    {
        return entry.HasUnchangedEncodedBytes()
            && entry.CodecMetadata is MajorasMaskMessageMetadata current
            && entry.OriginalCodecMetadata is MajorasMaskMessageMetadata original
            && current == original
            && entry.Type == original.Type
            && entry.Position == original.Position;
    }

    private static (int Bank, int Offset) GetFinalEndMarkerPointer(
        List<MessageEntry> entries,
        List<int> offsets,
        int fallbackOffset)
    {
        MessageEntry? template = entries.FirstOrDefault(entry => entry.OriginalFinalTableEndMarkerBank is not null);
        int bank = template?.OriginalFinalTableEndMarkerBank ?? 0;
        if (bank == 0)
        {
            return (0, 0);
        }

        int debuggerIndex = entries.FindLastIndex(entry => entry.Id == DebuggerEndMessageId);
        int offset = debuggerIndex >= 0 && debuggerIndex < offsets.Count
            ? offsets[debuggerIndex]
            : template?.OriginalFinalTableEndMarkerOffset ?? fallbackOffset;
        return (bank, offset);
    }

    private static void PadToFourByteBoundary(List<byte> output)
    {
        while ((output.Count & 3) != 0)
        {
            output.Add(0x00);
        }
    }

    private static void AppendMainMessageDataTail(List<MessageEntry> entries, List<byte> output)
    {
        if (!IsMainMessageData(entries) || EndsWithTail(output))
        {
            return;
        }

        output.AddRange(MainMessageDataTail);
        while ((output.Count & 0x0f) != 0)
        {
            output.Add(0x00);
        }
    }

    private static bool IsMainMessageData(List<MessageEntry> entries) =>
        entries.Any(entry => entry.Id == LastMainMessageId);

    private static bool EndsWithTail(List<byte> bytes)
    {
        int end = bytes.Count;
        while (end > 0 && bytes[end - 1] == 0x00)
        {
            end--;
        }

        if (end < MainMessageDataTail.Length)
        {
            return false;
        }

        int start = end - MainMessageDataTail.Length;
        for (int i = 0; i < MainMessageDataTail.Length; i++)
        {
            if (bytes[start + i] != MainMessageDataTail[i])
            {
                return false;
            }
        }

        return true;
    }

    public static bool LooksLikeMajorasMaskTable(byte[] tableBytes, byte[] messageBytes)
    {
        int tableOffset = FindTableStart(tableBytes);
        if (tableOffset < 0 || tableOffset + EntrySize > tableBytes.Length)
        {
            return false;
        }

        int pointer = ReadU32(tableBytes, tableOffset + 4);
        int bank = (pointer >> 24) & 0xff;
        int offset = pointer & 0x00ffffff;
        return IsMessageTableSegment(bank)
            && offset >= 0
            && offset + HeaderSize < messageBytes.Length
            && HasMajoraEndByte(messageBytes, offset);
    }

    private static int FindTableStart(byte[] tableBytes, int? tableSegment = null)
    {
        for (int i = 0; i + EntrySize <= tableBytes.Length; i += EntrySize)
        {
            int bank = tableBytes[i + 4];
            if (tableSegment is null ? IsMessageTableSegment(bank) : bank == tableSegment.Value)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsMessageTableSegment(int bank) => bank is 0x07 or 0x08;

    private static int GetEntryTableSegment(MessageEntry entry, List<MessageEntry> entries)
    {
        if (IsMessageTableSegment(entry.Bank))
        {
            return entry.Bank;
        }

        return entries.FirstOrDefault(candidate => IsMessageTableSegment(candidate.Bank))?.Bank ?? TableSegment;
    }

    private static MajorasMaskMessageMetadata ReadMetadata(byte[] messageBytes, int offset, byte tableTypePosition)
    {
        if (offset + HeaderSize > messageBytes.Length)
        {
            throw new InvalidDataException($"Message at 0x{offset:x6} is too short for a Majora's Mask header.");
        }

        return new MajorasMaskMessageMetadata(
            tableTypePosition,
            (ushort)ReadU16(messageBytes, offset),
            messageBytes[offset + 2],
            (ushort)ReadU16(messageBytes, offset + 3),
            (ushort)ReadU16(messageBytes, offset + 5),
            (ushort)ReadU16(messageBytes, offset + 7),
            (ushort)ReadU16(messageBytes, offset + 9));
    }

    private static bool HasMajoraEndByte(byte[] messageBytes, int offset)
    {
        int max = Math.Min(messageBytes.Length, offset + 0x4000);
        for (int i = offset + HeaderSize; i < max; i++)
        {
            if (messageBytes[i] == 0xbf)
            {
                return true;
            }
        }

        return false;
    }

    private static int FindNextMessageOffset(int[] sortedOffsets, int offset, int messageBytesLength)
    {
        foreach (int candidate in sortedOffsets)
        {
            if (candidate > offset)
            {
                return candidate;
            }
        }

        return messageBytesLength;
    }

    private static int ReadU16(byte[] bytes, int offset) => (bytes[offset] << 8) | bytes[offset + 1];

    private static int ReadU32(byte[] bytes, int offset) =>
        ((bytes[offset] << 24)
            | (bytes[offset + 1] << 16)
            | (bytes[offset + 2] << 8)
            | bytes[offset + 3]);

    private static void WriteEntry(List<byte> output, int id, byte typePosition, int bank, int offset)
    {
        output.Add((byte)((id >> 8) & 0xff));
        output.Add((byte)(id & 0xff));
        output.Add(typePosition);
        output.Add(0x00);
        output.Add((byte)bank);
        output.Add((byte)((offset >> 16) & 0xff));
        output.Add((byte)((offset >> 8) & 0xff));
        output.Add((byte)(offset & 0xff));
    }
}
