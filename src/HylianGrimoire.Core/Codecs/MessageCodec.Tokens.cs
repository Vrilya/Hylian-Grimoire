using System.Text;
using HylianGrimoire.Models;

namespace HylianGrimoire.Codecs;

public static partial class MessageCodec
{
    public static List<MessageToken> DecodeMessageTokens(
        byte[] raw,
        int startOffset,
        int byteCount,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        var tokens = new List<MessageToken>();
        if (startOffset < 0 || startOffset >= raw.Length || byteCount <= 0)
            return tokens;

        int i = startOffset;
        int end = Math.Min(raw.Length, startOffset + byteCount);
        var text = new StringBuilder();

        void FlushText()
        {
            if (text.Length == 0)
                return;

            tokens.Add(new TextToken(text.ToString()));
            text.Clear();
        }

        while (i < end)
        {
            byte b = raw[i];

            if (b == 0x00 || b == 0x03)
            {
                i++;
                continue;
            }
            else if (b == 0x02)
            {
                break;
            }
            else if (b == 0x01)
            {
                FlushText();
                tokens.Add(new LineBreakToken());
            }
            else if (NoArgCmds.ContainsKey(b))
            {
                FlushText();
                tokens.Add(new CommandToken((MessageCommand)b));
            }
            else if (b == 0x05)
            {
                if (i + 1 >= end)
                    break;

                FlushText();
                tokens.Add(new ColorToken((MessageColor)raw[++i]));
            }
            else if (b == 0x06)
            {
                if (i + 1 >= end)
                    break;

                FlushText();
                tokens.Add(new ShiftToken(raw[++i]));
            }
            else if (b == 0x07)
            {
                if (i + 2 >= end)
                    break;

                FlushText();
                ushort id = (ushort)((raw[i + 1] << 8) | raw[i + 2]);
                i += 2;
                tokens.Add(new TextIdToken(id));
            }
            else if (b == 0x0c)
            {
                if (i + 1 >= end)
                    break;

                FlushText();
                tokens.Add(new BreakDelayToken(raw[++i]));
            }
            else if (b == 0x0e)
            {
                if (i + 1 >= end)
                    break;

                FlushText();
                tokens.Add(new FadeToken(raw[++i]));
            }
            else if (b == 0x11)
            {
                if (i + 2 >= end)
                    break;

                FlushText();
                ushort frames = (ushort)((raw[i + 1] << 8) | raw[i + 2]);
                i += 2;
                tokens.Add(new EndFadeToken(frames));
            }
            else if (b == 0x12)
            {
                if (i + 2 >= end)
                    break;

                FlushText();
                ushort id = (ushort)((raw[i + 1] << 8) | raw[i + 2]);
                i += 2;
                tokens.Add(new SfxToken(id));
            }
            else if (b == 0x13)
            {
                if (i + 1 >= end)
                    break;

                FlushText();
                tokens.Add(new IconToken(raw[++i]));
            }
            else if (b == 0x14)
            {
                if (i + 1 >= end)
                    break;

                FlushText();
                tokens.Add(new TextSpeedToken(raw[++i]));
            }
            else if (b == 0x15)
            {
                if (i + 3 >= end)
                    break;

                FlushText();
                int rgb = (raw[i + 1] << 16) | (raw[i + 2] << 8) | raw[i + 3];
                i += 3;
                tokens.Add(new BackgroundToken(rgb));
            }
            else if (b == 0x1e)
            {
                if (i + 1 >= end)
                    break;

                FlushText();
                tokens.Add(new HighscoreToken(raw[++i]));
            }
            else if (ButtonBytes.ContainsKey(b))
            {
                FlushText();
                tokens.Add(new ButtonToken((MessageButton)b));
            }
            else if (encodingProfile.TryGetEditorChar(b, out char specialCh))
            {
                text.Append(specialCh);
            }
            else if (b >= 0x20 && b <= 0x7e)
            {
                text.Append((char)b);
            }
            else
            {
                FlushText();
                tokens.Add(new RawByteToken(b));
            }

            i++;
        }

        FlushText();
        return tokens;
    }

    public static byte[] EncodeMessageTokens(
        IEnumerable<MessageToken> tokens,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        var output = new List<byte>();

        foreach (MessageToken token in tokens)
        {
            switch (token)
            {
                case TextToken text:
                    AddTextBytes(output, text.Text, encodingProfile);
                    break;
                case RawByteToken raw:
                    output.Add(raw.Value);
                    break;
                case LineBreakToken:
                    output.Add(0x01);
                    break;
                case CommandToken command:
                    output.Add(command.Code);
                    break;
                case ColorToken color:
                    output.Add(0x05);
                    output.Add(color.Index);
                    break;
                case ShiftToken shift:
                    output.Add(0x06);
                    output.Add(shift.Pixels);
                    break;
                case TextIdToken textId:
                    output.Add(0x07);
                    output.Add((byte)((textId.Id >> 8) & 0xff));
                    output.Add((byte)(textId.Id & 0xff));
                    break;
                case BreakDelayToken breakDelay:
                    output.Add(0x0c);
                    output.Add(breakDelay.Frames);
                    break;
                case FadeToken fade:
                    output.Add(0x0e);
                    output.Add(fade.Frames);
                    break;
                case EndFadeToken endFade:
                    output.Add(0x11);
                    output.Add((byte)((endFade.Frames >> 8) & 0xff));
                    output.Add((byte)(endFade.Frames & 0xff));
                    break;
                case SfxToken sfx:
                    output.Add(0x12);
                    output.Add((byte)((sfx.Id >> 8) & 0xff));
                    output.Add((byte)(sfx.Id & 0xff));
                    break;
                case IconToken icon:
                    output.Add(0x13);
                    output.Add(icon.Id);
                    break;
                case TextSpeedToken textSpeed:
                    output.Add(0x14);
                    output.Add(textSpeed.Speed);
                    break;
                case BackgroundToken background:
                    output.Add(0x15);
                    output.Add((byte)((background.Rgb >> 16) & 0xff));
                    output.Add((byte)((background.Rgb >> 8) & 0xff));
                    output.Add((byte)(background.Rgb & 0xff));
                    break;
                case HighscoreToken highscore:
                    output.Add(0x1e);
                    output.Add(highscore.Id);
                    break;
                case ButtonToken button:
                    output.Add(button.Code);
                    break;
            }
        }

        output.Add(0x02);

        while (output.Count % 4 != 0)
            output.Add(0x00);

        return output.ToArray();
    }

    private static void AddTextBytes(List<byte> output, string text, MessageEncodingProfile encodingProfile)
    {
        foreach (char ch in text)
        {
            if (encodingProfile.TryGetByte(ch, out byte specialByte))
            {
                output.Add(specialByte);
            }
            else if (ch is >= '\u0020' and <= '\u007e')
            {
                output.Add((byte)ch);
            }
            else
            {
                throw new InvalidDataException($"Unsupported character '{ch}' (U+{(int)ch:X4}).");
            }
        }
    }
}
