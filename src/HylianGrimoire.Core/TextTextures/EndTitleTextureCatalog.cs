using HylianGrimoire.Games;
using HylianGrimoire.Rom;

using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static class EndTitleTextureCatalog
{
    private const string EndTitleGroup = "overlays/ovl_End_Title";

    public static readonly IReadOnlyList<EndTitleTextureSpec> Specs =
    [
        new("OcarinaOfTime", "Ocarina of Time", "sOcarinaOfTimeTex", "\u2013Ocarina of Time\u2122\u2013", 112, 16, EndTitleTextureStyle.OcarinaOfTime),
        new("LegendOfZelda", "The Legend of Zelda", "sTheLegendOfZeldaTex", "The Legend of ZELDA", 120, 24, EndTitleTextureStyle.LegendOfZelda),
        new("PresentedBy", "Presented by", "sPresentedByTex", "PRESENTED BY", 96, 16, EndTitleTextureStyle.PresentedBy),
        new("TheEnd", "The End", "sTheEndTex", "The End", 80, 24, EndTitleTextureStyle.TheEnd),
    ];

    private static readonly IReadOnlyDictionary<string, EndTitleTextureSpec> SpecsByName = Specs
        .ToDictionary(spec => spec.TextureName, StringComparer.Ordinal);

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<TextureDefinition> textures)
    {
        if (profile.Game != GameKind.OcarinaOfTime || !TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
        {
            textures = [];
            return false;
        }

        Dictionary<string, TextureDefinition> byName = catalog
            .Where(IsEndTitleTexture)
            .ToDictionary(texture => texture.Name, StringComparer.Ordinal);

        var result = new List<TextureDefinition>(Specs.Count);
        foreach (EndTitleTextureSpec spec in Specs)
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
            : throw new NotSupportedException($"End-title texture catalog is not available for {profile.Name}.");

    public static bool IsEndTitleTexture(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out EndTitleTextureSpec? spec)
            && string.Equals(texture.Group, EndTitleGroup, StringComparison.Ordinal)
            && texture.Width == spec.Width
            && texture.Height == spec.Height
            && texture.Format == TextureFormat.IA8;

    public static string GetDisplayText(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out EndTitleTextureSpec? spec)
            ? spec.SampleText
            : texture.Name;

    public static string GetLabel(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out EndTitleTextureSpec? spec)
            ? spec.Label
            : texture.Name;

    public static EndTitleTextureSpec GetSpec(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out EndTitleTextureSpec? spec)
            ? spec
            : throw new NotSupportedException($"End-title texture spec is not available for {texture.Name}.");
}

public sealed record EndTitleTextureSpec(
    string Key,
    string Label,
    string TextureName,
    string SampleText,
    int Width,
    int Height,
    EndTitleTextureStyle Style);

public enum EndTitleTextureStyle
{
    OcarinaOfTime,
    LegendOfZelda,
    PresentedBy,
    TheEnd,
}
