using System.Drawing;

namespace HylianGrimoire.TextTextures;

public static partial class GameOverTextureRenderer
{
    private static Bitmap Compose(
        Bitmap fillMask,
        Bitmap strokeMask,
        Bitmap blurredStrokeMask,
        GameOverTextureRenderSettings settings)
    {
        Bitmap output = CreateBlankCanvas();
        for (int y = 0; y < GameOverTextureCatalog.Height; y++)
        {
            for (int x = 0; x < GameOverTextureCatalog.Width; x++)
            {
                int fillValue = fillMask.GetPixel(x, y).A;
                if (fillValue > 0)
                {
                    int boosted = Math.Min(255, (int)Math.Round(fillValue * settings.FillBoost / 100d));
                    int gray = fillValue >= settings.WhiteThreshold ? 255 : NearestIa4Step(boosted);
                    output.SetPixel(x, y, Color.FromArgb(255, gray, gray, gray));
                    continue;
                }

                int strokeValue = strokeMask.GetPixel(x, y).A;
                int blurredValue = (int)Math.Round(blurredStrokeMask.GetPixel(x, y).A * settings.BlurStrength / 100d);
                int alpha = NearestIa4Step(Math.Min(255, Math.Max(strokeValue, blurredValue)));
                if (alpha > 0)
                {
                    output.SetPixel(x, y, Color.FromArgb(alpha, 0, 0, 0));
                }
            }
        }

        return output;
    }
}
