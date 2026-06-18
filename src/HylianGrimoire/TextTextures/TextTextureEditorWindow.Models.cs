using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private enum TextTextureKind
    {
        ItemNames,
        MapNames,
        MapPositionNames,
        MajorasMaskItemNames,
        MajorasMaskMapNames,
        MajorasMaskDungeonMapNames,
        MajorasMaskGameOver,
        MajorasMaskPausePrompts,
        PausePrompts,
        DungeonMapNames,
        FileSelect,
        PlaceTitleCards,
        BossTitleCards,
        GameOver,
        PauseHeaders,
        EndTitles,
    }

    private sealed class TextTextureKindDescriptor(
        TextTextureKind kind,
        string label,
        double previewScale,
        string emptyFileName,
        Func<RomVersionProfile, IReadOnlyList<TextTextureTargetItem>> getTargets,
        Func<TextTextureTargetItem, bool> isPreferredTarget)
    {
        public TextTextureKind Kind { get; } = kind;

        public string Label { get; } = label;

        public double PreviewScale { get; } = previewScale;

        public string EmptyFileName { get; } = emptyFileName;

        public IReadOnlyList<TextTextureTargetItem> GetTargets(RomVersionProfile profile)
            => getTargets(profile);

        public bool HasTargets(RomVersionProfile profile)
            => getTargets(profile).Count > 0;

        public bool IsPreferredTarget(TextTextureTargetItem target)
            => isPreferredTarget(target);

        public override string ToString() => Label;
    }

    private sealed record CompactTextUiSettings(double FontSize, double WidthScale, int YOffset);

    private sealed record DungeonMapNameUiSettings(
        bool Center,
        int XNudge,
        CompactTextUiSettings Text,
        int FillBoost);

    private sealed record FileSelectUiSettings(
        bool Center,
        int XNudge,
        CompactTextUiSettings Text,
        double OutlineWidth,
        int OutlineAlpha,
        double OutlineBlurRadius,
        int OutlineBlurStrength,
        double CharacterSpacing,
        double FillStrokeWidth,
        double BulletScale,
        double BulletYOffset);

    private sealed class TextTextureTargetItem
    {
        public TextTextureTargetItem(TextureDefinition texture, TextTextureKind kind)
        {
            Texture = texture;
            GameOverTarget = null;
            PauseHeaderTarget = null;
            if (kind == TextTextureKind.BossTitleCards)
            {
                BossTitleCardText bossText = BossTitleCardTextureCatalog.GetDisplayText(texture);
                DefaultText = bossText.BossText;
                DefaultTopText = bossText.TopText;
                DefaultBottomText = bossText.BossText;
                return;
            }

            DefaultText = kind switch
            {
                TextTextureKind.ItemNames => ItemNameTextureCatalog.GetDisplayText(texture),
                TextTextureKind.MapNames => MapNameTextureCatalog.GetDisplayText(texture),
                TextTextureKind.MapPositionNames => MapPositionNameTextureCatalog.GetDisplayText(texture),
                TextTextureKind.MajorasMaskItemNames => ItemNameTextureCatalog.GetDisplayText(texture),
                TextTextureKind.MajorasMaskMapNames => MajorasMaskMapNameTextureCatalog.GetDisplayText(texture),
                TextTextureKind.MajorasMaskDungeonMapNames => MajorasMaskDungeonMapNameTextureCatalog.GetDisplayText(texture),
                TextTextureKind.MajorasMaskPausePrompts => MajorasMaskPausePromptTextureCatalog.GetDisplayText(texture),
                TextTextureKind.PausePrompts => PausePromptTextureCatalog.GetDisplayText(texture),
                TextTextureKind.DungeonMapNames => DungeonMapNameTextureCatalog.GetDisplayText(texture),
                TextTextureKind.FileSelect => FileSelectTextureCatalog.GetDisplayText(texture),
                TextTextureKind.EndTitles => EndTitleTextureCatalog.GetDisplayText(texture),
                TextTextureKind.PlaceTitleCards => PlaceTitleCardTextureCatalog.GetDisplayText(texture),
                _ => texture.Name,
            };
            DefaultTopText = string.Empty;
            DefaultBottomText = DefaultText;
        }

        public TextTextureTargetItem(PauseHeaderTextureTarget target)
        {
            Texture = null;
            GameOverTarget = null;
            PauseHeaderTarget = target;
            DefaultText = target.Spec.SampleText;
            DefaultTopText = string.Empty;
            DefaultBottomText = DefaultText;
        }

        public TextTextureTargetItem(GameOverTextureTarget target)
        {
            Texture = null;
            GameOverTarget = target;
            PauseHeaderTarget = null;
            DefaultText = target.Spec.SampleText;
            DefaultTopText = string.Empty;
            DefaultBottomText = DefaultText;
        }

        public TextureDefinition? Texture { get; }

        public GameOverTextureTarget? GameOverTarget { get; }

        public PauseHeaderTextureTarget? PauseHeaderTarget { get; }

        public string DefaultText { get; }

        public string DefaultTopText { get; }

        public string DefaultBottomText { get; }

        public string TargetKey => Texture?.Name ?? GameOverTarget?.Spec.Key ?? PauseHeaderTarget?.Spec.Label ?? string.Empty;

        public string StatusLabel => PauseHeaderTarget is { } pauseTarget
            ? pauseTarget.Spec.Label
            : GameOverTarget is { } gameOverTarget
                ? gameOverTarget.Spec.Label
                : DefaultText.ReplaceLineEndings(" ");

        public string DisplayLabel => PauseHeaderTarget is { } pauseTarget
            ? $"{pauseTarget.Spec.Label}  {string.Join(" / ", pauseTarget.Spec.TextureNames)}"
            : GameOverTarget is { } gameOverTarget
                ? $"{gameOverTarget.Spec.Label}  {string.Join(" / ", gameOverTarget.Spec.TextureNames)}"
            : Texture is { } texture && PausePromptTextureCatalog.IsPausePromptTexture(texture)
                ? $"{PausePromptTextureCatalog.GetLanguage(texture)} - {DefaultText}  {texture.Name}"
            : Texture is { } majorasMaskPausePromptTexture && MajorasMaskPausePromptTextureCatalog.IsPausePromptTexture(majorasMaskPausePromptTexture)
                ? $"{MajorasMaskPausePromptTextureCatalog.GetLanguage(majorasMaskPausePromptTexture)} - {DefaultText}  {majorasMaskPausePromptTexture.Name}"
            : Texture is { } majorasMaskDungeonMapNameTexture && MajorasMaskDungeonMapNameTextureCatalog.IsDungeonMapNameTexture(majorasMaskDungeonMapNameTexture)
                ? $"{MajorasMaskDungeonMapNameTextureCatalog.GetLanguage(majorasMaskDungeonMapNameTexture)} - {DefaultText}  {majorasMaskDungeonMapNameTexture.Name}"
            : Texture is { } dungeonMapNameTexture && DungeonMapNameTextureCatalog.IsDungeonMapNameTexture(dungeonMapNameTexture)
                ? $"{DungeonMapNameTextureCatalog.GetLanguage(dungeonMapNameTexture)} - {DefaultText}  {dungeonMapNameTexture.Name}"
            : Texture is { } mapNameTexture && MapNameTextureCatalog.IsMapNameTexture(mapNameTexture)
                ? $"{MapNameTextureCatalog.GetLanguage(mapNameTexture)} - {DefaultText}  {mapNameTexture.Name}"
            : Texture is { } mapPositionNameTexture && MapPositionNameTextureCatalog.IsMapPositionNameTexture(mapPositionNameTexture)
                ? $"{MapPositionNameTextureCatalog.GetLanguage(mapPositionNameTexture)} - {DefaultText.ReplaceLineEndings(" ")}  {mapPositionNameTexture.Name}"
            : Texture is { } fileSelectTexture && FileSelectTextureCatalog.IsFileSelectTexture(fileSelectTexture)
                ? $"{FileSelectTextureCatalog.GetLanguage(fileSelectTexture)} - {DefaultText}  {fileSelectTexture.Name}"
            : Texture is { } endTitleTexture && EndTitleTextureCatalog.IsEndTitleTexture(endTitleTexture)
                ? $"{EndTitleTextureCatalog.GetLabel(endTitleTexture)}  {endTitleTexture.Name}"
            : $"{DefaultText}  {Texture?.Name}";

        public override string ToString() => DisplayLabel;
    }
}
