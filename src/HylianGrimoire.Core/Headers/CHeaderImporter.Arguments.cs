using System.Globalization;
using HylianGrimoire.Codecs;

namespace HylianGrimoire.Headers;

public static partial class CHeaderImporter
{
    private static string ParseCString(string text, ref int index, MessageEncodingProfile encodingProfile)
        => CHeaderLiteralParser.ParseCString(
            text,
            ref index,
            value => ReadHexEscapedByteAsText(value, encodingProfile),
            invalidHexEscapeMessage: "Invalid hexadecimal escape in C header string.");

    private static string ReadHexEscapedByteAsText(
        int value,
        MessageEncodingProfile encodingProfile)
    {
        string decoded = MessageTextSyntax.ToEditorText(MessageCodec.DecodeMessageTokens([(byte)value, 0x02], 0, 2, encodingProfile));
        return decoded.Length > 0 ? decoded : ((char)value).ToString();
    }

    private static string ReadIdentifier(string text, ref int index)
        => CHeaderLiteralParser.ReadIdentifier(text, ref index);

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
        => CHeaderLiteralParser.ParseNumericToken(text, ref index, "C header");

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
                        if (byteValue < 0)
                        {
                            throw new InvalidDataException("Invalid hexadecimal escape in C header argument.");
                        }

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
        => CHeaderLiteralParser.ReadHexByte(text, ref index);

    private static int ParseInt(string value)
        => CHeaderLiteralParser.ParseInt(value, "C header");

    private static int ParseHexSuffix(string value, string prefix)
    {
        string suffix = value[prefix.Length..].Trim();
        if (suffix.Length > 0 && int.TryParse(suffix, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int result))
            return result;

        throw new InvalidDataException($"Invalid hexadecimal suffix in C header: '{value}'.");
    }
}
