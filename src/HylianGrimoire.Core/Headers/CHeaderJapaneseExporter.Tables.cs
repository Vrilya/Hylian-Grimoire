using HylianGrimoire.Codecs;

namespace HylianGrimoire.Headers;

internal static partial class CHeaderJapaneseExporter
{
    private static readonly IReadOnlyDictionary<int, string> ColorStr = MessageTokenMaps.HeaderColors;
    private static readonly IReadOnlyDictionary<int, string> ModernHighscoreStr = MessageTokenMaps.ModernHeaderHighscores;
    private static readonly IReadOnlyDictionary<int, string> ItemStr = MessageTokenMaps.HeaderItems;
    private static readonly IReadOnlyDictionary<int, string> SfxStr = MessageSfxMaps.HeaderNames;
    private static readonly IReadOnlyDictionary<int, string> BackgroundStr = MessageTokenMaps.HeaderBackgrounds;
    private static readonly IReadOnlyDictionary<int, string> BackgroundForegroundStr = MessageTokenMaps.HeaderBackgroundForegroundColors;
    private static readonly IReadOnlyDictionary<int, string> BackgroundColorStr = MessageTokenMaps.HeaderBackgroundColors;
    private static readonly IReadOnlyDictionary<int, string> BackgroundYOffsetStr = MessageTokenMaps.HeaderBackgroundYOffsets;

    private static readonly IReadOnlyDictionary<int, string> TextReplacements = new Dictionary<int, string>
    {
        { 0x8160, "〜" },
        { 0x839f, "[A]" },
        { 0x83a0, "[B]" },
        { 0x83a1, "[C]" },
        { 0x83a2, "[L]" },
        { 0x83a3, "[R]" },
        { 0x83a4, "[Z]" },
        { 0x83a5, "[C-Up]" },
        { 0x83a6, "[C-Down]" },
        { 0x83a7, "[C-Left]" },
        { 0x83a8, "[C-Right]" },
        { 0x83a9, "▼" },
        { 0x83aa, "[Control-Pad]" },
        { 0x86d3, "┯" },
    };
}
