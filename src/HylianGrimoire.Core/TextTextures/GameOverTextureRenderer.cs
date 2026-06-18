using System.Drawing;
using System.Drawing.Drawing2D;

namespace HylianGrimoire.TextTextures;

public static partial class GameOverTextureRenderer
{
    public static Bitmap Render(string text, string fontPath, GameOverTextureRenderSettings settings)
        => Render(text, TextTextureFont.FromPath(fontPath), settings);

    public static Bitmap Render(string text, TextTextureFont font, GameOverTextureRenderSettings settings)
    {
        if (!File.Exists(font.Path))
        {
            throw new FileNotFoundException("Game Over texture font is missing.", font.Path);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return CreateBlankCanvas();
        }

        using TextTextureDrawingFont drawingFont = new(font);
        (Bitmap fillMask, Bitmap strokeMask) = CreateLineMasks(text, drawingFont.Family, drawingFont.Style, settings);
        using (fillMask)
        using (strokeMask)
        using (Bitmap adjustedStroke = AdjustStrokeMask(strokeMask, settings))
        using (Bitmap blurredStroke = BlurAlphaMask(adjustedStroke, settings.BlurRadius))
        {
            return Compose(fillMask, adjustedStroke, blurredStroke, settings);
        }
    }

    public static Bitmap CombineTriplet(IReadOnlyList<Bitmap> parts)
    {
        if (parts.Count != 3)
        {
            throw new InvalidDataException("Game Over preview requires exactly three texture parts.");
        }

        Bitmap output = CreateBlankCanvas();
        using Graphics graphics = Graphics.FromImage(output);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        for (int index = 0; index < parts.Count; index++)
        {
            Bitmap part = parts[index];
            if (part.Width != GameOverTextureCatalog.TileWidth || part.Height != GameOverTextureCatalog.TileHeight)
            {
                throw new InvalidDataException($"Game Over part {index + 1} must be {GameOverTextureCatalog.TileWidth}x{GameOverTextureCatalog.TileHeight}.");
            }

            graphics.DrawImageUnscaled(part, index * GameOverTextureCatalog.TileWidth, 0);
        }

        return output;
    }

    public static IReadOnlyList<Bitmap> SplitTriplet(Bitmap source)
    {
        if (source.Width != GameOverTextureCatalog.Width || source.Height != GameOverTextureCatalog.Height)
        {
            throw new InvalidDataException($"Game Over texture must be {GameOverTextureCatalog.Width}x{GameOverTextureCatalog.Height}.");
        }

        return
        [
            Crop(source, new Rectangle(0, 0, GameOverTextureCatalog.TileWidth, GameOverTextureCatalog.TileHeight)),
            Crop(source, new Rectangle(GameOverTextureCatalog.TileWidth, 0, GameOverTextureCatalog.TileWidth, GameOverTextureCatalog.TileHeight)),
            Crop(source, new Rectangle(GameOverTextureCatalog.TileWidth * 2, 0, GameOverTextureCatalog.TileWidth, GameOverTextureCatalog.TileHeight)),
        ];
    }
}
