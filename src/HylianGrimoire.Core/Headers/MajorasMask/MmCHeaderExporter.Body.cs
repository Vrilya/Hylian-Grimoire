using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;

namespace HylianGrimoire.Headers.MajorasMask;

public static partial class MmCHeaderExporter
{
    private static string FormatBody(
        string editorText,
        MessageEncodingProfile encodingProfile,
        MessageEncodingProfile headerEncodingProfile)
    {
        byte[] encoded = MmMessageTextCodec.Encode(editorText, encodingProfile);
        int count = Array.IndexOf(encoded, (byte)0xbf);
        if (count < 0)
        {
            count = encoded.Length;
        }

        if (count == 0)
        {
            return string.Empty;
        }

        var tokens = new List<(string TokType, string Data)>();
        var textRun = new StringBuilder();

        void FlushText()
        {
            if (textRun.Length == 0)
            {
                return;
            }

            tokens.Add(("TEXT", textRun.ToString()));
            textRun.Clear();
        }

        for (int i = 0; i < count; i++)
        {
            byte value = encoded[i];
            if (value == 0x11)
            {
                FlushText();
                tokens.Add(("NEWLINE", "NEWLINE"));
            }
            else if (MmMessageTokenMaps.ColorTags.TryGetValue(value, out string? color))
            {
                FlushText();
                tokens.Add(("COLOR", $"COLOR_{FormatColorMacro(color)}"));
            }
            else if (MmMessageTokenMaps.NoArgumentTags.TryGetValue(value, out string? tag))
            {
                FlushText();
                string tokenType = tag switch
                {
                    "break" or "break2" => "BOX_BREAK",
                    "twochoice" => "TWO_CHOICE",
                    "threechoice" => "THREE_CHOICE",
                    _ => "MACRO",
                };
                tokens.Add((tokenType, NoArgumentMacros[tag]));
            }
            else if (MmMessageTokenMaps.ButtonTags.TryGetValue(value, out string? button))
            {
                textRun.Append(FormatButtonText(button));
            }
            else if (MmMessageTokenMaps.OneByteArgumentTags.TryGetValue(value, out string? oneByteTag))
            {
                FlushText();
                if (i + 1 >= count)
                {
                    break;
                }

                byte argument = encoded[++i];
                tokens.Add(("MACRO", $"{FormatOneByteMacro(oneByteTag)}({argument})"));
            }
            else if (MmMessageTokenMaps.TwoByteArgumentTags.TryGetValue(value, out string? twoByteTag))
            {
                FlushText();
                if (i + 2 >= count)
                {
                    break;
                }

                ushort argument = (ushort)((encoded[++i] << 8) | encoded[++i]);
                string tokenType = twoByteTag.Equals("breakdelay", StringComparison.OrdinalIgnoreCase)
                    ? "BOX_BREAK_DELAYED"
                    : "MACRO";
                tokens.Add((tokenType, FormatTwoByteMacro(twoByteTag, argument)));
            }
            else if (TryAppendHeaderText(textRun, value, headerEncodingProfile))
            {
                continue;
            }
            else
            {
                FlushText();
                tokens.Add(("RAW", CultureInvariant($"0x{value:X2}")));
            }
        }

        FlushText();
        return CHeaderTokenEmitter.Emit(tokens, modern: true);
    }
}
