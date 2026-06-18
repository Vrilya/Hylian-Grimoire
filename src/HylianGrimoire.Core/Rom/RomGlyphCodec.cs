using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace HylianGrimoire.Rom;

public static class RomGlyphCodec
{
    public const int GlyphWidth = 16;
    public const int GlyphHeight = 16;

    public static Bitmap DecodeI4Glyph(ReadOnlySpan<byte> glyphBytes)
    {
        if (glyphBytes.Length != RomFontResources.GlyphByteSize)
        {
            throw new InvalidDataException($"ROM glyph payload must be exactly {RomFontResources.GlyphByteSize} bytes.");
        }

        var bitmap = new Bitmap(GlyphWidth, GlyphHeight, PixelFormat.Format32bppArgb);
        Rectangle bounds = new(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            int stride = Math.Abs(data.Stride);
            byte[] pixels = new byte[stride * bitmap.Height];

            for (int y = 0; y < bitmap.Height; y++)
            {
                int row = GetRowOffset(data.Stride, stride, bitmap.Height, y);
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int packedIndex = (y * bitmap.Width + x) / 2;
                    int nibble = x % 2 == 0
                        ? glyphBytes[packedIndex] >> 4
                        : glyphBytes[packedIndex] & 0x0f;
                    byte intensity = (byte)((nibble << 4) | nibble);
                    int offset = row + x * 4;

                    pixels[offset] = intensity;
                    pixels[offset + 1] = intensity;
                    pixels[offset + 2] = intensity;
                    pixels[offset + 3] = 255;
                }
            }

            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bitmap;
    }

    public static byte[] EncodeI4Glyph(string imagePath)
    {
        using var source = new Bitmap(imagePath);
        if (source.Width != GlyphWidth || source.Height != GlyphHeight)
        {
            throw new InvalidDataException($"Glyph image must be {GlyphWidth}x{GlyphHeight} pixels.");
        }

        using Bitmap input = source.PixelFormat == PixelFormat.Format32bppArgb
            ? (Bitmap)source.Clone()
            : CloneAsArgb(source);

        byte[] output = new byte[RomFontResources.GlyphByteSize];
        Rectangle bounds = new(0, 0, input.Width, input.Height);
        BitmapData data = input.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            int stride = Math.Abs(data.Stride);
            byte[] pixels = new byte[stride * input.Height];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

            for (int y = 0; y < input.Height; y++)
            {
                int row = GetRowOffset(data.Stride, stride, input.Height, y);
                for (int x = 0; x < input.Width; x++)
                {
                    int offset = row + x * 4;
                    byte b = pixels[offset];
                    byte g = pixels[offset + 1];
                    byte r = pixels[offset + 2];
                    byte a = pixels[offset + 3];
                    int intensity = Math.Max(r, Math.Max(g, b));
                    intensity = intensity * a / 255;
                    int nibble = Math.Clamp((int)Math.Round(intensity / 17.0), 0, 15);
                    int packedIndex = (y * input.Width + x) / 2;

                    if (x % 2 == 0)
                    {
                        output[packedIndex] = (byte)(nibble << 4);
                    }
                    else
                    {
                        output[packedIndex] |= (byte)nibble;
                    }
                }
            }
        }
        finally
        {
            input.UnlockBits(data);
        }

        return output;
    }

    private static int GetRowOffset(int stride, int absoluteStride, int height, int y)
    {
        return stride < 0 ? (height - 1 - y) * absoluteStride : y * absoluteStride;
    }

    private static Bitmap CloneAsArgb(Bitmap source)
    {
        var clone = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(clone);
        graphics.DrawImage(source, 0, 0, source.Width, source.Height);
        return clone;
    }
}
