using System.Globalization;
using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers.MajorasMask;

public static class MmCHeaderImporter
{
    private const string MessageMacro = "DEFINE_MESSAGE";
    private static readonly string[] MessageMacroNames = [MessageMacro];

    public static bool LooksLikeMajorasMask(string content)
    {
        if (!content.Contains("DEFINE_MESSAGE", StringComparison.Ordinal))
        {
            return false;
        }

        if (content.Contains("HEADER(", StringComparison.Ordinal))
        {
            return true;
        }

        return ContainsStaffCreditsMessages(content);
    }

    public static List<MessageEntry> Import(
        string content,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.MajorasMask;
        var entries = new List<MessageEntry>();
        int index = 0;

        while (CHeaderCallParser.TryFindCall(content, MessageMacroNames, ref index, out CHeaderCall call))
        {
            int? id = null;
            try
            {
                List<string> args = CHeaderCallParser.SplitTopLevel(call.Text);
                if (args.Count != 4)
                {
                    throw new InvalidDataException("MM DEFINE_MESSAGE must have four arguments.");
                }

                id = ParseInt(args[0]);
                int tableType = ParseNibble(args[1]);
                int tablePosition = ParseNibble(args[2]);
                string body = UnwrapMsgBody(args[3]);
                MajorasMaskMessageMetadata? metadata = TryParseHeader(body, tableType, tablePosition, out int bodyStart);
                string text = ParseMessageBody(body[bodyStart..], encodingProfile);

                int entryType = metadata?.Type ?? tableType;
                int entryPosition = metadata?.Position ?? tablePosition;
                int entryBank = metadata is null ? 0x07 : 0x08;
                entries.Add(new MessageEntry(id.Value, entryType, entryPosition, entryBank, 0)
                {
                    Text = text,
                    OriginalText = text,
                    CodecMetadata = metadata,
                    OriginalCodecMetadata = metadata,
                    TableEndMarkerId = 0xffff,
                    TableHasFinalEndMarker = true,
                });
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidDataException(FormatImportError(call.StartLine, id, ex.Message), ex);
            }
        }

        if (entries.Count == 0)
        {
            throw new InvalidDataException("No MM DEFINE_MESSAGE entries were found.");
        }

        return entries;
    }

    private static bool ContainsStaffCreditsMessages(string content)
    {
        int index = 0;
        while (CHeaderCallParser.TryFindCall(content, MessageMacroNames, ref index, out CHeaderCall call))
        {
            try
            {
                List<string> args = CHeaderCallParser.SplitTopLevel(call.Text);
                if (args.Count == 4)
                {
                    int id = ParseInt(args[0]);
                    if (id is >= 0x4e20 and <= 0x4e4c)
                    {
                        return true;
                    }
                }
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

        return false;
    }

    private static MajorasMaskMessageMetadata? TryParseHeader(
        string body,
        int tableType,
        int tablePosition,
        out int bodyStart)
    {
        int index = 0;
        SkipIgnorable(body, ref index);
        string macro = ReadIdentifier(body, ref index);
        if (!macro.Equals("HEADER", StringComparison.Ordinal))
        {
            bodyStart = 0;
            return null;
        }

        string headerArgs = CHeaderCallParser.ReadParenthesized(body, ref index);
        bodyStart = index;

        List<string> args = CHeaderCallParser.SplitTopLevel(headerArgs);
        if (args.Count != 6)
        {
            throw new InvalidDataException("MM HEADER must have six arguments.");
        }

        return new MajorasMaskMessageMetadata(
            TableTypePosition: (byte)(((tableType & 0x0f) << 4) | (tablePosition & 0x0f)),
            TextBoxProperties: ParseWord(args[0]),
            IconId: ParseByte(args[1]),
            NextTextId: ParseWord(args[2]),
            FirstChoicePrice: ParseWord(args[3]),
            SecondChoicePrice: ParseWord(args[4]),
            Unknown: ParseWord(args[5]));
    }

    private static string ParseMessageBody(string body, MessageEncodingProfile encodingProfile)
    {
        var text = new StringBuilder();
        int index = 0;

        while (index < body.Length)
        {
            if (SkipIgnorable(body, ref index))
            {
                continue;
            }

            if (index >= body.Length)
            {
                break;
            }

            char ch = body[index];
            if (ch == '"')
            {
                text.Append(ParseCString(body, ref index, encodingProfile));
            }
            else if (char.IsLetter(ch) || ch == '_')
            {
                string macro = ReadIdentifier(body, ref index);
                string argumentText = string.Empty;
                if (index < body.Length && body[index] == '(')
                {
                    argumentText = CHeaderCallParser.ReadParenthesized(body, ref index);
                }

                AppendMacro(text, macro, argumentText);
            }
            else if (char.IsDigit(ch))
            {
                text.Append(DecodeRawByte(ParseNumericToken(body, ref index), encodingProfile));
            }
            else
            {
                index++;
            }
        }

        return text.ToString();
    }

    private static void AppendMacro(StringBuilder text, string macro, string argumentText)
    {
        if (macro.Equals("END", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (TryAppendColor(text, macro))
        {
            return;
        }

        if (macro.Equals("NEWLINE", StringComparison.OrdinalIgnoreCase))
        {
            text.Append('\n');
        }
        else if (macro.Equals("SHIFT", StringComparison.OrdinalIgnoreCase))
        {
            text.Append($"[shift:{ParseByte(argumentText):x2}]");
        }
        else if (macro.Equals("BOX_BREAK_DELAYED", StringComparison.OrdinalIgnoreCase))
        {
            AppendWordTag(text, "breakdelay", argumentText);
        }
        else if (macro.Equals("FADE", StringComparison.OrdinalIgnoreCase))
        {
            AppendWordTag(text, "fade", argumentText);
        }
        else if (macro.Equals("FADE2", StringComparison.OrdinalIgnoreCase))
        {
            AppendWordTag(text, "endfade", argumentText);
        }
        else if (macro.Equals("FADE_SKIPPABLE", StringComparison.OrdinalIgnoreCase))
        {
            AppendWordTag(text, "fadeskippable", argumentText);
        }
        else if (macro.Equals("SFX", StringComparison.OrdinalIgnoreCase))
        {
            ushort value = ParseSfx(argumentText);
            text.Append($"[sfx:{value:x4}]");
        }
        else if (macro.Equals("DELAY", StringComparison.OrdinalIgnoreCase))
        {
            AppendWordTag(text, "delay", argumentText);
        }
        else if (MmMessageTokenMaps.NoArgumentBytes.TryGetValue(macro, out byte noArg)
            && MmMessageTokenMaps.NoArgumentTags.TryGetValue(noArg, out string? tag))
        {
            text.Append($"[{tag}]");
        }
        else
        {
            throw new InvalidDataException($"Unknown MM header macro: {macro}.");
        }
    }

    private static bool TryAppendColor(StringBuilder text, string macro)
    {
        if (!macro.StartsWith("COLOR_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string colorName = macro["COLOR_".Length..].Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        if (colorName == "lightblue")
        {
            colorName = "lightblue";
        }

        if (!MmMessageTokenMaps.ColorBytes.ContainsKey(colorName))
        {
            throw new InvalidDataException($"Unknown MM color macro: {macro}.");
        }

        text.Append($"[color:{colorName}]");
        return true;
    }

    private static void AppendWordTag(StringBuilder text, string tag, string argumentText)
        => text.Append($"[{tag}:{ParseWord(argumentText):x4}]");

    private static string ParseCString(string source, ref int index, MessageEncodingProfile encodingProfile)
    {
        if (source[index] != '"')
        {
            throw new InvalidDataException("Expected C string.");
        }

        index++;
        var result = new StringBuilder();

        while (index < source.Length)
        {
            char ch = source[index++];
            if (ch == '"')
            {
                break;
            }

            if (ch != '\\' || index >= source.Length)
            {
                result.Append(ch);
                continue;
            }

            char escaped = source[index++];
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
                    result.Append(DecodeRawByte(ReadHexByte(source, ref index), encodingProfile));
                    break;
                default:
                    result.Append(escaped);
                    break;
            }
        }

        return NormalizeInlineTags(result.ToString());
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
            .Replace("[Control-Pad]", "[Control-Pad]", StringComparison.Ordinal)
            .Replace("▼", "[Z-target]", StringComparison.Ordinal)
            .Replace("<TRIANGLE>", "[Z-target]", StringComparison.OrdinalIgnoreCase);
    }

    private static string DecodeRawByte(int value, MessageEncodingProfile encodingProfile)
        => MmMessageTextCodec.Decode([(byte)value, 0xbf], 0, 2, encodingProfile);

    private static string UnwrapMsgBody(string body)
    {
        int index = 0;
        SkipIgnorable(body, ref index);
        int identifierStart = index;
        string identifier = ReadIdentifier(body, ref index);
        if (!identifier.Equals("MSG", StringComparison.OrdinalIgnoreCase))
        {
            return body[identifierStart..];
        }

        SkipIgnorable(body, ref index);
        if (index >= body.Length || body[index] != '(')
        {
            return body;
        }

        return CHeaderCallParser.ReadParenthesized(body, ref index);
    }

    private static bool SkipIgnorable(string text, ref int index)
    {
        bool skipped = false;
        while (index < text.Length)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                index++;
                skipped = true;
                continue;
            }

            if (index + 1 < text.Length && text[index] == '/' && text[index + 1] == '/')
            {
                index += 2;
                while (index < text.Length && text[index] != '\n')
                {
                    index++;
                }

                skipped = true;
                continue;
            }

            if (index + 1 < text.Length && text[index] == '/' && text[index + 1] == '*')
            {
                index += 2;
                while (index + 1 < text.Length && (text[index] != '*' || text[index + 1] != '/'))
                {
                    index++;
                }

                index = Math.Min(index + 2, text.Length);
                skipped = true;
                continue;
            }

            break;
        }

        return skipped;
    }

    private static string ReadIdentifier(string text, ref int index)
    {
        int start = index;
        while (index < text.Length && (char.IsLetterOrDigit(text[index]) || text[index] == '_'))
        {
            index++;
        }

        return text[start..index];
    }

    private static int ParseNumericToken(string text, ref int index)
    {
        int start = index;
        while (index < text.Length && (IsHex(text[index]) || text[index] is 'x' or 'X'))
        {
            index++;
        }

        int value = ParseInt(text[start..index]);
        if (value is < 0 or > 0xff)
        {
            throw new InvalidDataException($"Expected byte-sized raw value, got 0x{value:X}.");
        }

        return value;
    }

    private static byte ParseByte(string value)
    {
        int result = ParseInt(value.Trim());
        if (result is < 0 or > 0xff)
        {
            throw new InvalidDataException($"Expected byte-sized argument, got 0x{result:X}.");
        }

        return (byte)result;
    }

    private static ushort ParseWord(string value)
    {
        int result = ParseInt(value.Trim());
        if (result == -1)
        {
            return 0xffff;
        }

        if (result is < 0 or > 0xffff)
        {
            throw new InvalidDataException($"Expected 16-bit argument, got 0x{result:X}.");
        }

        return (ushort)result;
    }

    private static ushort ParseSfx(string value)
    {
        string argument = value.Trim();
        if (MmMessageSfxMaps.Values.TryGetValue(argument, out ushort named))
        {
            return named;
        }

        return ParseWord(argument);
    }

    private static int ParseNibble(string value)
    {
        int result = ParseInt(value.Trim());
        if (result is < 0 or > 0x0f)
        {
            throw new InvalidDataException($"Expected 4-bit argument, got 0x{result:X}.");
        }

        return result;
    }

    private static int ParseInt(string value)
    {
        value = value.Trim();
        if (value.Length == 0)
        {
            throw new InvalidDataException("Invalid empty integer value in MM header.");
        }

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            string hex = value[2..];
            if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hexResult))
            {
                return hexResult;
            }

            throw new InvalidDataException($"Invalid hexadecimal integer value in MM header: '{value}'.");
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }

        throw new InvalidDataException($"Invalid integer value in MM header: '{value}'.");
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

        if (count == 0)
        {
            throw new InvalidDataException("Invalid hexadecimal escape in MM header string.");
        }

        return value;
    }

    private static string FormatImportError(int startLine, int? id, string message)
    {
        string location = id is int value
            ? $"DEFINE_MESSAGE 0x{value:x4} starting at line {startLine}"
            : $"DEFINE_MESSAGE starting at line {startLine}";
        return $"{location}: {message}";
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
