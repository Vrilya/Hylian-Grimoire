using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class BossTitleCardTextureRenderer
{
    public static Bitmap Render(
        string topText,
        string bottomText,
        string topFontPath,
        string bottomFontPath,
        BossTitleCardTextureRenderSettings settings)
        => Render(topText, bottomText, TextTextureFont.FromPath(topFontPath), TextTextureFont.FromPath(bottomFontPath), settings);

    public static Bitmap Render(
        string topText,
        string bottomText,
        TextTextureFont topFont,
        TextTextureFont bottomFont,
        BossTitleCardTextureRenderSettings settings)
    {
        if (!File.Exists(topFont.Path))
        {
            throw new FileNotFoundException("Boss title-card top font is missing.", topFont.Path);
        }

        if (!File.Exists(bottomFont.Path))
        {
            throw new FileNotFoundException("Boss title-card boss font is missing.", bottomFont.Path);
        }

        if (string.IsNullOrWhiteSpace(topText) && string.IsNullOrWhiteSpace(bottomText))
        {
            return new Bitmap(BossTitleCardTextureCatalog.Width, BossTitleCardTextureCatalog.Height, PixelFormat.Format32bppArgb);
        }

        using TextTextureDrawingFont topDrawingFont = new(topFont);
        using TextTextureDrawingFont bottomDrawingFont = new(bottomFont);

        BossTitleCardComponents components = RenderComponents(
            topText,
            bottomText,
            topDrawingFont.Family,
            topDrawingFont.Style,
            bottomDrawingFont.Family,
            bottomDrawingFont.Style,
            settings);
        using (components.TopFill)
        using (components.TopStroke)
        using (components.BottomFill)
        using (components.BottomStroke)
        {
            return Compose(components, settings);
        }
    }

    private static BossTitleCardComponents RenderComponents(
        string topText,
        string bottomText,
        FontFamily topFamily,
        FontStyle topStyle,
        FontFamily bottomFamily,
        FontStyle bottomStyle,
        BossTitleCardTextureRenderSettings settings)
    {
        Bitmap topFillCanvas = CreateBlankMask();
        Bitmap topStrokeCanvas = CreateBlankMask();
        Bitmap bottomFillCanvas = CreateBlankMask();
        Bitmap bottomStrokeCanvas = CreateBlankMask();

        (Bitmap topFill, Bitmap topStroke) = CreateLineMasks(
            topText,
            topFamily,
            topStyle,
            settings.TopFontSize,
            settings.TopStrokeWidth,
            100.0,
            settings.TopMaxWidth,
            tracking: 0,
            pairKerning: new Dictionary<string, int>(StringComparer.Ordinal),
            fitHeight: false,
            settings.RenderScale);
        using (topFill)
        using (topStroke)
        using (Bitmap adjustedTopStroke = AdjustStrokeMask(topStroke, settings.TopStrokeAlpha, settings.TopStrokeGamma))
        {
            Dictionary<string, int> bottomKerning = new(StringComparer.Ordinal)
            {
                ["LV"] = settings.BottomLvKerning,
            };
            (Bitmap bottomFill, Bitmap bottomStroke) = CreateLineMasks(
                bottomText,
                bottomFamily,
                bottomStyle,
                settings.BottomFontSize,
                settings.BottomStrokeWidth,
                settings.BottomWidthScale,
                settings.BottomMaxWidth,
                settings.BottomTracking,
                bottomKerning,
                fitHeight: false,
                settings.RenderScale);
            using (bottomFill)
            using (bottomStroke)
            using (Bitmap adjustedBottomStroke = AdjustStrokeMask(bottomStroke, settings.BottomStrokeAlpha, settings.BottomStrokeGamma))
            {
                if (!string.IsNullOrWhiteSpace(topText))
                {
                    PasteLine(topFill, topFillCanvas, settings.TopY, settings.TopCenter, settings.TopXNudge);
                    PasteLine(adjustedTopStroke, topStrokeCanvas, settings.TopY, settings.TopCenter, settings.TopXNudge);
                    PasteLine(bottomFill, bottomFillCanvas, settings.BottomY, settings.BottomCenter, settings.BottomXNudge);
                    PasteLine(adjustedBottomStroke, bottomStrokeCanvas, settings.BottomY, settings.BottomCenter, settings.BottomXNudge);
                }
                else
                {
                    int bottomY = (int)((BossTitleCardTextureCatalog.Height - adjustedBottomStroke.Height) / 2d + 0.5);
                    PasteLine(bottomFill, bottomFillCanvas, bottomY, settings.BottomCenter, settings.BottomXNudge);
                    PasteLine(adjustedBottomStroke, bottomStrokeCanvas, bottomY, settings.BottomCenter, settings.BottomXNudge);
                }
            }
        }

        return new BossTitleCardComponents(topFillCanvas, topStrokeCanvas, bottomFillCanvas, bottomStrokeCanvas);
    }
}
