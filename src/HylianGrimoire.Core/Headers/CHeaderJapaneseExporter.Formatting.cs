using System.Text;

namespace HylianGrimoire.Headers;

internal static partial class CHeaderJapaneseExporter
{
    private static string FormatByteInitializer(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(i % 12 == 0 ? "\n" : " ");
            }

            builder.Append("0x");
            builder.Append(bytes[i].ToString("X2"));
            builder.Append(',');
        }

        return builder.ToString();
    }

    private static string FormatColor(int value)
    {
        if ((value & 0xfff0) != 0x0c00)
        {
            return $"0x{value:X4}";
        }

        int color = 0x40 | (value & 0x0f);
        return ColorStr.TryGetValue(color, out string? colorName) ? colorName : $"0x{color:02X}";
    }

    private static string FormatBackground(ReadOnlySpan<byte> data, ref int offset)
    {
        int bgIndex = ReadSkippedByte(data, ref offset);
        int colors = ReadByte(data, ref offset);
        int y = ReadByte(data, ref offset);
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

    private static string FormatItem(int value)
        => ItemStr.TryGetValue(value, out string? item) ? item : $"\"\\x{value & 0xff:X2}\"";

    private static string FormatSfx(int value)
        => SfxStr.TryGetValue(value, out string? sfx) ? sfx : $"0x{value & 0xffff:X4}";

    private static string FormatHighscore(int value)
        => ModernHighscoreStr.TryGetValue(value, out string? highscore) ? highscore : $"{value}";

    private static string FormatNamedByte(int value, IReadOnlyDictionary<int, string> names)
        => names.TryGetValue(value, out string? name) ? name : $"{value}";
}
