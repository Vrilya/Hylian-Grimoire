using System.Text.RegularExpressions;
using HylianGrimoire.Games;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static partial class MapPositionNameTextureCatalog
{
    public const int Width = 80;
    public const int Height = 32;

    private const string MapNameGroup = "textures/map_name_static";
    private static readonly IReadOnlyDictionary<string, string> DisplayTextOverrides = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["DeathMountainCrater"] = "Death Mountain\r\nCrater",
        ["DeathMountainTrail"] = "Death Mountain\r\nTrail",
        ["DesertColossus"] = "Desert\r\nColossus",
        ["GanonsCastle"] = "Ganon's\r\nCastle",
        ["GerudoValley"] = "Gerudo\r\nValley",
        ["GerudosFortress"] = "Gerudo's\r\nFortress",
        ["GoronCity"] = "Goron City",
        ["Graveyard"] = "Graveyard",
        ["HauntedWasteland"] = "Haunted\r\nWasteland",
        ["HyruleCastle"] = "Hyrule\r\nCastle",
        ["HyruleField"] = "Hyrule Field",
        ["KakarikoVillage"] = "Kakariko\r\nVillage",
        ["KokiriForest"] = "Kokiri Forest",
        ["LakeHylia"] = "Lake Hylia",
        ["LonLonRanch"] = "Lon Lon\r\nRanch",
        ["LostWoods"] = "Lost Woods",
        ["Market"] = "Market",
        ["QuestionMark"] = "?",
        ["SacredForestMeadow"] = "Sacred Forest\r\nMeadow",
        ["ZorasDomain"] = "Zora's\r\nDomain",
        ["ZorasFountain"] = "Zora's\r\nFountain",
        ["ZorasRiver"] = "Zora's River",
    };

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<TextureDefinition> textures)
    {
        if (profile.Game != GameKind.OcarinaOfTime
            || !TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
        {
            textures = [];
            return false;
        }

        textures = catalog
            .Where(IsMapPositionNameTexture)
            .OrderBy(GetDisplayText, StringComparer.OrdinalIgnoreCase)
            .ThenBy(GetLanguage, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return textures.Count > 0;
    }

    public static IReadOnlyList<TextureDefinition> GetTargets(RomVersionProfile profile)
        => TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
            ? textures
            : throw new NotSupportedException($"Map-position-name texture catalog is not available for {profile.Name}.");

    public static bool IsMapPositionNameTexture(TextureDefinition texture)
        => string.Equals(texture.Group, MapNameGroup, StringComparison.Ordinal)
            && texture.Name.Contains("PositionName", StringComparison.Ordinal)
            && !texture.Name.EndsWith("JPNTex", StringComparison.Ordinal)
            && texture.Width == Width
            && texture.Height == Height
            && texture.Format == TextureFormat.IA8
            && texture.StorageKind == TextureStorageKind.Rom;

    public static string GetDisplayText(TextureDefinition texture)
        => GetDisplayText(texture.Name);

    public static string GetDisplayText(string textureName)
    {
        string name = textureName;
        name = LeadingGRegex().Replace(name, string.Empty);
        name = PositionNameTextureSuffixRegex().Replace(name, string.Empty);
        name = LanguageTextureSuffixRegex().Replace(name, string.Empty);

        if (DisplayTextOverrides.TryGetValue(name, out string? displayText))
        {
            return displayText;
        }

        MatchCollection matches = WordPartRegex().Matches(name);
        return matches.Count == 0
            ? name
            : string.Join(" ", matches.Select(match => match.Value));
    }

    public static string GetLanguage(TextureDefinition texture)
        => GetLanguage(texture.Name);

    private static string GetLanguage(string textureName)
    {
        if (textureName.EndsWith("FRATex", StringComparison.Ordinal))
        {
            return "French";
        }

        if (textureName.EndsWith("GERTex", StringComparison.Ordinal))
        {
            return "German";
        }

        return "English";
    }

    [GeneratedRegex("^g")]
    private static partial Regex LeadingGRegex();

    [GeneratedRegex("PositionName(?:ENG|FRA|GER|JPN)?Tex$")]
    private static partial Regex PositionNameTextureSuffixRegex();

    [GeneratedRegex("(?:ENG|GER|FRA|JPN)Tex$")]
    private static partial Regex LanguageTextureSuffixRegex();

    [GeneratedRegex("[A-Z]?[a-z]+|[A-Z]+(?=[A-Z]|$)|\\d+")]
    private static partial Regex WordPartRegex();
}
