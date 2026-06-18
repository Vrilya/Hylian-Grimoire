using System.Drawing;

namespace HylianGrimoire.TextTextures;

public static partial class BossTitleCardTextureRenderer
{
    private static void PasteLine(Bitmap source, Bitmap destination, int y, bool center, int xNudge)
    {
        if (GetContentBounds(source).IsEmpty)
        {
            return;
        }

        int x = center
            ? (int)((BossTitleCardTextureCatalog.Width - source.Width) / 2d + 0.5) + xNudge
            : xNudge;
        PasteMask(source, destination, x, y);
    }

    private static Rectangle GetContentBounds(Bitmap mask)
        => TextTextureBitmapOps.GetContentBounds(mask);

    private static Bitmap Crop(Bitmap source, Rectangle bounds)
        => TextTextureBitmapOps.Crop(source, bounds);

    private static Bitmap ResizeMask(Bitmap source, int width, int height)
        => TextTextureBitmapOps.ResizeMask(source, width, height);

    private static void PasteMask(Bitmap source, Bitmap destination, int destinationX, int destinationY)
        => TextTextureBitmapOps.PasteMask(source, destination, destinationX, destinationY);

    private static Bitmap CreateBlankMask()
        => TextTextureBitmapOps.CreateBlankMask(BossTitleCardTextureCatalog.Width, BossTitleCardTextureCatalog.Height);
}
