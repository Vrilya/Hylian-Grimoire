using System.Text.RegularExpressions;
using HylianGrimoire.Games;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static partial class MapNameTextureCatalog
{
    public const int Width = ItemNameTextureCatalog.Width;
    public const int Height = ItemNameTextureCatalog.Height;

    private const string MapNameGroup = "textures/map_name_static";
    private static readonly IReadOnlyDictionary<string, string> DisplayTextOverrides = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["GerudosFortress"] = "Gerudo's Fortress",
        ["HyliaLakeside"] = "Lake Hylia",
        ["ZorasDomain"] = "Zora's Domain",
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
            .Where(IsMapNameTexture)
            .OrderBy(GetDisplayText, StringComparer.OrdinalIgnoreCase)
            .ThenBy(GetLanguage, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return textures.Count > 0;
    }

    public static IReadOnlyList<TextureDefinition> GetTargets(RomVersionProfile profile)
        => TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
            ? textures
            : throw new NotSupportedException($"Map-name texture catalog is not available for {profile.Name}.");

    public static bool IsMapNameTexture(TextureDefinition texture)
        => string.Equals(texture.Group, MapNameGroup, StringComparison.Ordinal)
            && texture.Name.Contains("PointName", StringComparison.Ordinal)
            && !texture.Name.EndsWith("JPNTex", StringComparison.Ordinal)
            && texture.Width == Width
            && texture.Height == Height
            && texture.Format == TextureFormat.IA4
            && texture.StorageKind == TextureStorageKind.Rom;

    public static string GetDisplayText(TextureDefinition texture)
        => GetDisplayText(texture.Name);

    public static string GetDisplayText(string textureName)
    {
        string name = textureName;
        name = LeadingGRegex().Replace(name, string.Empty);
        name = PointNameTextureSuffixRegex().Replace(name, string.Empty);
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

    [GeneratedRegex("PointName(?:ENG|FRA|GER|JPN)?Tex$")]
    private static partial Regex PointNameTextureSuffixRegex();

    [GeneratedRegex("(?:ENG|GER|FRA|JPN)Tex$")]
    private static partial Regex LanguageTextureSuffixRegex();

    [GeneratedRegex("[A-Z]?[a-z]+|[A-Z]+(?=[A-Z]|$)|\\d+")]
    private static partial Regex WordPartRegex();
}
