using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers;

public static partial class CHeaderImporter
{
    public static List<MessageEntry> Import(
        string content,
        CHeaderMessageSlot preferredSlot = CHeaderMessageSlot.Nes,
        bool allowWesternFallback = true,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        var entries = new List<MessageEntry>();
        int index = 0;

        while (CHeaderCallParser.TryFindCall(content, MessageMacroNames, ref index, out CHeaderCall call))
        {
            int? id = null;
            try
            {
                var args = CHeaderCallParser.SplitTopLevel(call.Text);
                if (args.Count is not 4 and not 7)
                    throw new InvalidDataException($"{call.Name} must have four single-message arguments or seven multi-language arguments.");

                id = ParseInt(args[0]);
                int type = ParseBoxType(args[1]);
                int position = ParseBoxPosition(args[2]);
                string? body = args.Count == 4
                    ? SelectSingleMessageBody(args[3], preferredSlot)
                    : SelectModernMessageBody(call.Name, args, preferredSlot, allowWesternFallback);
                if (body is null)
                {
                    continue;
                }

                string text = ParseMessageBody(UnwrapMsgBody(body), encodingProfile);

                entries.Add(new MessageEntry(id.Value, type, position, 0x07, 0)
                {
                    Text = text,
                });
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidDataException(FormatImportError(call.StartLine, id, ex.Message), ex);
            }
        }

        if (entries.Count == 0)
            throw new HeaderMessageEntriesNotFoundException("No DEFINE_MESSAGE entries were found.");

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
