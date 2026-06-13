using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using HylianGrimoire.Preview;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private const int SmallTexturePreviewScale = 5;
    private const int PlaceTitleCardPreviewScale = 4;
    private const int BossTitleCardPreviewScale = 4;
    private const double GameOverPreviewScale = 3.0;
    private const double ContinuePlayingPreviewScale = 4.0;
    private const double PauseHeaderPreviewScale = 2.5;

    private const int DefaultEndTitleWidth = 112;
    private const int DefaultEndTitleHeight = 16;

    private void RefreshPreview()
    {
        if (_updatingControls)
        {
            return;
        }

        if (_romData is null || GetSelectedTarget() is not TextTextureTargetItem item)
        {
            ClearPreviews();
            SetStatus(string.Empty);
            return;
        }

        if (!TryFindRenderFonts(out TextTextureFont primaryFont, out TextTextureFont? secondaryFont, out string missingFontMessage))
        {
            ClearPreviews();
            SetStatus(missingFontMessage);
            return;
        }

        string? pauseTemplateRoot = null;
        if (_selectedTextureKind == TextTextureKind.PauseHeaders && !TryFindPauseHeaderTemplateRoot(out pauseTemplateRoot, out string missingTemplateMessage))
        {
            ClearPreviews();
            SetStatus(missingTemplateMessage);
            return;
        }

        try
        {
            using Bitmap generated = RenderTextTexture(item, primaryFont, secondaryFont, pauseTemplateRoot);
            using Bitmap reference = DecodeReference(_romData.DecompressedRom, item);
            ReplaceLastGenerated(generated);

            using Bitmap generatedDisplay = CreateDisplayBitmap(generated, item);
            using Bitmap referenceDisplay = CreateDisplayBitmap(reference, item);
            using Bitmap generatedPreview = CreatePreviewBitmap(generatedDisplay);
            using Bitmap referencePreview = CreatePreviewBitmap(referenceDisplay);
            SetPreviewSource(_generatedSlot, generatedPreview, "generated");
            SetPreviewSource(_romSlot, referencePreview, "rom");
            ReferenceLabel.Text = _selectedTextureKind == TextTextureKind.PauseHeaders && _showPauseOriginalColors ? "ROM colors" : "ROM";
            SetStatus($"Previewing {item.StatusLabel}.");
        }
        catch (Exception ex)
        {
            ClearPreviews();
            SetStatus($"Preview failed. {UiOperationExceptionHandler.GetDisplayMessage("Text texture preview failed", ex)}");
        }
    }

    private Bitmap RenderTextTexture(TextTextureTargetItem item, TextTextureFont primaryFont, TextTextureFont? secondaryFont, string? pauseTemplateRoot)
        => _selectedTextureKind switch
        {
            TextTextureKind.ItemNames => ItemNameTextureRenderer.Render(PrimaryTextBox.Text, primaryFont, _itemSettings),
            TextTextureKind.PausePrompts => RenderPausePromptTexture(item, primaryFont),
            TextTextureKind.DungeonMapNames => RenderDungeonMapNameTexture(item, primaryFont),
            TextTextureKind.FileSelect => RenderFileSelectTexture(item, primaryFont, secondaryFont),
            TextTextureKind.EndTitles => RenderEndTitleTexture(item, primaryFont),
            TextTextureKind.PlaceTitleCards => PlaceTitleCardTextureRenderer.Render(PrimaryTextBox.Text, primaryFont, _placeSettings),
            TextTextureKind.BossTitleCards => BossTitleCardTextureRenderer.Render(
                BossTopTextBox.Text,
                BossBottomTextBox.Text,
                primaryFont,
                secondaryFont ?? throw new InvalidOperationException("Boss title-card bottom font was not resolved."),
                _bossSettings),
            TextTextureKind.GameOver => RenderGameOverTexture(item, primaryFont),
            TextTextureKind.PauseHeaders => PauseHeaderTextureRenderer.Render(
                PrimaryTextBox.Text,
                primaryFont,
                pauseTemplateRoot ?? throw new InvalidOperationException("Pause-header template root was not resolved."),
                item.PauseHeaderTarget ?? throw new InvalidOperationException("Pause-header target was not resolved."),
                _pauseSettings),
            _ => throw new InvalidOperationException($"Unsupported text texture kind: {_selectedTextureKind}."),
        };

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
            ? ContinuePlayingTextureRenderer.Render(PrimaryTextBox.Text, primaryFont, _continuePlayingSettings)
            : GameOverTextureRenderer.Render(PrimaryTextBox.Text, primaryFont, _gameOverSettings);
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

    private static Bitmap DecodeReference(ReadOnlySpan<byte> rom, TextTextureTargetItem item)
    {
        if (item.PauseHeaderTarget is { } pauseTarget)
        {
            using Bitmap left = TextureRomService.Decode(rom, pauseTarget.Left);
            using Bitmap middle = TextureRomService.Decode(rom, pauseTarget.Middle);
            using Bitmap right = TextureRomService.Decode(rom, pauseTarget.Right);
            return PauseHeaderTextureRenderer.CombineTriplet([left, middle, right]);
        }

        if (item.GameOverTarget is { } gameOverTarget)
        {
            if (gameOverTarget.Spec.Kind == GameOverTextureTargetKind.ContinuePlaying)
            {
                return TextureRomService.Decode(rom, gameOverTarget.Texture);
            }

            using Bitmap part1 = TextureRomService.Decode(rom, gameOverTarget.Part1);
            using Bitmap part2 = TextureRomService.Decode(rom, gameOverTarget.Part2);
            using Bitmap part3 = TextureRomService.Decode(rom, gameOverTarget.Part3);
            return GameOverTextureRenderer.CombineTriplet([part1, part2, part3]);
        }

        return item.Texture is { } texture
            ? TextureRomService.Decode(rom, texture)
            : throw new InvalidOperationException("No texture target is selected.");
    }

    private static void ApplyPauseHeaderToRom(byte[] rom, PauseHeaderTextureTarget target, Bitmap row)
        => ApplyCompositeToRom(rom, target.Textures, row, PauseHeaderTextureRenderer.SplitTriplet);

    private static void ApplyGameOverToRom(byte[] rom, GameOverTextureTarget target, Bitmap row)
    {
        if (target.Spec.Kind == GameOverTextureTargetKind.ContinuePlaying)
        {
            TextureRomService.EncodeAndWrite(rom, target.Texture, row);
            return;
        }

        ApplyCompositeToRom(rom, target.Textures, row, GameOverTextureRenderer.SplitTriplet);
    }

    private static void ApplyCompositeToRom(
        byte[] rom,
        IReadOnlyList<TextureDefinition> textures,
        Bitmap row,
        Func<Bitmap, IReadOnlyList<Bitmap>> split)
    {
        byte[] romCopy = rom.ToArray();
        IReadOnlyList<Bitmap> images = split(row);
        try
        {
            if (images.Count != textures.Count)
            {
                throw new InvalidDataException("Generated texture part count does not match the selected ROM targets.");
            }

            for (int index = 0; index < images.Count; index++)
            {
                TextureRomService.EncodeAndWrite(romCopy, textures[index], images[index]);
            }

            romCopy.AsSpan().CopyTo(rom);
        }
        finally
        {
            foreach (Bitmap image in images)
            {
                image.Dispose();
            }
        }
    }

    private void ReplaceLastGenerated(Bitmap bitmap)
    {
        _lastGenerated?.Dispose();
        _lastGenerated = (Bitmap)bitmap.Clone();
    }

    private Bitmap CreateDisplayBitmap(Bitmap source, TextTextureTargetItem item)
    {
        if (_selectedTextureKind == TextTextureKind.PauseHeaders && _showPauseOriginalColors && item.PauseHeaderTarget is { } target)
        {
            return PauseHeaderTextureRenderer.ApplyOriginalColors(source, target);
        }

        return (Bitmap)source.Clone();
    }

    private Bitmap CreatePreviewBitmap(Bitmap source)
    {
        double scale = GetPreviewScale();
        int previewWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
        int previewHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
        Bitmap preview = CreateCheckerboard(previewWidth, previewHeight);
        using Graphics graphics = Graphics.FromImage(preview);
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.DrawImage(
            source,
            new Rectangle(0, 0, previewWidth, previewHeight),
            new Rectangle(0, 0, source.Width, source.Height),
            GraphicsUnit.Pixel);
        return preview;
    }

    private static Bitmap CreateCheckerboard(int width, int height)
    {
        const int cellSize = 20;
        Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        for (int y = 0; y < height; y += cellSize)
        {
            for (int x = 0; x < width; x += cellSize)
            {
                bool dark = ((x / cellSize) + (y / cellSize)) % 2 != 0;
                using SolidBrush brush = new(dark ? Color.FromArgb(180, 180, 180) : Color.FromArgb(220, 220, 220));
                graphics.FillRectangle(brush, x, y, cellSize, cellSize);
            }
        }

        return bitmap;
    }

    private void SetPreviewSource(PreviewImageSlot slot, Bitmap bitmap, string name)
    {
        Directory.CreateDirectory(_previewRoot);
        string path = Path.Combine(_previewRoot, $"{++_previewCounter:D5}_{name}.png");
        bitmap.Save(path, ImageFormat.Png);
        slot.SetSource(new Uri(path));
    }

    private void ClearPreviews()
    {
        _lastGenerated?.Dispose();
        _lastGenerated = null;
        int width = Math.Max(1, (int)Math.Round(GetCanvasWidth() * GetPreviewScale()));
        int height = Math.Max(1, (int)Math.Round(GetCanvasHeight() * GetPreviewScale()));
        using Bitmap generatedBlank = CreateCheckerboard(width, height);
        using Bitmap romBlank = CreateCheckerboard(width, height);
        SetPreviewSource(_generatedSlot, generatedBlank, "blank-generated");
        SetPreviewSource(_romSlot, romBlank, "blank-rom");
    }

    private int GetCanvasWidth()
        => _selectedTextureKind switch
        {
            TextTextureKind.ItemNames => ItemNameTextureCatalog.Width,
            TextTextureKind.PausePrompts => GetSelectedTarget()?.Texture?.Width ?? PausePromptTextureCatalog.MaxWidth,
            TextTextureKind.DungeonMapNames => DungeonMapNameTextureCatalog.Width,
            TextTextureKind.FileSelect => GetSelectedTarget()?.Texture?.Width ?? FileSelectTextureCatalog.MaxWidth,
            TextTextureKind.EndTitles => GetSelectedTarget()?.Texture?.Width ?? DefaultEndTitleWidth,
            TextTextureKind.PlaceTitleCards => PlaceTitleCardTextureCatalog.Width,
            TextTextureKind.BossTitleCards => BossTitleCardTextureCatalog.Width,
            TextTextureKind.GameOver => GetSelectedTarget()?.GameOverTarget?.Spec.Width ?? GameOverTextureCatalog.Width,
            TextTextureKind.PauseHeaders => PauseHeaderTextureCatalog.Width,
            _ => throw new InvalidOperationException($"Unsupported text texture kind: {_selectedTextureKind}."),
        };

    private int GetCanvasHeight()
        => _selectedTextureKind switch
        {
            TextTextureKind.ItemNames => ItemNameTextureCatalog.Height,
            TextTextureKind.PausePrompts => PausePromptTextureCatalog.Height,
            TextTextureKind.DungeonMapNames => DungeonMapNameTextureCatalog.Height,
            TextTextureKind.FileSelect => FileSelectTextureCatalog.Height,
            TextTextureKind.EndTitles => GetSelectedTarget()?.Texture?.Height ?? DefaultEndTitleHeight,
            TextTextureKind.PlaceTitleCards => PlaceTitleCardTextureCatalog.Height,
            TextTextureKind.BossTitleCards => BossTitleCardTextureCatalog.Height,
            TextTextureKind.GameOver => GetSelectedTarget()?.GameOverTarget?.Spec.Height ?? GameOverTextureCatalog.Height,
            TextTextureKind.PauseHeaders => PauseHeaderTextureCatalog.Height,
            _ => throw new InvalidOperationException($"Unsupported text texture kind: {_selectedTextureKind}."),
        };

    private double GetPreviewScale()
        => IsContinuePlayingTarget() ? ContinuePlayingPreviewScale : SelectedTextureKind.PreviewScale;
}
