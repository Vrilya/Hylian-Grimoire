using HylianGrimoire.Games;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static class MajorasMaskDungeonMapNameTextureCatalog
{
    public const int Width = 128;
    public const int Height = 16;

    private const string DungeonMapNameGroup = "interface/icon_item_jpn_static";

    public static readonly IReadOnlyList<MajorasMaskDungeonMapNameTextureSpec> Specs =
    [
        new("Woodfall Temple", "English", "gPauseWoodfallTitleENGTex", "Woodfall Temple"),
        new("Snowhead Temple", "English", "gPauseSnowheadTitleENGTex", "Snowhead Temple"),
        new("Great Bay Temple", "English", "gPauseGreatBayTitleENGTex", "Great Bay Temple"),
        new("Stone Tower Temple", "English", "gPauseStoneTowerTitleENGTex", "Stone Tower Temple"),
    ];

    private static readonly IReadOnlyDictionary<string, MajorasMaskDungeonMapNameTextureSpec> SpecsByName = Specs
        .ToDictionary(spec => spec.TextureName, StringComparer.Ordinal);

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<TextureDefinition> textures)
    {
        if (profile.Game != GameKind.MajorasMask
            || !TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
        {
            textures = [];
            return false;
        }

        Dictionary<string, TextureDefinition> byName = catalog
            .Where(IsDungeonMapNameTexture)
            .ToDictionary(texture => texture.Name, StringComparer.Ordinal);

        var result = new List<TextureDefinition>(Specs.Count);
        foreach (MajorasMaskDungeonMapNameTextureSpec spec in Specs)
        {
            if (byName.TryGetValue(spec.TextureName, out TextureDefinition? texture))
            {
                result.Add(texture);
            }
        }

        textures = result;
        return textures.Count > 0;
    }

    public static IReadOnlyList<TextureDefinition> GetTargets(RomVersionProfile profile)
        => TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
            ? textures
            : throw new NotSupportedException($"Majora's Mask dungeon map-name texture catalog is not available for {profile.Name}.");

    public static bool IsDungeonMapNameTexture(TextureDefinition texture)
        => SpecsByName.ContainsKey(texture.Name)
            && string.Equals(texture.Group, DungeonMapNameGroup, StringComparison.Ordinal)
            && texture.Width == Width
            && texture.Height == Height
            && texture.Format == TextureFormat.IA8
            && texture.StorageKind == TextureStorageKind.Rom;

    public static string GetDisplayText(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out MajorasMaskDungeonMapNameTextureSpec? spec)
            ? spec.SampleText
            : texture.Name;

    public static string GetLabel(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out MajorasMaskDungeonMapNameTextureSpec? spec)
            ? spec.Label
            : texture.Name;

    public static string GetLanguage(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out MajorasMaskDungeonMapNameTextureSpec? spec)
            ? spec.Language
            : "Unknown";
}

public sealed record MajorasMaskDungeonMapNameTextureSpec(
    string Label,
    string Language,
    string TextureName,
    string SampleText);
