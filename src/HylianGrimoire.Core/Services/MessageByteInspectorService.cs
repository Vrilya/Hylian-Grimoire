using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;
using HylianGrimoire.Games;
using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public enum MessageByteSegmentKind
{
    TableField,
    HeaderField,
    Text,
    LineBreak,
    ControlCode,
    Parameter,
    Terminator,
    Padding,
}

public enum MessageByteSectionKind
{
    MessageTableEntry,
    MessageHeader,
    MessageData,
}

public sealed record MessageByteInspection(
    GameKind GameKind,
    int MessageId,
    IReadOnlyList<byte> Bytes,
    IReadOnlyList<MessageByteSegment> Segments,
    IReadOnlyList<MessageByteSection> Sections);

public sealed record MessageByteSection(
    MessageByteSectionKind Kind,
    string Title,
    IReadOnlyList<byte> Bytes,
    IReadOnlyList<MessageByteSegment> Segments);

public sealed record MessageByteSegment(
    int Offset,
    int Length,
    MessageByteSegmentKind Kind,
    string Label,
    string Description,
    string? EditorSyntax)
{
    public bool Contains(int offset)
        => offset >= Offset && offset < Offset + Length;
}

public static class MessageByteInspectorService
{
    private const int MajorasMaskHeaderSize = 11;

    public static bool CanInspect(GameKind gameKind)
        => gameKind is GameKind.OcarinaOfTime or GameKind.MajorasMask;

    public static MessageByteInspection Inspect(
        GameKind gameKind,
        MessageEntry entry,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= gameKind is GameKind.MajorasMask
            ? MessageEncodingProfile.MajorasMask
            : MessageEncodingProfile.Default;
        if (!CanInspect(gameKind))
        {
            throw new NotSupportedException($"{gameKind} message byte inspection is not supported yet.");
        }

        return gameKind switch
        {
            GameKind.OcarinaOfTime => InspectOcarinaOfTime(entry, encodingProfile),
            GameKind.MajorasMask => InspectMajorasMask(entry, encodingProfile),
            _ => throw new NotSupportedException($"{gameKind} message byte inspection is not supported yet."),
        };
    }

    private static MessageByteInspection InspectOcarinaOfTime(MessageEntry entry, MessageEncodingProfile encodingProfile)
    {
        List<MessageToken> tokens = MessageTextSyntax.FromEditorText(entry.Text);
        var bytes = new List<byte>();
        var segments = new List<MessageByteSegment>();

        foreach (MessageToken token in tokens)
        {
            AddOcarinaToken(bytes, segments, token, encodingProfile);
        }

        AddSegment(bytes, segments, [0x02], MessageByteSegmentKind.Terminator, "Terminator", "Automatically ends the encoded message byte stream.", null);
        int paddingStart = bytes.Count;
        while (bytes.Count % 4 != 0)
        {
            bytes.Add(0x00);
        }

        if (bytes.Count > paddingStart)
        {
            segments.Add(new MessageByteSegment(
                paddingStart,
                bytes.Count - paddingStart,
                MessageByteSegmentKind.Padding,
                "Padding",
                "Zero padding used to align the encoded message to a 4-byte boundary.",
                null));
        }

        byte[] messageBytes = bytes.ToArray();
        MessageByteSegment[] messageSegments = segments.ToArray();
        var messageDataSection = new MessageByteSection(
            MessageByteSectionKind.MessageData,
            "Message data",
            messageBytes,
            messageSegments);

        return new MessageByteInspection(
            GameKind.OcarinaOfTime,
            entry.Id,
            messageBytes,
            messageSegments,
            [BuildOcarinaMessageTableSection(entry), messageDataSection]);
    }

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

    private static MessageByteSection BuildOcarinaMessageTableSection(MessageEntry entry)
    {
        int offset = entry.Offset & 0xffffff;
        byte[] tableBytes =
        [
            (byte)((entry.Id >> 8) & 0xff),
            (byte)(entry.Id & 0xff),
            (byte)(((entry.Type & 0x0f) << 4) | (entry.Position & 0x0f)),
            0x00,
            (byte)(entry.Bank & 0xff),
            (byte)((offset >> 16) & 0xff),
            (byte)((offset >> 8) & 0xff),
            (byte)(offset & 0xff),
        ];

        MessageByteSegment[] segments =
        [
            new(0, 2, MessageByteSegmentKind.TableField, "Message ID", $"Table message ID 0x{entry.Id:x4}.", null),
            new(2, 1, MessageByteSegmentKind.TableField, "Type / Position", $"Textbox type 0x{entry.Type:x1}, position 0x{entry.Position:x1}.", null),
            new(3, 1, MessageByteSegmentKind.TableField, "Reserved", "Reserved table byte.", null),
            new(4, 1, MessageByteSegmentKind.TableField, "Bank", $"Message data bank 0x{entry.Bank:x2}.", null),
            new(5, 3, MessageByteSegmentKind.TableField, "Message Offset", $"Message data offset 0x{offset:x6}.", null),
        ];

        return new MessageByteSection(
            MessageByteSectionKind.MessageTableEntry,
            "Message table entry",
            tableBytes,
            segments);
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

    private static void AddOcarinaToken(
        List<byte> bytes,
        List<MessageByteSegment> segments,
        MessageToken token,
        MessageEncodingProfile encodingProfile)
    {
        switch (token)
        {
            case TextToken text:
                AddText(bytes, segments, text.Text, encodingProfile);
                break;
            case LineBreakToken:
                AddSegment(bytes, segments, [0x01], MessageByteSegmentKind.LineBreak, "Line Break", "Moves to the next line within the current textbox.", "\\n");
                break;
            case CommandToken command:
                AddCommand(bytes, segments, command.Code, GetCommandTag(command.Code), GetCommandDescription(command.Code));
                break;
            case ColorToken color:
                AddCommandWithParameters(
                    bytes,
                    segments,
                    0x05,
                    [color.Index],
                    "Color",
                    GetColorLabel(color.Index),
                    "Changes the text color.",
                    $"[color:{GetColorTag(color.Index)}]");
                break;
            case ShiftToken shift:
                AddCommandWithParameters(bytes, segments, 0x06, [shift.Pixels], "Shift", $"{shift.Pixels} px", "Shifts the following text horizontally.", $"[shift:{shift.Pixels:x2}]");
                break;
            case TextIdToken textId:
                AddCommandWithParameters(bytes, segments, 0x07, ToBigEndian(textId.Id), "Next Text ID", $"0x{textId.Id:x4}", "Sets the next message ID and ends with a has-next textbox state.", $"[textid:{textId.Id:x4}]");
                break;
            case BreakDelayToken breakDelay:
                AddCommandWithParameters(bytes, segments, 0x0c, [breakDelay.Frames], "Textbox Delay", $"{breakDelay.Frames} frames", "Waits before switching to the next textbox.", $"[breakdelay:{breakDelay.Frames:x2}]");
                break;
            case FadeToken fade:
                AddCommandWithParameters(bytes, segments, 0x0e, [fade.Frames], "Fade", $"{fade.Frames} frames", "Waits before ending the textbox.", $"[fade:{fade.Frames:x2}]");
                break;
            case EndFadeToken endFade:
                AddCommandWithParameters(bytes, segments, 0x11, ToBigEndian(endFade.Frames), "Long Fade", $"{endFade.Frames} frames", "Waits before ending the textbox with a 16-bit duration.", $"[endfade:{endFade.Frames:x4}]");
                break;
            case SfxToken sfx:
                AddCommandWithParameters(bytes, segments, 0x12, ToBigEndian(sfx.Id), "Sound Effect", GetSfxLabel(sfx.Id), "Plays a sound effect.", $"[sfx:{GetSfxTag(sfx.Id)}]");
                break;
            case IconToken icon:
                AddCommandWithParameters(bytes, segments, 0x13, [icon.Id], "Item Icon", $"0x{icon.Id:x2}", "Draws an item icon.", $"[item:{icon.Id:x2}]");
                break;
            case TextSpeedToken textSpeed:
                AddCommandWithParameters(bytes, segments, 0x14, [textSpeed.Speed], "Text Speed", $"0x{textSpeed.Speed:x2}", "Sets the per-character text speed.", $"[textspeed:{textSpeed.Speed:x2}]");
                break;
            case BackgroundToken background:
                AddCommandWithParameters(bytes, segments, 0x15, ToRgb(background.Rgb), "Background", $"#{background.Rgb:x6}", "Draws a message background effect.", $"[background:{background.Rgb:x6}]");
                break;
            case HighscoreToken highscore:
                AddCommandWithParameters(bytes, segments, 0x1e, [highscore.Id], "High Score", GetHighscoreLabel(highscore.Id), "Prints a minigame high score.", GetHighscoreTag(highscore.Id));
                break;
            case ButtonToken button:
                AddSegment(bytes, segments, [button.Code], MessageByteSegmentKind.ControlCode, GetButtonLabel(button.Code), "Draws a controller button glyph.", GetButtonTag(button.Code));
                break;
        }
    }

    private static void AddText(
        List<byte> bytes,
        List<MessageByteSegment> segments,
        string text,
        MessageEncodingProfile encodingProfile)
    {
        if (text.Length == 0)
        {
            return;
        }

        int offset = bytes.Count;
        foreach (char ch in text)
        {
            if (encodingProfile.TryGetByte(ch, out byte mappedByte))
            {
                bytes.Add(mappedByte);
            }
            else if (ch is >= '\u0020' and <= '\u007e')
            {
                bytes.Add((byte)ch);
            }
            else
            {
                throw new InvalidDataException($"Unsupported character '{ch}' (U+{(int)ch:X4}).");
            }
        }

        segments.Add(new MessageByteSegment(
            offset,
            bytes.Count - offset,
            MessageByteSegmentKind.Text,
            "Text",
            $"Plain message text: {TrimForDescription(text)}",
            null));
    }

    private static void AddCommand(
        List<byte> bytes,
        List<MessageByteSegment> segments,
        byte command,
        string editorSyntax,
        string description)
    {
        AddSegment(bytes, segments, [command], MessageByteSegmentKind.ControlCode, GetTagLabel(editorSyntax), description, editorSyntax);
    }

    private static void AddCommandWithParameters(
        List<byte> bytes,
        List<MessageByteSegment> segments,
        byte command,
        IReadOnlyList<byte> parameters,
        string label,
        string parameterLabel,
        string description,
        string editorSyntax)
    {
        AddSegment(bytes, segments, [command], MessageByteSegmentKind.ControlCode, label, description, editorSyntax);
        AddSegment(bytes, segments, parameters, MessageByteSegmentKind.Parameter, $"{label} Parameter", parameterLabel, editorSyntax);
    }

    private static void AddSegment(
        List<byte> bytes,
        List<MessageByteSegment> segments,
        IReadOnlyList<byte> segmentBytes,
        MessageByteSegmentKind kind,
        string label,
        string description,
        string? editorSyntax)
    {
        if (segmentBytes.Count == 0)
        {
            return;
        }

        int offset = bytes.Count;
        bytes.AddRange(segmentBytes);
        segments.Add(new MessageByteSegment(offset, segmentBytes.Count, kind, label, description, editorSyntax));
    }

    private static byte[] ToBigEndian(ushort value)
        => [(byte)((value >> 8) & 0xff), (byte)(value & 0xff)];

    private static byte[] ToRgb(int rgb)
        => [(byte)((rgb >> 16) & 0xff), (byte)((rgb >> 8) & 0xff), (byte)(rgb & 0xff)];

    private static int ReadU16(byte[] bytes, int offset)
        => (bytes[offset] << 8) | bytes[offset + 1];

    private static MessageByteSegment ShiftSegment(MessageByteSegment segment, int offset)
        => segment with { Offset = segment.Offset + offset };

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

    private static string FormatYesNo(bool value)
        => value ? "yes" : "no";

    private static string FormatSignedWord(ushort value)
        => $"{unchecked((short)value)} (0x{value:x4})";

    private static string GetCommandTag(byte command)
        => MessageTokenMaps.CommandTags.TryGetValue(command, out string? tag)
            ? $"[{tag}]"
            : $"[0x{command:x2}]";

    private static string GetCommandDescription(byte command)
        => command switch
        {
            0x04 => "Starts a new textbox page.",
            0x08 => "Draws following text instantly.",
            0x09 => "Returns to normal text drawing.",
            0x0a => "Ends with a persistent textbox state; used by shop-style text.",
            0x0b => "Ends with an event-controlled textbox state.",
            0x0d => "Waits until the player presses a button.",
            0x0f => "Prints the player's file name.",
            0x10 => "Draws the ocarina staff.",
            0x16 => "Prints the Running Man marathon time.",
            0x17 => "Prints the last race timer value.",
            0x18 => "Prints horseback archery points.",
            0x19 => "Prints the current Gold Skulltula count.",
            0x1a => "Disallows skipping the textbox.",
            0x1b => "Shows a two-choice prompt.",
            0x1c => "Shows a three-choice prompt.",
            0x1d => "Prints caught fish information.",
            0x1f => "Prints the current in-game time.",
            _ => "Ocarina of Time control code.",
        };

    private static string GetMajorasMaskCommandDescription(byte command, string tag)
        => command switch
        {
            0x0a => "Text speed marker; this MM text format emits it without an explicit encoded argument.",
            0x0b => "Prints the Swamp Cruise Archery required-hit value.",
            0x0c => "Prints the current Stray Fairy count.",
            0x0d => "Prints the current Gold Skulltula token count.",
            0x0e => "Prints the current point value up to 99.",
            0x0f => "Prints the current point value up to 9999.",
            0x10 => "Starts a new textbox page.",
            0x12 => "Starts a new textbox page using the BOX_BREAK2 layout adjustment.",
            0x13 => "Resets horizontal text position without adding newline spacing.",
            0x14 => "Shifts the following text horizontally.",
            0x15 => "Ends the current message with the continue icon.",
            0x16 => "Prints the player's file name.",
            0x17 => "Draws following text instantly.",
            0x18 => "Returns to normal text drawing.",
            0x19 => "Ends with an event-controlled textbox state.",
            0x1a => "Ends with a persistent textbox state.",
            0x1b => "Starts a delayed textbox break after the frame count.",
            0x1c => "Ends with a normal fade after the frame count.",
            0x1d => "Ends with a skippable fade after the frame count.",
            0x1e => "Plays a sound effect.",
            0x1f => "Pauses text drawing for the specified frame count.",
            0xc1 => "Draws the Majora's Mask message background command.",
            0xc2 => "Sets the textbox to a two-choice prompt.",
            0xc3 => "Sets the textbox to a three-choice prompt.",
            0xc4 => "Prints the Postman timer.",
            0xc5 => "Prints minigame timer 1.",
            0xc6 => "Prints timer 2.",
            0xc7 => "Prints the Moon crash timer.",
            0xc8 => "Prints minigame timer 2.",
            0xc9 => "Prints the environmental hazard timer.",
            0xca => "Prints the current in-game time.",
            0xcb => "Prints the chest flag value.",
            0xcc => "Ends with bank rupee input behavior.",
            0xcd => "Prints the selected rupee amount.",
            0xce => "Prints the total rupee amount.",
            0xcf => "Prints remaining time until the Moon crashes.",
            0xd0 => "Ends with Doggy Racetrack bet input behavior.",
            0xd1 => "Ends with Bomber Code input behavior.",
            0xd2 => "Ends with pause-menu textbox behavior.",
            0xd3 => "Prints the active time speed.",
            0xd4 => "Prints the Song of Soaring destination.",
            0xd5 => "Ends with Lottery Code input behavior.",
            0xd6 => "Prints the full Spider House mask code.",
            0xd7 => "Prints remaining Woodfall stray fairies.",
            0xd8 => "Prints remaining Snowhead stray fairies.",
            0xd9 => "Prints remaining Great Bay stray fairies.",
            0xda => "Prints remaining Stone Tower stray fairies.",
            0xdb => "Prints the Swamp Cruise Archery score.",
            0xdc => "Prints the winning Lottery Code.",
            0xdd => "Prints the player's Lottery Code guess.",
            0xde => "Prints the held item price.",
            0xdf => "Prints the Bomber Code.",
            0xe0 => "Ends with the alternate event-controlled textbox state.",
            0xe1 => "Prints Spider House mask code part 1.",
            0xe2 => "Prints Spider House mask code part 2.",
            0xe3 => "Prints Spider House mask code part 3.",
            0xe4 => "Prints Spider House mask code part 4.",
            0xe5 => "Prints Spider House mask code part 5.",
            0xe6 => "Prints Spider House mask code part 6.",
            0xe7 => "Prints remaining hours until the Moon crashes.",
            0xe8 => "Prints remaining time until the next day.",
            0xf0 => "Prints the bank rupees high score.",
            0xf1 => "Prints high-score points value 1.",
            0xf2 => "Prints the fishing points high score.",
            0xf3 => "Prints the Boat Archery high score as a time.",
            0xf4 => "Prints the Horseback Balloon high score as a time.",
            0xf5 => "Prints the Lottery guess high score as a time.",
            0xf6 => "Prints the Town Shooting Gallery high score.",
            0xf7 => "Prints unknown high-score value 1.",
            0xf8 => "Prints unknown high-score value 3 lower digits.",
            0xf9 => "Prints the Horseback Balloon high score.",
            0xfa => "Prints the Deku Playground Day 1 high score.",
            0xfb => "Prints the Deku Playground Day 2 high score.",
            0xfc => "Prints the Deku Playground Day 3 high score.",
            0xfd => "Prints the Day 1 Deku Playground player name.",
            0xfe => "Prints the Day 2 Deku Playground player name.",
            0xff => "Prints the Day 3 Deku Playground player name.",
            _ => $"Majora's Mask control code [{tag}].",
        };

    private static string GetMajorasMaskParameterDescription(string tag, ushort argument)
    {
        if (tag.Equals("sfx", StringComparison.OrdinalIgnoreCase)
            && MmMessageSfxMaps.Names.TryGetValue(argument, out string? sfxName))
        {
            return $"{sfxName} (0x{argument:x4})";
        }

        return $"0x{argument:x4}";
    }

    private static string GetColorTag(byte value)
        => MessageTokenMaps.ColorTags.TryGetValue(value, out string? tag) ? tag : $"{value:x2}";

    private static string GetColorLabel(byte value)
        => MessageTokenMaps.ColorTags.TryGetValue(value, out string? tag) ? $"{GetTagLabel(tag)} (0x{value:x2})" : $"0x{value:x2}";

    private static string GetButtonTag(byte value)
        => MessageTokenMaps.ButtonTags.TryGetValue(value, out string? tag) ? $"[{tag}]" : $"[0x{value:x2}]";

    private static string GetButtonLabel(byte value)
        => MessageTokenMaps.ButtonTags.TryGetValue(value, out string? tag) ? GetTagLabel(tag) : $"Button 0x{value:x2}";

    private static string GetHighscoreTag(byte value)
        => MessageTokenMaps.HighscoreTags.TryGetValue(value, out string? tag) ? $"[{tag}]" : $"[minigame:{value:x2}]";

    private static string GetHighscoreLabel(byte value)
        => MessageTokenMaps.HighscoreTags.TryGetValue(value, out string? tag) ? $"{GetTagLabel(tag)} (0x{value:x2})" : $"Minigame 0x{value:x2}";

    private static string GetSfxTag(ushort value)
        => MessageSfxMaps.Tags.TryGetValue(value, out string? tag) ? tag : $"{value:x4}";

    private static string GetSfxLabel(ushort value)
        => MessageSfxMaps.Tags.TryGetValue(value, out string? tag) ? $"{tag} (0x{value:x4})" : $"0x{value:x4}";

    private static string GetTagLabel(string tag)
    {
        string cleaned = tag.Trim('[', ']');
        if (cleaned.Length == 0)
        {
            return tag;
        }

        return string.Join(
            " ",
            cleaned.Split(['-', ':'], StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static string TrimForDescription(string text)
    {
        string singleLine = text.Replace("\r", "\\r").Replace("\n", "\\n");
        return singleLine.Length <= 48 ? singleLine : $"{singleLine[..45]}...";
    }
}
