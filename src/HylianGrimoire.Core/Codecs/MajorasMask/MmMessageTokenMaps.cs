namespace HylianGrimoire.Codecs.MajorasMask;

internal static class MmMessageTokenMaps
{
    public static readonly IReadOnlyDictionary<byte, string> ColorTags = new Dictionary<byte, string>
    {
        [0x00] = "default",
        [0x01] = "red",
        [0x02] = "green",
        [0x03] = "blue",
        [0x04] = "yellow",
        [0x05] = "lightblue",
        [0x06] = "pink",
        [0x07] = "silver",
        [0x08] = "orange",
    };

    public static readonly IReadOnlyDictionary<string, byte> ColorBytes =
        DictionaryMaps.Reverse(ColorTags, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<byte, string> NoArgumentTags = new Dictionary<byte, string>
    {
        [0x0a] = "textspeed",
        [0x0b] = "hsboatarchery",
        [0x0c] = "strayfairies",
        [0x0d] = "tokens",
        [0x0e] = "pointstens",
        [0x0f] = "pointsthousands",
        [0x10] = "break",
        [0x12] = "break2",
        [0x13] = "carriagereturn",
        [0x15] = "continue",
        [0x16] = "name",
        [0x17] = "quicktexton",
        [0x18] = "quicktextoff",
        [0x19] = "event",
        [0x1a] = "persistent",
        [0xc1] = "background",
        [0xc2] = "twochoice",
        [0xc3] = "threechoice",
        [0xc4] = "timerpostman",
        [0xc5] = "timerminigame1",
        [0xc6] = "timer2",
        [0xc7] = "timermooncrash",
        [0xc8] = "timerminigame2",
        [0xc9] = "timerhazard",
        [0xca] = "time",
        [0xcb] = "chestflags",
        [0xcc] = "inputbank",
        [0xcd] = "rupeesselected",
        [0xce] = "rupeestotal",
        [0xcf] = "timeuntilmooncrash",
        [0xd0] = "inputdogbet",
        [0xd1] = "inputbombercode",
        [0xd2] = "pausemenu",
        [0xd3] = "timespeed",
        [0xd4] = "owlwarp",
        [0xd5] = "inputlotterycode",
        [0xd6] = "spiderhousemaskcode",
        [0xd7] = "fairieswoodfall",
        [0xd8] = "fairiessnowhead",
        [0xd9] = "fairiesgreatbay",
        [0xda] = "fairiesstonetower",
        [0xdb] = "pointsboatarchery",
        [0xdc] = "lotterycode",
        [0xdd] = "lotterycodeguess",
        [0xde] = "helditemprice",
        [0xdf] = "bombercode",
        [0xe0] = "event2",
        [0xe1] = "spiderhousemask1",
        [0xe2] = "spiderhousemask2",
        [0xe3] = "spiderhousemask3",
        [0xe4] = "spiderhousemask4",
        [0xe5] = "spiderhousemask5",
        [0xe6] = "spiderhousemask6",
        [0xe7] = "hoursuntilmooncrash",
        [0xe8] = "timeuntilnewday",
        [0xf0] = "hsbankrupees",
        [0xf1] = "hspointsunk1",
        [0xf2] = "hsfishingpoints",
        [0xf3] = "hsboatarcherytime",
        [0xf4] = "hshorseballoontime",
        [0xf5] = "hslotterytime",
        [0xf6] = "hstownshooting",
        [0xf7] = "hsunk1",
        [0xf8] = "hsunk3lower",
        [0xf9] = "hshorseballoon",
        [0xfa] = "hsdekuplayground1",
        [0xfb] = "hsdekuplayground2",
        [0xfc] = "hsdekuplayground3",
        [0xfd] = "dekuplaygroundname1",
        [0xfe] = "dekuplaygroundname2",
        [0xff] = "dekuplaygroundname3",
    };

    public static readonly IReadOnlyDictionary<string, byte> NoArgumentBytes = BuildNoArgumentBytes();

    public static readonly IReadOnlyDictionary<byte, string> ButtonTags = new Dictionary<byte, string>
    {
        [0xb0] = "A-button",
        [0xb1] = "B-button",
        [0xb2] = "C-button",
        [0xb3] = "L-button",
        [0xb4] = "R-button",
        [0xb5] = "Z-button",
        [0xb6] = "C-up",
        [0xb7] = "C-down",
        [0xb8] = "C-left",
        [0xb9] = "C-right",
        [0xba] = "Z-target",
        [0xbb] = "Control-Pad",
    };

    public static readonly IReadOnlyDictionary<string, byte> ButtonBytes = BuildButtonBytes();

    public static readonly IReadOnlyDictionary<byte, string> OneByteArgumentTags = new Dictionary<byte, string>
    {
        [0x14] = "shift",
    };

    public static readonly IReadOnlyDictionary<string, byte> OneByteArgumentBytes = BuildOneByteArgumentBytes();

    public static readonly IReadOnlyDictionary<byte, string> TwoByteArgumentTags = new Dictionary<byte, string>
    {
        [0x1b] = "breakdelay",
        [0x1c] = "fade",
        [0x1d] = "fadeskippable",
        [0x1e] = "sfx",
        [0x1f] = "delay",
    };

    public static readonly IReadOnlyDictionary<string, byte> TwoByteArgumentBytes = BuildTwoByteArgumentBytes();

    private static IReadOnlyDictionary<string, byte> BuildNoArgumentBytes()
    {
        var map = DictionaryMaps.Reverse(NoArgumentTags, StringComparer.OrdinalIgnoreCase).ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);

        AddAlias("shop", 0x1a);
        AddAlias("boatarchery", 0x0b);
        AddAlias("dogbet", 0xd0);
        AddAlias("bombercodeinput", 0xd1);
        AddAlias("lotterycodeinput", 0xd5);
        AddAlias("fairstonetower", 0xda);
        AddAlias("boatarcherypoints", 0xdb);
        AddAlias("lotteryguess", 0xdd);
        AddAlias("bankrupees", 0xf0);
        AddAlias("pointsunk1", 0xf1);
        AddAlias("fishingpoints", 0xf2);
        AddAlias("boatarcherytime", 0xf3);
        AddAlias("horseballoontime", 0xf4);
        AddAlias("lotterytime", 0xf5);
        AddAlias("townshooting", 0xf6);
        AddAlias("horseballoon", 0xf9);
        AddAlias("dekuplayground1", 0xfa);
        AddAlias("dekuplayground2", 0xfb);
        AddAlias("dekuplayground3", 0xfc);

        AddDecompAlias("HS_BOAT_ARCHERY", 0x0b);
        AddDecompAlias("STRAY_FAIRIES", 0x0c);
        AddDecompAlias("TOKENS", 0x0d);
        AddDecompAlias("POINTS_TENS", 0x0e);
        AddDecompAlias("POINTS_THOUSANDS", 0x0f);
        AddDecompAlias("BOX_BREAK", 0x10);
        AddDecompAlias("BOX_BREAK2", 0x12);
        AddDecompAlias("CARRIAGE_RETURN", 0x13);
        AddDecompAlias("CONTINUE", 0x15);
        AddDecompAlias("NAME", 0x16);
        AddDecompAlias("QUICKTEXT_ENABLE", 0x17);
        AddDecompAlias("QUICKTEXT_DISABLE", 0x18);
        AddDecompAlias("EVENT", 0x19);
        AddDecompAlias("PERSISTENT", 0x1a);
        AddDecompAlias("BACKGROUND", 0xc1);
        AddDecompAlias("TWO_CHOICE", 0xc2);
        AddDecompAlias("THREE_CHOICE", 0xc3);
        AddDecompAlias("TIMER_POSTMAN", 0xc4);
        AddDecompAlias("TIMER_MINIGAME_1", 0xc5);
        AddDecompAlias("TIMER_2", 0xc6);
        AddDecompAlias("TIMER_MOON_CRASH", 0xc7);
        AddDecompAlias("TIMER_MINIGAME_2", 0xc8);
        AddDecompAlias("TIMER_ENV_HAZARD", 0xc9);
        AddDecompAlias("TIMER_TIMER_ENV_HAZARD", 0xc9);
        AddDecompAlias("TIME", 0xca);
        AddDecompAlias("CHEST_FLAGS", 0xcb);
        AddDecompAlias("INPUT_BANK", 0xcc);
        AddDecompAlias("RUPEES_SELECTED", 0xcd);
        AddDecompAlias("RUPEES_TOTAL", 0xce);
        AddDecompAlias("TIME_UNTIL_MOON_CRASH", 0xcf);
        AddDecompAlias("INPUT_DOGGY_RACETRACK_BET", 0xd0);
        AddDecompAlias("INPUT_BOMBER_CODE", 0xd1);
        AddDecompAlias("PAUSE_MENU", 0xd2);
        AddDecompAlias("TIME_SPEED", 0xd3);
        AddDecompAlias("OWL_WARP", 0xd4);
        AddDecompAlias("INPUT_LOTTERY_CODE", 0xd5);
        AddDecompAlias("SPIDER_HOUSE_MASK_CODE", 0xd6);
        AddDecompAlias("STRAY_FAIRIES_LEFT_WOODFALL", 0xd7);
        AddDecompAlias("STRAY_FAIRIES_LEFT_SNOWHEAD", 0xd8);
        AddDecompAlias("STRAY_FAIRIES_LEFT_GREAT_BAY", 0xd9);
        AddDecompAlias("STRAY_FAIRIES_LEFT_STONE_TOWER", 0xda);
        AddDecompAlias("POINTS_BOAT_ARCHERY", 0xdb);
        AddDecompAlias("LOTTERY_CODE", 0xdc);
        AddDecompAlias("LOTTERY_CODE_GUESS", 0xdd);
        AddDecompAlias("HELD_ITEM_PRICE", 0xde);
        AddDecompAlias("BOMBER_CODE", 0xdf);
        AddDecompAlias("EVENT_2", 0xe0);
        AddDecompAlias("EVENT2", 0xe0);
        AddDecompAlias("TEXT_SPEED", 0x0a);
        AddDecompAlias("SPIDER_HOUSE_MASK_1", 0xe1);
        AddDecompAlias("SPIDER_HOUSE_MASK_2", 0xe2);
        AddDecompAlias("SPIDER_HOUSE_MASK_3", 0xe3);
        AddDecompAlias("SPIDER_HOUSE_MASK_4", 0xe4);
        AddDecompAlias("SPIDER_HOUSE_MASK_5", 0xe5);
        AddDecompAlias("SPIDER_HOUSE_MASK_6", 0xe6);
        AddDecompAlias("SPIDER_HOUSE_MASK_CODE_1", 0xe1);
        AddDecompAlias("SPIDER_HOUSE_MASK_CODE_2", 0xe2);
        AddDecompAlias("SPIDER_HOUSE_MASK_CODE_3", 0xe3);
        AddDecompAlias("SPIDER_HOUSE_MASK_CODE_4", 0xe4);
        AddDecompAlias("SPIDER_HOUSE_MASK_CODE_5", 0xe5);
        AddDecompAlias("SPIDER_HOUSE_MASK_CODE_6", 0xe6);
        AddDecompAlias("HOURS_UNTIL_MOON_CRASH", 0xe7);
        AddDecompAlias("TIME_UNTIL_NEW_DAY", 0xe8);
        AddDecompAlias("HS_POINTS_BANK_RUPEES", 0xf0);
        AddDecompAlias("HS_POINTS_UNK_1", 0xf1);
        AddDecompAlias("HS_POINTS_FISHING", 0xf2);
        AddDecompAlias("HS_TIME_BOAT_ARCHERY", 0xf3);
        AddDecompAlias("HS_TIME_HORSE_BACK_BALLOON", 0xf4);
        AddDecompAlias("HS_TIME_LOTTERY_GUESS", 0xf5);
        AddDecompAlias("HS_TOWN_SHOOTING_GALLERY", 0xf6);
        AddDecompAlias("HS_UNK_1", 0xf7);
        AddDecompAlias("HS_UNK_3_LOWER", 0xf8);
        AddDecompAlias("HS_HORSE_BACK_BALLOON", 0xf9);
        AddDecompAlias("HS_DEKU_PLAYGROUND_DAY_1", 0xfa);
        AddDecompAlias("HS_DEKU_PLAYGROUND_DAY_2", 0xfb);
        AddDecompAlias("HS_DEKU_PLAYGROUND_DAY_3", 0xfc);
        AddDecompAlias("DEKU_PLAYGROUND_NAME_DAY_1", 0xfd);
        AddDecompAlias("DEKU_PLAYGROUND_NAME_DAY_2", 0xfe);
        AddDecompAlias("DEKU_PLAYGROUND_NAME_DAY_3", 0xff);

        return map;

        void AddAlias(string alias, byte value) => map[alias] = value;

        void AddDecompAlias(string alias, byte value)
        {
            AddAlias(alias, value);
            AddAlias(alias.Replace("_", string.Empty, StringComparison.Ordinal), value);
        }
    }

    private static IReadOnlyDictionary<string, byte> BuildButtonBytes()
    {
        var map = DictionaryMaps.Reverse(ButtonTags, StringComparer.OrdinalIgnoreCase).ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);

        AddAlias("btn_a", 0xb0);
        AddAlias("btn_b", 0xb1);
        AddAlias("btn_c", 0xb2);
        AddAlias("btn_l", 0xb3);
        AddAlias("btn_r", 0xb4);
        AddAlias("btn_z", 0xb5);
        AddAlias("btn_cup", 0xb6);
        AddAlias("btn_cdown", 0xb7);
        AddAlias("btn_cleft", 0xb8);
        AddAlias("btn_cright", 0xb9);
        AddAlias("z_target", 0xba);
        AddAlias("control_pad", 0xbb);

        return map;

        void AddAlias(string alias, byte value) => map[alias] = value;
    }

    private static IReadOnlyDictionary<string, byte> BuildOneByteArgumentBytes()
    {
        return new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
        {
            ["shift"] = 0x14,
        };
    }

    private static IReadOnlyDictionary<string, byte> BuildTwoByteArgumentBytes()
    {
        var map = DictionaryMaps.Reverse(TwoByteArgumentTags, StringComparer.OrdinalIgnoreCase).ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);

        map["box_break_delayed"] = 0x1b;
        map["boxbreakdelayed"] = 0x1b;
        map["fade_skippable"] = 0x1d;
        return map;
    }
}
