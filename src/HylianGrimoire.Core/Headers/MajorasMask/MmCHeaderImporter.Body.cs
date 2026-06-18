using System.Text;
using HylianGrimoire.Codecs;

namespace HylianGrimoire.Headers.MajorasMask;

public static partial class MmCHeaderImporter
{
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
}
