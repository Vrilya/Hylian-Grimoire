using System.Drawing;

namespace HylianGrimoire.Textures;

public static partial class TextureCodec
{
    private static void EncodeI4(byte[] pixels, byte[] output, int width, int height, int bitmapDataStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = y * bitmapDataStride;
            for (int x = 0; x < width; x += 2)
            {
                byte high = Scale8To4(GetGray(pixels, row + x * 4));
                byte low = x + 1 < width ? Scale8To4(GetGray(pixels, row + (x + 1) * 4)) : (byte)0;
                output[index++] = (byte)((high << 4) | low);
            }
        }
    }

    private static void EncodeCI4(
        byte[] pixels,
        byte[] output,
        int width,
        int height,
        int bitmapDataStride,
        IReadOnlyList<Color> palette,
        ReadOnlySpan<byte> originalIndexedData)
    {
        if (palette.Count < 16)
        {
            throw new InvalidDataException("CI4 textures require at least 16 TLUT colors.");
        }

        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = y * bitmapDataStride;
            for (int x = 0; x < width; x += 2)
            {
                int packedIndex = index;
                byte high = FindPaletteIndex(pixels, row + x * 4, palette, 16, GetOriginalCI4Index(originalIndexedData, packedIndex, highNibble: true));
                byte low = x + 1 < width
                    ? FindPaletteIndex(pixels, row + (x + 1) * 4, palette, 16, GetOriginalCI4Index(originalIndexedData, packedIndex, highNibble: false))
                    : (byte)0;
                output[index++] = (byte)((high << 4) | low);
            }
        }
    }

    private static void EncodeCI8(
        byte[] pixels,
        byte[] output,
        int width,
        int height,
        int bitmapDataStride,
        IReadOnlyList<Color> palette,
        ReadOnlySpan<byte> originalIndexedData)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = y * bitmapDataStride;
            for (int x = 0; x < width; x++)
            {
                output[index] = FindPaletteIndex(pixels, row + x * 4, palette, palette.Count, GetOriginalCI8Index(originalIndexedData, index));
                index++;
            }
        }
    }

    private static void EncodeI8(byte[] pixels, byte[] output, int width, int height, int bitmapDataStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = y * bitmapDataStride;
            for (int x = 0; x < width; x++)
            {
                output[index++] = GetGray(pixels, row + x * 4);
            }
        }
    }

    private static void EncodeIA4(byte[] pixels, byte[] output, int width, int height, int bitmapDataStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = y * bitmapDataStride;
            for (int x = 0; x < width; x += 2)
            {
                byte high = EncodeIA4Pixel(pixels, row + x * 4);
                byte low = x + 1 < width ? EncodeIA4Pixel(pixels, row + (x + 1) * 4) : (byte)0;
                output[index++] = (byte)((high << 4) | low);
            }
        }
    }

    private static void EncodeIA8(byte[] pixels, byte[] output, int width, int height, int bitmapDataStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = y * bitmapDataStride;
            for (int x = 0; x < width; x++)
            {
                int offset = row + x * 4;
                output[index++] = (byte)((Scale8To4(GetGray(pixels, offset)) << 4) | Scale8To4(pixels[offset + 3]));
            }
        }
    }

    private static void EncodeIA16(byte[] pixels, byte[] output, int width, int height, int bitmapDataStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = y * bitmapDataStride;
            for (int x = 0; x < width; x++)
            {
                int offset = row + x * 4;
                output[index++] = GetGray(pixels, offset);
                output[index++] = pixels[offset + 3];
            }
        }
    }

    private static void EncodeRgba16(byte[] pixels, byte[] output, int width, int height, int bitmapDataStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = y * bitmapDataStride;
            for (int x = 0; x < width; x++)
            {
                int offset = row + x * 4;
                byte r5 = Scale8To5(pixels[offset + 2]);
                byte g5 = Scale8To5(pixels[offset + 1]);
                byte b5 = Scale8To5(pixels[offset]);
                byte a1 = (byte)(pixels[offset + 3] != 0 ? 1 : 0);
                ushort value = (ushort)((r5 << 11) | (g5 << 6) | (b5 << 1) | a1);
                output[index++] = (byte)(value >> 8);
                output[index++] = (byte)value;
            }
        }
    }

    private static void EncodeRgba32(byte[] pixels, byte[] output, int width, int height, int bitmapDataStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = y * bitmapDataStride;
            for (int x = 0; x < width; x++)
            {
                int offset = row + x * 4;
                output[index++] = pixels[offset + 2];
                output[index++] = pixels[offset + 1];
                output[index++] = pixels[offset];
                output[index++] = pixels[offset + 3];
            }
        }
    }

    private static byte FindPaletteIndex(byte[] pixels, int offset, IReadOnlyList<Color> palette, int searchCount, int? preferredIndex)
    {
        byte b = pixels[offset];
        byte g = pixels[offset + 1];
        byte r = pixels[offset + 2];
        byte a = pixels[offset + 3];
        int match = -1;
        bool hasDuplicateMatch = false;

        for (int i = 0; i < searchCount; i++)
        {
            Color color = palette[i];
            if (color.R == r && color.G == g && color.B == b && color.A == a)
            {
                match = i;
                if (preferredIndex == i)
                {
                    return (byte)i;
                }

                if (match >= 0)
                {
                    hasDuplicateMatch = true;
                }

                continue;
            }
        }

        if (match < 0)
        {
            throw new InvalidDataException($"PNG color RGBA({r}, {g}, {b}, {a}) does not exist in the texture TLUT.");
        }

        if (hasDuplicateMatch)
        {
            throw new InvalidDataException($"PNG color RGBA({r}, {g}, {b}, {a}) matches multiple TLUT entries and cannot be encoded unambiguously.");
        }

        return (byte)match;
    }

    private static int? GetOriginalCI4Index(ReadOnlySpan<byte> originalIndexedData, int packedIndex, bool highNibble)
    {
        if (originalIndexedData.Length == 0)
        {
            return null;
        }

        byte packed = originalIndexedData[packedIndex];
        return highNibble ? (packed >> 4) & 0x0f : packed & 0x0f;
    }

    private static int? GetOriginalCI8Index(ReadOnlySpan<byte> originalIndexedData, int index)
        => originalIndexedData.Length == 0 ? null : originalIndexedData[index];
}
