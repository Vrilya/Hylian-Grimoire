using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class CompactTextTextureRenderer
{
    private static readonly int[] Ia8Steps = [0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255];

    private static Bitmap AdjustStrokeMask(Bitmap mask, CompactTextTextureRenderSettings settings)
    {
        Bitmap output = new(mask.Width, mask.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < mask.Height; y++)
        {
            for (int x = 0; x < mask.Width; x++)
            {
                int value = mask.GetPixel(x, y).A;
                if (value == 0)
                {
                    continue;
                }

                int scaled = Math.Min(255, (int)Math.Round(value * settings.StrokeAlpha / 100d));
                output.SetPixel(x, y, Color.FromArgb(scaled, 255, 255, 255));
            }
        }

        return output;
    }

    private static Bitmap Compose(
        Bitmap fillMask,
        Bitmap strokeMask,
        Bitmap blurredStrokeMask,
        CompactTextTextureRenderSettings settings,
        int width,
        int height)
    {
        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int fillValue = fillMask.GetPixel(x, y).A;
                int strokeValue = strokeMask.GetPixel(x, y).A;
                int blurredValue = Math.Min(255, (int)Math.Round(blurredStrokeMask.GetPixel(x, y).A * settings.StrokeBlurStrength / 100d));
                bool hasFill = fillValue >= settings.FillThreshold;
                bool hasStroke = strokeValue >= settings.StrokeThreshold || blurredValue >= settings.StrokeThreshold;
                if (!hasFill && !hasStroke)
                {
                    continue;
                }

                if (hasFill)
                {
                    int boosted = Math.Min(255, (int)Math.Round(fillValue * settings.FillBoost / 100d));
                    int gray = GetFillGray(fillValue, boosted, settings);
                    if (settings.BlendFillAndStrokeEdges && hasStroke)
                    {
                        output.SetPixel(x, y, BlendFillOverStroke(gray, Math.Max(strokeValue, blurredValue)));
                        continue;
                    }

                    output.SetPixel(x, y, Color.FromArgb(255, gray, gray, gray));
                    continue;
                }

                int alpha = NearestIa8Step(Math.Max(strokeValue, blurredValue));
                if (alpha > 0)
                {
                    output.SetPixel(x, y, Color.FromArgb(alpha, 0, 0, 0));
                }
            }
        }

        return output;
    }

    private static int GetFillGray(int fillValue, int boosted, CompactTextTextureRenderSettings settings)
    {
        int gray = fillValue >= settings.WhiteThreshold ? 255 : NearestIa8Step(boosted);
        return Math.Max(gray, settings.FillFloor);
    }

    private static Color BlendFillOverStroke(int fillAlpha, int strokeAlpha)
    {
        double whiteAlpha = Math.Clamp(fillAlpha, 0, 255);
        double blackAlpha = Math.Clamp(strokeAlpha, 0, 255);
        double alpha = whiteAlpha + blackAlpha * (1 - whiteAlpha / 255d);
        if (alpha <= 0)
        {
            return Color.Transparent;
        }

        int outputAlpha = NearestIa8Step((int)Math.Round(alpha));
        int gray = NearestIa8Step((int)Math.Round(255 * whiteAlpha / alpha));
        return Color.FromArgb(outputAlpha, gray, gray, gray);
    }

    private static int NearestIa8Step(int value)
    {
        int nearest = Ia8Steps[0];
        int nearestDistance = Math.Abs(value - nearest);
        for (int i = 1; i < Ia8Steps.Length; i++)
        {
            int distance = Math.Abs(value - Ia8Steps[i]);
            if (distance < nearestDistance)
            {
                nearest = Ia8Steps[i];
                nearestDistance = distance;
            }
        }

        return nearest;
    }
}
