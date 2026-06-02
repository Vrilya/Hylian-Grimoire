using System.Drawing;

namespace HylianGrimoire.Textures;

public static partial class TextureCodec
{
    private static Color[] DecodeTlut(ReadOnlySpan<byte> tlutData, int colorCount)
    {
        if (colorCount <= 0)
        {
            throw new InvalidDataException("CI textures require a TLUT color count.");
        }

        int expectedLength = GetTlutByteLength(colorCount);
        if (tlutData.Length != expectedLength)
        {
            throw new InvalidDataException($"TLUT payload must be exactly {expectedLength} bytes for {colorCount} colors.");
        }

        Color[] palette = new Color[colorCount];
        int index = 0;
        for (int i = 0; i < palette.Length; i++)
        {
            ushort value = (ushort)((tlutData[index++] << 8) | tlutData[index++]);
            palette[i] = Color.FromArgb(
                (value & 1) != 0 ? 255 : 0,
                Expand5To8((value >> 11) & 0x1f),
                Expand5To8((value >> 6) & 0x1f),
                Expand5To8((value >> 1) & 0x1f));
        }

        return palette;
    }

    private static void DecodeCI4(ReadOnlySpan<byte> data, byte[] pixels, int width, int height, int bitmapStride, int absoluteStride, IReadOnlyList<Color> palette)
    {
        if (palette.Count < 16)
        {
            throw new InvalidDataException("CI4 textures require at least 16 TLUT colors.");
        }

        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = GetRowOffset(bitmapStride, absoluteStride, height, y);
            for (int x = 0; x < width; x += 2)
            {
                byte packed = data[index++];
                WriteColor(pixels, row + x * 4, palette[(packed >> 4) & 0x0f]);
                if (x + 1 < width)
                {
                    WriteColor(pixels, row + (x + 1) * 4, palette[packed & 0x0f]);
                }
            }
        }
    }

    private static void DecodeCI8(ReadOnlySpan<byte> data, byte[] pixels, int width, int height, int bitmapStride, int absoluteStride, IReadOnlyList<Color> palette)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = GetRowOffset(bitmapStride, absoluteStride, height, y);
            for (int x = 0; x < width; x++)
            {
                int paletteIndex = data[index++];
                if (paletteIndex >= palette.Count)
                {
                    throw new InvalidDataException($"CI8 texture uses palette index {paletteIndex}, but the TLUT only has {palette.Count} colors.");
                }

                WriteColor(pixels, row + x * 4, palette[paletteIndex]);
            }
        }
    }

    private static void DecodeI4(ReadOnlySpan<byte> data, byte[] pixels, int width, int height, int bitmapStride, int absoluteStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = GetRowOffset(bitmapStride, absoluteStride, height, y);
            for (int x = 0; x < width; x += 2)
            {
                byte packed = data[index++];
                WriteGray(pixels, row + x * 4, Expand4To8(packed >> 4), 255);
                if (x + 1 < width)
                {
                    WriteGray(pixels, row + (x + 1) * 4, Expand4To8(packed & 0x0f), 255);
                }
            }
        }
    }

    private static void DecodeI8(ReadOnlySpan<byte> data, byte[] pixels, int width, int height, int bitmapStride, int absoluteStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = GetRowOffset(bitmapStride, absoluteStride, height, y);
            for (int x = 0; x < width; x++)
            {
                WriteGray(pixels, row + x * 4, data[index++], 255);
            }
        }
    }

    private static void DecodeIA4(ReadOnlySpan<byte> data, byte[] pixels, int width, int height, int bitmapStride, int absoluteStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = GetRowOffset(bitmapStride, absoluteStride, height, y);
            for (int x = 0; x < width; x += 2)
            {
                byte packed = data[index++];
                WriteIA4Pixel(pixels, row + x * 4, packed >> 4);
                if (x + 1 < width)
                {
                    WriteIA4Pixel(pixels, row + (x + 1) * 4, packed & 0x0f);
                }
            }
        }
    }

    private static void DecodeIA8(ReadOnlySpan<byte> data, byte[] pixels, int width, int height, int bitmapStride, int absoluteStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = GetRowOffset(bitmapStride, absoluteStride, height, y);
            for (int x = 0; x < width; x++)
            {
                byte packed = data[index++];
                WriteGray(pixels, row + x * 4, Expand4To8(packed >> 4), Expand4To8(packed & 0x0f));
            }
        }
    }

    private static void DecodeIA16(ReadOnlySpan<byte> data, byte[] pixels, int width, int height, int bitmapStride, int absoluteStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = GetRowOffset(bitmapStride, absoluteStride, height, y);
            for (int x = 0; x < width; x++)
            {
                WriteGray(pixels, row + x * 4, data[index++], data[index++]);
            }
        }
    }

    private static void DecodeRgba16(ReadOnlySpan<byte> data, byte[] pixels, int width, int height, int bitmapStride, int absoluteStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = GetRowOffset(bitmapStride, absoluteStride, height, y);
            for (int x = 0; x < width; x++)
            {
                ushort value = (ushort)((data[index++] << 8) | data[index++]);
                byte r = Expand5To8((value >> 11) & 0x1f);
                byte g = Expand5To8((value >> 6) & 0x1f);
                byte b = Expand5To8((value >> 1) & 0x1f);
                byte a = (byte)((value & 1) != 0 ? 255 : 0);
                WriteRgba(pixels, row + x * 4, r, g, b, a);
            }
        }
    }

    private static void DecodeRgba32(ReadOnlySpan<byte> data, byte[] pixels, int width, int height, int bitmapStride, int absoluteStride)
    {
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            int row = GetRowOffset(bitmapStride, absoluteStride, height, y);
            for (int x = 0; x < width; x++)
            {
                WriteRgba(pixels, row + x * 4, data[index++], data[index++], data[index++], data[index++]);
            }
        }
    }
}
