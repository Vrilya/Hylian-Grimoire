using HylianGrimoire.Codecs;

namespace HylianGrimoire.Headers;

public static partial class CHeaderExporter
{
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

    // Control code table  (tokType, macroName, argFmt, formatters)
    // argFmt chars: 'b' = 1 byte, 'h' = 2 bytes big-endian, 'x' = skip 1 byte
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
}
