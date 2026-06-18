using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

internal static class TextTextureBitmapOps
{
    private static readonly int[] IaSteps = [0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255];

    public static Bitmap BlurAlphaMask(Bitmap source, double radius, bool cloneWhenRadiusIsZero = true)
    {
        if (cloneWhenRadiusIsZero && radius <= 0)
        {
            return (Bitmap)source.Clone();
        }

        int kernelRadius = Math.Max(1, (int)Math.Ceiling(radius * 2));
        double[] kernel = CreateGaussianKernel(radius, kernelRadius);
        using Bitmap horizontal = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
        Bitmap output = new(source.Width, source.Height, PixelFormat.Format32bppArgb);

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                double alpha = 0;
                for (int offset = -kernelRadius; offset <= kernelRadius; offset++)
                {
                    alpha += GetAlphaOrZero(source, x + offset, y) * kernel[offset + kernelRadius];
                }

                int value = Math.Clamp((int)Math.Round(alpha), 0, 255);
                horizontal.SetPixel(x, y, Color.FromArgb(value, 255, 255, 255));
            }
        }

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                double alpha = 0;
                for (int offset = -kernelRadius; offset <= kernelRadius; offset++)
                {
                    alpha += GetAlphaOrZero(horizontal, x, y + offset) * kernel[offset + kernelRadius];
                }

                int value = Math.Clamp((int)Math.Round(alpha), 0, 255);
                output.SetPixel(x, y, Color.FromArgb(value, 255, 255, 255));
            }
        }

        return output;
    }

    public static Rectangle GetContentBounds(Bitmap mask)
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

    public static Bitmap Crop(Bitmap source, Rectangle bounds)
    {
        Bitmap output = new(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(output);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.DrawImage(
            source,
            new Rectangle(0, 0, bounds.Width, bounds.Height),
            bounds,
            GraphicsUnit.Pixel);
        return output;
    }

    public static Bitmap CropPixels(Bitmap source, Rectangle bounds)
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

    public static Bitmap ResizeMask(Bitmap source, int width, int height)
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

    public static void PasteMask(Bitmap source, Bitmap destination, int destinationX, int destinationY)
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

    public static Bitmap ShiftMask(Bitmap source, int dx, int dy, int width, int height)
    {
        Bitmap output = CreateBlankMask(width, height);
        PasteMask(source, output, dx, dy);
        return output;
    }

    public static Bitmap CloneAsArgb(Bitmap source)
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

    public static Bitmap DownsampleAverage(Bitmap source, int scale)
    {
        int width = Math.Max(1, source.Width / scale);
        int height = Math.Max(1, source.Height / scale);
        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int total = 0;
                for (int sy = 0; sy < scale; sy++)
                {
                    for (int sx = 0; sx < scale; sx++)
                    {
                        total += source.GetPixel(x * scale + sx, y * scale + sy).A;
                    }
                }

                int value = (int)Math.Round(total / (double)(scale * scale));
                output.SetPixel(x, y, Color.FromArgb(value, 255, 255, 255));
            }
        }

        return output;
    }

    public static int NearestIaStep(int value)
        => NearestStep(value, IaSteps);

    public static int NearestStep(int value, int[] steps)
    {
        int nearest = steps[0];
        int nearestDistance = Math.Abs(value - nearest);
        for (int i = 1; i < steps.Length; i++)
        {
            int distance = Math.Abs(value - steps[i]);
            if (distance < nearestDistance)
            {
                nearest = steps[i];
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    public static Bitmap CreateBlankMask(int width, int height)
        => new(width, height, PixelFormat.Format32bppArgb);

    private static double[] CreateGaussianKernel(double radius, int kernelRadius)
    {
        double sigma = Math.Max(0.1, radius);
        double[] kernel = new double[kernelRadius * 2 + 1];
        double total = 0;
        for (int offset = -kernelRadius; offset <= kernelRadius; offset++)
        {
            double value = Math.Exp(-(offset * offset) / (2 * sigma * sigma));
            kernel[offset + kernelRadius] = value;
            total += value;
        }

        for (int i = 0; i < kernel.Length; i++)
        {
            kernel[i] /= total;
        }

        return kernel;
    }

    private static int GetAlphaOrZero(Bitmap source, int x, int y)
    {
        if (x < 0 || x >= source.Width || y < 0 || y >= source.Height)
        {
            return 0;
        }

        return source.GetPixel(x, y).A;
    }
}
