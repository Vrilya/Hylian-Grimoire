using HylianGrimoire.Codecs;

namespace HylianGrimoire.Headers;

public static partial class CHeaderImporter
{
    private static readonly string[] MessageMacroNames =
    [
        "DEFINE_MESSAGE_FFFC",
        "DEFINE_MESSAGE_NES",
        "DEFINE_MESSAGE_JPN",
        "DEFINE_MESSAGE",
    ];

    private static readonly IReadOnlyDictionary<string, int> BoxTypes = MessageTokenMaps.HeaderBoxTypeValues;
    private static readonly IReadOnlyDictionary<string, int> BoxPositions = MessageTokenMaps.HeaderBoxPositionValues;
    private static readonly IReadOnlyDictionary<string, string> NoArgTags = MessageTokenMaps.HeaderCommandTags;
    private static readonly IReadOnlyDictionary<string, string> Colors = MessageTokenMaps.HeaderColorTags;
    private static readonly IReadOnlyDictionary<string, int> Highscores = MessageTokenMaps.HeaderHighscoreValues;
    private static readonly IReadOnlyDictionary<string, int> ItemIds = MessageTokenMaps.HeaderItemValues;
    private static readonly IReadOnlyDictionary<string, int> Backgrounds = MessageTokenMaps.HeaderBackgroundValues;
    private static readonly IReadOnlyDictionary<string, int> BackgroundForegroundColors = MessageTokenMaps.HeaderBackgroundForegroundColorValues;
    private static readonly IReadOnlyDictionary<string, int> BackgroundColors = MessageTokenMaps.HeaderBackgroundColorValues;
    private static readonly IReadOnlyDictionary<string, int> BackgroundYOffsets = MessageTokenMaps.HeaderBackgroundYOffsetValues;
}
