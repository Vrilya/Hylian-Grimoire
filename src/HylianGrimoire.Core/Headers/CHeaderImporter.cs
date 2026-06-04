using System.Globalization;
using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers;

public static class CHeaderImporter
{
    private static readonly string[] MessageMacroNames =
    [
        "DEFINE_MESSAGE_FFFC",
        "DEFINE_MESSAGE_NES",
        "DEFINE_MESSAGE_JPN",
        "DEFINE_MESSAGE",
    ];

    private static readonly IReadOnlyDictionary<string, int> BoxTypes = MessageTokenMaps.HeaderBoxTypeValues;
    private static readonly IReadOnlyDictionary<string, int> BoxPositions = MessageTokenMaps.HeaderBoxPositionValues;
    private static readonly IReadOnlyDictionary<string, string> NoArgTags = MessageTokenMaps.HeaderCommandTags;
    private static readonly IReadOnlyDictionary<string, string> Colors = MessageTokenMaps.HeaderColorTags;
    private static readonly IReadOnlyDictionary<string, int> Highscores = MessageTokenMaps.HeaderHighscoreValues;
    private static readonly IReadOnlyDictionary<string, int> ItemIds = MessageTokenMaps.HeaderItemValues;
    private static readonly IReadOnlyDictionary<string, int> Backgrounds = MessageTokenMaps.HeaderBackgroundValues;
    private static readonly IReadOnlyDictionary<string, int> BackgroundForegroundColors = MessageTokenMaps.HeaderBackgroundForegroundColorValues;
    private static readonly IReadOnlyDictionary<string, int> BackgroundColors = MessageTokenMaps.HeaderBackgroundColorValues;
    private static readonly IReadOnlyDictionary<string, int> BackgroundYOffsets = MessageTokenMaps.HeaderBackgroundYOffsetValues;

    public static List<MessageEntry> Import(
        string content,
        CHeaderMessageSlot preferredSlot = CHeaderMessageSlot.Nes,
        bool allowWesternFallback = true,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        var entries = new List<MessageEntry>();
        int index = 0;

        while (CHeaderCallParser.TryFindCall(content, MessageMacroNames, ref index, out CHeaderCall call))
        {
            int? id = null;
            try
            {
                var args = CHeaderCallParser.SplitTopLevel(call.Text);
                if (args.Count is not 4 and not 7)
                    throw new InvalidDataException($"{call.Name} must have four legacy arguments or seven modern arguments.");

                id = ParseInt(args[0]);
                int type = ParseBoxType(args[1]);
                int position = ParseBoxPosition(args[2]);
                string? body = args.Count == 4
                    ? SelectLegacyMessageBody(args[3], preferredSlot)
                    : SelectModernMessageBody(call.Name, args, preferredSlot, allowWesternFallback);
                if (body is null)
                {
                    continue;
                }

                string text = ParseMessageBody(UnwrapMsgBody(body), encodingProfile);

                entries.Add(new MessageEntry(id.Value, type, position, 0x07, 0)
                {
                    Text = text,
                });
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidDataException(FormatImportError(call.StartLine, id, ex.Message), ex);
            }
        }

        if (entries.Count == 0)
            throw new HeaderMessageEntriesNotFoundException("No DEFINE_MESSAGE entries were found.");

        return entries;
    }

    private static string FormatImportError(int startLine, int? id, string message)
    {
        string location = id is int value
            ? $"DEFINE_MESSAGE 0x{value:x4} starting at line {startLine}"
            : $"DEFINE_MESSAGE starting at line {startLine}";
        return $"{location}: {message}";
    }

    private static string ParseMessageBody(string body, MessageEncodingProfile encodingProfile)
    {
        return MessageTextSyntax.ToEditorText(ParseMessageBodyTokens(body, encodingProfile));
    }

    private static string? SelectLegacyMessageBody(string body, CHeaderMessageSlot preferredSlot)
        => preferredSlot == CHeaderMessageSlot.Nes ? body : null;

    private static string? SelectModernMessageBody(
        string macroName,
        List<string> args,
        CHeaderMessageSlot preferredSlot,
        bool allowWesternFallback)
    {
        int preferredIndex = 3 + (int)preferredSlot;
        if (macroName.Equals("DEFINE_MESSAGE_JPN", StringComparison.OrdinalIgnoreCase))
        {
            if (preferredSlot != CHeaderMessageSlot.Jpn)
            {
                return null;
            }

            preferredIndex = 3;
        }
        else if (macroName.Equals("DEFINE_MESSAGE_NES", StringComparison.OrdinalIgnoreCase))
        {
            if (preferredSlot == CHeaderMessageSlot.Jpn)
            {
                return null;
            }
        }

        if (preferredIndex >= 3 && preferredIndex < args.Count && !IsMissingMsg(args[preferredIndex]))
        {
            return args[preferredIndex];
        }

        if (preferredSlot == CHeaderMessageSlot.Jpn || !allowWesternFallback)
        {
            return null;
        }

        // Western imports may fall back to another western slot, but never to JPN.
        for (int i = 4; i < args.Count; i++)
        {
            if (!IsMissingMsg(args[i]))
            {
                return args[i];
            }
        }

        return null;
    }

    private static bool IsMissingMsg(string body)
    {
        string unwrapped = UnwrapMsgBody(body).Trim();
        return unwrapped.Length == 0
            || unwrapped.Equals("None", StringComparison.OrdinalIgnoreCase)
            || unwrapped.Contains("MISSING", StringComparison.OrdinalIgnoreCase)
            || unwrapped.Contains("UNUSED", StringComparison.OrdinalIgnoreCase);
    }

    private static string UnwrapMsgBody(string body)
    {
        int index = 0;
        while (index < body.Length && char.IsWhiteSpace(body[index]))
            index++;

        int identifierStart = index;
        if (identifierStart >= body.Length || !char.IsLetter(body[identifierStart]))
            return body;

        string identifier = ReadIdentifier(body, ref index);
        if (!identifier.Equals("MSG", StringComparison.OrdinalIgnoreCase))
            return body;

        while (index < body.Length && char.IsWhiteSpace(body[index]))
            index++;

        if (index >= body.Length || body[index] != '(')
            return body;

        return CHeaderCallParser.ReadParenthesized(body, ref index);
    }

    private static List<MessageToken> ParseMessageBodyTokens(string body, MessageEncodingProfile encodingProfile)
    {
        var tokens = new List<MessageToken>();
        if (body.Trim().Equals("None", StringComparison.OrdinalIgnoreCase))
            return tokens;

        int i = 0;

        while (i < body.Length)
        {
            char ch = body[i];

            if (ch == '/' && TrySkipComment(body, ref i))
            {
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                i++;
                continue;
            }

            if (ch == '"')
            {
                AppendText(tokens, ParseCString(body, ref i, encodingProfile), encodingProfile);
                continue;
            }

            if (char.IsLetter(ch) || ch == '_')
            {
                string macro = ReadIdentifier(body, ref i);
                string argumentText = string.Empty;
                if (i < body.Length && body[i] == '(')
                    argumentText = CHeaderCallParser.ReadParenthesized(body, ref i);

                AppendMacro(tokens, macro, argumentText);
                continue;
            }

            if (char.IsDigit(ch))
            {
                AppendRawByte(tokens, (byte)ParseNumericToken(body, ref i), encodingProfile);
                continue;
            }

            i++;
        }

        return tokens;
    }

    private static bool TrySkipComment(string text, ref int index)
    {
        if (index + 1 >= text.Length || text[index] != '/')
        {
            return false;
        }

        if (text[index + 1] == '/')
        {
            index += 2;
            while (index < text.Length && text[index] != '\n')
            {
                index++;
            }

            return true;
        }

        if (text[index + 1] == '*')
        {
            index += 2;
            while (index + 1 < text.Length && (text[index] != '*' || text[index + 1] != '/'))
            {
                index++;
            }

            index = Math.Min(index + 2, text.Length);
            return true;
        }

        return false;
    }

    private static string NormalizeInlineTags(string text)
    {
        return text
            .Replace("[A]", "[A-button]", StringComparison.Ordinal)
            .Replace("[B]", "[B-button]", StringComparison.Ordinal)
            .Replace("[C]", "[C-button]", StringComparison.Ordinal)
            .Replace("[L]", "[L-button]", StringComparison.Ordinal)
            .Replace("[R]", "[R-button]", StringComparison.Ordinal)
            .Replace("[Z]", "[Z-button]", StringComparison.Ordinal)
            .Replace("[C-Up]", "[C-up]", StringComparison.Ordinal)
            .Replace("[C-Down]", "[C-down]", StringComparison.Ordinal)
            .Replace("[C-Left]", "[C-left]", StringComparison.Ordinal)
            .Replace("[C-Right]", "[C-right]", StringComparison.Ordinal)
            .Replace("[Control-Pad]", "[Stick]", StringComparison.Ordinal)
            .Replace("â–¼", "[Triangle]", StringComparison.Ordinal)
            .Replace("▼", "[Triangle]", StringComparison.Ordinal)
            .Replace("<TRIANGLE>", "[Triangle]", StringComparison.OrdinalIgnoreCase);
    }

    private static void AppendText(List<MessageToken> tokens, string text, MessageEncodingProfile encodingProfile)
    {
        tokens.AddRange(MessageTextSyntax.FromEditorText(encodingProfile.HeaderTextToEditorText(NormalizeInlineTags(text))));
    }

    private static void AppendRawByte(List<MessageToken> tokens, byte value, MessageEncodingProfile encodingProfile)
    {
        tokens.AddRange(MessageCodec.DecodeMessageTokens([value, 0x02], 0, 2, encodingProfile));
    }

    private static void AppendMacro(List<MessageToken> tokens, string macro, string argumentText)
    {
        if (macro.Equals("NEWLINE", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new LineBreakToken());
        }
        else if (macro.Equals("BOX_BREAK", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new CommandToken(MessageCommand.Break));
        }
        else if (macro.Equals("BOX_BREAK_DELAYED", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new BreakDelayToken((byte)ParseArgumentByte(argumentText)));
        }
        else if (macro.Equals("COLOR", StringComparison.OrdinalIgnoreCase))
        {
            byte color = Colors.TryGetValue(argumentText.Trim(), out string? mapped)
                && MessageTokenMaps.ColorBytes.TryGetValue(mapped, out byte mappedColor)
                    ? mappedColor
                    : (byte)ParseArgumentByte(argumentText);
            tokens.Add(new ColorToken((MessageColor)color));
        }
        else if (macro.Equals("SHIFT", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new ShiftToken((byte)ParseArgumentByte(argumentText)));
        }
        else if (macro.Equals("TEXTID", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new TextIdToken((ushort)ParseArgumentWord(argumentText)));
        }
        else if (macro.Equals("FADE", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new FadeToken((byte)ParseArgumentByte(argumentText)));
        }
        else if (macro.Equals("FADE2", StringComparison.OrdinalIgnoreCase)
            || macro.Equals("END_FADE", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new EndFadeToken((ushort)ParseArgumentWord(argumentText)));
        }
        else if (macro.Equals("SFX", StringComparison.OrdinalIgnoreCase))
        {
            string argument = argumentText.Trim();
            int value = MessageSfxMaps.HeaderValues.TryGetValue(argument, out int headerMapped)
                ? headerMapped
                : MessageSfxMaps.Values.TryGetValue(argument, out int mapped)
                ? mapped
                : ParseArgumentWord(argumentText);
            tokens.Add(new SfxToken((ushort)value));
        }
        else if (macro.Equals("ITEM_ICON", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new IconToken((byte)ParseItemArgument(argumentText)));
        }
        else if (macro.Equals("TEXT_SPEED", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new TextSpeedToken((byte)ParseArgumentByte(argumentText)));
        }
        else if (macro.Equals("BACKGROUND", StringComparison.OrdinalIgnoreCase))
        {
            var args = CHeaderCallParser.SplitTopLevel(argumentText);
            int value = args.Count switch
            {
                3 => ParseArgumentByte(args[0]) << 16
                    | ParseArgumentByte(args[1]) << 8
                    | ParseArgumentByte(args[2]),
                5 => ParseBackground(args),
                _ => throw new InvalidDataException("BACKGROUND must have three legacy arguments or five modern arguments."),
            };
            tokens.Add(new BackgroundToken(value));
        }
        else if (macro.Equals("HIGHSCORE", StringComparison.OrdinalIgnoreCase))
        {
            int value = Highscores.TryGetValue(argumentText.Trim(), out int mapped)
                ? mapped
                : ParseArgumentByte(argumentText);
            tokens.Add(new HighscoreToken((byte)value));
        }
        else if (NoArgTags.TryGetValue(macro, out string? tag))
        {
            tokens.Add(new CommandToken((MessageCommand)MessageTokenMaps.CommandBytes[tag]));
        }
        else
        {
            throw new InvalidDataException($"Unknown C header message macro: {macro}.");
        }
    }

    private static string ParseCString(string text, ref int index, MessageEncodingProfile encodingProfile)
    {
        if (text[index] != '"')
            throw new InvalidDataException("Expected C string.");

        index++;
        var result = new StringBuilder();

        while (index < text.Length)
        {
            char ch = text[index++];
            if (ch == '"')
                break;

            if (ch != '\\' || index >= text.Length)
            {
                result.Append(ch);
                continue;
            }

            char escaped = text[index++];
            switch (escaped)
            {
                case 'n':
                    result.Append('\n');
                    break;
                case 'r':
                    break;
                case 't':
                    result.Append('\t');
                    break;
                case '\\':
                    result.Append('\\');
                    break;
                case '"':
                    result.Append('"');
                    break;
                case 'x':
                    result.Append(ReadHexEscapedByteAsText(text, ref index, encodingProfile));
                    break;
                default:
                    result.Append(escaped);
                    break;
            }
        }

        return result.ToString();
    }

    private static string ReadHexEscapedByteAsText(
        string text,
        ref int index,
        MessageEncodingProfile encodingProfile)
    {
        int value = ReadHexByte(text, ref index);
        if (value < 0)
            return string.Empty;

        string decoded = MessageTextSyntax.ToEditorText(MessageCodec.DecodeMessageTokens([(byte)value, 0x02], 0, 2, encodingProfile));
        return decoded.Length > 0 ? decoded : ((char)value).ToString();
    }

    private static string ReadIdentifier(string text, ref int index)
    {
        int start = index;
        while (index < text.Length && (char.IsLetterOrDigit(text[index]) || text[index] == '_'))
            index++;
        return text[start..index];
    }

    private static int ParseBoxType(string value)
    {
        value = value.Trim();
        if (BoxTypes.TryGetValue(value, out int type))
            return type;

        const string unknownPrefix = "TEXTBOX_TYPE_UNK_";
        if (value.StartsWith(unknownPrefix, StringComparison.OrdinalIgnoreCase))
            return ParseHexSuffix(value, unknownPrefix);

        return ParseInt(value);
    }

    private static int ParseBoxPosition(string value)
    {
        value = value.Trim();
        if (BoxPositions.TryGetValue(value, out int position))
            return position;

        const string unknownPrefix = "TEXTBOX_POS_UNK_";
        if (value.StartsWith(unknownPrefix, StringComparison.OrdinalIgnoreCase))
            return ParseHexSuffix(value, unknownPrefix);

        return ParseInt(value);
    }

    private static int ParseArgumentByte(string value)
    {
        int result = ParseArgumentValue(value);
        if (result is < 0 or > 0xff)
        {
            throw new InvalidDataException($"Expected byte-sized argument, got 0x{result:X}.");
        }

        return result;
    }

    private static int ParseItemArgument(string value)
    {
        string argument = value.Trim();
        if (ItemIds.TryGetValue(argument, out int itemId))
        {
            return itemId;
        }

        return ParseArgumentByte(value);
    }

    private static int ParseBackground(List<string> args)
    {
        int bgIndex = ParseNamedByte(args[0], Backgrounds);
        int foreground = ParseNamedByte(args[1], BackgroundForegroundColors);
        int background = ParseNamedByte(args[2], BackgroundColors);
        int yOffset = ParseNamedByte(args[3], BackgroundYOffsets);
        int unknown = ParseArgumentByte(args[4]);
        return (bgIndex << 16) | ((foreground << 4 | background) << 8) | (yOffset << 4 | unknown);
    }

    private static int ParseNamedByte(string value, IReadOnlyDictionary<string, int> names)
    {
        string argument = value.Trim();
        if (names.TryGetValue(argument, out int named))
        {
            return named;
        }

        return ParseArgumentByte(value);
    }

    private static int ParseNumericToken(string text, ref int index)
    {
        int start = index;
        while (index < text.Length && (IsHex(text[index]) || text[index] is 'x' or 'X'))
        {
            index++;
        }

        string token = text[start..index];
        int value = ParseInt(token);
        if (value is < 0 or > 0xff)
        {
            throw new InvalidDataException($"Expected byte-sized raw value, got 0x{value:X}.");
        }

        return value;
    }

    private static int ParseArgumentWord(string value)
    {
        int result = ParseArgumentValue(value);
        if (result is < 0 or > 0xffff)
        {
            throw new InvalidDataException($"Expected 16-bit argument, got 0x{result:X}.");
        }

        return result;
    }

    private static int ParseArgumentValue(string value)
    {
        value = value.Trim();
        if (value.StartsWith('"'))
            return ParseQuotedBytesAsInt(value);

        return ParseInt(value);
    }

    private static int ParseQuotedBytesAsInt(string value)
    {
        int index = 0;
        var bytes = new List<byte>();

        while (index < value.Length)
        {
            if (value[index] != '"')
            {
                index++;
                continue;
            }

            index++;
            while (index < value.Length && value[index] != '"')
            {
                char ch = value[index++];
                if (ch == '\\' && index < value.Length)
                {
                    char escaped = value[index++];
                    if (escaped == 'x')
                    {
                        int byteValue = ReadHexByte(value, ref index);
                        if (byteValue >= 0)
                            bytes.Add((byte)byteValue);
                    }
                    else if (escaped == 'n')
                    {
                        bytes.Add(0x01);
                    }
                    else
                    {
                        bytes.Add((byte)escaped);
                    }
                }
                else
                {
                    bytes.Add((byte)ch);
                }
            }

            if (index < value.Length && value[index] == '"')
                index++;
        }

        int result = 0;
        foreach (byte b in bytes)
            result = (result << 8) | b;
        return result;
    }

    private static int ReadHexByte(string text, ref int index)
    {
        int value = 0;
        int count = 0;

        while (index < text.Length && count < 2 && IsHex(text[index]))
        {
            value = (value << 4) | HexValue(text[index]);
            index++;
            count++;
        }

        return count == 0 ? -1 : value;
    }

    private static int ParseInt(string value)
    {
        value = value.Trim();
        if (value.Length == 0)
            throw new InvalidDataException("Invalid empty integer value in C header.");

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            string hex = value[2..];
            if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hexResult))
                return hexResult;

            throw new InvalidDataException($"Invalid hexadecimal integer value in C header: '{value}'.");
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            return result;

        throw new InvalidDataException($"Invalid integer value in C header: '{value}'.");
    }

    private static int ParseHexSuffix(string value, string prefix)
    {
        string suffix = value[prefix.Length..].Trim();
        if (suffix.Length > 0 && int.TryParse(suffix, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int result))
            return result;

        throw new InvalidDataException($"Invalid hexadecimal suffix in C header: '{value}'.");
    }

    private static bool IsHex(char ch) =>
        ch is >= '0' and <= '9'
        || ch is >= 'a' and <= 'f'
        || ch is >= 'A' and <= 'F';

    private static int HexValue(char ch) => ch switch
    {
        >= '0' and <= '9' => ch - '0',
        >= 'a' and <= 'f' => ch - 'a' + 10,
        >= 'A' and <= 'F' => ch - 'A' + 10,
        _ => 0,
    };
}
