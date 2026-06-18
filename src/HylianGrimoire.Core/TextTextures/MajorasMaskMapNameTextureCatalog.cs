using System.Text.RegularExpressions;
using HylianGrimoire.Games;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static partial class MajorasMaskMapNameTextureCatalog
{
    public const int Width = ItemNameTextureCatalog.Width;
    public const int Height = ItemNameTextureCatalog.Height;

    private const string MapNameGroup = "archives/map_name_static";

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<TextureDefinition> textures)
    {
        if (profile.Game != GameKind.MajorasMask
            || !TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
        {
            textures = [];
            return false;
        }

        textures = catalog
            .Where(IsMapNameTexture)
            .OrderBy(GetDisplayText, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return textures.Count > 0;
    }

    public static IReadOnlyList<TextureDefinition> GetTargets(RomVersionProfile profile)
        => TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
            ? textures
            : throw new NotSupportedException($"Majora's Mask map-name texture catalog is not available for {profile.Name}.");

    public static bool IsMapNameTexture(TextureDefinition texture)
        => string.Equals(texture.Group, MapNameGroup, StringComparison.Ordinal)
            && texture.Width == Width
            && texture.Height == Height
            && texture.Format == TextureFormat.IA4
            && texture.StorageKind == TextureStorageKind.CmpDmaArchive;

    public static string GetDisplayText(TextureDefinition texture)
        => GetDisplayText(texture.Name);

    public static string GetDisplayText(string textureName)
    {
        string name = textureName;
        name = LeadingMapPointRegex().Replace(name, string.Empty);
        name = LanguageTextureSuffixRegex().Replace(name, string.Empty);

        MatchCollection matches = WordPartRegex().Matches(name);
        return matches.Count == 0
            ? name
            : string.Join(" ", matches.Select(match => match.Value));
    }

    [GeneratedRegex("^gMapPoint")]
    private static partial Regex LeadingMapPointRegex();

    [GeneratedRegex("(?:ENG|GER|FRA|JPN)Tex$")]
    private static partial Regex LanguageTextureSuffixRegex();

    [GeneratedRegex("[A-Z]?[a-z]+|[A-Z]+(?=[A-Z]|$)|\\d+")]
    private static partial Regex WordPartRegex();
}
