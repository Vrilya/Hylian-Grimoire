namespace HylianGrimoire.Headers;

public static partial class CHeaderImporter
{
    private static string? SelectSingleMessageBody(string body, CHeaderMessageSlot preferredSlot)
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
            || ContainsOnlyComments(unwrapped);
    }

    private static bool ContainsOnlyComments(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                index++;
                continue;
            }

            if (TrySkipComment(text, ref index))
            {
                continue;
            }

            return false;
        }

        return true;
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
}
