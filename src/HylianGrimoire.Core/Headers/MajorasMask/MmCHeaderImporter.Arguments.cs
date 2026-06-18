using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;

namespace HylianGrimoire.Headers.MajorasMask;

public static partial class MmCHeaderImporter
{
    private static string ParseCString(string source, ref int index, MessageEncodingProfile encodingProfile)
        => CHeaderLiteralParser.ParseCString(
            source,
            ref index,
            value => DecodeRawByte(value, encodingProfile),
            invalidHexEscapeMessage: "Invalid hexadecimal escape in MM header string.",
            normalize: NormalizeInlineTags);

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
            .Replace("▼", "[Z-target]", StringComparison.Ordinal)
            .Replace("â–¼", "[Z-target]", StringComparison.Ordinal)
            .Replace("<TRIANGLE>", "[Z-target]", StringComparison.OrdinalIgnoreCase);
    }

    private static string DecodeRawByte(int value, MessageEncodingProfile encodingProfile)
        => MmMessageTextCodec.Decode([(byte)value, 0xbf], 0, 2, encodingProfile);

    private static string ReadIdentifier(string text, ref int index)
        => CHeaderLiteralParser.ReadIdentifier(text, ref index);

    private static int ParseNumericToken(string text, ref int index)
        => CHeaderLiteralParser.ParseNumericToken(text, ref index, "MM header");

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
        => CHeaderLiteralParser.ParseInt(value, "MM header");
}
