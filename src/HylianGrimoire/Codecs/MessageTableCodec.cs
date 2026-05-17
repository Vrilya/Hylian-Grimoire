using HylianGrimoire.Models;

namespace HylianGrimoire.Codecs;

/// <summary>
/// Reads and writes OoT message table/data file pairs.
/// </summary>
public static class MessageTableCodec
{
    /// <summary>
    /// Parse the message table and return a list of MessageEntry objects with decoded text.
    /// </summary>
    public static List<MessageEntry> ParseTable(byte[] tblRaw, byte[] messageBytes)
    {
        var entries = new List<MessageEntry>();

        int i = 0;
        while (i + 7 < tblRaw.Length && tblRaw[i + 4] != 0x07)
            i += 8;

        if (i + 7 >= tblRaw.Length)
            throw new InvalidDataException("Could not find a valid OoT message table section.");

        int endMarkerId = 0xfffd;
        int? endMarkerOffset = null;

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
                    endMarkerOffset = offs;
                break;
            }

            entries.Add(new MessageEntry(id, type, pos, bank, offs));
        }

        var validBounds = entries
            .Select(entry => entry.Offset)
            .Where(offset => offset >= 0 && offset <= messageBytes.Length)
            .ToList();
        if (endMarkerOffset is int markerOffset && markerOffset >= 0 && markerOffset <= messageBytes.Length)
            validBounds.Add(markerOffset);
        validBounds.Add(messageBytes.Length);

        foreach (var e in entries)
        {
            e.TableEndMarkerId = endMarkerId;

            if (e.Offset >= 0 && e.Offset < messageBytes.Length)
            {
                int nextOffset = validBounds
                    .Where(offset => offset > e.Offset)
                    .DefaultIfEmpty(messageBytes.Length)
                    .Min();
                int byteCount = nextOffset - e.Offset;
                e.Text = MessageTextSyntax.ToEditorText(MessageCodec.DecodeMessageTokens(messageBytes, e.Offset, byteCount));
                e.OriginalText = e.Text;
                e.OriginalEncodedBytes = messageBytes[e.Offset..nextOffset];
            }
            else
            {
                e.PreserveOffsetWithoutMessageData = true;
            }
        }

        return entries;
    }

    /// <summary>
    /// Re-encode all entries and return (tableBytes, msgBytes).
    /// </summary>
    public static (byte[] tableBytes, byte[] msgBytes) BuildFiles(List<MessageEntry> entries)
    {
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
            if (e.HasUnchangedEncodedBytes())
            {
                msgOut.AddRange(e.OriginalEncodedBytes!);
            }
            else
            {
                try
                {
                    msgOut.AddRange(MessageCodec.EncodeMessageTokens(MessageTextSyntax.FromEditorText(e.Text)));
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
        WriteEntry(0xffff, 0x00, 0x00, 0x00, 0x000000);

        return (tblOut.ToArray(), msgOut.ToArray());
    }
}
