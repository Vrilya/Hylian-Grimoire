using HylianGrimoire.Games;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static class MajorasMaskGameOverTextureCatalog
{
    private const string GameOverGroup = "interface/icon_item_gameover_static";

    public static readonly GameOverTextureSpec Spec = new(
        "MajorasMaskGameOver",
        "Game Over",
        ["gGameOverP1Tex", "gGameOverP2Tex", "gGameOverP3Tex"],
        "GAME OVER",
        GameOverTextureTargetKind.GameOver,
        GameOverTextureCatalog.Width,
        GameOverTextureCatalog.Height);

    public static readonly GameOverTextureSpec ContinuePlayingSpec = new(
        "MajorasMaskContinuePlaying",
        "Continue playing?",
        ["gContinuePlayingNESTex"],
        "Continue playing ?",
        GameOverTextureTargetKind.ContinuePlaying,
        GameOverTextureCatalog.ContinuePlayingWidth,
        GameOverTextureCatalog.ContinuePlayingHeight);

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<GameOverTextureTarget> targets)
    {
        if (profile.Game != GameKind.MajorasMask
            || !TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
        {
            targets = [];
            return false;
        }

        Dictionary<string, TextureDefinition> byName = catalog
            .Where(IsGameOverTexture)
            .GroupBy(texture => texture.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var result = new List<GameOverTextureTarget>();
        if (byName.TryGetValue(Spec.TextureNames[0], out TextureDefinition? part1)
            && byName.TryGetValue(Spec.TextureNames[1], out TextureDefinition? part2)
            && byName.TryGetValue(Spec.TextureNames[2], out TextureDefinition? part3))
        {
            result.Add(new GameOverTextureTarget(Spec, part1, part2, part3));
        }

        if (byName.TryGetValue(ContinuePlayingSpec.TextureNames[0], out TextureDefinition? continuePlaying))
        {
            result.Add(new GameOverTextureTarget(ContinuePlayingSpec, continuePlaying));
        }

        targets = result;
        return targets.Count > 0;
    }

    public static IReadOnlyList<GameOverTextureTarget> GetTargets(RomVersionProfile profile)
        => TryGetTargets(profile, out IReadOnlyList<GameOverTextureTarget>? targets)
            ? targets
            : throw new NotSupportedException($"Majora's Mask Game Over texture catalog is not available for {profile.Name}.");

    public static bool IsGameOverTexture(TextureDefinition texture)
        => IsGameOverTripletTexture(texture) || IsContinuePlayingTexture(texture);

    public static bool IsGameOverTripletTexture(TextureDefinition texture)
        => string.Equals(texture.Group, GameOverGroup, StringComparison.Ordinal)
            && Spec.TextureNames.Contains(texture.Name)
            && texture.Width == GameOverTextureCatalog.TileWidth
            && texture.Height == GameOverTextureCatalog.TileHeight
            && texture.Format == TextureFormat.IA8
            && texture.StorageKind == TextureStorageKind.Rom;

    public static bool IsContinuePlayingTexture(TextureDefinition texture)
        => string.Equals(texture.Group, GameOverGroup, StringComparison.Ordinal)
            && ContinuePlayingSpec.TextureNames.Contains(texture.Name)
            && texture.Width == GameOverTextureCatalog.ContinuePlayingWidth
            && texture.Height == GameOverTextureCatalog.ContinuePlayingHeight
            && texture.Format == TextureFormat.IA8
            && texture.StorageKind == TextureStorageKind.Rom;
}
