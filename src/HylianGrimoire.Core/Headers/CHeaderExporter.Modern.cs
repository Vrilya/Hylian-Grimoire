using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers;

public static partial class CHeaderExporter
{
    private const int CreditsTextBoxType = 0x0b;

    public static string ExportModernLanguages(
        IReadOnlyList<MessageEntry>? jpnEntries,
        IReadOnlyList<MessageEntry>? nesEntries,
        IReadOnlyList<MessageEntry>? gerEntries,
        IReadOnlyList<MessageEntry>? fraEntries,
        MessageEncodingProfile? encodingProfile = null,
        MessageEncodingProfile? headerEncodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.Default;
        headerEncodingProfile ??= MessageEncodingProfile.Original;
        Dictionary<int, MessageEntry> jpn = ToEntryMap(jpnEntries);
        Dictionary<int, MessageEntry> nes = ToEntryMap(nesEntries);
        Dictionary<int, MessageEntry> ger = ToEntryMap(gerEntries);
        Dictionary<int, MessageEntry> fra = ToEntryMap(fraEntries);
        var parts = new List<string>();

        foreach (int id in EnumerateOrderedIds(nesEntries, gerEntries, fraEntries, jpnEntries))
        {
            if (id == 0xfffc)
            {
                continue;
            }

            MessageEntry entry = GetFirstEntry(id, jpn, nes, ger, fra);
            string boxType = BoxTypeStr.TryGetValue(entry.Type, out var bt) ? bt : $"TEXTBOX_TYPE_UNK_{entry.Type:X}";
            string boxPos = BoxPosStr.TryGetValue(entry.Position, out var bp) ? bp : $"TEXTBOX_POS_UNK_{entry.Position:X}";

            bool hasJpn = jpn.TryGetValue(id, out MessageEntry? jpnEntry);
            bool hasNes = nes.TryGetValue(id, out MessageEntry? nesEntry);
            bool hasGer = ger.TryGetValue(id, out MessageEntry? gerEntry);
            bool hasFra = fra.TryGetValue(id, out MessageEntry? fraEntry);

            var slots = new ModernMessageSlots(
                Jpn: hasJpn ? DecodeModernSlot(jpnEntry!, encodingProfile, headerEncodingProfile) : null,
                Nes: hasNes ? DecodeModernSlot(nesEntry!, encodingProfile, headerEncodingProfile) : null,
                Ger: hasGer ? DecodeModernSlot(gerEntry!, encodingProfile, headerEncodingProfile) : null,
                Fra: hasFra ? DecodeModernSlot(fraEntry!, encodingProfile, headerEncodingProfile) : null);

            if (hasJpn
                && hasNes
                && !hasGer
                && !hasFra
                && (jpnEntry!.Type != nesEntry!.Type || jpnEntry.Position != nesEntry.Position))
            {
                string jpnBoxType = BoxTypeStr.TryGetValue(jpnEntry.Type, out var jbt) ? jbt : $"TEXTBOX_TYPE_UNK_{jpnEntry.Type:X}";
                string jpnBoxPos = BoxPosStr.TryGetValue(jpnEntry.Position, out var jbp) ? jbp : $"TEXTBOX_POS_UNK_{jpnEntry.Position:X}";
                string nesBoxType = BoxTypeStr.TryGetValue(nesEntry.Type, out var nbt) ? nbt : $"TEXTBOX_TYPE_UNK_{nesEntry.Type:X}";
                string nesBoxPos = BoxPosStr.TryGetValue(nesEntry.Position, out var nbp) ? nbp : $"TEXTBOX_POS_UNK_{nesEntry.Position:X}";

                string jpnMessage = ExportModernEntry(
                    jpnEntry,
                    jpnBoxType,
                    jpnBoxPos,
                    slots with { Nes = null },
                    fullLanguageSet: true,
                    macroOverride: "DEFINE_MESSAGE_JPN").TrimEnd('\r', '\n');
                string nesMessage = ExportModernEntry(
                    nesEntry,
                    nesBoxType,
                    nesBoxPos,
                    slots with { Jpn = null },
                    fullLanguageSet: true,
                    macroOverride: "DEFINE_MESSAGE_NES");
                parts.Add($"{jpnMessage}\n{nesMessage}");
            }
            else
            {
                parts.Add(ExportModernEntry(entry, boxType, boxPos, slots, fullLanguageSet: true));
            }
        }

        return NormalizeHeaderLineEndings(string.Join("\n", parts));
    }

    private static string ExportModernEntry(
        MessageEntry entry,
        string boxType,
        string boxPos,
        ModernMessageSlots slots,
        bool fullLanguageSet,
        string? macroOverride = null)
    {
        if (entry.Type == CreditsTextBoxType)
        {
            string staffBody = FormatModernMsg(slots.Nes ?? slots.Ger ?? slots.Fra ?? slots.Jpn);
            return $"DEFINE_MESSAGE(0x{entry.Id:X4}, {boxType}, {boxPos},\n{staffBody}\n)\n";
        }

        string macro = macroOverride ?? (entry.Id == 0xfffc
            ? "DEFINE_MESSAGE_FFFC"
            : fullLanguageSet ? ChooseModernLanguageMacro(slots) : "DEFINE_MESSAGE_NES");
        return $"""
{macro}(0x{entry.Id:X4}, {boxType}, {boxPos},
{FormatModernMsg(slots.Jpn)}
,
{FormatModernMsg(slots.Nes)}
,
{FormatModernMsg(slots.Ger)}
,
{FormatModernMsg(slots.Fra)}
)

""";
    }

    private static string ChooseModernLanguageMacro(ModernMessageSlots slots)
    {
        bool hasJpn = slots.Jpn is not null;
        bool hasNes = slots.Nes is not null;
        bool hasGer = slots.Ger is not null;
        bool hasFra = slots.Fra is not null;

        if (hasJpn && !hasNes && !hasGer && !hasFra)
        {
            return "DEFINE_MESSAGE_JPN";
        }

        if (!hasJpn && hasNes && !hasGer && !hasFra)
        {
            return "DEFINE_MESSAGE_NES";
        }

        return "DEFINE_MESSAGE";
    }

    private static string ExportModernSelectedEntry(MessageEntry entry, string boxType, string boxPos, string decoded)
    {
        string macro = entry.Id == 0xfffc ? "DEFINE_MESSAGE_FFFC" : "DEFINE_MESSAGE";
        if (entry.Type == CreditsTextBoxType)
        {
            return $"{macro}(0x{entry.Id:X4}, {boxType}, {boxPos},\n{FormatModernMsg(decoded)}\n)\n";
        }

        return $"{macro}(0x{entry.Id:X4}, {boxType}, {boxPos},\n{decoded}\n)\n";
    }

    private static string FormatModernMsg(string? decoded)
        => decoded is null
            ? "MSG(/* MISSING */)"
            : decoded.Length == 0 ? "MSG()" : $"MSG(\n{decoded}\n)";

    private static string DecodeModernSlot(
        MessageEntry entry,
        MessageEncodingProfile encodingProfile,
        MessageEncodingProfile headerEncodingProfile)
        => entry.Bank == 0x08
            ? CHeaderJapaneseExporter.DecodeRawEntry(entry)
            : DecodeEntry(
                entry,
                encodingProfile,
                headerEncodingProfile,
                modern: true,
                staffCredits: entry.Type == CreditsTextBoxType);

    private static Dictionary<int, MessageEntry> ToEntryMap(IReadOnlyList<MessageEntry>? entries)
        => entries?.ToDictionary(entry => entry.Id) ?? [];

    private static IEnumerable<int> EnumerateOrderedIds(params IReadOnlyList<MessageEntry>?[] entrySets)
    {
        var seen = new SortedSet<int>();
        foreach (IReadOnlyList<MessageEntry>? entries in entrySets)
        {
            if (entries is null)
            {
                continue;
            }

            foreach (MessageEntry entry in entries)
            {
                seen.Add(entry.Id);
            }
        }

        foreach (int id in seen)
        {
            yield return id;
        }
    }

    private static MessageEntry GetFirstEntry(
        int id,
        Dictionary<int, MessageEntry> jpn,
        Dictionary<int, MessageEntry> nes,
        Dictionary<int, MessageEntry> ger,
        Dictionary<int, MessageEntry> fra)
    {
        if (nes.TryGetValue(id, out MessageEntry? nesEntry))
        {
            return nesEntry;
        }

        if (ger.TryGetValue(id, out MessageEntry? gerEntry))
        {
            return gerEntry;
        }

        if (fra.TryGetValue(id, out MessageEntry? fraEntry))
        {
            return fraEntry;
        }

        return jpn[id];
    }

    private readonly record struct ModernMessageSlots(string? Jpn, string? Nes, string? Ger, string? Fra);
}
