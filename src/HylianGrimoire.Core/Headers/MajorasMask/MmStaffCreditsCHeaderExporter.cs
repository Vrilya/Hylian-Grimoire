using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers.MajorasMask;

internal static class MmStaffCreditsCHeaderExporter
{
    private static readonly MessageEncodingProfile StaffEncodingProfile = MessageEncodingProfile.MajorasMaskOriginal;

    public static bool IsEntry(MessageEntry entry)
        => entry.CodecMetadata is not MajorasMaskMessageMetadata
            && entry.Id is >= 0x4e20 and <= 0x4e4c;

    public static void Append(StringBuilder sb, MessageEntry entry)
    {
        int tableType = entry.Type == 0 ? 0x0b : entry.Type & 0x0f;
        int tablePosition = entry.Position & 0x0f;

        sb.Append(CultureInvariant($"DEFINE_MESSAGE(0x{entry.Id:X4}, 0x{tableType:X2}, 0x{tablePosition:X2},"));
        sb.AppendLine();
        sb.AppendLine("MSG(");

        string body = string.IsNullOrEmpty(entry.Text)
            ? string.Empty
            : FormatBody(entry.Text);
        if (body.Length > 0)
        {
            sb.Append(body);
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("\"\"");
        }

        sb.AppendLine(")");
        sb.AppendLine(")");
        sb.AppendLine();
    }

    private static string FormatBody(string editorText)
    {
        string normalized = editorText.Replace("[persistent]", "[shop]", StringComparison.OrdinalIgnoreCase);
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

        foreach (MessageToken token in MessageTextSyntax.FromEditorText(normalized))
        {
            switch (token)
            {
                case TextToken text:
                    textRun.Append(StaffEncodingProfile.ToHeaderText(text.Text));
                    break;
                case LineBreakToken:
                    FlushText();
                    tokens.Add(("NEWLINE", "NEWLINE"));
                    break;
                case CommandToken command:
                    if (MessageTokenMaps.CommandTags.TryGetValue(command.Code, out string? commandName))
                    {
                        FlushText();
                        tokens.Add((FormatCommandTokenType(commandName), FormatCommand(commandName)));
                    }
                    break;
                case ColorToken color:
                    FlushText();
                    tokens.Add(("COLOR", $"COLOR({FormatColor(MessageTokenMaps.ColorTags.TryGetValue(color.Index, out string? knownColor) ? knownColor : $"{color.Index:x2}")})"));
                    break;
                case ShiftToken shift:
                    FlushText();
                    tokens.Add(("SHIFT", $"SHIFT({shift.Pixels})"));
                    break;
                case FadeToken fade:
                    FlushText();
                    tokens.Add(("FADE", $"FADE({fade.Frames})"));
                    break;
                case EndFadeToken endFade:
                    FlushText();
                    tokens.Add(("FADE2", $"FADE2({endFade.Frames})"));
                    break;
                case SfxToken sfx:
                    FlushText();
                    tokens.Add(("SFX", $"SFX({FormatSfx(sfx.Id)})"));
                    break;
                case ButtonToken button:
                    if (MessageTokenMaps.ButtonTags.TryGetValue(button.Code, out string? buttonName))
                    {
                        textRun.Append(FormatButtonText(buttonName));
                    }
                    break;
            }
        }

        FlushText();
        return CHeaderTokenEmitter.Emit(tokens, modern: true);
    }

    private static string FormatCommandTokenType(string commandName)
    {
        return commandName.ToLowerInvariant() switch
        {
            "break" => "BOX_BREAK",
            "twochoice" => "TWO_CHOICE",
            "threechoice" => "THREE_CHOICE",
            _ => "MACRO",
        };
    }

    private static string FormatCommand(string commandName)
    {
        if (commandName.Equals("shop", StringComparison.OrdinalIgnoreCase))
        {
            return "PERSISTENT";
        }

        return commandName.ToLowerInvariant() switch
        {
            "quicktexton" => "QUICKTEXT_ENABLE",
            "quicktextoff" => "QUICKTEXT_DISABLE",
            "break" => "BOX_BREAK",
            "event" => "EVENT",
            "waitbutton" => "AWAIT_BUTTON_PRESS",
            "twochoice" => "TWO_CHOICE",
            "threechoice" => "THREE_CHOICE",
            _ => commandName.ToUpperInvariant(),
        };
    }

    private static string FormatColor(string color)
        => color.ToUpperInvariant();

    private static string FormatSfx(ushort value)
        => MmMessageSfxMaps.Names.TryGetValue(value, out string? name)
            ? name
            : CultureInvariant($"0x{value:X4}");

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

    private static string CultureInvariant(FormattableString text)
        => FormattableString.Invariant(text);
}
