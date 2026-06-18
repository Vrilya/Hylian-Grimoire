using System.Drawing;

namespace HylianGrimoire.TextTextures;

public static partial class PauseHeaderTextureRenderer
{
    private static Bitmap Shifted(Bitmap mask, int dx, int dy)
        => TextTextureBitmapOps.ShiftMask(mask, dx, dy, PauseHeaderTextureCatalog.Width, PauseHeaderTextureCatalog.Height);

    private static Rectangle GetContentBounds(Bitmap mask)
        => TextTextureBitmapOps.GetContentBounds(mask);

    private static Bitmap Crop(Bitmap source, Rectangle bounds)
        => TextTextureBitmapOps.CropPixels(source, bounds);

    private static Bitmap ResizeMask(Bitmap source, int width, int height)
        => TextTextureBitmapOps.ResizeMask(source, width, height);

    private static void PasteMask(Bitmap source, Bitmap destination, int destinationX, int destinationY)
        => TextTextureBitmapOps.PasteMask(source, destination, destinationX, destinationY);

    private static Bitmap CloneAsArgb(Bitmap source)
        => TextTextureBitmapOps.CloneAsArgb(source);

    private static int NearestGray(int value)
    {
        int clamped = Math.Clamp(value, 0, 255);
        return TextTextureBitmapOps.NearestStep(clamped, GrayPalette);
    }

    private static Bitmap CreateBlankMask()
        => TextTextureBitmapOps.CreateBlankMask(PauseHeaderTextureCatalog.Width, PauseHeaderTextureCatalog.Height);

    private static Bitmap CreateBlankHighMask(int scale)
        => TextTextureBitmapOps.CreateBlankMask(PauseHeaderTextureCatalog.Width * scale, PauseHeaderTextureCatalog.Height * scale);
}
