namespace HylianGrimoire.Headers.MajorasMask;

public static partial class MmCHeaderImporter
{
    private static readonly string[] HeaderMacroNames = ["HEADER"];

    private static bool ContainsHeaderMacro(string content)
    {
        int index = 0;
        while (CHeaderCallParser.TryFindCall(content, MessageMacroNames, ref index, out CHeaderCall call))
        {
            try
            {
                List<string> args = CHeaderCallParser.SplitTopLevel(call.Text);
                if (args.Count != 4)
                {
                    continue;
                }

                string body = UnwrapMsgBody(args[3]);
                int bodyIndex = 0;
                if (CHeaderCallParser.TryFindCall(body, HeaderMacroNames, ref bodyIndex, out _))
                {
                    return true;
                }
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

        return false;
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
}
