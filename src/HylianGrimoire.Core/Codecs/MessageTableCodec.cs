using HylianGrimoire.Models;

namespace HylianGrimoire.Codecs;

/// <summary>
/// Reads and writes OoT message table/data file pairs.
/// </summary>
public static class MessageTableCodec
{
    private const int EntrySize = EncodedMessageTableEntry.Size;
    private const int EndMarkerId = 0xfffd;
    private const int FinalEndMarkerId = 0xffff;
    private const int DefaultTableSegment = 0x07;

    public static bool LooksLikeTableFiles(byte[] tableBytes, byte[] messageBytes, int tableSegment = DefaultTableSegment)
    {
        int tableOffset = FindTableStart(tableBytes, tableSegment);
        if (tableOffset < 0)
        {
            return false;
        }

        EncodedMessageTableEntry tableEntry = EncodedMessageTableEntry.Read(tableBytes.AsSpan(tableOffset, EntrySize));
        int offset = tableEntry.Offset;
        return tableEntry.Id is not EndMarkerId and not FinalEndMarkerId
            && offset >= 0
            && offset < messageBytes.Length
            && HasMessageEndByte(messageBytes, offset);
    }

    /// <summary>
    /// Parse the message table and return a list of MessageEntry objects with decoded text.
    /// </summary>
    public static List<MessageEntry> ParseTable(
        byte[] tblRaw,
        byte[] messageBytes,
        bool useSequentialMessageOffsets = false,
        bool excludeFontMessage = false,
        IReadOnlyList<int>? explicitMessageBounds = null,
        MessageEncodingProfile? encodingProfile = null,
        int tableSegment = DefaultTableSegment,
        bool decodeMessages = true)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        var entries = new List<MessageEntry>();

        int i = FindTableStart(tblRaw, tableSegment);
        if (i < 0)
            throw new InvalidDataException("Could not find a valid OoT message table section.");

        int endMarkerId = EndMarkerId;
        int? endMarkerOffset = null;
        bool hasFinalEndMarker = true;

        // Normal message tables use 0xfffd followed by 0xffff; staff credits use 0xffff directly.
        while (i + EntrySize <= tblRaw.Length)
        {
            EncodedMessageTableEntry tableEntry = EncodedMessageTableEntry.Read(tblRaw.AsSpan(i, EntrySize));
            i += EntrySize;

            if (tableEntry.Id is EndMarkerId or FinalEndMarkerId)
            {
                endMarkerId = tableEntry.Id;
                if (tableEntry.Id == EndMarkerId)
                {
                    endMarkerOffset = tableEntry.Offset;
                    hasFinalEndMarker = HasFollowingEndMarker(tblRaw, i);
                }
                break;
            }

            entries.Add(new MessageEntry(tableEntry.Id, tableEntry.Type, tableEntry.Position, tableEntry.Bank, tableEntry.Offset));
        }

        if (excludeFontMessage)
        {
            entries.RemoveAll(entry => entry.Id == 0xfffc);
        }

        var validBounds = entries
            .Select(entry => entry.Offset)
            .Where(offset => offset >= 0 && offset <= messageBytes.Length)
            .ToList();
        if (endMarkerOffset is int markerOffset && markerOffset >= 0 && markerOffset <= messageBytes.Length)
            validBounds.Add(markerOffset);
        validBounds.Add(messageBytes.Length);
        int? trailingDataOffset = endMarkerOffset;

        if (explicitMessageBounds is not null)
        {
            if (explicitMessageBounds.Count < entries.Count + 1)
            {
                throw new InvalidDataException("Message pointer table does not contain enough entries for this message table.");
            }

            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                entries[entryIndex].Offset = explicitMessageBounds[entryIndex];
            }

            validBounds = explicitMessageBounds
                .Where(offset => offset >= 0 && offset <= messageBytes.Length)
                .ToList();
            trailingDataOffset = explicitMessageBounds[entries.Count];
        }
        else if (useSequentialMessageOffsets)
        {
            ApplySequentialOffsets(entries, messageBytes);
            validBounds = entries
                .Select(entry => entry.Offset)
                .Where(offset => offset >= 0 && offset <= messageBytes.Length)
                .ToList();
            validBounds.Add(messageBytes.Length);
        }

        foreach (var e in entries)
        {
            e.TableEndMarkerId = endMarkerId;
            e.TableHasFinalEndMarker = hasFinalEndMarker;

            if (e.Offset >= 0 && e.Offset < messageBytes.Length)
            {
                int nextOffset = validBounds
                    .Where(offset => offset > e.Offset)
                    .DefaultIfEmpty(messageBytes.Length)
                    .Min();
                int byteCount = nextOffset - e.Offset;
                byte[] encodedBytes = messageBytes[e.Offset..nextOffset];
                e.Text = decodeMessages
                    ? e.Id == 0xfffc
                        ? FontOrderCodec.ToEditorText(encodedBytes, encodingProfile)
                        : MessageTextSyntax.ToEditorText(MessageCodec.DecodeMessageTokens(messageBytes, e.Offset, byteCount, encodingProfile))
                    : string.Empty;
                e.OriginalText = e.Text;
                e.OriginalEncodedBytes = encodedBytes;
            }
            else
            {
                e.PreserveOffsetWithoutMessageData = true;
            }
        }

        if (entries.Count > 0 && trailingDataOffset is int trailingOffset && trailingOffset >= 0 && trailingOffset <= messageBytes.Length)
        {
            byte[] trailingData = messageBytes[trailingOffset..];
            foreach (MessageEntry entry in entries)
            {
                entry.OriginalTrailingMessageData = trailingData;
                entry.OriginalMessageDataSize = messageBytes.Length;
            }
        }

        return entries;
    }

    private static void ApplySequentialOffsets(List<MessageEntry> entries, byte[] messageBytes)
    {
        int offset = 0;
        foreach (MessageEntry entry in entries)
        {
            entry.Offset = offset;
            int byteCount = GetEncodedMessageByteCount(messageBytes, offset);
            offset += byteCount;
        }
    }

    private static int GetEncodedMessageByteCount(byte[] messageBytes, int offset)
    {
        if (offset < 0 || offset >= messageBytes.Length)
        {
            return 0;
        }

        int i = offset;
        while (i < messageBytes.Length)
        {
            byte value = messageBytes[i++];
            if (value == 0x02)
            {
                break;
            }

            i += GetArgumentByteCount(value);
        }

        int byteCount = i - offset;
        int paddedCount = (byteCount + 3) & ~3;
        return Math.Min(paddedCount, messageBytes.Length - offset);
    }

    private static int GetArgumentByteCount(byte value) =>
        value switch
        {
            0x05 or 0x06 or 0x0c or 0x0e or 0x13 or 0x14 or 0x1e => 1,
            0x07 or 0x11 or 0x12 => 2,
            0x15 => 3,
            _ => 0,
        };

    /// <summary>
    /// Re-encode all entries and return (tableBytes, msgBytes).
    /// </summary>
    public static (byte[] tableBytes, byte[] msgBytes) BuildFiles(
        List<MessageEntry> entries,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        var msgOut = new List<byte>();
        var newOffsets = new List<int>();

        foreach (var e in entries)
        {
            if (e.PreserveOffsetWithoutMessageData && string.IsNullOrEmpty(e.Text))
            {
                newOffsets.Add(e.Offset);
                continue;
            }

            newOffsets.Add(msgOut.Count);
            if (e.EncodedBytesOverride is not null)
            {
                msgOut.AddRange(e.EncodedBytesOverride);
            }
            else if (e.Id == 0xfffc && e.OriginalEncodedBytes is not null)
            {
                msgOut.AddRange(e.OriginalEncodedBytes);
            }
            else if (e.HasUnchangedEncodedBytes())
            {
                msgOut.AddRange(e.OriginalEncodedBytes!);
            }
            else
            {
                try
                {
                    msgOut.AddRange(MessageCodec.EncodeMessageTokens(MessageTextSyntax.FromEditorText(e.Text), encodingProfile));
                }
                catch (InvalidDataException ex)
                {
                    throw new InvalidDataException($"Message 0x{e.Id:x4}: {ex.Message}", ex);
                }
            }
        }

        int sentinelOffset = msgOut.Count;

        var tblOut = new List<byte>();

        for (int idx = 0; idx < entries.Count; idx++)
        {
            var e = entries[idx];
            WriteEntry(tblOut, e.Id, e.Type, e.Position, e.Bank, newOffsets[idx]);
        }

        int endMarkerId = entries.Count > 0 ? entries[0].TableEndMarkerId : EndMarkerId;
        if (endMarkerId == FinalEndMarkerId)
        {
            WriteEntry(tblOut, FinalEndMarkerId, 0x00, 0x00, 0x00, 0x000000);
            return (tblOut.ToArray(), msgOut.ToArray());
        }

        WriteEntry(tblOut, EndMarkerId, 0x00, 0x00, DefaultTableSegment, sentinelOffset);
        bool hasFinalEndMarker = entries.Count == 0 || entries[0].TableHasFinalEndMarker;
        if (hasFinalEndMarker)
        {
            WriteEntry(tblOut, FinalEndMarkerId, 0x00, 0x00, 0x00, 0x000000);
        }

        MessageEntry? firstEntry = entries.FirstOrDefault();
        if (firstEntry?.OriginalTrailingMessageData is { Length: > 0 } trailingData
            && firstEntry.OriginalMessageDataSize is int originalMessageDataSize
            && msgOut.Count + trailingData.Length <= originalMessageDataSize)
        {
            msgOut.AddRange(trailingData);
        }

        return (tblOut.ToArray(), msgOut.ToArray());
    }

    private static int FindTableStart(byte[] tableBytes, int tableSegment)
    {
        for (int offset = 0; offset + EntrySize <= tableBytes.Length; offset += EntrySize)
        {
            EncodedMessageTableEntry entry = EncodedMessageTableEntry.Read(tableBytes.AsSpan(offset, EntrySize));
            if (entry.Bank == tableSegment)
            {
                return offset;
            }
        }

        return -1;
    }

    private static void WriteEntry(List<byte> output, int id, int type, int position, int bank, int offset)
        => EncodedMessageTableEntry.FromFields(id, type, position, bank, offset).WriteTo(output);

    private static bool HasFollowingEndMarker(byte[] tblRaw, int offset)
    {
        if (offset + EntrySize > tblRaw.Length)
        {
            return false;
        }

        EncodedMessageTableEntry entry = EncodedMessageTableEntry.Read(tblRaw.AsSpan(offset, EntrySize));
        return entry.Id == FinalEndMarkerId;
    }

    private static bool HasMessageEndByte(byte[] messageBytes, int offset)
    {
        int max = Math.Min(messageBytes.Length, offset + 0x4000);
        for (int i = offset; i < max; i++)
        {
            if (messageBytes[i] == 0x02)
            {
                return true;
            }
        }

        return false;
    }
}
