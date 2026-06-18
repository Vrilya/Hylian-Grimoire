using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;

namespace HylianGrimoire.Headers.MajorasMask;

public static partial class MmCHeaderExporter
{
    private static string FormatSignedWord(ushort value)
        => CultureInvariant($"0x{value:X4}");

    private static string FormatColorMacro(string color)
        => color.ToUpperInvariant();

    private static string FormatOneByteMacro(string tag)
    {
        return tag.ToLowerInvariant() switch
        {
            "shift" => "SHIFT",
            _ => tag.ToUpperInvariant(),
        };
    }

    private static string FormatTwoByteMacro(string tag, ushort argument)
    {
        return tag.ToLowerInvariant() switch
        {
            "breakdelay" => CultureInvariant($"BOX_BREAK_DELAYED({argument})"),
            "fade" => CultureInvariant($"FADE({argument})"),
            "fadeskippable" => CultureInvariant($"FADE_SKIPPABLE({argument})"),
            "sfx" => CultureInvariant($"SFX({FormatSfx(argument)})"),
            "delay" => CultureInvariant($"DELAY({argument})"),
            _ => CultureInvariant($"{tag.ToUpperInvariant()}({argument})"),
        };
    }

    private static string FormatSfx(ushort value)
        => MmMessageSfxMaps.Names.TryGetValue(value, out string? name)
            ? name
            : CultureInvariant($"0x{value:X4}");

    private static bool TryAppendHeaderText(
        StringBuilder textRun,
        byte value,
        MessageEncodingProfile encodingProfile)
    {
        if (encodingProfile.TryGetEditorChar(value, out char special))
        {
            textRun.Append(EscapeHeaderString(special.ToString()));
            return true;
        }

        if (value is >= 0x20 and <= 0x7e)
        {
            textRun.Append(EscapeHeaderChar((char)value));
            return true;
        }

        return false;
    }

    private static string FormatButtonText(string button)
    {
        return button switch
        {
            "A-button" => "[A]",
            "B-button" => "[B]",
            "C-button" => "[C]",
            "L-button" => "[L]",
            "R-button" => "[R]",
            "Z-button" => "[Z]",
            "C-up" => "[C-Up]",
            "C-down" => "[C-Down]",
            "C-left" => "[C-Left]",
            "C-right" => "[C-Right]",
            "Control-Pad" => "[Control-Pad]",
            "Z-target" => "<TRIANGLE>",
            _ => $"[{button}]",
        };
    }

    private static string EscapeHeaderString(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (char ch in value)
        {
            result.Append(EscapeHeaderChar(ch));
        }

        return result.ToString();
    }

    private static string EscapeHeaderChar(char ch)
    {
        return ch switch
        {
            '\\' => "\\\\",
            '"' => "\\\"",
            '\t' => "\\t",
            _ when !char.IsControl(ch) => ch.ToString(),
            _ => CultureInvariant($"\\x{(int)ch:X2}"),
        };
    }

    private static string CultureInvariant(FormattableString text)
        => FormattableString.Invariant(text);
}
