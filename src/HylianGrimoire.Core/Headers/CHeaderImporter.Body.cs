using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers;

public static partial class CHeaderImporter
{
    private static string ParseMessageBody(string body, MessageEncodingProfile encodingProfile)
    {
        return MessageTextSyntax.ToEditorText(ParseMessageBodyTokens(body, encodingProfile));
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
            .Replace("▼", "[Triangle]", StringComparison.Ordinal)
            .Replace("Ã¢â€“Â¼", "[Triangle]", StringComparison.Ordinal)
            .Replace("â–¼", "[Triangle]", StringComparison.Ordinal)
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
}
