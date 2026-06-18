using System.Drawing;

namespace HylianGrimoire.TextTextures;

public static partial class ContinuePlayingTextureRenderer
{
    public static Bitmap Render(string text, string fontPath, ContinuePlayingTextureRenderSettings settings)
        => Render(text, TextTextureFont.FromPath(fontPath), settings);

    public static Bitmap Render(string text, TextTextureFont font, ContinuePlayingTextureRenderSettings settings)
    {
        if (!File.Exists(font.Path))
        {
            throw new FileNotFoundException("Continue Playing texture font is missing.", font.Path);
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
}
