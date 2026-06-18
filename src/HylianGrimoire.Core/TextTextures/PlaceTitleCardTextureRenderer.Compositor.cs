using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class PlaceTitleCardTextureRenderer
{
    private static Bitmap Compose(Bitmap fillMask, Bitmap strokeMask, PlaceTitleCardTextureRenderSettings settings)
    {
        Bitmap output = new(PlaceTitleCardTextureCatalog.Width, PlaceTitleCardTextureCatalog.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < PlaceTitleCardTextureCatalog.Height; y++)
        {
            for (int x = 0; x < PlaceTitleCardTextureCatalog.Width; x++)
            {
                int fillValue = fillMask.GetPixel(x, y).A;
                int strokeValue = strokeMask.GetPixel(x, y).A;
                if (fillValue >= settings.FillThreshold)
                {
                    int boosted = Math.Min(255, (int)Math.Round(fillValue * settings.FillBoost / 100d));
                    int gray = fillValue >= settings.WhiteThreshold ? 255 : NearestIa4Step(boosted);
                    gray = Math.Max(gray, settings.FillFloor);
                    output.SetPixel(x, y, Color.FromArgb(255, gray, gray, gray));
                }
                else if (strokeValue > 0)
                {
                    strokeValue = Math.Min(255, (int)Math.Round(strokeValue * settings.StrokeAlpha / 100d));
                    int alpha = NearestIa4Step(strokeValue);
                    if (alpha > 0)
                    {
                        output.SetPixel(x, y, Color.FromArgb(alpha, 0, 0, 0));
                    }
                }
            }
        }

        return output;
    }
}
