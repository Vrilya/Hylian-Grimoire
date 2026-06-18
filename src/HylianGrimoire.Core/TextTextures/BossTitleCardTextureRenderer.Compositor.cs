using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class BossTitleCardTextureRenderer
{
    private static Bitmap Compose(BossTitleCardComponents components, BossTitleCardTextureRenderSettings settings)
    {
        Bitmap output = new(BossTitleCardTextureCatalog.Width, BossTitleCardTextureCatalog.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < BossTitleCardTextureCatalog.Height; y++)
        {
            for (int x = 0; x < BossTitleCardTextureCatalog.Width; x++)
            {
                int bottomFillValue = components.BottomFill.GetPixel(x, y).A;
                int bottomStrokeValue = components.BottomStroke.GetPixel(x, y).A;
                int topFillValue = components.TopFill.GetPixel(x, y).A;
                int topStrokeValue = components.TopStroke.GetPixel(x, y).A;

                if (bottomFillValue > 0)
                {
                    int gray = bottomFillValue >= settings.BottomWhiteThreshold
                        ? 255
                        : NearestIa4Step(Math.Min(255, (int)Math.Round(bottomFillValue * settings.BottomFillBoost / 100d)));
                    output.SetPixel(x, y, Color.FromArgb(255, gray, gray, gray));
                }
                else if (bottomStrokeValue > 0)
                {
                    int alpha = NearestIa4Step(bottomStrokeValue);
                    if (alpha > 0)
                    {
                        output.SetPixel(x, y, Color.FromArgb(alpha, 0, 0, 0));
                    }
                }
                else if (topFillValue >= settings.TopFillMin)
                {
                    int gray = topFillValue >= settings.TopWhiteThreshold
                        ? 255
                        : NearestIa4Step(Math.Min(255, (int)Math.Round(topFillValue * settings.TopFillBoost / 100d)));
                    output.SetPixel(x, y, Color.FromArgb(255, gray, gray, gray));
                }
                else if (topStrokeValue > 0)
                {
                    int alpha = NearestIa4Step(topStrokeValue);
                    if (alpha > 0)
                    {
                        output.SetPixel(x, y, Color.FromArgb(alpha, 0, 0, 0));
                    }
                }
            }
        }

        return output;
    }

    private static int NearestIa4Step(int value)
        => TextTextureBitmapOps.NearestIaStep(value);

    private sealed record BossTitleCardComponents(Bitmap TopFill, Bitmap TopStroke, Bitmap BottomFill, Bitmap BottomStroke);
}
