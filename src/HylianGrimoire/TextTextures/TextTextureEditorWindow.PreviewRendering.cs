using System.Drawing;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private Bitmap RenderTextTexture(TextTextureTargetItem item, TextTextureFont primaryFont, TextTextureFont? secondaryFont, string? pauseTemplateRoot)
        => _selectedTextureKind switch
        {
            TextTextureKind.ItemNames
                or TextTextureKind.MapNames
                or TextTextureKind.MajorasMaskItemNames
                or TextTextureKind.MajorasMaskMapNames
                or TextTextureKind.MajorasMaskPausePrompts
                => RenderItemStyleTexture(item, primaryFont),
            TextTextureKind.MapPositionNames => MapPositionNameTextureRenderer.Render(
                MapPositionLine1TextBox.Text,
                MapPositionLine2TextBox.Text,
                primaryFont,
                _mapPositionNameSettings),
            TextTextureKind.PausePrompts => RenderPausePromptTexture(item, primaryFont),
            TextTextureKind.DungeonMapNames => RenderDungeonMapNameTexture(item, primaryFont),
            TextTextureKind.MajorasMaskDungeonMapNames => RenderMajorasMaskDungeonMapNameTexture(item, primaryFont),
            TextTextureKind.FileSelect => RenderFileSelectTexture(item, primaryFont, secondaryFont),
            TextTextureKind.EndTitles => RenderEndTitleTexture(item, primaryFont),
            TextTextureKind.PlaceTitleCards => PlaceTitleCardTextureRenderer.Render(PrimaryTextBox.Text, primaryFont, _placeSettings),
            TextTextureKind.BossTitleCards => BossTitleCardTextureRenderer.Render(
                BossTopTextBox.Text,
                BossBottomTextBox.Text,
                primaryFont,
                secondaryFont ?? throw new InvalidOperationException("Boss title-card bottom font was not resolved."),
                _bossSettings),
            TextTextureKind.GameOver or TextTextureKind.MajorasMaskGameOver => RenderGameOverTexture(item, primaryFont),
            TextTextureKind.PauseHeaders => PauseHeaderTextureRenderer.Render(
                PrimaryTextBox.Text,
                primaryFont,
                pauseTemplateRoot ?? throw new InvalidOperationException("Pause-header template root was not resolved."),
                item.PauseHeaderTarget ?? throw new InvalidOperationException("Pause-header target was not resolved."),
                _pauseSettings),
            _ => throw new InvalidOperationException($"Unsupported text texture kind: {_selectedTextureKind}."),
        };

    private Bitmap RenderItemStyleTexture(TextTextureTargetItem item, TextTextureFont primaryFont)
    {
        TextureDefinition texture = item.Texture
            ?? throw new InvalidOperationException("Item-style texture target was not resolved.");
        ItemNameTextureRenderSettings settings = GetEffectiveItemStyleSettingsForTexture(texture);
        return ItemNameTextureRenderer.Render(PrimaryTextBox.Text, primaryFont, settings, texture.Width, texture.Height);
    }

    private Bitmap RenderPausePromptTexture(TextTextureTargetItem item, TextTextureFont primaryFont)
    {
        TextureDefinition texture = item.Texture
            ?? throw new InvalidOperationException("Pause-prompt texture target was not resolved.");
        CompactTextTextureRenderSettings settings = GetEffectivePromptSettings(texture);
        return CompactTextTextureRenderer.Render(PrimaryTextBox.Text, primaryFont, settings, texture.Width, texture.Height);
    }

    private Bitmap RenderDungeonMapNameTexture(TextTextureTargetItem item, TextTextureFont primaryFont)
    {
        TextureDefinition texture = item.Texture
            ?? throw new InvalidOperationException("Dungeon map name texture target was not resolved.");
        CompactTextTextureRenderSettings settings = GetEffectiveDungeonMapNameSettings(texture);
        return CompactTextTextureRenderer.Render(PrimaryTextBox.Text, primaryFont, settings, texture.Width, texture.Height);
    }

    private Bitmap RenderMajorasMaskDungeonMapNameTexture(TextTextureTargetItem item, TextTextureFont primaryFont)
    {
        TextureDefinition texture = item.Texture
            ?? throw new InvalidOperationException("Majora's Mask dungeon map name texture target was not resolved.");
        CompactTextTextureRenderSettings settings = GetEffectiveMajorasMaskDungeonMapNameSettings(texture);
        return CompactTextTextureRenderer.Render(PrimaryTextBox.Text, primaryFont, settings, texture.Width, texture.Height);
    }

    private Bitmap RenderFileSelectTexture(TextTextureTargetItem item, TextTextureFont primaryFont, TextTextureFont? secondaryFont)
    {
        TextureDefinition texture = item.Texture
            ?? throw new InvalidOperationException("File Select texture target was not resolved.");
        CompactTextTextureRenderSettings settings = GetEffectiveFileSelectSettings(texture);
        if (IsFileSelectControlsTexture(texture))
        {
            TextTextureFont boldFont = secondaryFont
                ?? throw new InvalidOperationException("File Select controls bold font was not resolved.");
            FileSelectUiSettings uiSettings = GetFileSelectSettings(texture);
            IReadOnlyList<CompactTextTextureTextRun> runs = FileSelectControlsTextRunBuilder.Create(
                PrimaryTextBox.Text,
                primaryFont,
                boldFont,
                uiSettings.BulletScale,
                uiSettings.BulletYOffset);
            return CompactTextTextureRenderer.Render(runs, settings, texture.Width, texture.Height);
        }

        return CompactTextTextureRenderer.Render(PrimaryTextBox.Text, primaryFont, settings, texture.Width, texture.Height);
    }

    private Bitmap RenderGameOverTexture(TextTextureTargetItem item, TextTextureFont primaryFont)
    {
        GameOverTextureTarget target = item.GameOverTarget
            ?? throw new InvalidOperationException("Game Over texture target was not resolved.");
        return target.Spec.Kind == GameOverTextureTargetKind.ContinuePlaying
            ? ContinuePlayingTextureRenderer.Render(PrimaryTextBox.Text, primaryFont, GetCurrentContinuePlayingSettings())
            : GameOverTextureRenderer.Render(PrimaryTextBox.Text, primaryFont, GetCurrentGameOverSettings());
    }

    private Bitmap RenderEndTitleTexture(TextTextureTargetItem item, TextTextureFont primaryFont)
    {
        TextureDefinition texture = item.Texture
            ?? throw new InvalidOperationException("End-title texture target was not resolved.");
        EndTitleTextureSpec spec = EndTitleTextureCatalog.GetSpec(texture);
        bool segmented = spec.Style == EndTitleTextureStyle.OcarinaOfTime;
        EndTitleTextureAssets assets = new();
        if (spec.Style == EndTitleTextureStyle.LegendOfZelda && _endTitleSettings.LegendShowRegistered)
        {
            if (!TryFindEndTitleTemplateAsset("R.png", out string legendStampPath, out string missingAssetMessage))
            {
                throw new InvalidOperationException(missingAssetMessage);
            }

            assets = assets with { LegendRegisteredPath = legendStampPath };
        }

        if (segmented)
        {
            if (!TryFindMiscAsset("TM.png", out string tmPath, out string missingTmMessage))
            {
                throw new InvalidOperationException(missingTmMessage);
            }

            if (!TryFindMiscAsset("-.png", out string ornamentPath, out string missingOrnamentMessage))
            {
                throw new InvalidOperationException(missingOrnamentMessage);
            }

            assets = assets with
            {
                OcarinaTmPath = tmPath,
                OcarinaOrnamentPath = ornamentPath,
            };
        }

        EndTitleTextParts ocarinaParts = segmented
            ? EndTitleTextParts.Parse(spec.SampleText)
            : EndTitleTextParts.Empty;
        return EndTitleTextureRenderer.Render(
            segmented ? ocarinaParts.Prefix : string.Empty,
            PrimaryTextBox.Text,
            segmented ? ocarinaParts.Tm : string.Empty,
            segmented ? ocarinaParts.Suffix : string.Empty,
            primaryFont,
            spec,
            _endTitleSettings,
            texture.Width,
            texture.Height,
            assets);
    }
}
