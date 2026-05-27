using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace HylianGrimoire.Textures;

public static class TextureCodec
{
    public static int GetByteLength(int width, int height, TextureFormat format)
    {
        ValidateDimensions(width, height);

        return format switch
        {
            TextureFormat.CI4 or TextureFormat.I4 or TextureFormat.IA4 => ((width + 1) / 2) * height,
            TextureFormat.CI8 or TextureFormat.I8 or TextureFormat.IA8 => width * height,
            TextureFormat.IA16 or TextureFormat.Rgba16 => width * height * 2,
            TextureFormat.Rgba32 => width * height * 4,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown texture format."),
        };
    }

    public static int GetTlutByteLength(int colorCount)
    {
        if (colorCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(colorCount), "TLUT color count must be positive.");
        }

        return colorCount * 2;
    }

    public static Bitmap Decode(ReadOnlySpan<byte> data, int width, int height, TextureFormat format)
        => Decode(data, width, height, format, tlutData: [], tlutColorCount: 0);

    public static Bitmap Decode(
        ReadOnlySpan<byte> data,
        int width,
        int height,
        TextureFormat format,
        ReadOnlySpan<byte> tlutData,
        int tlutColorCount)
    {
        int expectedLength = GetByteLength(width, height, format);
        if (data.Length != expectedLength)
        {
            throw new InvalidDataException($"Texture payload must be exactly {expectedLength} bytes for {width}x{height} {format}.");
        }

        Color[] palette = format is TextureFormat.CI4 or TextureFormat.CI8
            ? DecodeTlut(tlutData, tlutColorCount)
            : [];

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        Rectangle bounds = new(0, 0, width, height);
        BitmapData bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            int stride = Math.Abs(bitmapData.Stride);
            byte[] pixels = new byte[stride * height];

            switch (format)
            {
                case TextureFormat.CI4:
                    DecodeCI4(data, pixels, width, height, bitmapData.Stride, stride, palette);
                    break;
                case TextureFormat.CI8:
                    DecodeCI8(data, pixels, width, height, bitmapData.Stride, stride, palette);
                    break;
                case TextureFormat.I4:
                    DecodeI4(data, pixels, width, height, bitmapData.Stride, stride);
                    break;
                case TextureFormat.I8:
                    DecodeI8(data, pixels, width, height, bitmapData.Stride, stride);
                    break;
                case TextureFormat.IA4:
                    DecodeIA4(data, pixels, width, height, bitmapData.Stride, stride);
                    break;
                case TextureFormat.IA8:
                    DecodeIA8(data, pixels, width, height, bitmapData.Stride, stride);
                    break;
                case TextureFormat.IA16:
                    DecodeIA16(data, pixels, width, height, bitmapData.Stride, stride);
                    break;
                case TextureFormat.Rgba16:
                    DecodeRgba16(data, pixels, width, height, bitmapData.Stride, stride);
                    break;
                case TextureFormat.Rgba32:
                    DecodeRgba32(data, pixels, width, height, bitmapData.Stride, stride);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown texture format.");
            }

            Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

    public static byte[] Encode(string imagePath, int width, int height, TextureFormat format)
    {
        using var bitmap = new Bitmap(imagePath);
        return Encode(bitmap, width, height, format);
    }

    public static byte[] Encode(string imagePath, int width, int height, TextureFormat format, ReadOnlySpan<byte> tlutData, int tlutColorCount)
    {
        using var bitmap = new Bitmap(imagePath);
        return Encode(bitmap, width, height, format, tlutData, tlutColorCount);
    }

    public static byte[] Encode(
        string imagePath,
        int width,
        int height,
        TextureFormat format,
        ReadOnlySpan<byte> tlutData,
        int tlutColorCount,
        ReadOnlySpan<byte> originalIndexedData)
    {
        using var bitmap = new Bitmap(imagePath);
        return Encode(bitmap, width, height, format, tlutData, tlutColorCount, originalIndexedData);
    }

    public static byte[] Encode(Bitmap source, int width, int height, TextureFormat format)
        => Encode(source, width, height, format, tlutData: [], tlutColorCount: 0);

    public static byte[] Encode(
        Bitmap source,
        int width,
        int height,
        TextureFormat format,
        ReadOnlySpan<byte> tlutData,
        int tlutColorCount)
        => Encode(source, width, height, format, tlutData, tlutColorCount, originalIndexedData: []);

    public static byte[] Encode(
        Bitmap source,
        int width,
        int height,
        TextureFormat format,
        ReadOnlySpan<byte> tlutData,
        int tlutColorCount,
        ReadOnlySpan<byte> originalIndexedData)
    {
        ValidateDimensions(width, height);
        if (source.Width != width || source.Height != height)
        {
            throw new InvalidDataException($"Texture image must be {width}x{height} pixels.");
        }

        using Bitmap bitmap = CloneAsArgb(source);
        byte[] pixels = ReadArgbPixels(bitmap);
        byte[] output = new byte[GetByteLength(width, height, format)];
        Color[] palette = format is TextureFormat.CI4 or TextureFormat.CI8
            ? DecodeTlut(tlutData, tlutColorCount)
            : [];
        if ((format is TextureFormat.CI4 or TextureFormat.CI8) && originalIndexedData.Length is not 0 && originalIndexedData.Length != output.Length)
        {
            throw new InvalidDataException($"Original indexed payload must be exactly {output.Length} bytes for {width}x{height} {format}.");
        }

        switch (format)
        {
            case TextureFormat.CI4:
                EncodeCI4(pixels, output, width, height, bitmapDataStride: width * 4, palette, originalIndexedData);
                break;
            case TextureFormat.CI8:
                EncodeCI8(pixels, output, width, height, bitmapDataStride: width * 4, palette, originalIndexedData);
                break;
            case TextureFormat.I4:
                EncodeI4(pixels, output, width, height, bitmapDataStride: width * 4);
                break;
            case TextureFormat.I8:
                EncodeI8(pixels, output, width, height, bitmapDataStride: width * 4);
                break;
            case TextureFormat.IA4:
                EncodeIA4(pixels, output, width, height, bitmapDataStride: width * 4);
                break;
            case TextureFormat.IA8:
                EncodeIA8(pixels, output, width, height, bitmapDataStride: width * 4);
                break;
            case TextureFormat.IA16:
                EncodeIA16(pixels, output, width, height, bitmapDataStride: width * 4);
                break;
            case TextureFormat.Rgba16:
                EncodeRgba16(pixels, output, width, height, bitmapDataStride: width * 4);
                break;
            case TextureFormat.Rgba32:
                EncodeRgba32(pixels, output, width, height, bitmapDataStride: width * 4);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown texture format.");
        }

        return output;
    }

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
