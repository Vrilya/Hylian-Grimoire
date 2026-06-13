using System.Text.RegularExpressions;
using HylianGrimoire.Rom;

using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static partial class ItemNameTextureCatalog
{
    public const int Width = 128;
    public const int Height = 16;

    private const string ItemNameGroup = "textures/item_name_static";

    private static readonly IReadOnlyDictionary<string, string> Possessives = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Dins"] = "Din's",
        ["Eponas"] = "Epona's",
        ["Farores"] = "Farore's",
        ["Nayrus"] = "Nayru's",
        ["Giants"] = "Giant's",
        ["Gorons"] = "Goron's",
        ["Biggorons"] = "Biggoron's",
        ["Gerudos"] = "Gerudo's",
        ["Rutos"] = "Ruto's",
        ["Sarias"] = "Saria's",
        ["Suns"] = "Sun's",
        ["Zeldas"] = "Zelda's",
        ["Zoras"] = "Zora's",
    };

    private static readonly HashSet<string> SmallWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "of",
        "the",
        "to",
        "in",
        "on",
        "and",
    };

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<TextureDefinition> textures)
    {
        if (!TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
        {
            textures = [];
            return false;
        }

        textures = catalog
            .Where(IsItemNameTexture)
            .OrderBy(texture => texture.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return textures.Count > 0;
    }

    public static IReadOnlyList<TextureDefinition> GetTargets(RomVersionProfile profile)
        => TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
            ? textures
            : throw new NotSupportedException($"Item-name texture catalog is not available for {profile.Name}.");

    public static bool IsItemNameTexture(TextureDefinition texture)
        => string.Equals(texture.Group, ItemNameGroup, StringComparison.Ordinal)
            && texture.Width == Width
            && texture.Height == Height
            && texture.Format == TextureFormat.IA4;

    public static string GetDisplayText(TextureDefinition texture)
        => GetDisplayText(texture.Name);

    public static string GetDisplayText(string textureName)
    {
        if (DisplayTextCatalog.TryGetValue(textureName, out string? displayText))
        {
            return displayText;
        }

        string name = textureName;
        name = LeadingGRegex().Replace(name, string.Empty);
        name = ItemNameSuffixRegex().Replace(name, string.Empty);
        name = LowercaseOfRegex().Replace(name, "Of");

        if (string.Equals(name, "SOLDOUT", StringComparison.Ordinal))
        {
            return "SOLD OUT";
        }

        MatchCollection matches = WordPartRegex().Matches(name);
        if (matches.Count == 0)
        {
            return name;
        }

        var words = new List<string>(matches.Count);
        for (int i = 0; i < matches.Count; i++)
        {
            string word = matches[i].Value;
            if (Possessives.TryGetValue(word, out string? possessive))
            {
                words.Add(possessive);
                continue;
            }

            words.Add(i > 0 && SmallWords.Contains(word) ? word.ToLowerInvariant() : word);
        }

        return string.Join(" ", words);
    }

    [GeneratedRegex("^g")]
    private static partial Regex LeadingGRegex();

    [GeneratedRegex("ItemName(?:[0-9]+)?[A-Z]*Tex$")]
    private static partial Regex ItemNameSuffixRegex();

    [GeneratedRegex("(?<=[a-z])of(?=[A-Z])")]
    private static partial Regex LowercaseOfRegex();

    [GeneratedRegex("[A-Z]?[a-z]+|[A-Z]+(?=[A-Z]|$)|\\d+")]
    private static partial Regex WordPartRegex();
}
