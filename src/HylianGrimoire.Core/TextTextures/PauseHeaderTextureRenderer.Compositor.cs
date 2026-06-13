using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class PauseHeaderTextureRenderer
{
    private static Bitmap Compose(Bitmap baseRow, Bitmap fillMask, Bitmap strokeMask, PauseHeaderTextureRenderSettings settings)
    {
        using Bitmap highlightMask = Shifted(strokeMask, settings.HighlightDx, settings.HighlightDy);
        using Bitmap shadowMask = Shifted(strokeMask, settings.ShadowDx, settings.ShadowDy);
        Bitmap output = new(PauseHeaderTextureCatalog.Width, PauseHeaderTextureCatalog.Height, PixelFormat.Format32bppArgb);

        for (int y = 0; y < PauseHeaderTextureCatalog.Height; y++)
        {
            for (int x = 0; x < PauseHeaderTextureCatalog.Width; x++)
            {
                Color baseColor = baseRow.GetPixel(x, y);
                double gray = baseColor.R;
                gray = BlendToward(gray, highlightMask.GetPixel(x, y).A, settings.HighlightGray, settings.HighlightStrength);
                gray = BlendToward(gray, shadowMask.GetPixel(x, y).A, 0, settings.ShadowStrength);

                int strokeOnly = Math.Max(0, strokeMask.GetPixel(x, y).A - fillMask.GetPixel(x, y).A);
                gray = BlendToward(gray, strokeOnly, 0, settings.StrokeStrength);
                gray = BlendToward(gray, fillMask.GetPixel(x, y).A, 0, settings.FillStrength);

                int snapped = NearestGray((int)Math.Round(gray));
                output.SetPixel(x, y, Color.FromArgb(baseColor.A, snapped, snapped, snapped));
            }
        }

        return output;
    }

    private static PauseHeaderPageColor GetInterpolatedPageColor(PauseHeaderColorRamp ramp, int x)
    {
        int tileIndex = Math.Clamp(x / PauseHeaderTextureCatalog.TileWidth, 0, 2);
        int localX = x - tileIndex * PauseHeaderTextureCatalog.TileWidth;
        double t = localX / (double)(PauseHeaderTextureCatalog.TileWidth - 1);
        PauseHeaderPageColor left = ramp.GetColumn(tileIndex);
        PauseHeaderPageColor right = ramp.GetColumn(tileIndex + 1);
        return new(
            Interpolate(left.Red, right.Red, t),
            Interpolate(left.Green, right.Green, t),
            Interpolate(left.Blue, right.Blue, t));
    }

    private static byte Interpolate(byte left, byte right, double t)
        => (byte)Math.Clamp((int)Math.Round(left + (right - left) * t), 0, 255);

    private static int Modulate(byte color, int intensity)
        => Math.Clamp((int)Math.Round(color * Math.Clamp(intensity, 0, 255) / 255d), 0, 255);

    private static double BlendToward(double value, int maskAlpha, int color, double strength)
    {
        if (strength <= 0 || maskAlpha <= 0)
        {
            return value;
        }

        double alpha = Math.Clamp(maskAlpha / 255d * strength, 0, 1);
        return value * (1 - alpha) + color * alpha;
    }
}
