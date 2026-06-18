using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace HylianGrimoire.Textures;

public static partial class TextureCodec
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
}
