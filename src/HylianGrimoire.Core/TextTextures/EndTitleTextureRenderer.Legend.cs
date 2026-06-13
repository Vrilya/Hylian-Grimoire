using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class EndTitleTextureRenderer
{
    private const double LegendWidthScale = 0.81;
    private const double LegendHeightScale = 0.842;
    private const double LegendTracking = -0.55;
    private const double LegendX = 2.0;
    private const double LegendY = 7.0;
    private const double LegendStrokeWidth = 2.20;
    private const double LegendStrokeStrength = 0.80;
    private const double LegendBlurRadius = 0.90;
    private const double LegendBlurStrength = 1.20;
    private const double LegendWhiteStrength = 1.00;
    private const double LegendWhiteSpread = 0.20;
    private const double LegendRegX = 110.0;
    private const double LegendRegY = 0.0;
    private const int LegendRegStampThreshold = 170;
    private const int LegendRegStampX = 2;
    private const int LegendRegStampY = 2;
    private const double LegendRegStrokeWidth = 1.10;
    private const double LegendRegStrokeStrength = 0.80;
    private const double LegendRegBlurRadius = 0.65;
    private const double LegendRegBlurStrength = 0.95;

    private static Bitmap RenderLegendOfZelda(
        string text,
        TextTextureFont font,
        string? legendStampPath,
        EndTitleTextureRenderSettings settings,
        int width,
        int height)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        if (settings.LegendShowRegistered
            && (string.IsNullOrWhiteSpace(legendStampPath) || !File.Exists(legendStampPath)))
        {
            throw new FileNotFoundException("Legend of Zelda registered-mark stamp asset is missing.", legendStampPath);
        }

        CachedDrawingFontFace face = GetDrawingFontFace(font);

        double x = LegendX + settings.XNudge;
        double y = LegendY + settings.YOffset;
        using Bitmap fill = RenderTrackedLineMask(
            text.Trim(),
            face.Family,
            face.Style,
            settings,
            width,
            height,
            Math.Max(1.0, settings.LegendFontSize),
            LegendWidthScale,
            LegendHeightScale,
            x,
            y,
            Math.Max(LegendStrokeWidth, LegendWhiteSpread),
            LegendWhiteSpread,
            LegendWhiteStrength,
            LegendTracking);
        using Bitmap stroke = RenderTrackedLineMask(
            text.Trim(),
            face.Family,
            face.Style,
            settings,
            width,
            height,
            Math.Max(1.0, settings.LegendFontSize),
            LegendWidthScale,
            LegendHeightScale,
            x,
            y,
            Math.Max(LegendStrokeWidth, LegendWhiteSpread),
            LegendStrokeWidth,
            alphaStrength: 1.0,
            tracking: LegendTracking);
        double textBlack = Math.Max(0, settings.LegendTextBlack);
        Bitmap composed = Compose(
            fill,
            stroke,
            width,
            height,
            LegendStrokeStrength * textBlack,
            LegendBlurRadius,
            LegendBlurStrength * textBlack);
        if (!settings.LegendShowRegistered)
        {
            return composed;
        }

        try
        {
            using Bitmap stamp = RenderLegendRegisteredStamp(legendStampPath!, settings, width, height);
            AlphaComposite(stamp, composed);
            QuantizeBitmap(composed);
            return composed;
        }
        catch
        {
            composed.Dispose();
            throw;
        }
    }

    private static Bitmap RenderLegendRegisteredStamp(
        string stampPath,
        EndTitleTextureRenderSettings settings,
        int width,
        int height)
    {
        using Bitmap source = new(stampPath);
        Bitmap fill = new(width, height, PixelFormat.Format32bppArgb);
        int targetX = (int)Math.Round(LegendRegX + settings.XNudge + settings.LegendRegisteredXNudge) + LegendRegStampX;
        int targetY = (int)Math.Round(LegendRegY + settings.YOffset) + LegendRegStampY;

        for (int y = 0; y < source.Height; y++)
        {
            int destinationY = targetY + y;
            if (destinationY < 0 || destinationY >= height)
            {
                continue;
            }

            for (int x = 0; x < source.Width; x++)
            {
                int destinationX = targetX + x;
                if (destinationX < 0 || destinationX >= width)
                {
                    continue;
                }

                Color pixel = source.GetPixel(x, y);
                if (pixel.A <= 0
                    || pixel.R < LegendRegStampThreshold
                    || pixel.G < LegendRegStampThreshold
                    || pixel.B < LegendRegStampThreshold)
                {
                    continue;
                }

                fill.SetPixel(destinationX, destinationY, pixel);
            }
        }

        try
        {
            int scale = Math.Max(1, settings.RenderScale);
            using Bitmap high = ResizeNearest(fill, width * scale, height * scale);
            using Bitmap dilated = MaxFilterAlpha(high, Math.Max(1, (int)Math.Round(LegendRegStrokeWidth * scale)));
            using Bitmap strokeLow = ResizeMaskLanczos(dilated, width, height);
            double[,] blurred = BlurAlpha(dilated, LegendRegBlurRadius * scale);
            using Bitmap blurredHigh = CreateAlphaBitmap(blurred, dilated.Width, dilated.Height);
            using Bitmap blurLow = ResizeMaskLanczos(blurredHigh, width, height);

            Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
            double regBlack = Math.Max(0, settings.LegendRegisteredBlack);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double strokeAlpha = strokeLow.GetPixel(x, y).A * LegendRegStrokeStrength * regBlack;
                    double blurAlpha = blurLow.GetPixel(x, y).A * LegendRegBlurStrength * regBlack;
                    int alpha = Quantize(Math.Max(strokeAlpha, blurAlpha));
                    if (alpha > 0)
                    {
                        output.SetPixel(x, y, Color.FromArgb(alpha, 0, 0, 0));
                    }
                }
            }

            AlphaComposite(fill, output);
            return output;
        }
        finally
        {
            fill.Dispose();
        }
    }
}
