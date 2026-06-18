using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace HylianGrimoire.Textures;

public static partial class TextureCodec
{
    private static void WriteIA4Pixel(byte[] pixels, int offset, int nibble)
    {
        int gray4 = nibble & 0x0e;
        byte gray = (byte)((gray4 << 4) | (gray4 << 1) | (gray4 >> 2));
        byte alpha = (byte)((nibble & 1) != 0 ? 255 : 0);
        WriteGray(pixels, offset, gray, alpha);
    }

    private static byte EncodeIA4Pixel(byte[] pixels, int offset)
        => (byte)((Scale8To3(GetGray(pixels, offset)) << 1) | (pixels[offset + 3] != 0 ? 1 : 0));

    private static void WriteGray(byte[] pixels, int offset, int gray, int alpha)
    {
        pixels[offset] = (byte)gray;
        pixels[offset + 1] = (byte)gray;
        pixels[offset + 2] = (byte)gray;
        pixels[offset + 3] = (byte)alpha;
    }

    private static void WriteRgba(byte[] pixels, int offset, byte r, byte g, byte b, byte a)
    {
        pixels[offset] = b;
        pixels[offset + 1] = g;
        pixels[offset + 2] = r;
        pixels[offset + 3] = a;
    }

    private static void WriteColor(byte[] pixels, int offset, Color color)
        => WriteRgba(pixels, offset, color.R, color.G, color.B, color.A);

    private static byte GetGray(byte[] pixels, int offset)
    {
        byte b = pixels[offset];
        byte g = pixels[offset + 1];
        byte r = pixels[offset + 2];
        return r == g && g == b
            ? r
            : (byte)(0.299 * r + 0.587 * g + 0.114 * b);
    }

    private static byte Expand4To8(int value)
        => (byte)(((value & 0x0f) << 4) | (value & 0x0f));

    private static byte Expand5To8(int value)
    {
        value &= 0x1f;
        return (byte)((value << 3) | (value >> 2));
    }

    private static byte Scale8To3(byte value)
        => (byte)((value >> 5) & 0x07);

    private static byte Scale8To4(byte value)
        => (byte)((value >> 4) & 0x0f);

    private static byte Scale8To5(byte value)
        => (byte)((value >> 3) & 0x1f);

    private static int GetRowOffset(int stride, int absoluteStride, int height, int y)
        => stride < 0 ? (height - 1 - y) * absoluteStride : y * absoluteStride;

    private static byte[] ReadArgbPixels(Bitmap bitmap)
    {
        Rectangle bounds = new(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            int stride = Math.Abs(data.Stride);
            byte[] raw = new byte[stride * bitmap.Height];
            Marshal.Copy(data.Scan0, raw, 0, raw.Length);

            if (data.Stride > 0)
            {
                return raw;
            }

            byte[] topDown = new byte[bitmap.Width * bitmap.Height * 4];
            for (int y = 0; y < bitmap.Height; y++)
            {
                Buffer.BlockCopy(raw, (bitmap.Height - 1 - y) * stride, topDown, y * bitmap.Width * 4, bitmap.Width * 4);
            }

            return topDown;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static Bitmap CloneAsArgb(Bitmap source)
    {
        if (source.PixelFormat == PixelFormat.Format32bppArgb)
        {
            return (Bitmap)source.Clone();
        }

        var clone = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(clone);
        graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
        graphics.DrawImage(source, 0, 0, source.Width, source.Height);
        return clone;
    }

    private static void ValidateDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Texture dimensions must be positive.");
        }
    }
}
