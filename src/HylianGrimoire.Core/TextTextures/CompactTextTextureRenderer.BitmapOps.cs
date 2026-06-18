using System.Drawing;

namespace HylianGrimoire.TextTextures;

public static partial class CompactTextTextureRenderer
{
    private static Bitmap BlurAlphaMask(Bitmap source, double radius)
        => TextTextureBitmapOps.BlurAlphaMask(source, radius);

    private static Bitmap DownsampleAverage(Bitmap source, int scale)
        => TextTextureBitmapOps.DownsampleAverage(source, scale);

    private static Bitmap CreateBlankMask(int width, int height)
        => TextTextureBitmapOps.CreateBlankMask(width, height);
}
