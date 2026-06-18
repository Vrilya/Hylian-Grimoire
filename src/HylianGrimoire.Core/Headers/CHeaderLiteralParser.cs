using System.Globalization;
using System.Text;

namespace HylianGrimoire.Headers;

internal static class CHeaderLiteralParser
{
    public static string ParseCString(
        string text,
        ref int index,
        Func<int, string> decodeHexEscapedByte,
        string? invalidHexEscapeMessage = null,
        Func<string, string>? normalize = null)
    {
        if (text[index] != '"')
        {
            throw new InvalidDataException("Expected C string.");
        }

        index++;
        var result = new StringBuilder();

        while (index < text.Length)
        {
            char ch = text[index++];
            if (ch == '"')
            {
                break;
            }

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
                    int value = ReadHexByte(text, ref index);
                    if (value < 0 && invalidHexEscapeMessage is not null)
                    {
                        throw new InvalidDataException(invalidHexEscapeMessage);
                    }

                    result.Append(decodeHexEscapedByte(value));
                    break;
                default:
                    result.Append(escaped);
                    break;
            }
        }

        string parsed = result.ToString();
        return normalize is null ? parsed : normalize(parsed);
    }

    public static string ReadIdentifier(string text, ref int index)
    {
        int start = index;
        while (index < text.Length && (char.IsLetterOrDigit(text[index]) || text[index] == '_'))
        {
            index++;
        }

        return text[start..index];
    }

    public static int ParseNumericToken(string text, ref int index, string integerContext)
    {
        int start = index;
        while (index < text.Length && (IsHex(text[index]) || text[index] is 'x' or 'X'))
        {
            index++;
        }

        int value = ParseInt(text[start..index], integerContext);
        if (value is < 0 or > 0xff)
        {
            throw new InvalidDataException($"Expected byte-sized raw value, got 0x{value:X}.");
        }

        return value;
    }

    public static int ParseInt(string value, string context)
    {
        value = value.Trim();
        if (value.Length == 0)
        {
            throw new InvalidDataException($"Invalid empty integer value in {context}.");
        }

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            string hex = value[2..];
            if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hexResult))
            {
                return hexResult;
            }

            throw new InvalidDataException($"Invalid hexadecimal integer value in {context}: '{value}'.");
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }

        throw new InvalidDataException($"Invalid integer value in {context}: '{value}'.");
    }

    public static int ReadHexByte(string text, ref int index)
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
