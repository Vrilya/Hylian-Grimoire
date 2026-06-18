using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers;

public static partial class CHeaderExporter
{
    private static string DecodeEntry(
        MessageEntry entry,
        MessageEncodingProfile encodingProfile,
        MessageEncodingProfile headerEncodingProfile,
        bool modern = false,
        bool otrMod = false,
        bool staffCredits = false)
    {
        try
        {
            return DecodeMessageHeader(
                MessageTextSyntax.FromEditorText(entry.Text),
                encodingProfile,
                headerEncodingProfile,
                modern,
                otrMod,
                staffCredits);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException($"Message 0x{entry.Id:x4}: {ex.Message}", ex);
        }
    }

    private static string DecodeMessageHeader(
        IEnumerable<MessageToken> messageTokens,
        MessageEncodingProfile encodingProfile,
        MessageEncodingProfile headerEncodingProfile,
        bool modern,
        bool otrMod = false,
        bool staffCredits = false)
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
                case RawByteToken raw:
                    FlushText();
                    tokens.Add(("RAW", FormatByteString(raw.Value)));
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
                    tokens.Add(("FADE2", $"FADE2({FormatEndFadeArgument(endFade.Frames, modern, staffCredits)})"));
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
}
