using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.PromptEditor;

public static partial class PromptEditorPreviewRenderer
{
    private static Bitmap Ia8ToBitmap(ReadOnlySpan<byte> data, int width, int height, int drawWidth, Color color)
    {
        drawWidth = Math.Clamp(drawWidth, 1, width);
        var bitmap = new Bitmap(drawWidth, height, PixelFormat.Format32bppArgb);
        Rectangle bounds = new(0, 0, drawWidth, height);
        BitmapData bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            int stride = Math.Abs(bitmapData.Stride);
            byte[] pixels = new byte[stride * height];
            for (int y = 0; y < height; y++)
            {
                int row = bitmapData.Stride < 0 ? (height - 1 - y) * stride : y * stride;
                for (int x = 0; x < drawWidth; x++)
                {
                    byte value = data[y * width + x];
                    int intensity = (value >> 4) * 17;
                    int alpha = (value & 0x0f) * 17 * color.A / 255;
                    int offset = row + x * 4;
                    pixels[offset] = (byte)(color.B * intensity / 255);
                    pixels[offset + 1] = (byte)(color.G * intensity / 255);
                    pixels[offset + 2] = (byte)(color.R * intensity / 255);
                    pixels[offset + 3] = (byte)alpha;
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

    private static Bitmap ScaleBitmap(Bitmap source, float scale)
    {
        var bitmap = new Bitmap(Scale(source.Width, scale), Scale(source.Height, scale), PixelFormat.Format32bppArgb);
        Rectangle sourceBounds = new(0, 0, source.Width, source.Height);
        Rectangle targetBounds = new(0, 0, bitmap.Width, bitmap.Height);
        BitmapData sourceData = source.LockBits(sourceBounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData targetData = bitmap.LockBits(targetBounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            int sourceStride = Math.Abs(sourceData.Stride);
            int targetStride = Math.Abs(targetData.Stride);
            byte[] sourcePixels = new byte[sourceStride * source.Height];
            byte[] targetPixels = new byte[targetStride * bitmap.Height];
            System.Runtime.InteropServices.Marshal.Copy(sourceData.Scan0, sourcePixels, 0, sourcePixels.Length);

            for (int y = 0; y < bitmap.Height; y++)
            {
                int sourceY = Math.Min(source.Height - 1, (int)(y / scale));
                int sourceRow = sourceData.Stride < 0 ? (source.Height - 1 - sourceY) * sourceStride : sourceY * sourceStride;
                int targetRow = targetData.Stride < 0 ? (bitmap.Height - 1 - y) * targetStride : y * targetStride;
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int sourceX = Math.Min(source.Width - 1, (int)(x / scale));
                    int sourceOffset = sourceRow + sourceX * 4;
                    int targetOffset = targetRow + x * 4;
                    targetPixels[targetOffset] = sourcePixels[sourceOffset];
                    targetPixels[targetOffset + 1] = sourcePixels[sourceOffset + 1];
                    targetPixels[targetOffset + 2] = sourcePixels[sourceOffset + 2];
                    targetPixels[targetOffset + 3] = sourcePixels[sourceOffset + 3];
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(targetPixels, 0, targetData.Scan0, targetPixels.Length);
        }
        finally
        {
            source.UnlockBits(sourceData);
            bitmap.UnlockBits(targetData);
        }

        return bitmap;
    }

    private static Bitmap Ia4ToBitmap(ReadOnlySpan<byte> data, int width, int height, int drawWidth, Color color)
    {
        drawWidth = Math.Clamp(drawWidth, 1, width);
        var bitmap = new Bitmap(drawWidth, height, PixelFormat.Format32bppArgb);
        Rectangle bounds = new(0, 0, drawWidth, height);
        BitmapData bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            int stride = Math.Abs(bitmapData.Stride);
            byte[] pixels = new byte[stride * height];
            for (int y = 0; y < height; y++)
            {
                int row = bitmapData.Stride < 0 ? (height - 1 - y) * stride : y * stride;
                for (int x = 0; x < drawWidth; x++)
                {
                    int pixelIndex = y * width + x;
                    byte packed = data[pixelIndex / 2];
                    int value = (pixelIndex & 1) == 0 ? packed >> 4 : packed & 0x0f;
                    int intensity = ((value >> 1) & 0x07) * 255 / 7;
                    int alpha = (value & 0x01) == 0 ? 0 : color.A;
                    int offset = row + x * 4;
                    pixels[offset] = (byte)(color.B * intensity / 255);
                    pixels[offset + 1] = (byte)(color.G * intensity / 255);
                    pixels[offset + 2] = (byte)(color.R * intensity / 255);
                    pixels[offset + 3] = (byte)alpha;
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }
}
