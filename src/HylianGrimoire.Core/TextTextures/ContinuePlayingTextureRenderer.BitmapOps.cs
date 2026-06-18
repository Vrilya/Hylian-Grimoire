using System.Drawing;

namespace HylianGrimoire.TextTextures;

public static partial class ContinuePlayingTextureRenderer
{
    private static Bitmap BlurAlphaMask(Bitmap source, double radius)
        => TextTextureBitmapOps.BlurAlphaMask(source, radius);

    private static Rectangle GetContentBounds(Bitmap mask)
        => TextTextureBitmapOps.GetContentBounds(mask);

    private static Bitmap Crop(Bitmap source, Rectangle bounds)
        => TextTextureBitmapOps.Crop(source, bounds);

    private static Bitmap ResizeMask(Bitmap source, int width, int height)
        => TextTextureBitmapOps.ResizeMask(source, width, height);

    private static void PasteMask(Bitmap source, Bitmap destination, int destinationX, int destinationY)
        => TextTextureBitmapOps.PasteMask(source, destination, destinationX, destinationY);

    private static int NearestIa8Step(int value)
        => TextTextureBitmapOps.NearestIaStep(value);

    private static Bitmap CreateBlankCanvas()
        => TextTextureBitmapOps.CreateBlankMask(GameOverTextureCatalog.ContinuePlayingWidth, GameOverTextureCatalog.ContinuePlayingHeight);

    private static Bitmap CreateBlankMask()
        => CreateBlankCanvas();
}
