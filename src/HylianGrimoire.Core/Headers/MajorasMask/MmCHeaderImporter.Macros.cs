using System.Text;
using HylianGrimoire.Codecs.MajorasMask;

namespace HylianGrimoire.Headers.MajorasMask;

public static partial class MmCHeaderImporter
{
    private static void AppendMacro(StringBuilder text, string macro, string argumentText)
    {
        if (macro.Equals("END", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (TryAppendColor(text, macro))
        {
            return;
        }

        if (macro.Equals("NEWLINE", StringComparison.OrdinalIgnoreCase))
        {
            text.Append('\n');
        }
        else if (macro.Equals("SHIFT", StringComparison.OrdinalIgnoreCase))
        {
            text.Append($"[shift:{ParseByte(argumentText):x2}]");
        }
        else if (macro.Equals("BOX_BREAK_DELAYED", StringComparison.OrdinalIgnoreCase))
        {
            AppendWordTag(text, "breakdelay", argumentText);
        }
        else if (macro.Equals("FADE", StringComparison.OrdinalIgnoreCase))
        {
            AppendWordTag(text, "fade", argumentText);
        }
        else if (macro.Equals("FADE2", StringComparison.OrdinalIgnoreCase))
        {
            AppendWordTag(text, "endfade", argumentText);
        }
        else if (macro.Equals("FADE_SKIPPABLE", StringComparison.OrdinalIgnoreCase))
        {
            AppendWordTag(text, "fadeskippable", argumentText);
        }
        else if (macro.Equals("SFX", StringComparison.OrdinalIgnoreCase))
        {
            ushort value = ParseSfx(argumentText);
            text.Append($"[sfx:{value:x4}]");
        }
        else if (macro.Equals("DELAY", StringComparison.OrdinalIgnoreCase))
        {
            AppendWordTag(text, "delay", argumentText);
        }
        else if (MmMessageTokenMaps.NoArgumentBytes.TryGetValue(macro, out byte noArg)
            && MmMessageTokenMaps.NoArgumentTags.TryGetValue(noArg, out string? tag))
        {
            text.Append($"[{tag}]");
        }
        else
        {
            throw new InvalidDataException($"Unknown MM header macro: {macro}.");
        }
    }

    private static bool TryAppendColor(StringBuilder text, string macro)
    {
        if (!macro.StartsWith("COLOR_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string colorName = macro["COLOR_".Length..].Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        if (colorName == "lightblue")
        {
            colorName = "lightblue";
        }

        if (!MmMessageTokenMaps.ColorBytes.ContainsKey(colorName))
        {
            throw new InvalidDataException($"Unknown MM color macro: {macro}.");
        }

        text.Append($"[color:{colorName}]");
        return true;
    }

    private static void AppendWordTag(StringBuilder text, string tag, string argumentText)
        => text.Append($"[{tag}:{ParseWord(argumentText):x4}]");
}
