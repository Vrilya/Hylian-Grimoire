using HylianGrimoire.Games;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static class GameOverTextureCatalog
{
    public const int Width = 192;
    public const int Height = 32;
    public const int TileWidth = 64;
    public const int TileHeight = 32;
    public const int ContinuePlayingWidth = 152;
    public const int ContinuePlayingHeight = 16;

    public static readonly GameOverTextureSpec Spec = new(
        "GameOver",
        "Game Over",
        ["gGameOverP1Tex", "gGameOverP2Tex", "gGameOverP3Tex"],
        "GAME OVER",
        GameOverTextureTargetKind.GameOver,
        Width,
        Height);

    public static readonly GameOverTextureSpec ContinuePlayingSpec = new(
        "ContinuePlaying",
        "Continue playing?",
        ["gContinuePlayingENGTex"],
        "Continue playing ?",
        GameOverTextureTargetKind.ContinuePlaying,
        ContinuePlayingWidth,
        ContinuePlayingHeight);

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<GameOverTextureTarget> targets)
    {
        if (profile.Game != GameKind.OcarinaOfTime || !TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
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
            : throw new NotSupportedException($"Game Over texture catalog is not available for {profile.Name}.");

    public static bool IsGameOverTexture(TextureDefinition texture)
        => IsGameOverTripletTexture(texture) || IsContinuePlayingTexture(texture);

    public static bool IsGameOverTripletTexture(TextureDefinition texture)
        => Spec.TextureNames.Contains(texture.Name)
            && texture.Width == TileWidth
            && texture.Height == TileHeight
            && texture.Format == TextureFormat.IA8;

    public static bool IsContinuePlayingTexture(TextureDefinition texture)
        => ContinuePlayingSpec.TextureNames.Contains(texture.Name)
            && texture.Width == ContinuePlayingWidth
            && texture.Height == ContinuePlayingHeight
            && texture.Format == TextureFormat.IA8;
}

public sealed record GameOverTextureSpec(
    string Key,
    string Label,
    IReadOnlyList<string> TextureNames,
    string SampleText,
    GameOverTextureTargetKind Kind,
    int Width,
    int Height);

public enum GameOverTextureTargetKind
{
    GameOver,
    ContinuePlaying,
}

public sealed class GameOverTextureTarget
{
    public GameOverTextureTarget(GameOverTextureSpec spec, TextureDefinition texture)
    {
        Spec = spec;
        Textures = [texture];
    }

    public GameOverTextureTarget(GameOverTextureSpec spec, TextureDefinition part1, TextureDefinition part2, TextureDefinition part3)
    {
        Spec = spec;
        Textures = [part1, part2, part3];
    }

    public GameOverTextureSpec Spec { get; }

    public IReadOnlyList<TextureDefinition> Textures { get; }

    public TextureDefinition Texture
        => Textures.Count == 1
            ? Textures[0]
            : throw new InvalidOperationException("The selected Game Over target is not a single texture.");

    public TextureDefinition Part1
        => Textures.Count == 3
            ? Textures[0]
            : throw new InvalidOperationException("The selected Game Over target is not a texture triplet.");

    public TextureDefinition Part2
        => Textures.Count == 3
            ? Textures[1]
            : throw new InvalidOperationException("The selected Game Over target is not a texture triplet.");

    public TextureDefinition Part3
        => Textures.Count == 3
            ? Textures[2]
            : throw new InvalidOperationException("The selected Game Over target is not a texture triplet.");
}
