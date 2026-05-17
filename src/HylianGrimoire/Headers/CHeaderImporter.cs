using System.Globalization;
using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers;

public static class CHeaderImporter
{
    private static readonly IReadOnlyDictionary<string, int> BoxTypes = MessageTokenMaps.HeaderBoxTypeValues;
    private static readonly IReadOnlyDictionary<string, int> BoxPositions = MessageTokenMaps.HeaderBoxPositionValues;
    private static readonly IReadOnlyDictionary<string, string> NoArgTags = MessageTokenMaps.HeaderCommandTags;
    private static readonly IReadOnlyDictionary<string, string> Colors = MessageTokenMaps.HeaderColorTags;
    private static readonly IReadOnlyDictionary<string, int> Highscores = MessageTokenMaps.HeaderHighscoreValues;
    private static readonly MessageEncodingProfile EncodingProfile = MessageEncodingProfile.Default;

    public static List<MessageEntry> Import(string content)
    {
        var entries = new List<MessageEntry>();
        int index = 0;

        while (TryFindCall(content, "DEFINE_MESSAGE", ref index, out HeaderCall call))
        {
            int? id = null;
            try
            {
                var args = SplitTopLevel(call.Text, expectedParts: 4);
                if (args.Count != 4)
                    throw new InvalidDataException("DEFINE_MESSAGE must have four arguments.");

                id = ParseInt(args[0]);
                int type = ParseBoxType(args[1]);
                int position = ParseBoxPosition(args[2]);
                string text = ParseMessageBody(args[3]);

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
            throw new InvalidDataException("No DEFINE_MESSAGE entries were found.");

        return entries;
    }

    private static bool TryFindCall(string text, string name, ref int index, out HeaderCall call)
    {
        call = default;

        int start = text.IndexOf(name, index, StringComparison.Ordinal);
        if (start < 0)
            return false;

        int startLine = GetLineNumber(text, start);
        int open = text.IndexOf('(', start + name.Length);
        if (open < 0)
            throw new InvalidDataException($"{name} starting at line {startLine} is missing an opening parenthesis.");

        int depth = 1;
        bool inString = false;
        bool escape = false;

        for (int i = open + 1; i < text.Length; i++)
        {
            char ch = text[i];

            if (inString)
            {
                if (escape)
                {
                    escape = false;
                }
                else if (ch == '\\')
                {
                    escape = true;
                }
                else if (ch == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (ch == '"')
            {
                inString = true;
            }
            else if (ch == '(')
            {
                depth++;
            }
            else if (ch == ')')
            {
                depth--;
                if (depth == 0)
                {
                    call = new HeaderCall(text[(open + 1)..i], startLine);
                    index = i + 1;
                    return true;
                }
            }
            else if (depth == 1 && IsIdentifierAt(text, i, name))
            {
                throw new InvalidDataException($"{name} starting at line {startLine} is missing a closing parenthesis before line {GetLineNumber(text, i)}.");
            }
        }

        throw new InvalidDataException($"{name} starting at line {startLine} is missing a closing parenthesis.");
    }

    private static string FormatImportError(int startLine, int? id, string message)
    {
        string location = id is int value
            ? $"DEFINE_MESSAGE 0x{value:x4} starting at line {startLine}"
            : $"DEFINE_MESSAGE starting at line {startLine}";
        return $"{location}: {message}";
    }

    private static bool IsIdentifierAt(string text, int index, string identifier)
    {
        if (!text.AsSpan(index).StartsWith(identifier, StringComparison.Ordinal))
        {
            return false;
        }

        int before = index - 1;
        int after = index + identifier.Length;
        return (before < 0 || !IsIdentifierChar(text[before]))
            && (after >= text.Length || !IsIdentifierChar(text[after]));
    }

    private static bool IsIdentifierChar(char ch) => char.IsLetterOrDigit(ch) || ch == '_';

    private static int GetLineNumber(string text, int index)
    {
        int line = 1;
        for (int i = 0; i < index && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    private readonly record struct HeaderCall(string Text, int StartLine);

    private static List<string> SplitTopLevel(string text, int expectedParts = int.MaxValue)
    {
        var parts = new List<string>();
        int start = 0;
        int depth = 0;
        bool inString = false;
        bool escape = false;

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];

            if (inString)
            {
                if (escape)
                    escape = false;
                else if (ch == '\\')
                    escape = true;
                else if (ch == '"')
                    inString = false;
                continue;
            }

            if (ch == '"')
                inString = true;
            else if (ch == '(')
                depth++;
            else if (ch == ')')
                depth--;
            else if (ch == ',' && depth == 0 && parts.Count < expectedParts - 1)
            {
                parts.Add(text[start..i].Trim());
                start = i + 1;
            }
        }

        parts.Add(text[start..].Trim());
        return parts;
    }

    private static string ParseMessageBody(string body)
    {
        return MessageTextSyntax.ToEditorText(ParseMessageBodyTokens(body));
    }

    private static List<MessageToken> ParseMessageBodyTokens(string body)
    {
        var tokens = new List<MessageToken>();
        if (body.Trim().Equals("None", StringComparison.OrdinalIgnoreCase))
            return tokens;

        int i = 0;

        while (i < body.Length)
        {
            char ch = body[i];

            if (char.IsWhiteSpace(ch))
            {
                i++;
                continue;
            }

            if (ch == '"')
            {
                AppendText(tokens, ParseCString(body, ref i));
                continue;
            }

            if (char.IsLetter(ch) || ch == '_')
            {
                string macro = ReadIdentifier(body, ref i);
                string argumentText = string.Empty;
                if (i < body.Length && body[i] == '(')
                    argumentText = ReadParenthesized(body, ref i);

                AppendMacro(tokens, macro, argumentText);
                continue;
            }

            i++;
        }

        return tokens;
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

    private static void AppendText(List<MessageToken> tokens, string text)
    {
        tokens.AddRange(MessageTextSyntax.FromEditorText(EncodingProfile.HeaderTextToEditorText(NormalizeInlineTags(text))));
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
            int value = MessageSfxMaps.Values.TryGetValue(argument, out int mapped)
                ? mapped
                : ParseArgumentWord(argumentText);
            tokens.Add(new SfxToken((ushort)value));
        }
        else if (macro.Equals("ITEM_ICON", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new IconToken((byte)ParseArgumentByte(argumentText)));
        }
        else if (macro.Equals("TEXT_SPEED", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new TextSpeedToken((byte)ParseArgumentByte(argumentText)));
        }
        else if (macro.Equals("BACKGROUND", StringComparison.OrdinalIgnoreCase))
        {
            var args = SplitTopLevel(argumentText);
            if (args.Count != 3)
                throw new InvalidDataException("BACKGROUND must have three arguments.");
            int value = ParseArgumentByte(args[0]) << 16
                | ParseArgumentByte(args[1]) << 8
                | ParseArgumentByte(args[2]);
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

    private static string ParseCString(string text, ref int index)
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
                    result.Append(ReadHexEscapedByteAsText(text, ref index));
                    break;
                default:
                    result.Append(escaped);
                    break;
            }
        }

        return result.ToString();
    }

    private static string ReadHexEscapedByteAsText(string text, ref int index)
    {
        int value = ReadHexByte(text, ref index);
        if (value < 0)
            return string.Empty;

        string decoded = MessageTextSyntax.ToEditorText(MessageCodec.DecodeMessageTokens([(byte)value, 0x02], 0, 2));
        return decoded.Length > 0 ? decoded : ((char)value).ToString();
    }

    private static string ReadIdentifier(string text, ref int index)
    {
        int start = index;
        while (index < text.Length && (char.IsLetterOrDigit(text[index]) || text[index] == '_'))
            index++;
        return text[start..index];
    }

    private static string ReadParenthesized(string text, ref int index)
    {
        if (text[index] != '(')
            throw new InvalidDataException("Expected argument list.");

        int open = ++index;
        int depth = 1;
        bool inString = false;
        bool escape = false;

        while (index < text.Length)
        {
            char ch = text[index];

            if (inString)
            {
                if (escape)
                    escape = false;
                else if (ch == '\\')
                    escape = true;
                else if (ch == '"')
                    inString = false;

                index++;
                continue;
            }

            if (ch == '"')
            {
                inString = true;
            }
            else if (ch == '(')
            {
                depth++;
            }
            else if (ch == ')')
            {
                depth--;
                if (depth == 0)
                {
                    string value = text[open..index];
                    index++;
                    return value.Trim();
                }
            }

            index++;
        }

        throw new InvalidDataException("Unclosed argument list.");
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
