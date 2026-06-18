using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HylianGrimoire.TextTextures;

public static partial class EndTitleTextureRenderer
{
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

    private static Bitmap ResizeNearest(Bitmap source, int width, int height)
    {
        if (source.Width == width && source.Height == height)
        {
            return (Bitmap)source.Clone();
        }

        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(output);
        graphics.Clear(Color.Transparent);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.DrawImage(
            source,
            new Rectangle(0, 0, width, height),
            new Rectangle(0, 0, source.Width, source.Height),
            GraphicsUnit.Pixel);
        return output;
    }

    private static Bitmap ResizeMaskLanczos(Bitmap source, int width, int height)
    {
        if (source.Width == width && source.Height == height)
        {
            return (Bitmap)source.Clone();
        }

        using SixLabors.ImageSharp.Image<Rgba32> image = ToImageSharp(source);
        image.Mutate(context => context.Resize(width, height, KnownResamplers.Lanczos3));
        return ToBitmap(image);
    }

    private static SixLabors.ImageSharp.Image<Rgba32> ToImageSharp(Bitmap source)
    {
        SixLabors.ImageSharp.Image<Rgba32> image = new(source.Width, source.Height);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Color color = source.GetPixel(x, y);
                image[x, y] = new Rgba32(color.R, color.G, color.B, color.A);
            }
        }

        return image;
    }

    private static Bitmap ToBitmap(SixLabors.ImageSharp.Image<Rgba32> image)
    {
        Bitmap bitmap = new(image.Width, image.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Rgba32 pixel = image[x, y];
                bitmap.SetPixel(x, y, Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B));
            }
        }

        return bitmap;
    }

    private static Bitmap MaxFilterAlpha(Bitmap source, int radius)
    {
        int width = source.Width;
        int height = source.Height;
        double[,] alpha = new double[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                alpha[x, y] = source.GetPixel(x, y).A;
            }
        }

        double[,] temp = new double[width, height];
        double[,] output = new double[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int sampleX = Math.Clamp(x + k, 0, width - 1);
                    value = Math.Max(value, alpha[sampleX, y]);
                }

                temp[x, y] = value;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int sampleY = Math.Clamp(y + k, 0, height - 1);
                    value = Math.Max(value, temp[x, sampleY]);
                }

                output[x, y] = value;
            }
        }

        return CreateAlphaBitmap(output, width, height);
    }

    private static Bitmap CreateAlphaBitmap(double[,] alpha, int width, int height)
    {
        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int value = Math.Clamp((int)Math.Round(alpha[x, y]), 0, 255);
                if (value > 0)
                {
                    output.SetPixel(x, y, Color.FromArgb(value, 255, 255, 255));
                }
            }
        }

        return output;
    }

    private static Bitmap FitAlphaToCanvas(Bitmap source, int width, int height, bool center, int destinationX)
    {
        Rectangle bounds = GetAlphaBounds(source);
        if (bounds.IsEmpty)
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        int maxWidth = Math.Max(1, width - PresentedByFitHorizontalInset * 2);
        int maxHeight = Math.Max(1, height);
        double widthFitScale = bounds.Width > maxWidth ? maxWidth / (double)bounds.Width : 1d;
        double heightFitScale = bounds.Height > maxHeight ? maxHeight / (double)bounds.Height : 1d;
        double fitScale = Math.Min(widthFitScale, heightFitScale);
        int targetWidth = Math.Max(1, (int)Math.Round(bounds.Width * fitScale));
        int targetHeight = Math.Max(1, (int)Math.Round(bounds.Height * fitScale));

        using Bitmap cropped = Crop(source, bounds);
        using Bitmap fitted = ResizeMaskLanczos(cropped, targetWidth, targetHeight);
        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        int targetX = center
            ? (int)((width - targetWidth) / 2d + 0.5)
            : destinationX;
        int targetY = (int)((height - targetHeight) / 2d + 0.5);
        PasteBitmap(fitted, output, targetX, targetY);
        return output;
    }

    private static Bitmap Crop(Bitmap source, Rectangle bounds)
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

    private static Rectangle GetAlphaBounds(Bitmap source)
    {
        int left = source.Width;
        int top = source.Height;
        int right = -1;
        int bottom = -1;
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                if (source.GetPixel(x, y).A == 0)
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return right < left || bottom < top
            ? Rectangle.Empty
            : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static double[,] BlurAlpha(Bitmap source, double radius)
    {
        int width = source.Width;
        int height = source.Height;
        double[,] alpha = new double[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                alpha[x, y] = source.GetPixel(x, y).A;
            }
        }

        if (radius <= 0)
        {
            return alpha;
        }

        double[] kernel = CreateGaussianKernel(radius);
        int kernelRadius = kernel.Length / 2;
        double[,] temp = new double[width, height];
        double[,] output = new double[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = 0;
                for (int k = -kernelRadius; k <= kernelRadius; k++)
                {
                    int sampleX = Math.Clamp(x + k, 0, width - 1);
                    value += alpha[sampleX, y] * kernel[k + kernelRadius];
                }

                temp[x, y] = value;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = 0;
                for (int k = -kernelRadius; k <= kernelRadius; k++)
                {
                    int sampleY = Math.Clamp(y + k, 0, height - 1);
                    value += temp[x, sampleY] * kernel[k + kernelRadius];
                }

                output[x, y] = value;
            }
        }

        return output;
    }

    private static double[] CreateGaussianKernel(double radius)
    {
        int kernelRadius = Math.Max(1, (int)Math.Ceiling(radius * 3));
        double sigma = Math.Max(0.001, radius);
        double[] kernel = new double[kernelRadius * 2 + 1];
        double total = 0;
        for (int i = -kernelRadius; i <= kernelRadius; i++)
        {
            double value = Math.Exp(-(i * i) / (2 * sigma * sigma));
            kernel[i + kernelRadius] = value;
            total += value;
        }

        for (int i = 0; i < kernel.Length; i++)
        {
            kernel[i] /= total;
        }

        return kernel;
    }
}
