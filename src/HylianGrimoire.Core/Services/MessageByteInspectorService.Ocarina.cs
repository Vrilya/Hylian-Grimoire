using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public static partial class MessageByteInspectorService
{
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
            case RawByteToken raw:
                AddSegment(
                    bytes,
                    segments,
                    [raw.Value],
                    MessageByteSegmentKind.RawByte,
                    "Raw Byte",
                    $"Preserved raw byte 0x{raw.Value:x2}; this byte is not mapped to known OoT editor text or control syntax.",
                    $"[byte:{raw.Value:x2}]");
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
}
