using HylianGrimoire.Games;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private static readonly HashSet<TextTextureKind> MajorasMaskTextureKinds =
    [
        TextTextureKind.MajorasMaskItemNames,
        TextTextureKind.MajorasMaskMapNames,
        TextTextureKind.MajorasMaskDungeonMapNames,
        TextTextureKind.MajorasMaskGameOver,
        TextTextureKind.MajorasMaskPausePrompts,
    ];

    private static TextTextureKindDescriptor[] CreateMajorasMaskTextureKindDescriptors()
        =>
        [
            new(
                TextTextureKind.MajorasMaskItemNames,
                "Item Names",
                SmallTexturePreviewScale,
                "mm_item_name",
                profile => ItemNameTextureCatalog.TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
                    ? textures.Select(texture => new TextTextureTargetItem(texture, TextTextureKind.MajorasMaskItemNames)).ToArray()
                    : [],
                target => string.Equals(target.TargetKey, PreferredMajorasMaskItemNameTextureName, StringComparison.Ordinal)),
            new(
                TextTextureKind.MajorasMaskMapNames,
                "Map Names",
                SmallTexturePreviewScale,
                "mm_map_name",
                profile => MajorasMaskMapNameTextureCatalog.TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
                    ? textures.Select(texture => new TextTextureTargetItem(texture, TextTextureKind.MajorasMaskMapNames)).ToArray()
                    : [],
                target => string.Equals(target.TargetKey, PreferredMajorasMaskMapNameTextureName, StringComparison.Ordinal)),
            new(
                TextTextureKind.MajorasMaskDungeonMapNames,
                "Dungeon Map Names",
                SmallTexturePreviewScale,
                "mm_dungeon_map_name",
                profile => MajorasMaskDungeonMapNameTextureCatalog.TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
                    ? textures.Select(texture => new TextTextureTargetItem(texture, TextTextureKind.MajorasMaskDungeonMapNames)).ToArray()
                    : [],
                target => string.Equals(target.TargetKey, PreferredMajorasMaskDungeonMapNameTextureName, StringComparison.Ordinal)),
            new(
                TextTextureKind.MajorasMaskPausePrompts,
                "Pause Prompts",
                SmallTexturePreviewScale,
                "mm_pause_prompt",
                profile => MajorasMaskPausePromptTextureCatalog.TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
                    ? textures.Select(texture => new TextTextureTargetItem(texture, TextTextureKind.MajorasMaskPausePrompts)).ToArray()
                    : [],
                target => string.Equals(target.TargetKey, PreferredMajorasMaskPausePromptTextureName, StringComparison.Ordinal)),
            new(
                TextTextureKind.MajorasMaskGameOver,
                "Game Over",
                GameOverPreviewScale,
                "mm_game_over",
                profile => MajorasMaskGameOverTextureCatalog.TryGetTargets(profile, out IReadOnlyList<GameOverTextureTarget>? targets)
                    ? targets.Select(target => new TextTextureTargetItem(target)).ToArray()
                    : [],
                target => target.GameOverTarget?.Spec.Key == MajorasMaskGameOverTextureCatalog.Spec.Key),
        ];

    private const string PreferredMajorasMaskItemNameTextureName = "gItemNameOcarinaOfTimeENGTex";
    private const string PreferredMajorasMaskMapNameTextureName = "gMapPointClockTownENGTex";
    private const string PreferredMajorasMaskDungeonMapNameTextureName = "gPauseWoodfallTitleENGTex";
    private const string PreferredMajorasMaskPausePromptTextureName = "gPauseToEquipENGTex";

    private static bool IsTextureKindSupportedByProfile(TextTextureKindDescriptor kind, RomVersionProfile profile)
        => profile.Game == GameKind.MajorasMask
            ? MajorasMaskTextureKinds.Contains(kind.Kind)
            : !MajorasMaskTextureKinds.Contains(kind.Kind);
}
