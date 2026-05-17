using System;
using System.Collections.Generic;
using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers;

/// <summary>
/// Exports the current message entries to an OoT C header format.
/// </summary>
public static class CHeaderExporter
{
    // --------------------------------------------------------
    // Lookup tables
    // --------------------------------------------------------

    private static readonly IReadOnlyDictionary<int, string> BoxTypeStr = MessageTokenMaps.HeaderBoxTypes;
    private static readonly IReadOnlyDictionary<int, string> BoxPosStr = MessageTokenMaps.HeaderBoxPositions;
    private static readonly IReadOnlyDictionary<int, string> ColorStr = MessageTokenMaps.HeaderColors;
    private static readonly IReadOnlyDictionary<int, string> HighscoreStr = MessageTokenMaps.HeaderHighscores;
    private static readonly MessageEncodingProfile EncodingProfile = MessageEncodingProfile.Default;

    private static readonly Dictionary<byte, string> HeaderButtonText = new()
    {
        { 0x9F, "[A]"         }, { 0xA0, "[B]"         },
        { 0xA1, "[C]"         }, { 0xA2, "[L]"         }, { 0xA3, "[R]"         },
        { 0xA4, "[Z]"         }, { 0xA5, "[C-Up]"      }, { 0xA6, "[C-Down]"    },
        { 0xA7, "[C-Left]"    }, { 0xA8, "[C-Right]"   },
        { 0xAA, "[Control-Pad]" }, { 0xAB, "[D-Pad]"   },
    };

    // --------------------------------------------------------
    // Control code table  (tokType, macroName, argFmt, formatters)
    // argFmt chars: 'b' = 1 byte, 'h' = 2 bytes big-endian, 'x' = skip 1 byte
    // --------------------------------------------------------

    private static readonly Dictionary<byte, (string TokType, string Name, string ArgFmt, Func<int, string>[]? Fmts)> ControlCodes;

    static CHeaderExporter()
    {
        static string FmtByte(int c)      => $"\"\\x{c:X2}\"";
        static string FmtTwoBytes(int c)  => $"\"\\x{(c >> 8) & 0xFF:X2}\\x{c & 0xFF:X2}\"";
        static string FmtColor(int c)     => ColorStr.TryGetValue(c, out var s) ? s : $"0x{c:02X}";
        static string FmtHighscore(int c) => HighscoreStr.TryGetValue(c, out var s) ? s : $"{c}";

        ControlCodes = new()
        {
            { 0x01, ("NEWLINE",            "NEWLINE",            "",    null) },
            { 0x02, ("END",                "END",                "",    null) },
            { 0x04, ("BOX_BREAK",          "BOX_BREAK",          "",    null) },
            { 0x05, ("COLOR",              "COLOR",              "b",   [FmtColor    ]) },
            { 0x06, ("SHIFT",              "SHIFT",              "b",   [FmtByte     ]) },
            { 0x07, ("TEXTID",             "TEXTID",             "h",   [FmtTwoBytes ]) },
            { 0x08, ("QUICKTEXT_ENABLE",   "QUICKTEXT_ENABLE",   "",    null) },
            { 0x09, ("QUICKTEXT_DISABLE",  "QUICKTEXT_DISABLE",  "",    null) },
            { 0x0A, ("PERSISTENT",         "PERSISTENT",         "",    null) },
            { 0x0B, ("EVENT",              "EVENT",              "",    null) },
            { 0x0C, ("BOX_BREAK_DELAYED",  "BOX_BREAK_DELAYED",  "b",   [FmtByte     ]) },
            { 0x0D, ("AWAIT_BUTTON_PRESS", "AWAIT_BUTTON_PRESS", "",    null) },
            { 0x0E, ("FADE",               "FADE",               "b",   [FmtByte     ]) },
            { 0x0F, ("NAME",               "NAME",               "",    null) },
            { 0x10, ("OCARINA",            "OCARINA",            "",    null) },
            { 0x11, ("FADE2",              "FADE2",              "h",   [FmtTwoBytes ]) },
            { 0x12, ("SFX",                "SFX",                "h",   [FmtTwoBytes ]) },
            { 0x13, ("ITEM_ICON",          "ITEM_ICON",          "b",   [FmtByte     ]) },
            { 0x14, ("TEXT_SPEED",         "TEXT_SPEED",         "b",   [FmtByte     ]) },
            { 0x15, ("BACKGROUND",         "BACKGROUND",         "bbb", [FmtByte, FmtByte, FmtByte]) },
            { 0x16, ("MARATHON_TIME",      "MARATHON_TIME",      "",    null) },
            { 0x17, ("RACE_TIME",          "RACE_TIME",          "",    null) },
            { 0x18, ("POINTS",             "POINTS",             "",    null) },
            { 0x19, ("TOKENS",             "TOKENS",             "",    null) },
            { 0x1A, ("UNSKIPPABLE",        "UNSKIPPABLE",        "",    null) },
            { 0x1B, ("TWO_CHOICE",         "TWO_CHOICE",         "",    null) },
            { 0x1C, ("THREE_CHOICE",       "THREE_CHOICE",       "",    null) },
            { 0x1D, ("FISH_INFO",          "FISH_INFO",          "",    null) },
            { 0x1E, ("HIGHSCORE",          "HIGHSCORE",          "b",   [FmtHighscore]) },
            { 0x1F, ("TIME",               "TIME",               "",    null) },
        };
    }

    // --------------------------------------------------------
    // Public API
    // --------------------------------------------------------

    /// <summary>
    /// Produces an OoT C header file from the given message entries.
    /// </summary>
    public static string Export(List<MessageEntry> entries)
    {
        var parts = new List<string>(entries.Count);

        foreach (var entry in entries)
        {
            string decoded;
            try
            {
                decoded = DecodeMessageHeader(MessageTextSyntax.FromEditorText(entry.Text));
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidDataException($"Message 0x{entry.Id:x4}: {ex.Message}", ex);
            }

            string boxType = BoxTypeStr.TryGetValue(entry.Type,     out var bt) ? bt : $"TEXTBOX_TYPE_UNK_{entry.Type:X}";
            string boxPos  = BoxPosStr .TryGetValue(entry.Position, out var bp) ? bp : $"TEXTBOX_POS_UNK_{entry.Position:X}";

            parts.Add($"DEFINE_MESSAGE(0x{entry.Id:X4}, {boxType}, {boxPos},\n{decoded}\n)\n");
        }

        return string.Join("\n", parts);
    }

    // --------------------------------------------------------
    // Decoder
    // --------------------------------------------------------

    private static string DecodeMessageHeader(IEnumerable<MessageToken> messageTokens)
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
                    textRun.Append(ToHeaderText(text.Text));
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
                    tokens.Add(("SHIFT", $"SHIFT({FormatByteString(shift.Pixels)})"));
                    break;
                case TextIdToken textId:
                    FlushText();
                    tokens.Add(("TEXTID", $"TEXTID({FormatTwoByteString(textId.Id)})"));
                    break;
                case BreakDelayToken breakDelay:
                    FlushText();
                    tokens.Add(("BOX_BREAK_DELAYED", $"BOX_BREAK_DELAYED({FormatByteString(breakDelay.Frames)})"));
                    break;
                case FadeToken fade:
                    FlushText();
                    tokens.Add(("FADE", $"FADE({FormatByteString(fade.Frames)})"));
                    break;
                case EndFadeToken endFade:
                    FlushText();
                    tokens.Add(("FADE2", $"FADE2({FormatTwoByteString(endFade.Frames)})"));
                    break;
                case SfxToken sfx:
                    FlushText();
                    tokens.Add(("SFX", $"SFX({FormatTwoByteString(sfx.Id)})"));
                    break;
                case IconToken icon:
                    FlushText();
                    tokens.Add(("ITEM_ICON", $"ITEM_ICON({FormatByteString(icon.Id)})"));
                    break;
                case TextSpeedToken textSpeed:
                    FlushText();
                    tokens.Add(("TEXT_SPEED", $"TEXT_SPEED({FormatByteString(textSpeed.Speed)})"));
                    break;
                case BackgroundToken background:
                    FlushText();
                    tokens.Add(("BACKGROUND", $"BACKGROUND({FormatByteString((background.Rgb >> 16) & 0xff)}, {FormatByteString((background.Rgb >> 8) & 0xff)}, {FormatByteString(background.Rgb & 0xff)})"));
                    break;
                case HighscoreToken highscore:
                    FlushText();
                    tokens.Add(("HIGHSCORE", $"HIGHSCORE({FormatHighscore(highscore.Id)})"));
                    break;
                case ButtonToken button:
                    textRun.Append(ToHeaderByteText(button.Code));
                    break;
            }
        }

        FlushText();
        return EmitTokens(tokens);
    }

    private static string ToHeaderText(string text)
        => EncodingProfile.ToHeaderText(text);

    private static string ToHeaderByteText(byte value)
    {
        if (value is >= 0x80 and <= 0x9e)
            return EncodingProfile.GetHeaderText(value);

        if (value == 0xa9)
            return "[Triangle]";

        return HeaderButtonText.TryGetValue(value, out string? text) ? text : ((char)value).ToString();
    }

    private static string FormatByteString(int value) => $"\"\\x{value & 0xff:X2}\"";

    private static string FormatTwoByteString(int value) => $"\"\\x{(value >> 8) & 0xff:X2}\\x{value & 0xff:X2}\"";

    private static string FormatColor(int value) => ColorStr.TryGetValue(value, out string? color) ? color : $"0x{value:02X}";

    private static string FormatHighscore(int value) => HighscoreStr.TryGetValue(value, out string? highscore) ? highscore : $"{value}";

    /// <summary>
    /// Formats a token list into the C string literal style used by header.
    /// </summary>
    private static string EmitTokens(List<(string TokType, string Data)> tokens)
    {
        if (tokens.Count == 0)
            return "\"\"";

        var sb = new StringBuilder();
        bool qState = false;  // currently inside an open "
        bool sState = false;  // need a space before next non-break token

        void MaybeEnterQ()
        {
            if (!qState) { sb.Append('"'); qState = true; }
        }

        void MaybeExitQ(bool space = false)
        {
            if (qState)
            {
                sb.Append('"');
                if (space) sb.Append(' ');
                qState = false;
            }
        }

        foreach (var (tokType, tokDat) in tokens)
        {
            if (tokType is "BOX_BREAK" or "BOX_BREAK_DELAYED")
            {
                MaybeExitQ();
                sState = false;
                sb.Append('\n');
                sb.Append(tokDat);
                sb.Append('\n');
                continue;
            }

            if (sState) { sb.Append(' '); sState = false; }

            if (tokType == "NEWLINE")
            {
                MaybeEnterQ();
                sb.Append("\\n\"\n");
                qState = false;
            }
            else if (tokType == "TEXT")
            {
                MaybeEnterQ();
                sb.Append(tokDat);
            }
            else
            {
                MaybeExitQ(space: true);
                sb.Append(tokDat);
                if (tokType is "TWO_CHOICE" or "THREE_CHOICE")
                    sb.Append('\n');
                else
                    sState = true;
            }
        }

        MaybeExitQ();

        string result = sb.ToString();
        if (result.Length > 0 && result[^1] == '\n')
            result = result[..^1];

        return result;
    }
}
