using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers.MajorasMask;

public static partial class MmCHeaderImporter
{
    public static bool LooksLikeMajorasMask(string content)
    {
        if (!content.Contains("DEFINE_MESSAGE", StringComparison.Ordinal))
        {
            return false;
        }

        if (ContainsHeaderMacro(content))
        {
            return true;
        }

        return ContainsStaffCreditsMessages(content);
    }

    public static List<MessageEntry> Import(
        string content,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.MajorasMask;
        var entries = new List<MessageEntry>();
        int index = 0;

        while (CHeaderCallParser.TryFindCall(content, MessageMacroNames, ref index, out CHeaderCall call))
        {
            int? id = null;
            try
            {
                List<string> args = CHeaderCallParser.SplitTopLevel(call.Text);
                if (args.Count != 4)
                {
                    throw new InvalidDataException("MM DEFINE_MESSAGE must have four arguments.");
                }

                id = ParseInt(args[0]);
                int tableType = ParseNibble(args[1]);
                int tablePosition = ParseNibble(args[2]);
                string body = UnwrapMsgBody(args[3]);
                MajorasMaskMessageMetadata? metadata = TryParseHeader(body, tableType, tablePosition, out int bodyStart);
                string text = ParseMessageBody(body[bodyStart..], encodingProfile);

                int entryType = metadata?.Type ?? tableType;
                int entryPosition = metadata?.Position ?? tablePosition;
                int entryBank = metadata is null ? 0x07 : 0x08;
                entries.Add(new MessageEntry(id.Value, entryType, entryPosition, entryBank, 0)
                {
                    Text = text,
                    OriginalText = text,
                    CodecMetadata = metadata,
                    OriginalCodecMetadata = metadata,
                    TableEndMarkerId = 0xffff,
                    TableHasFinalEndMarker = true,
                });
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidDataException(FormatImportError(call.StartLine, id, ex.Message), ex);
            }
        }

        if (entries.Count == 0)
        {
            throw new InvalidDataException("No MM DEFINE_MESSAGE entries were found.");
        }

        return entries;
    }

    private static string FormatImportError(int startLine, int? id, string message)
    {
        string location = id is int value
            ? $"DEFINE_MESSAGE 0x{value:x4} starting at line {startLine}"
            : $"DEFINE_MESSAGE starting at line {startLine}";
        return $"{location}: {message}";
    }
}
