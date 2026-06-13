using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class CompactTextTextureRenderer
{
    private static Bitmap BlurAlphaMask(Bitmap source, double radius)
    {
        if (radius <= 0)
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

    private static Bitmap DownsampleAverage(Bitmap source, int scale)
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

    private static Bitmap CreateBlankMask(int width, int height)
        => new(width, height, PixelFormat.Format32bppArgb);
}
