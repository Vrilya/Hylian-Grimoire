using HylianGrimoire.Models;

namespace HylianGrimoire.Headers.MajorasMask;

public static partial class MmCHeaderImporter
{
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
}
