using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace HylianGrimoire.TextTextures;

public static partial class EndTitleTextureRenderer
{
    private const int QuantizationStep = 17;

    private static Bitmap Compose(
        Bitmap fill,
        Bitmap stroke,
        int width,
        int height,
        double strokeStrength,
        double blurRadius,
        double blurStrength,
        Bitmap? assetStroke = null,
        double assetStrokeStrength = 0,
        double assetBlurStrength = 0)
    {
        double[,] blur = BlurAlpha(stroke, blurRadius);
        double[,]? assetBlur = assetStroke is null ? null : BlurAlpha(assetStroke, blurRadius);
        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double strokeAlpha = stroke.GetPixel(x, y).A * strokeStrength;
                double blurAlpha = blur[x, y] * blurStrength;
                double assetStrokeAlpha = assetStroke is null ? 0 : assetStroke.GetPixel(x, y).A * assetStrokeStrength;
                double assetBlurAlpha = assetBlur is null ? 0 : assetBlur[x, y] * assetBlurStrength;
                double blackAlpha = Math.Clamp(Math.Max(Math.Max(strokeAlpha, blurAlpha), Math.Max(assetStrokeAlpha, assetBlurAlpha)), 0, 255);
                double fillAlpha = fill.GetPixel(x, y).A;
                if (blackAlpha <= 0 && fillAlpha <= 0)
                {
                    continue;
                }

                double alpha = fillAlpha + blackAlpha * (1 - fillAlpha / 255d);
                int gray = alpha <= 0 ? 0 : Quantize(255 * fillAlpha / alpha);
                int red = gray;
                int green = gray;
                int blue = gray;
                output.SetPixel(x, y, Color.FromArgb(Quantize(alpha), red, green, blue));
            }
        }

        return output;
    }

    private static void PasteBitmap(Bitmap source, Bitmap destination, int destinationX, int destinationY)
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

    private static void PasteNonTransparent(Bitmap source, Bitmap destination, int destinationX, int destinationY)
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

                Color sourceColor = source.GetPixel(x, y);
                if (sourceColor.A > 0)
                {
                    destination.SetPixel(targetX, targetY, sourceColor);
                }
            }
        }
    }

    private static void AlphaComposite(Bitmap source, Bitmap destination)
    {
        int width = Math.Min(source.Width, destination.Width);
        int height = Math.Min(source.Height, destination.Height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color top = source.GetPixel(x, y);
                if (top.A == 0)
                {
                    continue;
                }

                Color bottom = destination.GetPixel(x, y);
                double topAlpha = top.A / 255d;
                double bottomAlpha = bottom.A / 255d;
                double outputAlpha = topAlpha + bottomAlpha * (1 - topAlpha);
                if (outputAlpha <= 0)
                {
                    destination.SetPixel(x, y, Color.Transparent);
                    continue;
                }

                int red = (int)Math.Round((top.R * topAlpha + bottom.R * bottomAlpha * (1 - topAlpha)) / outputAlpha);
                int green = (int)Math.Round((top.G * topAlpha + bottom.G * bottomAlpha * (1 - topAlpha)) / outputAlpha);
                int blue = (int)Math.Round((top.B * topAlpha + bottom.B * bottomAlpha * (1 - topAlpha)) / outputAlpha);
                destination.SetPixel(
                    x,
                    y,
                    Color.FromArgb(
                        Math.Clamp((int)Math.Round(outputAlpha * 255), 0, 255),
                        Math.Clamp(red, 0, 255),
                        Math.Clamp(green, 0, 255),
                        Math.Clamp(blue, 0, 255)));
            }
        }
    }

    private static void QuantizeBitmap(Bitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color color = bitmap.GetPixel(x, y);
                if (color.A == 0)
                {
                    continue;
                }

                bitmap.SetPixel(
                    x,
                    y,
                    Color.FromArgb(
                        Quantize(color.A),
                        Quantize(color.R),
                        Quantize(color.G),
                        Quantize(color.B)));
            }
        }
    }

    private static void PasteMaxAlpha(Bitmap source, Bitmap destination, int destinationX, int destinationY, double alphaStrength)
    {
        Rectangle sourceBounds = new(0, 0, source.Width, source.Height);
        Rectangle destinationBounds = new(0, 0, destination.Width, destination.Height);
        BitmapData sourceData = source.LockBits(sourceBounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData destinationData = destination.LockBits(destinationBounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        bool useFallback = sourceData.Stride < 0 || destinationData.Stride < 0;
        try
        {
            if (!useFallback)
            {
                int sourceStride = Math.Abs(sourceData.Stride);
                int destinationStride = Math.Abs(destinationData.Stride);
                byte[] sourcePixels = new byte[sourceStride * source.Height];
                byte[] destinationPixels = new byte[destinationStride * destination.Height];
                Marshal.Copy(sourceData.Scan0, sourcePixels, 0, sourcePixels.Length);
                Marshal.Copy(destinationData.Scan0, destinationPixels, 0, destinationPixels.Length);

                for (int y = 0; y < source.Height; y++)
                {
                    int targetY = destinationY + y;
                    if (targetY < 0 || targetY >= destination.Height)
                    {
                        continue;
                    }

                    int sourceRow = GetBitmapRowOffset(sourceData, sourceStride, source.Height, y);
                    int destinationRow = GetBitmapRowOffset(destinationData, destinationStride, destination.Height, targetY);
                    for (int x = 0; x < source.Width; x++)
                    {
                        int targetX = destinationX + x;
                        if (targetX < 0 || targetX >= destination.Width)
                        {
                            continue;
                        }

                        int sourceOffset = sourceRow + x * 4;
                        int alpha = Math.Clamp((int)Math.Round(sourcePixels[sourceOffset + 3] * alphaStrength), 0, 255);
                        if (alpha <= 0)
                        {
                            continue;
                        }

                        int destinationOffset = destinationRow + targetX * 4;
                        if (alpha > destinationPixels[destinationOffset + 3])
                        {
                            destinationPixels[destinationOffset + 0] = sourcePixels[sourceOffset + 0];
                            destinationPixels[destinationOffset + 1] = sourcePixels[sourceOffset + 1];
                            destinationPixels[destinationOffset + 2] = sourcePixels[sourceOffset + 2];
                            destinationPixels[destinationOffset + 3] = (byte)alpha;
                        }
                    }
                }

                Marshal.Copy(destinationPixels, 0, destinationData.Scan0, destinationPixels.Length);
            }
        }
        finally
        {
            source.UnlockBits(sourceData);
            destination.UnlockBits(destinationData);
        }

        if (useFallback)
        {
            PasteMaxAlphaSlow(source, destination, destinationX, destinationY, alphaStrength);
        }
    }

    private static int GetBitmapRowOffset(BitmapData data, int stride, int height, int y)
        => data.Stride >= 0 ? y * stride : (height - 1 - y) * stride;

    private static void PasteMaxAlphaSlow(Bitmap source, Bitmap destination, int destinationX, int destinationY, double alphaStrength)
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

                Color sourceColor = source.GetPixel(x, y);
                int alpha = Math.Clamp((int)Math.Round(sourceColor.A * alphaStrength), 0, 255);
                Color destinationColor = destination.GetPixel(targetX, targetY);
                if (alpha > destinationColor.A)
                {
                    destination.SetPixel(targetX, targetY, Color.FromArgb(alpha, sourceColor.R, sourceColor.G, sourceColor.B));
                }
            }
        }
    }

    private static int Quantize(double value)
    {
        int snapped = (int)Math.Round(Math.Clamp(value, 0, 255) / QuantizationStep) * QuantizationStep;
        return Math.Clamp(snapped, 0, 255);
    }
}
