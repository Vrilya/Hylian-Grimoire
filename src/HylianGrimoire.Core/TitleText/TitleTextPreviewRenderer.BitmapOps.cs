using System.Drawing;
using System.Drawing.Imaging;
using HylianGrimoire.Rom;

namespace HylianGrimoire.TitleText;

public static partial class TitleTextPreviewRenderer
{
    private static Bitmap LoadBackground(string backgroundPath)
    {
        if (File.Exists(backgroundPath))
        {
            using var source = new Bitmap(backgroundPath);
            return source.Clone(new Rectangle(0, 0, source.Width, source.Height), PixelFormat.Format32bppArgb);
        }

        var fallback = new Bitmap(700, 525, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(fallback);
        graphics.Clear(Color.Black);
        return fallback;
    }

    private static Bitmap CreateTintedGlyph(ReadOnlySpan<byte> glyphBytes, Color color)
    {
        if (glyphBytes.Length != RomFontResources.GlyphByteSize)
        {
            throw new InvalidDataException($"ROM glyph payload must be exactly {RomFontResources.GlyphByteSize} bytes.");
        }

        var output = new Bitmap(GlyphSourceSize, GlyphSourceSize, PixelFormat.Format32bppArgb);
        Rectangle bounds = new(0, 0, output.Width, output.Height);
        BitmapData data = output.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            int stride = Math.Abs(data.Stride);
            byte[] pixels = new byte[stride * output.Height];

            for (int y = 0; y < output.Height; y++)
            {
                int row = data.Stride < 0 ? (output.Height - 1 - y) * stride : y * stride;
                for (int x = 0; x < output.Width; x++)
                {
                    int packedIndex = (y * output.Width + x) / 2;
                    int nibble = x % 2 == 0
                        ? glyphBytes[packedIndex] >> 4
                        : glyphBytes[packedIndex] & 0x0f;
                    byte alpha = (byte)((nibble << 4) | nibble);
                    int offset = row + x * 4;
                    pixels[offset] = color.B;
                    pixels[offset + 1] = color.G;
                    pixels[offset + 2] = color.R;
                    pixels[offset + 3] = alpha;
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        }
        finally
        {
            output.UnlockBits(data);
        }

        return output;
    }
}
