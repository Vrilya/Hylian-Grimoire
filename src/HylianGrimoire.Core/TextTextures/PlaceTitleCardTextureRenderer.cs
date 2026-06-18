using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class PlaceTitleCardTextureRenderer
{
    public static Bitmap Render(string text, string fontPath, PlaceTitleCardTextureRenderSettings settings)
        => Render(text, TextTextureFont.FromPath(fontPath), settings);

    public static Bitmap Render(string text, TextTextureFont font, PlaceTitleCardTextureRenderSettings settings)
    {
        if (!File.Exists(font.Path))
        {
            throw new FileNotFoundException("Place title-card texture font is missing.", font.Path);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new Bitmap(PlaceTitleCardTextureCatalog.Width, PlaceTitleCardTextureCatalog.Height, PixelFormat.Format32bppArgb);
        }

        using TextTextureDrawingFont drawingFont = new(font);

        (Bitmap fillMask, Bitmap strokeMask) = CreateTextMasks(text, drawingFont.Family, drawingFont.Style, settings);
        using (fillMask)
        using (strokeMask)
        {
            return Compose(fillMask, strokeMask, settings);
        }
    }
}
