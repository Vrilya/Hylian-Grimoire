using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers;

/// <summary>
/// Exports the current message entries to an OoT C header format.
/// </summary>
public static partial class CHeaderExporter
{
    /// <summary>
    /// Produces an OoT C header file from the given message entries.
    /// </summary>
    public static string Export(
        List<MessageEntry> entries,
        CHeaderExportFormat format = CHeaderExportFormat.Legacy,
        MessageEncodingProfile? encodingProfile = null,
        MessageEncodingProfile? headerEncodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        headerEncodingProfile ??= MessageEncodingProfile.Original;
        var parts = new List<string>(entries.Count);

        foreach (var entry in entries)
        {
            string boxType = BoxTypeStr.TryGetValue(entry.Type, out var bt) ? bt : $"TEXTBOX_TYPE_UNK_{entry.Type:X}";
            string boxPos = BoxPosStr.TryGetValue(entry.Position, out var bp) ? bp : $"TEXTBOX_POS_UNK_{entry.Position:X}";
            bool modern = format == CHeaderExportFormat.Modern;
            bool otrMod = format == CHeaderExportFormat.OTRMod;
            string decoded = DecodeEntry(
                entry,
                encodingProfile,
                headerEncodingProfile,
                modern,
                otrMod,
                staffCredits: modern && entry.Type == CreditsTextBoxType);

            parts.Add(format == CHeaderExportFormat.Modern
                ? ExportModernSelectedEntry(entry, boxType, boxPos, decoded)
                : $"DEFINE_MESSAGE(0x{entry.Id:X4}, {boxType}, {boxPos},\n{decoded}\n)\n");
        }

        return NormalizeHeaderLineEndings(string.Join("\n", parts));
    }
}
