using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers;

/// <summary>
/// Exports the current message entries to an OoT C header format.
/// </summary>
public static class CHeaderExporter
{
    // --------------------------------------------------------
    // Lookup tables
    // --------------------------------------------------------

    private static readonly IReadOnlyDictionary<int, string> BoxTypeStr = MessageTokenMaps.HeaderBoxTypes;
    private static readonly IReadOnlyDictionary<int, string> BoxPosStr = MessageTokenMaps.HeaderBoxPositions;
    private static readonly IReadOnlyDictionary<int, string> ColorStr = MessageTokenMaps.HeaderColors;
    private static readonly IReadOnlyDictionary<int, string> HighscoreStr = MessageTokenMaps.HeaderHighscores;
    private static readonly IReadOnlyDictionary<int, string> ModernHighscoreStr = MessageTokenMaps.ModernHeaderHighscores;
    private static readonly IReadOnlyDictionary<int, string> ItemStr = MessageTokenMaps.HeaderItems;
    private static readonly IReadOnlyDictionary<int, string> SfxStr = MessageSfxMaps.HeaderNames;
    private static readonly IReadOnlyDictionary<int, string> BackgroundStr = MessageTokenMaps.HeaderBackgrounds;
    private static readonly IReadOnlyDictionary<int, string> BackgroundForegroundStr = MessageTokenMaps.HeaderBackgroundForegroundColors;
    private static readonly IReadOnlyDictionary<int, string> BackgroundColorStr = MessageTokenMaps.HeaderBackgroundColors;
    private static readonly IReadOnlyDictionary<int, string> BackgroundYOffsetStr = MessageTokenMaps.HeaderBackgroundYOffsets;
    private static readonly Dictionary<byte, string> HeaderButtonText = new()
    {
        { 0x9F, "[A]"         }, { 0xA0, "[B]"         },
        { 0xA1, "[C]"         }, { 0xA2, "[L]"         }, { 0xA3, "[R]"         },
        { 0xA4, "[Z]"         }, { 0xA5, "[C-Up]"      }, { 0xA6, "[C-Down]"    },
        { 0xA7, "[C-Left]"    }, { 0xA8, "[C-Right]"   },
        { 0xAA, "[Control-Pad]" }, { 0xAB, "[D-Pad]"   },
    };

    // --------------------------------------------------------
    // Control code table  (tokType, macroName, argFmt, formatters)
    // argFmt chars: 'b' = 1 byte, 'h' = 2 bytes big-endian, 'x' = skip 1 byte
    // --------------------------------------------------------

    private static readonly Dictionary<byte, (string TokType, string Name, string ArgFmt, Func<int, string>[]? Fmts)> ControlCodes;

    static CHeaderExporter()
    {
        static string FmtByte(int c) => $"\"\\x{c:X2}\"";
        static string FmtTwoBytes(int c) => $"\"\\x{(c >> 8) & 0xFF:X2}\\x{c & 0xFF:X2}\"";
        static string FmtColor(int c) => ColorStr.TryGetValue(c, out var s) ? s : $"0x{c:02X}";
        static string FmtHighscore(int c) => HighscoreStr.TryGetValue(c, out var s) ? s : $"{c}";

        ControlCodes = new()
        {
            { 0x01, ("NEWLINE",            "NEWLINE",            "",    null) },
            { 0x02, ("END",                "END",                "",    null) },
            { 0x04, ("BOX_BREAK",          "BOX_BREAK",          "",    null) },
            { 0x05, ("COLOR",              "COLOR",              "b",   [FmtColor    ]) },
            { 0x06, ("SHIFT",              "SHIFT",              "b",   [FmtByte     ]) },
            { 0x07, ("TEXTID",             "TEXTID",             "h",   [FmtTwoBytes ]) },
            { 0x08, ("QUICKTEXT_ENABLE",   "QUICKTEXT_ENABLE",   "",    null) },
            { 0x09, ("QUICKTEXT_DISABLE",  "QUICKTEXT_DISABLE",  "",    null) },
            { 0x0A, ("PERSISTENT",         "PERSISTENT",         "",    null) },
            { 0x0B, ("EVENT",              "EVENT",              "",    null) },
            { 0x0C, ("BOX_BREAK_DELAYED",  "BOX_BREAK_DELAYED",  "b",   [FmtByte     ]) },
            { 0x0D, ("AWAIT_BUTTON_PRESS", "AWAIT_BUTTON_PRESS", "",    null) },
            { 0x0E, ("FADE",               "FADE",               "b",   [FmtByte     ]) },
            { 0x0F, ("NAME",               "NAME",               "",    null) },
            { 0x10, ("OCARINA",            "OCARINA",            "",    null) },
            { 0x11, ("FADE2",              "FADE2",              "h",   [FmtTwoBytes ]) },
            { 0x12, ("SFX",                "SFX",                "h",   [FmtTwoBytes ]) },
            { 0x13, ("ITEM_ICON",          "ITEM_ICON",          "b",   [FmtByte     ]) },
            { 0x14, ("TEXT_SPEED",         "TEXT_SPEED",         "b",   [FmtByte     ]) },
            { 0x15, ("BACKGROUND",         "BACKGROUND",         "bbb", [FmtByte, FmtByte, FmtByte]) },
            { 0x16, ("MARATHON_TIME",      "MARATHON_TIME",      "",    null) },
            { 0x17, ("RACE_TIME",          "RACE_TIME",          "",    null) },
            { 0x18, ("POINTS",             "POINTS",             "",    null) },
            { 0x19, ("TOKENS",             "TOKENS",             "",    null) },
            { 0x1A, ("UNSKIPPABLE",        "UNSKIPPABLE",        "",    null) },
            { 0x1B, ("TWO_CHOICE",         "TWO_CHOICE",         "",    null) },
            { 0x1C, ("THREE_CHOICE",       "THREE_CHOICE",       "",    null) },
            { 0x1D, ("FISH_INFO",          "FISH_INFO",          "",    null) },
            { 0x1E, ("HIGHSCORE",          "HIGHSCORE",          "b",   [FmtHighscore]) },
            { 0x1F, ("TIME",               "TIME",               "",    null) },
        };
    }

    // --------------------------------------------------------
    // Public API
    // --------------------------------------------------------

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
            string decoded = DecodeEntry(entry, encodingProfile, headerEncodingProfile, modern, otrMod);

            parts.Add(format == CHeaderExportFormat.Modern
                ? ExportModernSelectedEntry(entry, boxType, boxPos, decoded)
                : $"DEFINE_MESSAGE(0x{entry.Id:X4}, {boxType}, {boxPos},\n{decoded}\n)\n");
        }

        return NormalizeHeaderLineEndings(string.Join("\n", parts));
    }

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

    private static string DecodeEntry(
        MessageEntry entry,
        MessageEncodingProfile encodingProfile,
        MessageEncodingProfile headerEncodingProfile,
        bool modern = false,
        bool otrMod = false)
    {
        try
        {
            return DecodeMessageHeader(
                MessageTextSyntax.FromEditorText(entry.Text),
                encodingProfile,
                headerEncodingProfile,
                modern,
                otrMod);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException($"Message 0x{entry.Id:x4}: {ex.Message}", ex);
        }
    }

    private static string ExportModernEntry(
        MessageEntry entry,
        string boxType,
        string boxPos,
        ModernMessageSlots slots,
        bool fullLanguageSet,
        string? macroOverride = null)
    {
        if (entry.Type == 0x0b)
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
        return $"{macro}(0x{entry.Id:X4}, {boxType}, {boxPos},\n{decoded}\n)\n";
    }

    private static string FormatModernMsg(string? decoded)
        => decoded is null
            ? "MSG(/* MISSING */)"
            : decoded.Length == 0 ? "MSG()" : $"MSG(\n{decoded}\n)";

    private static string NormalizeHeaderLineEndings(string content)
        => content.ReplaceLineEndings("\n");

    private static string DecodeModernSlot(
        MessageEntry entry,
        MessageEncodingProfile encodingProfile,
        MessageEncodingProfile headerEncodingProfile)
        => entry.Bank == 0x08
            ? CHeaderJapaneseExporter.DecodeRawEntry(entry)
            : DecodeEntry(entry, encodingProfile, headerEncodingProfile, modern: true);

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

    // --------------------------------------------------------
    // Decoder
    // --------------------------------------------------------

    private static string DecodeMessageHeader(
        IEnumerable<MessageToken> messageTokens,
        MessageEncodingProfile encodingProfile,
        MessageEncodingProfile headerEncodingProfile,
        bool modern,
        bool otrMod = false)
    {
        var tokens = new List<(string TokType, string Data)>();
        var textRun = new StringBuilder();

        void FlushText()
        {
            if (textRun.Length > 0)
            {
                tokens.Add(("TEXT", textRun.ToString()));
                textRun.Clear();
            }
        }

        foreach (MessageToken messageToken in messageTokens)
        {
            switch (messageToken)
            {
                case TextToken text:
                    textRun.Append(ToHeaderText(text.Text, encodingProfile, headerEncodingProfile));
                    break;
                case LineBreakToken:
                    FlushText();
                    tokens.Add(("NEWLINE", "NEWLINE"));
                    break;
                case CommandToken command:
                    if (ControlCodes.TryGetValue(command.Code, out var commandControl))
                    {
                        FlushText();
                        tokens.Add((commandControl.TokType, commandControl.Name));
                    }
                    break;
                case ColorToken color:
                    FlushText();
                    tokens.Add(("COLOR", $"COLOR({FormatColor(color.Index)})"));
                    break;
                case ShiftToken shift:
                    FlushText();
                    tokens.Add(("SHIFT", $"SHIFT({FormatByteArgument(shift.Pixels, modern)})"));
                    break;
                case TextIdToken textId:
                    FlushText();
                    tokens.Add(("TEXTID", $"TEXTID({FormatWordArgument(textId.Id, modern)})"));
                    break;
                case BreakDelayToken breakDelay:
                    FlushText();
                    tokens.Add(("BOX_BREAK_DELAYED", $"BOX_BREAK_DELAYED({FormatByteArgument(breakDelay.Frames, modern)})"));
                    break;
                case FadeToken fade:
                    FlushText();
                    tokens.Add(("FADE", $"FADE({FormatByteArgument(fade.Frames, modern)})"));
                    break;
                case EndFadeToken endFade:
                    FlushText();
                    tokens.Add(("FADE2", $"FADE2({FormatWordArgument(endFade.Frames, modern)})"));
                    break;
                case SfxToken sfx:
                    FlushText();
                    tokens.Add(("SFX", $"SFX({FormatSfx(sfx.Id, modern)})"));
                    break;
                case IconToken icon:
                    FlushText();
                    tokens.Add(("ITEM_ICON", $"ITEM_ICON({FormatItem(icon.Id, modern)})"));
                    break;
                case TextSpeedToken textSpeed:
                    FlushText();
                    tokens.Add(("TEXT_SPEED", $"TEXT_SPEED({FormatByteArgument(textSpeed.Speed, modern)})"));
                    break;
                case BackgroundToken background:
                    FlushText();
                    tokens.Add(("BACKGROUND", FormatBackground(background.Rgb, modern)));
                    break;
                case HighscoreToken highscore:
                    FlushText();
                    tokens.Add(("HIGHSCORE", $"HIGHSCORE({FormatHighscore(highscore.Id, modern)})"));
                    break;
                case ButtonToken button:
                    textRun.Append(ToHeaderByteText(button.Code, headerEncodingProfile, modern, otrMod));
                    break;
            }
        }

        FlushText();
        return CHeaderTokenEmitter.Emit(tokens, modern, otrMod);
    }

    private static string ToHeaderText(
        string text,
        MessageEncodingProfile encodingProfile,
        MessageEncodingProfile headerEncodingProfile)
    {
        var result = new StringBuilder();
        foreach (char ch in text)
        {
            if (ch == '"')
            {
                result.Append("\\\"");
            }
            else if (encodingProfile.TryGetByte(ch, out byte value))
            {
                result.Append(headerEncodingProfile.GetHeaderText(value));
            }
            else
            {
                result.Append(ch);
            }
        }

        return result.ToString();
    }

    private static string ToHeaderByteText(
        byte value,
        MessageEncodingProfile encodingProfile,
        bool modern,
        bool otrMod)
    {
        if (value is >= 0x80 and <= 0x9e)
            return encodingProfile.GetHeaderText(value);

        if (value == 0xa9)
            return modern || otrMod ? "▼" : "[Triangle]";

        return HeaderButtonText.TryGetValue(value, out string? text) ? text : ((char)value).ToString();
    }

    private static string FormatByteString(int value) => $"\"\\x{value & 0xff:X2}\"";

    private static string FormatTwoByteString(int value) => $"\"\\x{(value >> 8) & 0xff:X2}\\x{value & 0xff:X2}\"";

    private static string FormatByteArgument(int value, bool modern)
        => modern ? $"{value & 0xff}" : FormatByteString(value);

    private static string FormatWordArgument(int value, bool modern)
        => modern ? $"0x{value & 0xffff:X4}" : FormatTwoByteString(value);

    private static string FormatItem(int value, bool modern)
        => modern && ItemStr.TryGetValue(value, out string? item)
            ? item
            : FormatByteString(value);

    private static string FormatSfx(int value, bool modern)
        => modern && SfxStr.TryGetValue(value, out string? sfx)
            ? sfx
            : modern ? $"0x{value & 0xffff:X4}" : FormatTwoByteString(value);

    private static string FormatBackground(int value, bool modern)
    {
        int bgIndex = (value >> 16) & 0xff;
        int colors = (value >> 8) & 0xff;
        int y = value & 0xff;
        if (!modern)
        {
            return $"BACKGROUND({FormatByteString(bgIndex)}, {FormatByteString(colors)}, {FormatByteString(y)})";
        }

        int foreground = (colors >> 4) & 0xf;
        int background = colors & 0xf;
        int yOffset = (y >> 4) & 0xf;
        int unknown = y & 0xf;
        return "BACKGROUND("
            + $"{FormatNamedByte(bgIndex, BackgroundStr)}, "
            + $"{FormatNamedByte(foreground, BackgroundForegroundStr)}, "
            + $"{FormatNamedByte(background, BackgroundColorStr)}, "
            + $"{FormatNamedByte(yOffset, BackgroundYOffsetStr)}, "
            + $"{unknown})";
    }

    private static string FormatNamedByte(int value, IReadOnlyDictionary<int, string> names)
        => names.TryGetValue(value, out string? name) ? name : $"{value}";

    private static string FormatColor(int value) => ColorStr.TryGetValue(value, out string? color) ? color : $"0x{value:02X}";

    private static string FormatHighscore(int value, bool modern)
    {
        IReadOnlyDictionary<int, string> map = modern ? ModernHighscoreStr : HighscoreStr;
        return map.TryGetValue(value, out string? highscore) ? highscore : $"{value}";
    }

}
