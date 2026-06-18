using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using HylianGrimoire.Preview;
using HylianGrimoire.Services;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private void ReplaceLastGenerated(Bitmap bitmap, TextTexturePreviewSourceSignature sourceSignature)
    {
        _lastGenerated?.Dispose();
        _lastGenerated = (Bitmap)bitmap.Clone();
        _lastGeneratedSourceSignature = sourceSignature;
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
        PngFileWriter.SaveDirect(bitmap, path);
        slot.SetSource(new Uri(path));
    }

    private void ClearPreviews()
    {
        _lastGenerated?.Dispose();
        _lastGenerated = null;
        _lastGeneratedSourceSignature = null;
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
            TextTextureKind.MapNames => MapNameTextureCatalog.Width,
            TextTextureKind.MapPositionNames => MapPositionNameTextureCatalog.Width,
            TextTextureKind.MajorasMaskItemNames => ItemNameTextureCatalog.Width,
            TextTextureKind.MajorasMaskMapNames => MajorasMaskMapNameTextureCatalog.Width,
            TextTextureKind.PausePrompts => GetSelectedTarget()?.Texture?.Width ?? PausePromptTextureCatalog.MaxWidth,
            TextTextureKind.MajorasMaskPausePrompts => GetSelectedTarget()?.Texture?.Width ?? MajorasMaskPausePromptTextureCatalog.MaxWidth,
            TextTextureKind.DungeonMapNames => DungeonMapNameTextureCatalog.Width,
            TextTextureKind.MajorasMaskDungeonMapNames => MajorasMaskDungeonMapNameTextureCatalog.Width,
            TextTextureKind.FileSelect => GetSelectedTarget()?.Texture?.Width ?? FileSelectTextureCatalog.MaxWidth,
            TextTextureKind.EndTitles => GetSelectedTarget()?.Texture?.Width ?? DefaultEndTitleWidth,
            TextTextureKind.PlaceTitleCards => PlaceTitleCardTextureCatalog.Width,
            TextTextureKind.BossTitleCards => BossTitleCardTextureCatalog.Width,
            TextTextureKind.GameOver => GetSelectedTarget()?.GameOverTarget?.Spec.Width ?? GameOverTextureCatalog.Width,
            TextTextureKind.MajorasMaskGameOver => GetSelectedTarget()?.GameOverTarget?.Spec.Width ?? GameOverTextureCatalog.Width,
            TextTextureKind.PauseHeaders => PauseHeaderTextureCatalog.Width,
            _ => throw new InvalidOperationException($"Unsupported text texture kind: {_selectedTextureKind}."),
        };

    private int GetCanvasHeight()
        => _selectedTextureKind switch
        {
            TextTextureKind.ItemNames => ItemNameTextureCatalog.Height,
            TextTextureKind.MapNames => MapNameTextureCatalog.Height,
            TextTextureKind.MapPositionNames => MapPositionNameTextureCatalog.Height,
            TextTextureKind.MajorasMaskItemNames => ItemNameTextureCatalog.Height,
            TextTextureKind.MajorasMaskMapNames => MajorasMaskMapNameTextureCatalog.Height,
            TextTextureKind.PausePrompts => PausePromptTextureCatalog.Height,
            TextTextureKind.MajorasMaskPausePrompts => MajorasMaskPausePromptTextureCatalog.Height,
            TextTextureKind.DungeonMapNames => DungeonMapNameTextureCatalog.Height,
            TextTextureKind.MajorasMaskDungeonMapNames => MajorasMaskDungeonMapNameTextureCatalog.Height,
            TextTextureKind.FileSelect => FileSelectTextureCatalog.Height,
            TextTextureKind.EndTitles => GetSelectedTarget()?.Texture?.Height ?? DefaultEndTitleHeight,
            TextTextureKind.PlaceTitleCards => PlaceTitleCardTextureCatalog.Height,
            TextTextureKind.BossTitleCards => BossTitleCardTextureCatalog.Height,
            TextTextureKind.GameOver => GetSelectedTarget()?.GameOverTarget?.Spec.Height ?? GameOverTextureCatalog.Height,
            TextTextureKind.MajorasMaskGameOver => GetSelectedTarget()?.GameOverTarget?.Spec.Height ?? GameOverTextureCatalog.Height,
            TextTextureKind.PauseHeaders => PauseHeaderTextureCatalog.Height,
            _ => throw new InvalidOperationException($"Unsupported text texture kind: {_selectedTextureKind}."),
        };

    private double GetPreviewScale()
        => IsContinuePlayingTarget() ? ContinuePlayingPreviewScale : SelectedTextureKind.PreviewScale;
}
