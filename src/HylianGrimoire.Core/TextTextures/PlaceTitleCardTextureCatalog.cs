using System.Text.RegularExpressions;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static partial class PlaceTitleCardTextureCatalog
{
    public const int Width = 144;
    public const int Height = 24;

    private const string PlaceTitleCardGroupPrefix = "textures/g_pn_";
    private const string EnglishTitleCardSuffix = "TitleCardENGTex";

    private static readonly IReadOnlyDictionary<string, string> TitleExceptions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Deku Tree"] = "Inside the Deku Tree",
        ["Jabu Jabu"] = "Inside Jabu-Jabu's Belly",
        ["Dodongos Cavern"] = "Dodongo's Cavern",
        ["Gravekeepers Hut"] = "Gravekeeper's Hut",
        ["Zoras Domain"] = "Zora's Domain",
        ["Zoras Fountain"] = "Zora's Fountain",
        ["Ganons Castle"] = "Ganon's Castle",
        ["Inside Ganons Castle"] = "Inside Ganon's Castle",
        ["Great Fairys Fountain"] = "Great Fairy's Fountain",
        ["Fairys Fountain"] = "Fairy's Fountain",
        ["Royal Familys Tomb"] = "Royal Family's Tomb",
        ["Gerudos Fortress"] = "Gerudo's Fortress",
        ["Thieves Hideout"] = "Thieves' Hideout",
        ["Question Mark"] = "?",
    };

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<TextureDefinition> textures)
    {
        if (!TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
        {
            textures = [];
            return false;
        }

        textures = catalog
            .Where(IsPlaceTitleCardTexture)
            .OrderBy(texture => texture.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(texture => texture.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return textures.Count > 0;
    }

    public static IReadOnlyList<TextureDefinition> GetTargets(RomVersionProfile profile)
        => TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
            ? textures
            : throw new NotSupportedException($"Place title-card texture catalog is not available for {profile.Name}.");

    public static bool IsPlaceTitleCardTexture(TextureDefinition texture)
        => texture.Group.StartsWith(PlaceTitleCardGroupPrefix, StringComparison.Ordinal)
            && texture.Name.EndsWith(EnglishTitleCardSuffix, StringComparison.Ordinal)
            && texture.Width == Width
            && texture.Height == Height
            && texture.Format == TextureFormat.IA8;

    public static string GetDisplayText(TextureDefinition texture)
        => GetDisplayText(texture.Name);

    public static string GetDisplayText(string textureName)
    {
        string name = textureName;
        name = LeadingGRegex().Replace(name, string.Empty);
        name = TitleCardSuffixRegex().Replace(name, string.Empty);
        name = GerudoPrefixRegex().Replace(name, "Gerudo");

        MatchCollection matches = WordPartRegex().Matches(name);
        if (matches.Count == 0)
        {
            return name;
        }

        string text = string.Join(" ", matches.Select(match => match.Value));
        return TitleExceptions.TryGetValue(text, out string? replacement) ? replacement : text;
    }

    [GeneratedRegex("^g")]
    private static partial Regex LeadingGRegex();

    [GeneratedRegex("(TitleCard|Card)ENGTex$")]
    private static partial Regex TitleCardSuffixRegex();

    [GeneratedRegex("GERudo")]
    private static partial Regex GerudoPrefixRegex();

    [GeneratedRegex("[A-Z]+(?=[A-Z][a-z]|$)|[A-Z]?[a-z]+|\\d+")]
    private static partial Regex WordPartRegex();
}
