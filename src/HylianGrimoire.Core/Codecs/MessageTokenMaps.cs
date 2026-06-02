namespace HylianGrimoire.Codecs;

public static class MessageTokenMaps
{
    public static readonly IReadOnlyDictionary<byte, string> ButtonTags = new Dictionary<byte, string>
    {
        { 0x9f, "A-button" }, { 0xa0, "B-button" }, { 0xa1, "C-button" }, { 0xa2, "L-button" },
        { 0xa3, "R-button" }, { 0xa4, "Z-button" }, { 0xa5, "C-up" }, { 0xa6, "C-down" },
        { 0xa7, "C-left" }, { 0xa8, "C-right" }, { 0xa9, "Triangle" }, { 0xaa, "Stick" },
    };

    public static readonly IReadOnlyDictionary<string, byte> ButtonBytes = DictionaryMaps.Reverse(ButtonTags, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<byte, string> ColorTags = new Dictionary<byte, string>
    {
        { 0x40, "default" }, { 0x41, "red" }, { 0x42, "green" }, { 0x43, "blue" },
        { 0x44, "lightblue" }, { 0x45, "purple" }, { 0x46, "yellow" }, { 0x47, "black" },
    };

    public static readonly IReadOnlyDictionary<string, byte> ColorBytes = DictionaryMaps.Reverse(ColorTags, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<byte, string> CommandTags = new Dictionary<byte, string>
    {
        { 0x0a, "shop" }, { 0x0b, "event" }, { 0x0d, "waitbutton" }, { 0x0f, "name" },
        { 0x08, "quicktexton" }, { 0x09, "quicktextoff" }, { 0x10, "ocarina" },
        { 0x16, "marathontime" }, { 0x17, "racetime" }, { 0x18, "points" }, { 0x19, "skulltulas" },
        { 0x1a, "unskippable" }, { 0x1b, "twochoice" }, { 0x1c, "threechoice" },
        { 0x1d, "fishinfo" }, { 0x1f, "time" }, { 0x04, "break" }, { 0xfd, "center" },
    };

    public static readonly IReadOnlyDictionary<string, byte> CommandBytes = DictionaryMaps.Reverse(CommandTags, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<byte, string> HighscoreTags = new Dictionary<byte, string>
    {
        { 0x00, "archery" },
        { 0x01, "poe" },
        { 0x02, "fish" },
        { 0x03, "horserace" },
        { 0x04, "marathon" },
    };

    public static readonly IReadOnlyDictionary<string, byte> HighscoreBytes = DictionaryMaps.Reverse(HighscoreTags, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<int, string> HeaderBoxTypes = new Dictionary<int, string>
    {
        { 0, "TEXTBOX_TYPE_BLACK" },
        { 1, "TEXTBOX_TYPE_WOODEN" },
        { 2, "TEXTBOX_TYPE_BLUE" },
        { 3, "TEXTBOX_TYPE_OCARINA" },
        { 4, "TEXTBOX_TYPE_NONE_BOTTOM" },
        { 5, "TEXTBOX_TYPE_NONE_NO_SHADOW" },
        { 0xB, "TEXTBOX_TYPE_CREDITS" },
    };

    public static readonly IReadOnlyDictionary<string, int> HeaderBoxTypeValues = DictionaryMaps.Reverse(HeaderBoxTypes, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<int, string> HeaderBoxPositions = new Dictionary<int, string>
    {
        { 0, "TEXTBOX_POS_VARIABLE" },
        { 1, "TEXTBOX_POS_TOP" },
        { 2, "TEXTBOX_POS_MIDDLE" },
        { 3, "TEXTBOX_POS_BOTTOM" },
    };

    public static readonly IReadOnlyDictionary<string, int> HeaderBoxPositionValues = DictionaryMaps.Reverse(HeaderBoxPositions, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<int, string> HeaderColors = new Dictionary<int, string>
    {
        { 0x40, "DEFAULT" },
        { 0x41, "RED" },
        { 0x42, "ADJUSTABLE" },
        { 0x43, "BLUE" },
        { 0x44, "LIGHTBLUE" },
        { 0x45, "PURPLE" },
        { 0x46, "YELLOW" },
        { 0x47, "BLACK" },
    };

    public static readonly IReadOnlyDictionary<string, string> HeaderColorTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["DEFAULT"] = "default",
        ["RED"] = "red",
        ["ADJUSTABLE"] = "green",
        ["GREEN"] = "green",
        ["BLUE"] = "blue",
        ["LIGHTBLUE"] = "lightblue",
        ["PURPLE"] = "purple",
        ["YELLOW"] = "yellow",
        ["BLACK"] = "black",
    };

    public static readonly IReadOnlyDictionary<int, string> HeaderHighscores = new Dictionary<int, string>
    {
        { 0, "HS_HORSE_ARCHERY" },
        { 1, "HS_POE_POINTS" },
        { 2, "HS_LARGEST_FISH" },
        { 3, "HS_HORSE_RACE" },
        { 4, "HS_MARATHON" },
        { 5, "HS_UNK_05" },
        { 6, "HS_DAMPE_RACE" },
    };

    public static readonly IReadOnlyDictionary<int, string> ModernHeaderHighscores = new Dictionary<int, string>
    {
        { 0, "HS_HBA" },
        { 1, "HS_POE_POINTS" },
        { 2, "HS_FISHING" },
        { 3, "HS_HORSE_RACE" },
        { 4, "HS_MARATHON" },
        { 5, "HS_UNK_05" },
        { 6, "HS_DAMPE_RACE" },
    };

    public static readonly IReadOnlyDictionary<string, int> HeaderHighscoreValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["HS_HORSE_ARCHERY"] = 0,
        ["HS_HBA"] = 0,
        ["HS_POE_POINTS"] = 1,
        ["HS_LARGEST_FISH"] = 2,
        ["HS_FISHING"] = 2,
        ["HS_HORSE_RACE"] = 3,
        ["HS_MARATHON"] = 4,
        ["HS_UNK_05"] = 5,
        ["HS_DAMPE_RACE"] = 6,
    };

    public static readonly IReadOnlyDictionary<int, string> HeaderItems = new Dictionary<int, string>
    {
        { 0x00, "ITEM_DEKU_STICK" }, { 0x01, "ITEM_DEKU_NUT" }, { 0x02, "ITEM_BOMB" }, { 0x03, "ITEM_BOW" },
        { 0x04, "ITEM_ARROW_FIRE" }, { 0x05, "ITEM_DINS_FIRE" }, { 0x06, "ITEM_SLINGSHOT" }, { 0x07, "ITEM_OCARINA_FAIRY" },
        { 0x08, "ITEM_OCARINA_OF_TIME" }, { 0x09, "ITEM_BOMBCHU" }, { 0x0A, "ITEM_HOOKSHOT" }, { 0x0B, "ITEM_LONGSHOT" },
        { 0x0C, "ITEM_ARROW_ICE" }, { 0x0D, "ITEM_FARORES_WIND" }, { 0x0E, "ITEM_BOOMERANG" }, { 0x0F, "ITEM_LENS_OF_TRUTH" },
        { 0x10, "ITEM_MAGIC_BEAN" }, { 0x11, "ITEM_HAMMER" }, { 0x12, "ITEM_ARROW_LIGHT" }, { 0x13, "ITEM_NAYRUS_LOVE" },
        { 0x14, "ITEM_BOTTLE_EMPTY" }, { 0x15, "ITEM_BOTTLE_POTION_RED" }, { 0x16, "ITEM_BOTTLE_POTION_GREEN" },
        { 0x17, "ITEM_BOTTLE_POTION_BLUE" }, { 0x18, "ITEM_BOTTLE_FAIRY" }, { 0x19, "ITEM_BOTTLE_FISH" },
        { 0x1A, "ITEM_BOTTLE_MILK_FULL" }, { 0x1B, "ITEM_BOTTLE_RUTOS_LETTER" }, { 0x1C, "ITEM_BOTTLE_BLUE_FIRE" },
        { 0x1D, "ITEM_BOTTLE_BUG" }, { 0x1E, "ITEM_BOTTLE_BIG_POE" }, { 0x1F, "ITEM_BOTTLE_MILK_HALF" },
        { 0x20, "ITEM_BOTTLE_POE" }, { 0x21, "ITEM_WEIRD_EGG" }, { 0x22, "ITEM_CHICKEN" }, { 0x23, "ITEM_ZELDAS_LETTER" },
        { 0x24, "ITEM_MASK_KEATON" }, { 0x25, "ITEM_MASK_SKULL" }, { 0x26, "ITEM_MASK_SPOOKY" },
        { 0x27, "ITEM_MASK_BUNNY_HOOD" }, { 0x28, "ITEM_MASK_GORON" }, { 0x29, "ITEM_MASK_ZORA" },
        { 0x2A, "ITEM_MASK_GERUDO" }, { 0x2B, "ITEM_MASK_TRUTH" }, { 0x2C, "ITEM_SOLD_OUT" },
        { 0x2D, "ITEM_POCKET_EGG" }, { 0x2E, "ITEM_POCKET_CUCCO" }, { 0x2F, "ITEM_COJIRO" },
        { 0x30, "ITEM_ODD_MUSHROOM" }, { 0x31, "ITEM_ODD_POTION" }, { 0x32, "ITEM_POACHERS_SAW" },
        { 0x33, "ITEM_BROKEN_GORONS_SWORD" }, { 0x34, "ITEM_PRESCRIPTION" }, { 0x35, "ITEM_EYEBALL_FROG" },
        { 0x36, "ITEM_EYE_DROPS" }, { 0x37, "ITEM_CLAIM_CHECK" }, { 0x38, "ITEM_BOW_FIRE" }, { 0x39, "ITEM_BOW_ICE" },
        { 0x3A, "ITEM_BOW_LIGHT" }, { 0x3B, "ITEM_SWORD_KOKIRI" }, { 0x3C, "ITEM_SWORD_MASTER" },
        { 0x3D, "ITEM_SWORD_BIGGORON" }, { 0x3E, "ITEM_SHIELD_DEKU" }, { 0x3F, "ITEM_SHIELD_HYLIAN" },
        { 0x40, "ITEM_SHIELD_MIRROR" }, { 0x41, "ITEM_TUNIC_KOKIRI" }, { 0x42, "ITEM_TUNIC_GORON" },
        { 0x43, "ITEM_TUNIC_ZORA" }, { 0x44, "ITEM_BOOTS_KOKIRI" }, { 0x45, "ITEM_BOOTS_IRON" },
        { 0x46, "ITEM_BOOTS_HOVER" }, { 0x47, "ITEM_BULLET_BAG_30" }, { 0x48, "ITEM_BULLET_BAG_40" },
        { 0x49, "ITEM_BULLET_BAG_50" }, { 0x4A, "ITEM_QUIVER_30" }, { 0x4B, "ITEM_QUIVER_40" },
        { 0x4C, "ITEM_QUIVER_50" }, { 0x4D, "ITEM_BOMB_BAG_20" }, { 0x4E, "ITEM_BOMB_BAG_30" },
        { 0x4F, "ITEM_BOMB_BAG_40" }, { 0x50, "ITEM_STRENGTH_GORONS_BRACELET" },
        { 0x51, "ITEM_STRENGTH_SILVER_GAUNTLETS" }, { 0x52, "ITEM_STRENGTH_GOLD_GAUNTLETS" },
        { 0x53, "ITEM_SCALE_SILVER" }, { 0x54, "ITEM_SCALE_GOLDEN" }, { 0x55, "ITEM_GIANTS_KNIFE" },
        { 0x56, "ITEM_ADULTS_WALLET" }, { 0x57, "ITEM_GIANTS_WALLET" }, { 0x58, "ITEM_DEKU_SEEDS" },
        { 0x59, "ITEM_FISHING_POLE" }, { 0x66, "ITEM_MEDALLION_FOREST" }, { 0x67, "ITEM_MEDALLION_FIRE" },
        { 0x68, "ITEM_MEDALLION_WATER" }, { 0x69, "ITEM_MEDALLION_SPIRIT" }, { 0x6A, "ITEM_MEDALLION_SHADOW" },
        { 0x6B, "ITEM_MEDALLION_LIGHT" }, { 0x6C, "ITEM_KOKIRI_EMERALD" }, { 0x6D, "ITEM_GORON_RUBY" },
        { 0x6E, "ITEM_ZORA_SAPPHIRE" }, { 0x6F, "ITEM_STONE_OF_AGONY" }, { 0x70, "ITEM_GERUDOS_CARD" },
        { 0x71, "ITEM_SKULL_TOKEN" }, { 0x72, "ITEM_HEART_CONTAINER" }, { 0x73, "ITEM_HEART_PIECE" },
        { 0x74, "ITEM_DUNGEON_BOSS_KEY" }, { 0x75, "ITEM_DUNGEON_COMPASS" }, { 0x76, "ITEM_DUNGEON_MAP" },
        { 0x77, "ITEM_SMALL_KEY" }, { 0x78, "ITEM_MAGIC_JAR_SMALL" }, { 0x79, "ITEM_MAGIC_JAR_BIG" },
    };

    public static readonly IReadOnlyDictionary<string, int> HeaderItemValues = DictionaryMaps.Reverse(HeaderItems, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<int, string> HeaderBackgrounds = new Dictionary<int, string>
    {
        { 0, "X_LEFT" },
        { 1, "X_RIGHT" },
    };

    public static readonly IReadOnlyDictionary<string, int> HeaderBackgroundValues = DictionaryMaps.Reverse(HeaderBackgrounds, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<int, string> HeaderBackgroundForegroundColors = new Dictionary<int, string>
    {
        { 0, "WHITE" },
        { 1, "DARK_RED" },
        { 2, "ORANGE" },
        { 3, "WHITE_3" },
        { 4, "WHITE_4" },
        { 5, "WHITE_5" },
        { 6, "WHITE_6" },
        { 7, "WHITE_7" },
    };

    public static readonly IReadOnlyDictionary<string, int> HeaderBackgroundForegroundColorValues =
        DictionaryMaps.Reverse(HeaderBackgroundForegroundColors, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<int, string> HeaderBackgroundColors = new Dictionary<int, string>
    {
        { 0, "BLACK" },
        { 1, "GOLD" },
        { 2, "BLACK_2" },
        { 3, "BLACK_3" },
    };

    public static readonly IReadOnlyDictionary<string, int> HeaderBackgroundColorValues =
        DictionaryMaps.Reverse(HeaderBackgroundColors, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<int, string> HeaderBackgroundYOffsets = new Dictionary<int, string>
    {
        { 0, "1" },
        { 1, "2" },
    };

    public static readonly IReadOnlyDictionary<string, int> HeaderBackgroundYOffsetValues =
        DictionaryMaps.Reverse(HeaderBackgroundYOffsets, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<string, string> HeaderCommandTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["QUICKTEXT_ENABLE"] = "quicktexton",
        ["QUICKTEXT_DISABLE"] = "quicktextoff",
        ["PERSISTENT"] = "shop",
        ["EVENT"] = "event",
        ["AWAIT_BUTTON_PRESS"] = "waitbutton",
        ["NAME"] = "name",
        ["OCARINA"] = "ocarina",
        ["MARATHON_TIME"] = "marathontime",
        ["RACE_TIME"] = "racetime",
        ["POINTS"] = "points",
        ["TOKENS"] = "skulltulas",
        ["UNSKIPPABLE"] = "unskippable",
        ["TWO_CHOICE"] = "twochoice",
        ["THREE_CHOICE"] = "threechoice",
        ["FISH_INFO"] = "fishinfo",
        ["TIME"] = "time",
    };

}
