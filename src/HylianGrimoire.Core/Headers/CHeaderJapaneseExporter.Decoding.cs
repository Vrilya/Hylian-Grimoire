using System.Text;

namespace HylianGrimoire.Headers;

internal static partial class CHeaderJapaneseExporter
{
    private static readonly Encoding ShiftJisEncoding = CreateShiftJisEncoding();

    private static string DecodeMessageHeader(byte[] encoded)
    {
        ReadOnlySpan<byte> payload = TrimEnd(encoded);
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

        int offset = 0;
        while (offset < payload.Length)
        {
            int value = ReadWord(payload, offset);
            offset += 2;

            switch (value)
            {
                case 0x000a:
                    FlushText();
                    tokens.Add(("NEWLINE", "NEWLINE"));
                    break;
                case 0x81a5:
                    FlushText();
                    tokens.Add(("BOX_BREAK", "BOX_BREAK"));
                    break;
                case 0x000b:
                    FlushText();
                    tokens.Add(("COLOR", $"COLOR({FormatColor(ReadWord(payload, ref offset))})"));
                    break;
                case 0x86c7:
                    FlushText();
                    tokens.Add(("SHIFT", $"SHIFT({ReadSkippedByte(payload, ref offset)})"));
                    break;
                case 0x81cb:
                    FlushText();
                    tokens.Add(("TEXTID", $"TEXTID(0x{ReadWord(payload, ref offset):X4})"));
                    break;
                case 0x8189:
                    FlushText();
                    tokens.Add(("QUICKTEXT_ENABLE", "QUICKTEXT_ENABLE"));
                    break;
                case 0x818a:
                    FlushText();
                    tokens.Add(("QUICKTEXT_DISABLE", "QUICKTEXT_DISABLE"));
                    break;
                case 0x86c8:
                    FlushText();
                    tokens.Add(("PERSISTENT", "PERSISTENT"));
                    break;
                case 0x819f:
                    FlushText();
                    tokens.Add(("EVENT", "EVENT"));
                    break;
                case 0x81a3:
                    FlushText();
                    tokens.Add(("BOX_BREAK_DELAYED", $"BOX_BREAK_DELAYED({ReadSkippedByte(payload, ref offset)})"));
                    break;
                case 0x81a4:
                    FlushText();
                    tokens.Add(("AWAIT_BUTTON_PRESS", "AWAIT_BUTTON_PRESS"));
                    break;
                case 0x819e:
                    FlushText();
                    tokens.Add(("FADE", $"FADE({ReadSkippedByte(payload, ref offset)})"));
                    break;
                case 0x874f:
                    FlushText();
                    tokens.Add(("NAME", "NAME"));
                    break;
                case 0x81f0:
                    FlushText();
                    tokens.Add(("OCARINA", "OCARINA"));
                    break;
                case 0x81f4:
                    FlushText();
                    tokens.Add(("FADE2", $"FADE2({ReadWord(payload, ref offset)})"));
                    break;
                case 0x81f3:
                    FlushText();
                    tokens.Add(("SFX", $"SFX({FormatSfx(ReadWord(payload, ref offset))})"));
                    break;
                case 0x819a:
                    FlushText();
                    tokens.Add(("ITEM_ICON", $"ITEM_ICON({FormatItem(ReadSkippedByte(payload, ref offset))})"));
                    break;
                case 0x86c9:
                    FlushText();
                    tokens.Add(("TEXT_SPEED", $"TEXT_SPEED({ReadSkippedByte(payload, ref offset)})"));
                    break;
                case 0x86b3:
                    FlushText();
                    tokens.Add(("BACKGROUND", FormatBackground(payload, ref offset)));
                    break;
                case 0x8791:
                    FlushText();
                    tokens.Add(("MARATHON_TIME", "MARATHON_TIME"));
                    break;
                case 0x8792:
                    FlushText();
                    tokens.Add(("RACE_TIME", "RACE_TIME"));
                    break;
                case 0x879b:
                    FlushText();
                    tokens.Add(("POINTS", "POINTS"));
                    break;
                case 0x86a3:
                    FlushText();
                    tokens.Add(("TOKENS", "TOKENS"));
                    break;
                case 0x8199:
                    FlushText();
                    tokens.Add(("UNSKIPPABLE", "UNSKIPPABLE"));
                    break;
                case 0x81bc:
                    FlushText();
                    tokens.Add(("TWO_CHOICE", "TWO_CHOICE"));
                    break;
                case 0x81b8:
                    FlushText();
                    tokens.Add(("THREE_CHOICE", "THREE_CHOICE"));
                    break;
                case 0x86a4:
                    FlushText();
                    tokens.Add(("FISH_INFO", "FISH_INFO"));
                    break;
                case 0x869f:
                    FlushText();
                    tokens.Add(("HIGHSCORE", $"HIGHSCORE({FormatHighscore(ReadSkippedByte(payload, ref offset))})"));
                    break;
                case 0x81a1:
                    FlushText();
                    tokens.Add(("TIME", "TIME"));
                    break;
                default:
                    textRun.Append(DecodeCharacter(value));
                    break;
            }
        }

        FlushText();
        return CHeaderTokenEmitter.Emit(tokens, modern: true);
    }

    private static Encoding CreateShiftJisEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(
            "shift_jis",
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
    }

    private static string DecodeCharacter(int value)
    {
        if (TextReplacements.TryGetValue(value, out string? replacement))
        {
            return replacement;
        }

        string text = ShiftJisEncoding.GetString([(byte)(value >> 8), (byte)value]);
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
