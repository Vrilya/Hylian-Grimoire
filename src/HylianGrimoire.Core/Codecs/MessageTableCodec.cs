using HylianGrimoire.Models;

namespace HylianGrimoire.Codecs;

/// <summary>
/// Reads and writes OoT message table/data file pairs.
/// </summary>
public static class MessageTableCodec
{
    public static bool LooksLikeTableFiles(byte[] tableBytes, byte[] messageBytes, int tableSegment = 0x07)
    {
        int i = 0;
        while (i + 7 < tableBytes.Length && tableBytes[i + 4] != tableSegment)
        {
            i += 8;
        }

        if (i + 7 >= tableBytes.Length)
        {
            return false;
        }

        int id = (tableBytes[i] << 8) | tableBytes[i + 1];
        int offset = (tableBytes[i + 5] << 16) | (tableBytes[i + 6] << 8) | tableBytes[i + 7];
        return id is not 0xfffd and not 0xffff
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
        int tableSegment = 0x07,
        bool decodeMessages = true)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        var entries = new List<MessageEntry>();

        int i = 0;
        while (i + 7 < tblRaw.Length && tblRaw[i + 4] != tableSegment)
            i += 8;

        if (i + 7 >= tblRaw.Length)
            throw new InvalidDataException("Could not find a valid OoT message table section.");

        int endMarkerId = 0xfffd;
        int? endMarkerOffset = null;
        bool hasFinalEndMarker = true;

        // Normal message tables use 0xfffd followed by 0xffff; staff credits use 0xffff directly.
        while (i + 7 < tblRaw.Length)
        {
            int id = (tblRaw[i] << 8) | tblRaw[i + 1];
            int type = (tblRaw[i + 2] >> 4) & 0x0f;
            int pos = tblRaw[i + 2] & 0x0f;
            int bank = tblRaw[i + 4];
            int offs = (tblRaw[i + 5] << 16) | (tblRaw[i + 6] << 8) | tblRaw[i + 7];

            i += 8;

            if (id is 0xfffd or 0xffff)
            {
                endMarkerId = id;
                if (id == 0xfffd)
                {
                    endMarkerOffset = offs;
                    hasFinalEndMarker = HasFollowingEndMarker(tblRaw, i);
                }
                break;
            }

            entries.Add(new MessageEntry(id, type, pos, bank, offs));
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

        void WriteEntry(int id, int type, int position, int bank, int offset)
        {
            tblOut.Add((byte)((id >> 8) & 0xff));
            tblOut.Add((byte)(id & 0xff));
            tblOut.Add((byte)(((type & 0x0f) << 4) | (position & 0x0f)));
            tblOut.Add(0x00);
            tblOut.Add((byte)bank);
            tblOut.Add((byte)((offset >> 16) & 0xff));
            tblOut.Add((byte)((offset >> 8) & 0xff));
            tblOut.Add((byte)(offset & 0xff));
        }

        for (int idx = 0; idx < entries.Count; idx++)
        {
            var e = entries[idx];
            WriteEntry(e.Id, e.Type, e.Position, e.Bank, newOffsets[idx]);
        }

        int endMarkerId = entries.Count > 0 ? entries[0].TableEndMarkerId : 0xfffd;
        if (endMarkerId == 0xffff)
        {
            WriteEntry(0xffff, 0x00, 0x00, 0x00, 0x000000);
            return (tblOut.ToArray(), msgOut.ToArray());
        }

        WriteEntry(0xfffd, 0x00, 0x00, 0x07, sentinelOffset);
        bool hasFinalEndMarker = entries.Count == 0 || entries[0].TableHasFinalEndMarker;
        if (hasFinalEndMarker)
        {
            WriteEntry(0xffff, 0x00, 0x00, 0x00, 0x000000);
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

    private static bool HasFollowingEndMarker(byte[] tblRaw, int offset)
    {
        if (offset + 7 >= tblRaw.Length)
        {
            return false;
        }

        int id = (tblRaw[offset] << 8) | tblRaw[offset + 1];
        return id == 0xffff;
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
