using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;
using HylianGrimoire.Games;
using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public static partial class MessageByteInspectorService
{
    private const int MajorasMaskHeaderSize = 11;

    private static MessageByteInspection InspectMajorasMask(MessageEntry entry, MessageEncodingProfile encodingProfile)
    {
        MajorasMaskMessageMetadata metadata = GetMajorasMaskMetadata(entry);
        byte[] headerBytes = metadata.BuildHeader(entry.Type, entry.Position);
        MessageByteSegment[] headerSegments = BuildMajorasMaskHeaderSegments(entry, metadata, headerBytes);

        var messageDataBytes = MmMessageTextCodec.Encode(entry.Text, encodingProfile).ToList();
        var messageDataSegments = BuildMajorasMaskMessageDataSegments(messageDataBytes.ToArray(), encodingProfile);
        int paddingStart = messageDataBytes.Count;
        while (((MajorasMaskHeaderSize + messageDataBytes.Count) & 3) != 0)
        {
            messageDataBytes.Add(0x00);
        }

        if (messageDataBytes.Count > paddingStart)
        {
            messageDataSegments.Add(new MessageByteSegment(
                paddingStart,
                messageDataBytes.Count - paddingStart,
                MessageByteSegmentKind.Padding,
                "Padding",
                "Zero padding used to align the Majora's Mask message header and data to a 4-byte boundary.",
                null));
        }

        byte[] messageData = messageDataBytes.ToArray();
        byte[] messageBytes = headerBytes.Concat(messageData).ToArray();
        MessageByteSegment[] combinedSegments = headerSegments
            .Concat(messageDataSegments.Select(segment => ShiftSegment(segment, MajorasMaskHeaderSize)))
            .ToArray();

        var headerSection = new MessageByteSection(
            MessageByteSectionKind.MessageHeader,
            "Message header",
            headerBytes,
            headerSegments);

        var messageDataSection = new MessageByteSection(
            MessageByteSectionKind.MessageData,
            "Message data",
            messageData,
            messageDataSegments.ToArray());

        return new MessageByteInspection(
            GameKind.MajorasMask,
            entry.Id,
            messageBytes,
            combinedSegments,
            [BuildMajorasMaskMessageTableSection(entry, metadata), headerSection, messageDataSection]);
    }

    private static MessageByteSection BuildMajorasMaskMessageTableSection(
        MessageEntry entry,
        MajorasMaskMessageMetadata metadata)
    {
        int offset = entry.Offset & 0xffffff;
        int bank = GetMajorasMaskTableBank(entry);
        byte[] tableBytes =
        [
            (byte)((entry.Id >> 8) & 0xff),
            (byte)(entry.Id & 0xff),
            metadata.TableTypePosition,
            0x00,
            (byte)bank,
            (byte)((offset >> 16) & 0xff),
            (byte)((offset >> 8) & 0xff),
            (byte)(offset & 0xff),
        ];

        MessageByteSegment[] segments =
        [
            new(0, 2, MessageByteSegmentKind.TableField, "Message ID", $"Table message ID 0x{entry.Id:x4}.", null),
            new(2, 1, MessageByteSegmentKind.TableField, "Table Type / Position", $"Table type 0x{metadata.TableTypePosition >> 4:x1}, position 0x{metadata.TableTypePosition & 0x0f:x1}.", null),
            new(3, 1, MessageByteSegmentKind.TableField, "Reserved", "Reserved table byte.", null),
            new(4, 1, MessageByteSegmentKind.TableField, "Pointer Bank", $"Message data bank 0x{bank:x2}.", null),
            new(5, 3, MessageByteSegmentKind.TableField, "Pointer Offset", $"Message data offset 0x{offset:x6}.", null),
        ];

        return new MessageByteSection(
            MessageByteSectionKind.MessageTableEntry,
            "Message table entry",
            tableBytes,
            segments);
    }

    private static MessageByteSegment[] BuildMajorasMaskHeaderSegments(
        MessageEntry entry,
        MajorasMaskMessageMetadata metadata,
        byte[] headerBytes)
    {
        int properties = ReadU16(headerBytes, 0);
        return
        [
            new(0, 2, MessageByteSegmentKind.HeaderField, "Textbox Properties", $"Properties 0x{properties:x4}; type 0x{entry.Type:x1}, position 0x{entry.Position:x1}, centered {FormatYesNo(metadata.IsCentered)}, unskippable {FormatYesNo(metadata.IsUnskippable)}, instant {FormatYesNo(metadata.DrawInstantly)}.", null),
            new(2, 1, MessageByteSegmentKind.HeaderField, "Icon ID", $"Message icon 0x{metadata.IconId:x2}.", null),
            new(3, 2, MessageByteSegmentKind.HeaderField, "Next Text ID", $"Next message ID 0x{metadata.NextTextId:x4}.", null),
            new(5, 2, MessageByteSegmentKind.HeaderField, "First Choice Price", $"First choice price {FormatSignedWord(metadata.FirstChoicePrice)}.", null),
            new(7, 2, MessageByteSegmentKind.HeaderField, "Second Choice Price", $"Second choice price {FormatSignedWord(metadata.SecondChoicePrice)}.", null),
            new(9, 2, MessageByteSegmentKind.HeaderField, "Unknown", $"Unknown header field 0x{metadata.Unknown:x4}.", null),
        ];
    }

    private static List<MessageByteSegment> BuildMajorasMaskMessageDataSegments(
        byte[] messageDataBytes,
        MessageEncodingProfile encodingProfile)
    {
        var segments = new List<MessageByteSegment>();
        int offset = 0;
        while (offset < messageDataBytes.Length)
        {
            byte value = messageDataBytes[offset];
            if (value == 0xbf)
            {
                segments.Add(new MessageByteSegment(
                    offset,
                    1,
                    MessageByteSegmentKind.Terminator,
                    "Terminator",
                    "Automatically ends the encoded Majora's Mask message byte stream.",
                    null));
                break;
            }

            if (value == 0x11)
            {
                segments.Add(new MessageByteSegment(
                    offset,
                    1,
                    MessageByteSegmentKind.LineBreak,
                    "Line Break",
                    "Moves to the next line within the current textbox.",
                    "\\n"));
                offset++;
                continue;
            }

            if (MmMessageTokenMaps.ColorTags.TryGetValue(value, out string? colorTag))
            {
                segments.Add(new MessageByteSegment(
                    offset,
                    1,
                    MessageByteSegmentKind.ControlCode,
                    "Color",
                    $"Changes the text color to {GetTagLabel(colorTag)}.",
                    $"[color:{colorTag}]"));
                offset++;
                continue;
            }

            if (MmMessageTokenMaps.NoArgumentTags.TryGetValue(value, out string? noArgumentTag))
            {
                segments.Add(new MessageByteSegment(
                    offset,
                    1,
                    MessageByteSegmentKind.ControlCode,
                    GetTagLabel(noArgumentTag),
                    GetMajorasMaskCommandDescription(value, noArgumentTag),
                    $"[{noArgumentTag}]"));
                offset++;
                continue;
            }

            if (MmMessageTokenMaps.ButtonTags.TryGetValue(value, out string? buttonTag))
            {
                segments.Add(new MessageByteSegment(
                    offset,
                    1,
                    MessageByteSegmentKind.ControlCode,
                    GetTagLabel(buttonTag),
                    "Draws a controller button glyph.",
                    $"[{buttonTag}]"));
                offset++;
                continue;
            }

            if (MmMessageTokenMaps.OneByteArgumentTags.TryGetValue(value, out string? oneByteTag)
                && offset + 1 < messageDataBytes.Length)
            {
                byte argument = messageDataBytes[offset + 1];
                string editorSyntax = $"[{oneByteTag}:{argument:x2}]";
                segments.Add(new MessageByteSegment(
                    offset,
                    1,
                    MessageByteSegmentKind.ControlCode,
                    GetTagLabel(oneByteTag),
                    GetMajorasMaskCommandDescription(value, oneByteTag),
                    editorSyntax));
                segments.Add(new MessageByteSegment(
                    offset + 1,
                    1,
                    MessageByteSegmentKind.Parameter,
                    $"{GetTagLabel(oneByteTag)} Parameter",
                    $"0x{argument:x2}",
                    editorSyntax));
                offset += 2;
                continue;
            }

            if (MmMessageTokenMaps.TwoByteArgumentTags.TryGetValue(value, out string? twoByteTag)
                && offset + 2 < messageDataBytes.Length)
            {
                ushort argument = (ushort)((messageDataBytes[offset + 1] << 8) | messageDataBytes[offset + 2]);
                string editorSyntax = $"[{twoByteTag}:{argument:x4}]";
                segments.Add(new MessageByteSegment(
                    offset,
                    1,
                    MessageByteSegmentKind.ControlCode,
                    GetTagLabel(twoByteTag),
                    GetMajorasMaskCommandDescription(value, twoByteTag),
                    editorSyntax));
                segments.Add(new MessageByteSegment(
                    offset + 1,
                    2,
                    MessageByteSegmentKind.Parameter,
                    $"{GetTagLabel(twoByteTag)} Parameter",
                    GetMajorasMaskParameterDescription(twoByteTag, argument),
                    editorSyntax));
                offset += 3;
                continue;
            }

            int textStart = offset;
            while (offset < messageDataBytes.Length
                && messageDataBytes[offset] != 0xbf
                && !IsMajorasMaskControlByte(messageDataBytes[offset]))
            {
                offset++;
            }

            if (offset == textStart)
            {
                offset++;
            }

            segments.Add(new MessageByteSegment(
                textStart,
                offset - textStart,
                MessageByteSegmentKind.Text,
                "Text",
                $"Plain message text: {TrimForDescription(DecodeMajorasMaskTextRun(messageDataBytes, textStart, offset - textStart, encodingProfile))}",
                null));
        }

        return segments;
    }

    private static MajorasMaskMessageMetadata GetMajorasMaskMetadata(MessageEntry entry)
    {
        if (entry.CodecMetadata is MajorasMaskMessageMetadata metadata)
        {
            return metadata;
        }

        ushort properties = (ushort)(((entry.Type & 0x0f) << 8) | ((entry.Position & 0x0f) << 4));
        return new MajorasMaskMessageMetadata(
            TableTypePosition: (byte)(((entry.Type & 0x0f) << 4) | (entry.Position & 0x0f)),
            TextBoxProperties: properties,
            IconId: 0xfe,
            NextTextId: 0xffff,
            FirstChoicePrice: 0xffff,
            SecondChoicePrice: 0xffff,
            Unknown: 0xffff);
    }

    private static int GetMajorasMaskTableBank(MessageEntry entry)
        => entry.Bank is 0x07 or 0x08 ? entry.Bank : 0x08;

    private static bool IsMajorasMaskControlByte(byte value)
        => value == 0x11
            || MmMessageTokenMaps.ColorTags.ContainsKey(value)
            || MmMessageTokenMaps.NoArgumentTags.ContainsKey(value)
            || MmMessageTokenMaps.ButtonTags.ContainsKey(value)
            || MmMessageTokenMaps.OneByteArgumentTags.ContainsKey(value)
            || MmMessageTokenMaps.TwoByteArgumentTags.ContainsKey(value);

    private static string DecodeMajorasMaskTextRun(
        byte[] bytes,
        int offset,
        int length,
        MessageEncodingProfile encodingProfile)
    {
        return string.Concat(bytes
            .Skip(offset)
            .Take(length)
            .Select(value =>
            {
                if (encodingProfile.TryGetEditorChar(value, out char mapped))
                {
                    return mapped.ToString();
                }

                return value is >= 0x20 and <= 0x7e
                    ? ((char)value).ToString()
                    : $"[byte:{value:x2}]";
            }));
    }

    private static int ReadU16(byte[] bytes, int offset)
        => (bytes[offset] << 8) | bytes[offset + 1];

    private static MessageByteSegment ShiftSegment(MessageByteSegment segment, int offset)
        => segment with { Offset = segment.Offset + offset };

    private static string FormatYesNo(bool value)
        => value ? "yes" : "no";

    private static string FormatSignedWord(ushort value)
        => $"{unchecked((short)value)} (0x{value:x4})";
}
