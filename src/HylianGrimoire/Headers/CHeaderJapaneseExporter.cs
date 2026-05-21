using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers;

internal static class CHeaderJapaneseExporter
{
    private static readonly IReadOnlyDictionary<int, string> ColorStr = MessageTokenMaps.HeaderColors;
    private static readonly IReadOnlyDictionary<int, string> ModernHighscoreStr = MessageTokenMaps.ModernHeaderHighscores;
    private static readonly IReadOnlyDictionary<int, string> ItemStr = MessageTokenMaps.HeaderItems;
    private static readonly IReadOnlyDictionary<int, string> SfxStr = MessageSfxMaps.HeaderNames;
    private static readonly IReadOnlyDictionary<int, string> BackgroundStr = MessageTokenMaps.HeaderBackgrounds;
    private static readonly IReadOnlyDictionary<int, string> BackgroundForegroundStr = MessageTokenMaps.HeaderBackgroundForegroundColors;
    private static readonly IReadOnlyDictionary<int, string> BackgroundColorStr = MessageTokenMaps.HeaderBackgroundColors;
    private static readonly IReadOnlyDictionary<int, string> BackgroundYOffsetStr = MessageTokenMaps.HeaderBackgroundYOffsets;
    private static readonly Encoding ShiftJisEncoding = CreateShiftJisEncoding();

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

    public static string DecodeRawEntry(MessageEntry entry)
    {
        byte[] encoded = entry.EncodedBytesOverride
            ?? entry.OriginalEncodedBytes
            ?? throw new InvalidDataException($"Message 0x{entry.Id:x4}: Japanese ROM message has no encoded data to export.");

        try
        {
            return DecodeMessageHeader(encoded);
        }
        catch (Exception ex) when (ex is ArgumentException or DecoderFallbackException or InvalidDataException)
        {
            return FormatByteInitializer(TrimEnd(encoded));
        }
    }

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

    private static ReadOnlySpan<byte> TrimEnd(byte[] encoded)
    {
        int end = encoded.Length;
        bool foundEnd = false;
        for (int i = 0; i + 1 < encoded.Length; i += 2)
        {
            if (encoded[i] == 0x81 && encoded[i + 1] == 0x70)
            {
                end = i;
                foundEnd = true;
                break;
            }
        }

        if (!foundEnd)
        {
            while (end > 0 && encoded[end - 1] == 0)
            {
                end--;
            }
        }

        return encoded.AsSpan(0, end);
    }

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

    private static Encoding CreateShiftJisEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(
            "shift_jis",
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
    }

    private static int ReadWord(ReadOnlySpan<byte> data, int offset)
    {
        if (offset + 1 >= data.Length)
        {
            throw new InvalidDataException("Japanese message ended in the middle of a wide value.");
        }

        return (data[offset] << 8) | data[offset + 1];
    }

    private static int ReadWord(ReadOnlySpan<byte> data, ref int offset)
    {
        int value = ReadWord(data, offset);
        offset += 2;
        return value;
    }

    private static int ReadSkippedByte(ReadOnlySpan<byte> data, ref int offset)
    {
        int value = ReadWord(data, ref offset);
        if ((value & 0xff00) != 0)
        {
            throw new InvalidDataException("Japanese message argument expected a zero high byte.");
        }

        return value & 0xff;
    }

    private static int ReadByte(ReadOnlySpan<byte> data, ref int offset)
    {
        if (offset >= data.Length)
        {
            throw new InvalidDataException("Japanese message ended in the middle of a byte argument.");
        }

        return data[offset++];
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
