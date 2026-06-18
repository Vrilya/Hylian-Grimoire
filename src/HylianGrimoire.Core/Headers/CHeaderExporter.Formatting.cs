namespace HylianGrimoire.Headers;

public static partial class CHeaderExporter
{
    private static string NormalizeHeaderLineEndings(string content)
        => content.ReplaceLineEndings("\n");

    private static string FormatByteString(int value) => $"\"\\x{value & 0xff:X2}\"";

    private static string FormatTwoByteString(int value) => $"\"\\x{(value >> 8) & 0xff:X2}\\x{value & 0xff:X2}\"";

    private static string FormatByteArgument(int value, bool modern)
        => modern ? $"{value & 0xff}" : FormatByteString(value);

    private static string FormatWordArgument(int value, bool modern)
        => modern ? $"0x{value & 0xffff:X4}" : FormatTwoByteString(value);

    private static string FormatEndFadeArgument(int value, bool modern, bool staffCredits)
        => modern && staffCredits ? $"{value & 0xffff}" : FormatWordArgument(value, modern);

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
