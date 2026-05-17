using System.Collections.Generic;

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

    public static readonly IReadOnlyDictionary<string, int> HeaderHighscoreValues = DictionaryMaps.Reverse(HeaderHighscores, StringComparer.OrdinalIgnoreCase);

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
