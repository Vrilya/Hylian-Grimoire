using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class EndTitleTextureRenderer
{
    private const double PresentedByFontSize = 12.0;
    private const double PresentedByWidthScale = 0.88;
    private const double PresentedByX = 9.0;
    private const double PresentedByY = 4.0;
    private const double PresentedByStrokeWidth = 1.50;
    private const double PresentedByStrokeStrength = 0.85;
    private const double PresentedByBlurRadius = 1.00;
    private const double PresentedByBlurStrength = 1.60;
    private const double PresentedByWhiteStrength = 1.00;
    private const double PresentedByWhiteSpread = 0.40;
    private const int PresentedByFitHorizontalInset = 1;
    private const double TheEndFontSize = 21.0;
    private const double TheEndWidthScale = 0.869;
    private const double TheEndX = 5.0;
    private const double TheEndY = 4.0;
    private const double TheEndStrokeWidth = 2.10;
    private const double TheEndStrokeStrength = 1.65;
    private const double TheEndBlurRadius = 1.10;
    private const double TheEndBlurStrength = 2.15;
    private const double TheEndWhiteStrength = 1.00;
    private const double TheEndWhiteSpread = 0.20;

    private static Bitmap RenderPresentedBy(
        string text,
        TextTextureFont font,
        EndTitleTextureRenderSettings settings,
        int width,
        int height)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        CachedImageSharpFontFace face = GetImageSharpFontFace(font);
        SixLabors.Fonts.FontStyle style = settings.PresentedByBold
            ? SixLabors.Fonts.FontStyle.Bold
            : face.Style;

        int horizontalPadding = Math.Max(width, 128);
        int renderWidth = width + horizontalPadding * 2;
        double x = horizontalPadding + PresentedByX + settings.XNudge;
        double y = PresentedByY + settings.YOffset;

        using Bitmap fill = RenderPresentedByMask(
            text.Trim(),
            face.Family,
            style,
            settings,
            renderWidth,
            height,
            x,
            y,
            PresentedByWhiteSpread,
            alphaStrength: PresentedByWhiteStrength);
        using Bitmap stroke = RenderPresentedByMask(
            text.Trim(),
            face.Family,
            style,
            settings,
            renderWidth,
            height,
            x,
            y,
            PresentedByStrokeWidth,
            alphaStrength: 1.0);
        Bitmap composed = Compose(fill, stroke, renderWidth, height, PresentedByStrokeStrength, PresentedByBlurRadius, PresentedByBlurStrength);
        using (composed)
        {
            return FitAlphaToCanvas(composed, width, height, settings.Center, (int)Math.Round(PresentedByX + settings.XNudge));
        }
    }

    private static Bitmap RenderTheEnd(
        string text,
        TextTextureFont font,
        EndTitleTextureRenderSettings settings,
        int width,
        int height)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        CachedImageSharpFontFace face = GetImageSharpFontFace(font);

        int horizontalPadding = settings.Center ? Math.Max(width, 128) : 0;
        int renderWidth = width + horizontalPadding * 2;
        double x = horizontalPadding + TheEndX + (settings.Center ? 0 : settings.XNudge);
        double y = TheEndY + settings.YOffset;
        using Bitmap fill = RenderSimpleLineMask(
            text.Trim(),
            face.Family,
            face.Style,
            settings,
            renderWidth,
            height,
            TheEndFontSize,
            TheEndWidthScale,
            x,
            y,
            TheEndStrokeWidth,
            TheEndWhiteSpread,
            alphaStrength: TheEndWhiteStrength);
        using Bitmap stroke = RenderSimpleLineMask(
            text.Trim(),
            face.Family,
            face.Style,
            settings,
            renderWidth,
            height,
            TheEndFontSize,
            TheEndWidthScale,
            x,
            y,
            TheEndStrokeWidth,
            TheEndStrokeWidth,
            alphaStrength: 1.0);
        Bitmap composed = Compose(fill, stroke, renderWidth, height, TheEndStrokeStrength, TheEndBlurRadius, TheEndBlurStrength);
        if (!settings.Center)
        {
            return composed;
        }

        using (composed)
        {
            return FitAlphaToCanvas(composed, width, height, center: true, destinationX: 0);
        }
    }

    private static Bitmap RenderPresentedByMask(
        string text,
        SixLabors.Fonts.FontFamily family,
        SixLabors.Fonts.FontStyle style,
        EndTitleTextureRenderSettings settings,
        int width,
        int height,
        double x,
        double y,
        double strokeWidth,
        double alphaStrength)
        => RenderSimpleLineMask(
            text,
            family,
            style,
            settings,
            width,
            height,
            PresentedByFontSize,
            PresentedByWidthScale,
            x,
            y,
            Math.Max(PresentedByStrokeWidth, PresentedByWhiteSpread),
            strokeWidth,
            alphaStrength);
}
