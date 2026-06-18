using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HylianGrimoire.Models;

namespace HylianGrimoire.Codecs;

public static partial class MessageTextSyntax
{
    [GeneratedRegex(@"\[breakdelay:[^\]]*\]")]
    private static partial Regex BreakDelayTag();

    [GeneratedRegex(@"\n?\[break\]\n?")]
    private static partial Regex BreakTagFull();

    [GeneratedRegex(@"\n?(\[breakdelay:[^\]]*\])\n?")]
    private static partial Regex BreakDelayTagFull();

    public static string ToEditorText(IEnumerable<MessageToken> tokens)
    {
        var result = new StringBuilder();

        foreach (MessageToken token in tokens)
        {
            switch (token)
            {
                case TextToken text:
                    result.Append(text.Text);
                    break;
                case RawByteToken raw:
                    result.Append($"[byte:{raw.Value:x2}]");
                    break;
                case LineBreakToken:
                    result.Append('\n');
                    break;
                case CommandToken command:
                    if (MessageTokenMaps.CommandTags.TryGetValue(command.Code, out string? commandName))
                        result.Append($"[{commandName}]");
                    break;
                case ColorToken color:
                    string colorName = MessageTokenMaps.ColorTags.TryGetValue(color.Index, out string? knownColor)
                        ? knownColor
                        : $"{color.Index:x2}";
                    result.Append($"[color:{colorName}]");
                    break;
                case ShiftToken shift:
                    result.Append($"[shift:{shift.Pixels:x2}]");
                    break;
                case TextIdToken textId:
                    result.Append($"[textid:{textId.Id:x4}]");
                    break;
                case BreakDelayToken breakDelay:
                    result.Append($"[breakdelay:{breakDelay.Frames:x2}]");
                    break;
                case FadeToken fade:
                    result.Append($"[fade:{fade.Frames:x2}]");
                    break;
                case EndFadeToken endFade:
                    result.Append($"[endfade:{endFade.Frames:x4}]");
                    break;
                case SfxToken sfx:
                    string sfxName = MessageSfxMaps.Tags.TryGetValue(sfx.Id, out string? knownSfx)
                        ? knownSfx
                        : $"{sfx.Id:x4}";
                    result.Append($"[sfx:{sfxName}]");
                    break;
                case IconToken icon:
                    result.Append($"[item:{icon.Id:x2}]");
                    break;
                case TextSpeedToken textSpeed:
                    result.Append($"[textspeed:{textSpeed.Speed:x2}]");
                    break;
                case BackgroundToken background:
                    result.Append($"[background:{background.Rgb:x6}]");
                    break;
                case HighscoreToken highscore:
                    string highscoreName = MessageTokenMaps.HighscoreTags.TryGetValue(highscore.Id, out string? knownHighscore)
                        ? knownHighscore
                        : $"minigame:{highscore.Id:x2}";
                    result.Append($"[{highscoreName}]");
                    break;
                case ButtonToken button:
                    if (MessageTokenMaps.ButtonTags.TryGetValue(button.Code, out string? buttonName))
                        result.Append($"[{buttonName}]");
                    break;
            }
        }

        return result.ToString();
    }

    public static string ToDisplay(string editorText)
    {
        editorText = editorText.Replace("[break]", "\n[break]\n");
        editorText = BreakDelayTag().Replace(editorText, m => $"\n{m.Value}\n");
        return editorText;
    }

    public static string FromDisplay(string displayText)
    {
        displayText = BreakTagFull().Replace(displayText, "[break]");
        displayText = BreakDelayTagFull().Replace(displayText, "$1");
        return displayText;
    }

    public static List<MessageToken> FromEditorText(string text)
    {
        var tokens = new List<MessageToken>();
        var plainText = new StringBuilder();
        int i = 0;

        void FlushText()
        {
            if (plainText.Length == 0)
                return;

            tokens.Add(new TextToken(plainText.ToString()));
            plainText.Clear();
        }

        while (i < text.Length)
        {
            char ch = text[i];

            if (ch == '[')
            {
                int j = text.IndexOf(']', i);
                if (j < 0)
                {
                    plainText.Append(ch);
                    i++;
                    continue;
                }

                string originalTagText = text[i..(j + 1)];
                string rawToken = text[(i + 1)..j];
                i = j + 1;

                string name;
                string value;
                int colon = rawToken.IndexOf(':');
                if (colon >= 0)
                {
                    name = rawToken[..colon];
                    value = rawToken[(colon + 1)..];
                }
                else
                {
                    name = rawToken;
                    value = string.Empty;
                }

                MessageToken? token = CreateTokenFromEditorTag(name, value);
                if (token is not null)
                {
                    FlushText();
                    tokens.Add(token);
                }
                else
                {
                    plainText.Append(originalTagText);
                }
            }
            else if (ch == '\n')
            {
                FlushText();
                tokens.Add(new LineBreakToken());
                i++;
            }
            else
            {
                plainText.Append(ch);
                i++;
            }
        }

        FlushText();
        return tokens;
    }

    public static string ApplyCurrentEncodingProfile(string editorText)
    {
        byte[] bytes = MessageCodec.EncodeMessageTokens(FromEditorText(editorText));
        return ToEditorText(MessageCodec.DecodeMessageTokens(bytes, 0, bytes.Length));
    }

    public static bool TryNormalizeEditorText(string editorText, out string normalized)
    {
        try
        {
            normalized = ToEditorText(FromEditorText(editorText));
            return true;
        }
        catch (InvalidDataException)
        {
            normalized = editorText;
            return false;
        }
    }

    private static MessageToken? CreateTokenFromEditorTag(string name, string value)
    {
        if (MessageTokenMaps.HighscoreBytes.TryGetValue(name, out byte highscoreByte))
            return new HighscoreToken(highscoreByte);

        if (name.Equals("byte", StringComparison.OrdinalIgnoreCase))
            return new RawByteToken(ParseHexByte(name, value));

        if (MessageTokenMaps.CommandBytes.TryGetValue(name, out byte noArgByte))
            return new CommandToken((MessageCommand)noArgByte);

        if (name.Equals("color", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
            return new ColorToken((MessageColor)(MessageTokenMaps.ColorBytes.TryGetValue(value, out byte colorByte) ? colorByte : ParseHexByte(name, value)));

        if (name.Equals("shift", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
            return new ShiftToken(ParseHexByte(name, value));

        if (name.Equals("textid", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
            return new TextIdToken(ParseHexUShort(name, value));

        if (name.Equals("breakdelay", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
            return new BreakDelayToken(ParseHexByte(name, value));

        if (name.Equals("fade", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
            return new FadeToken(ParseHexByte(name, value));

        if ((name.Equals("endfade", StringComparison.OrdinalIgnoreCase) || name.Equals("fade2", StringComparison.OrdinalIgnoreCase)) && value.Length > 0)
            return new EndFadeToken(ParseHexUShort(name, value));

        if (name.Equals("sfx", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
        {
            ushort sfx = MessageSfxMaps.Values.TryGetValue(value, out int sfxValue)
                ? (ushort)sfxValue
                : ParseHexUShort(name, value);
            return new SfxToken(sfx);
        }

        if (name.Equals("item", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
            return new IconToken(ParseHexByte(name, value));

        if (name.Equals("textspeed", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
            return new TextSpeedToken(ParseHexByte(name, value));

        if (name.Equals("background", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
            return new BackgroundToken(ParseHexRgb(name, value));

        if (name.Equals("minigame", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
            return new HighscoreToken(ParseHexByte(name, value));

        if (MessageTokenMaps.ButtonBytes.TryGetValue(name, out byte buttonByte))
            return new ButtonToken((MessageButton)buttonByte);

        return null;
    }

    private static byte ParseHexByte(string tagName, string value)
    {
        int parsed = ParseHexInt(tagName, value, 0xff, "hexadecimal byte");
        return (byte)parsed;
    }

    private static ushort ParseHexUShort(string tagName, string value)
    {
        int parsed = ParseHexInt(tagName, value, 0xffff, "hexadecimal 16-bit value");
        return (ushort)parsed;
    }

    private static int ParseHexRgb(string tagName, string value)
    {
        return ParseHexInt(tagName, value, 0xffffff, "hexadecimal RGB value");
    }

    private static int ParseHexInt(string tagName, string value, int maxValue, string expected)
    {
        string normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        if (normalized.Length == 0)
            throw new InvalidDataException($"Invalid value for [{tagName}:{value}]. Expected {expected}.");

        if (!int.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int parsed) || parsed < 0 || parsed > maxValue)
            throw new InvalidDataException($"Invalid value for [{tagName}:{value}]. Expected {expected}.");

        return parsed;
    }
}
