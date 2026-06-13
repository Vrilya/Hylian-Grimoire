using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class PauseHeaderTextureRenderer
{
    private static Bitmap Shifted(Bitmap mask, int dx, int dy)
    {
        Bitmap output = CreateBlankMask();
        for (int y = 0; y < mask.Height; y++)
        {
            int targetY = y + dy;
            if (targetY < 0 || targetY >= mask.Height)
            {
                continue;
            }

            for (int x = 0; x < mask.Width; x++)
            {
                int targetX = x + dx;
                if (targetX < 0 || targetX >= mask.Width)
                {
                    continue;
                }

                output.SetPixel(targetX, targetY, mask.GetPixel(x, y));
            }
        }

        return output;
    }

    private static Rectangle GetContentBounds(Bitmap mask)
    {
        int left = mask.Width;
        int top = mask.Height;
        int right = -1;
        int bottom = -1;
        for (int y = 0; y < mask.Height; y++)
        {
            for (int x = 0; x < mask.Width; x++)
            {
                if (mask.GetPixel(x, y).A == 0)
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return right < left || bottom < top ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static Bitmap Crop(Bitmap source, Rectangle bounds)
    {
        Bitmap output = new(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < bounds.Height; y++)
        {
            for (int x = 0; x < bounds.Width; x++)
            {
                output.SetPixel(x, y, source.GetPixel(bounds.X + x, bounds.Y + y));
            }
        }

        return output;
    }

    private static Bitmap ResizeMask(Bitmap source, int width, int height)
    {
        if (source.Width == width && source.Height == height)
        {
            return (Bitmap)source.Clone();
        }

        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(output);
        graphics.Clear(Color.Transparent);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.DrawImage(
            source,
            new Rectangle(0, 0, width, height),
            new Rectangle(0, 0, source.Width, source.Height),
            GraphicsUnit.Pixel);
        return output;
    }

    private static void PasteMask(Bitmap source, Bitmap destination, int destinationX, int destinationY)
    {
        for (int y = 0; y < source.Height; y++)
        {
            int targetY = destinationY + y;
            if (targetY < 0 || targetY >= destination.Height)
            {
                continue;
            }

            for (int x = 0; x < source.Width; x++)
            {
                int targetX = destinationX + x;
                if (targetX < 0 || targetX >= destination.Width)
                {
                    continue;
                }

                destination.SetPixel(targetX, targetY, source.GetPixel(x, y));
            }
        }
    }

    private static Bitmap CloneAsArgb(Bitmap source)
    {
        Bitmap output = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                output.SetPixel(x, y, source.GetPixel(x, y));
            }
        }

        return output;
    }

    private static int NearestGray(int value)
    {
        int clamped = Math.Clamp(value, 0, 255);
        int nearest = GrayPalette[0];
        int nearestDistance = Math.Abs(clamped - nearest);
        for (int i = 1; i < GrayPalette.Length; i++)
        {
            int distance = Math.Abs(clamped - GrayPalette[i]);
            if (distance < nearestDistance)
            {
                nearest = GrayPalette[i];
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private static Bitmap CreateBlankMask()
        => new(PauseHeaderTextureCatalog.Width, PauseHeaderTextureCatalog.Height, PixelFormat.Format32bppArgb);

    private static Bitmap CreateBlankHighMask(int scale)
        => new(PauseHeaderTextureCatalog.Width * scale, PauseHeaderTextureCatalog.Height * scale, PixelFormat.Format32bppArgb);
}
