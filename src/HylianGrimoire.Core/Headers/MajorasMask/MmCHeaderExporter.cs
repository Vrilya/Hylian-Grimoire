using System.Text;
using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers.MajorasMask;

public static class MmCHeaderExporter
{
    private static readonly IReadOnlyDictionary<string, string> NoArgumentMacros =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["textspeed"] = "TEXT_SPEED",
            ["hsboatarchery"] = "HS_BOAT_ARCHERY",
            ["strayfairies"] = "STRAY_FAIRIES",
            ["tokens"] = "TOKENS",
            ["pointstens"] = "POINTS_TENS",
            ["pointsthousands"] = "POINTS_THOUSANDS",
            ["break"] = "BOX_BREAK",
            ["break2"] = "BOX_BREAK2",
            ["carriagereturn"] = "CARRIAGE_RETURN",
            ["continue"] = "CONTINUE",
            ["name"] = "NAME",
            ["quicktexton"] = "QUICKTEXT_ENABLE",
            ["quicktextoff"] = "QUICKTEXT_DISABLE",
            ["event"] = "EVENT",
            ["persistent"] = "PERSISTENT",
            ["background"] = "BACKGROUND",
            ["twochoice"] = "TWO_CHOICE",
            ["threechoice"] = "THREE_CHOICE",
            ["timerpostman"] = "TIMER_POSTMAN",
            ["timerminigame1"] = "TIMER_MINIGAME_1",
            ["timer2"] = "TIMER_2",
            ["timermooncrash"] = "TIMER_MOON_CRASH",
            ["timerminigame2"] = "TIMER_MINIGAME_2",
            ["timerhazard"] = "TIMER_ENV_HAZARD",
            ["time"] = "TIME",
            ["chestflags"] = "CHEST_FLAGS",
            ["inputbank"] = "INPUT_BANK",
            ["rupeesselected"] = "RUPEES_SELECTED",
            ["rupeestotal"] = "RUPEES_TOTAL",
            ["timeuntilmooncrash"] = "TIME_UNTIL_MOON_CRASH",
            ["inputdogbet"] = "INPUT_DOGGY_RACETRACK_BET",
            ["inputbombercode"] = "INPUT_BOMBER_CODE",
            ["pausemenu"] = "PAUSE_MENU",
            ["timespeed"] = "TIME_SPEED",
            ["owlwarp"] = "OWL_WARP",
            ["inputlotterycode"] = "INPUT_LOTTERY_CODE",
            ["spiderhousemaskcode"] = "SPIDER_HOUSE_MASK_CODE",
            ["fairieswoodfall"] = "STRAY_FAIRIES_LEFT_WOODFALL",
            ["fairiessnowhead"] = "STRAY_FAIRIES_LEFT_SNOWHEAD",
            ["fairiesgreatbay"] = "STRAY_FAIRIES_LEFT_GREAT_BAY",
            ["fairiesstonetower"] = "STRAY_FAIRIES_LEFT_STONE_TOWER",
            ["pointsboatarchery"] = "POINTS_BOAT_ARCHERY",
            ["lotterycode"] = "LOTTERY_CODE",
            ["lotterycodeguess"] = "LOTTERY_CODE_GUESS",
            ["helditemprice"] = "HELD_ITEM_PRICE",
            ["bombercode"] = "BOMBER_CODE",
            ["event2"] = "EVENT2",
            ["spiderhousemask1"] = "SPIDER_HOUSE_MASK_CODE_1",
            ["spiderhousemask2"] = "SPIDER_HOUSE_MASK_CODE_2",
            ["spiderhousemask3"] = "SPIDER_HOUSE_MASK_CODE_3",
            ["spiderhousemask4"] = "SPIDER_HOUSE_MASK_CODE_4",
            ["spiderhousemask5"] = "SPIDER_HOUSE_MASK_CODE_5",
            ["spiderhousemask6"] = "SPIDER_HOUSE_MASK_CODE_6",
            ["hoursuntilmooncrash"] = "HOURS_UNTIL_MOON_CRASH",
            ["timeuntilnewday"] = "TIME_UNTIL_NEW_DAY",
            ["hsbankrupees"] = "HS_POINTS_BANK_RUPEES",
            ["hspointsunk1"] = "HS_POINTS_UNK_1",
            ["hsfishingpoints"] = "HS_POINTS_FISHING",
            ["hsboatarcherytime"] = "HS_TIME_BOAT_ARCHERY",
            ["hshorseballoontime"] = "HS_TIME_HORSE_BACK_BALLOON",
            ["hslotterytime"] = "HS_TIME_LOTTERY_GUESS",
            ["hstownshooting"] = "HS_TOWN_SHOOTING_GALLERY",
            ["hsunk1"] = "HS_UNK_1",
            ["hsunk3lower"] = "HS_UNK_3_LOWER",
            ["hshorseballoon"] = "HS_HORSE_BACK_BALLOON",
            ["hsdekuplayground1"] = "HS_DEKU_PLAYGROUND_DAY_1",
            ["hsdekuplayground2"] = "HS_DEKU_PLAYGROUND_DAY_2",
            ["hsdekuplayground3"] = "HS_DEKU_PLAYGROUND_DAY_3",
            ["dekuplaygroundname1"] = "DEKU_PLAYGROUND_NAME_DAY_1",
            ["dekuplaygroundname2"] = "DEKU_PLAYGROUND_NAME_DAY_2",
            ["dekuplaygroundname3"] = "DEKU_PLAYGROUND_NAME_DAY_3",
        };

    public static string Export(
        IReadOnlyList<MessageEntry> entries,
        MessageEncodingProfile? encodingProfile = null,
        MessageEncodingProfile? headerEncodingProfile = null)
    {
        encodingProfile ??= MessageEncodingProfile.MajorasMask;
        headerEncodingProfile ??= MessageEncodingProfile.MajorasMaskOriginal;
        var sb = new StringBuilder();
        foreach (MessageEntry entry in entries.Where(entry => !IsBuildGeneratedHelperEntry(entry)).OrderBy(entry => entry.Id))
        {
            if (MmStaffCreditsCHeaderExporter.IsEntry(entry))
            {
                MmStaffCreditsCHeaderExporter.Append(sb, entry);
                continue;
            }

            MajorasMaskMessageMetadata metadata = GetMetadata(entry);
            int tableType = (metadata.TableTypePosition >> 4) & 0x0f;
            int tablePosition = metadata.TableTypePosition & 0x0f;
            ushort textBoxProperties = BuildTextBoxProperties(metadata, entry);

            sb.Append(CultureInvariant($"DEFINE_MESSAGE(0x{entry.Id:X4}, 0x{tableType:X2}, 0x{tablePosition:X2},"));
            sb.AppendLine();
            sb.AppendLine("MSG(");
            sb.Append(CultureInvariant(
                $"HEADER(0x{textBoxProperties:X4}, 0x{metadata.IconId:X2}, 0x{metadata.NextTextId:X4}, {FormatSignedWord(metadata.FirstChoicePrice)}, {FormatSignedWord(metadata.SecondChoicePrice)}, 0x{metadata.Unknown:X4})"));
            string body = string.IsNullOrEmpty(entry.Text)
                ? string.Empty
                : FormatBody(entry.Text, encodingProfile, headerEncodingProfile);
            if (body.Length > 0)
            {
                sb.AppendLine();
                sb.Append(body);
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine();
            }

            sb.AppendLine(")");
            sb.AppendLine(")");
            sb.AppendLine();
        }

        string result = sb.ToString().ReplaceLineEndings("\n");
        return result.EndsWith("\n\n", StringComparison.Ordinal)
            ? result[..^1]
            : result;
    }

    private static bool IsBuildGeneratedHelperEntry(MessageEntry entry)
        => entry.Id is FontOrderCodec.MessageId or MmMessageTableCodec.DebuggerEndMessageId;

    private static MajorasMaskMessageMetadata GetMetadata(MessageEntry entry)
    {
        if (entry.CodecMetadata is MajorasMaskMessageMetadata metadata)
        {
            return metadata;
        }

        ushort properties = (ushort)(((entry.Type & 0x0f) << 8) | ((entry.Position & 0x0f) << 4));
        return new MajorasMaskMessageMetadata(
            TableTypePosition: 0,
            TextBoxProperties: properties,
            IconId: 0xfe,
            NextTextId: 0xffff,
            FirstChoicePrice: 0xffff,
            SecondChoicePrice: 0xffff,
            Unknown: 0xffff);
    }

    private static ushort BuildTextBoxProperties(MajorasMaskMessageMetadata metadata, MessageEntry entry)
    {
        return (ushort)((metadata.TextBoxProperties & 0xf00f)
            | ((entry.Type & 0x0f) << 8)
            | ((entry.Position & 0x0f) << 4));
    }

    private static string FormatSignedWord(ushort value)
        => CultureInvariant($"0x{value:X4}");

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
