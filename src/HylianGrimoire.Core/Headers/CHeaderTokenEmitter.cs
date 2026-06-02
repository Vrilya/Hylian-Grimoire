using System.Text;

namespace HylianGrimoire.Headers;

internal static class CHeaderTokenEmitter
{
    public static string Emit(IReadOnlyList<(string TokType, string Data)> tokens, bool modern, bool otrMod = false)
    {
        if (tokens.Count == 0)
        {
            return "\"\"";
        }

        var sb = new StringBuilder();
        bool qState = false;
        bool sState = false;
        bool lineStart = true;
        bool choiceIndent = false;

        void MaybeEnterQ()
        {
            if (!qState)
            {
                AppendIndentIfNeeded();
                sb.Append('"');
                qState = true;
                lineStart = false;
            }
        }

        void MaybeExitQ(bool space = false)
        {
            if (qState)
            {
                sb.Append('"');
                if (space)
                {
                    sb.Append(' ');
                }

                qState = false;
            }
            else if (otrMod && space)
            {
                sb.Append(' ');
            }
        }

        void AppendIndentIfNeeded()
        {
            if (modern && choiceIndent && lineStart)
            {
                sb.Append("    ");
                lineStart = false;
            }
        }

        void AppendLineBreak()
        {
            sb.Append('\n');
            lineStart = true;
        }

        foreach (var (tokType, tokDat) in tokens)
        {
            if (tokType is "BOX_BREAK" or "BOX_BREAK_DELAYED")
            {
                MaybeExitQ();
                sState = false;
                choiceIndent = false;
                AppendLineBreak();
                sb.Append(tokDat);
                lineStart = false;
                AppendLineBreak();
                if (modern)
                {
                    AppendLineBreak();
                }

                continue;
            }

            if (sState)
            {
                sb.Append(' ');
                sState = false;
            }

            if (tokType == "NEWLINE")
            {
                MaybeEnterQ();
                sb.Append("\\n\"\n");
                qState = false;
                lineStart = true;
            }
            else if (tokType == "TEXT")
            {
                MaybeEnterQ();
                sb.Append(tokDat);
            }
            else
            {
                MaybeExitQ(space: true);
                AppendIndentIfNeeded();
                sb.Append(tokDat);
                lineStart = false;
                if (tokType is "TWO_CHOICE" or "THREE_CHOICE")
                {
                    choiceIndent = modern;
                    AppendLineBreak();
                }
                else
                {
                    sState = true;
                }
            }
        }

        MaybeExitQ();

        string result = sb.ToString();
        if (result.Length > 0 && result[^1] == '\n')
        {
            result = result[..^1];
        }

        return result;
    }
}
