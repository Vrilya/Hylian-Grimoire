namespace HylianGrimoire.Headers;

internal readonly record struct CHeaderCall(string Name, string Text, int StartLine);

internal static class CHeaderCallParser
{
    public static bool TryFindCall(
        string text,
        IReadOnlyList<string> macroNames,
        ref int index,
        out CHeaderCall call)
    {
        call = default;

        int bestStart = -1;
        string? bestName = null;
        foreach (string name in macroNames)
        {
            int start = FindIdentifier(text, name, index);
            if (start >= 0 && (bestStart < 0 || start < bestStart))
            {
                bestStart = start;
                bestName = name;
            }
        }

        return bestName is not null && TryReadCallAt(text, bestName, bestStart, ref index, out call);
    }

    public static List<string> SplitTopLevel(string text, int expectedParts = int.MaxValue)
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
            }
            else if (ch == ',' && depth == 0 && parts.Count < expectedParts - 1)
            {
                parts.Add(text[start..i].Trim());
                start = i + 1;
            }
        }

        parts.Add(text[start..].Trim());
        return parts;
    }

    public static string ReadParenthesized(string text, ref int index)
    {
        if (index >= text.Length || text[index] != '(')
        {
            throw new InvalidDataException("Expected argument list.");
        }

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

    private static int FindIdentifier(string text, string name, int startIndex)
    {
        int search = startIndex;
        while (true)
        {
            int found = text.IndexOf(name, search, StringComparison.Ordinal);
            if (found < 0)
            {
                return -1;
            }

            if (IsIdentifierAt(text, found, name))
            {
                return found;
            }

            search = found + name.Length;
        }
    }

    private static bool TryReadCallAt(
        string text,
        string name,
        int start,
        ref int index,
        out CHeaderCall call)
    {
        call = default;

        int startLine = GetLineNumber(text, start);
        int open = text.IndexOf('(', start + name.Length);
        if (open < 0)
        {
            throw new InvalidDataException($"{name} starting at line {startLine} is missing an opening parenthesis.");
        }

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
                    call = new CHeaderCall(name, text[(open + 1)..i], startLine);
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
}
