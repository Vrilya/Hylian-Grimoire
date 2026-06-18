using System.Drawing;
using HylianGrimoire.Textures;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class TextureCodecTests
{
    [Theory]
    [InlineData(TextureFormat.CI4, 5, 3, 9)]
    [InlineData(TextureFormat.CI8, 5, 3, 15)]
    [InlineData(TextureFormat.I4, 5, 3, 9)]
    [InlineData(TextureFormat.I8, 5, 3, 15)]
    [InlineData(TextureFormat.IA4, 5, 3, 9)]
    [InlineData(TextureFormat.IA8, 5, 3, 15)]
    [InlineData(TextureFormat.IA16, 5, 3, 30)]
    [InlineData(TextureFormat.Rgba16, 5, 3, 30)]
    [InlineData(TextureFormat.Rgba32, 5, 3, 60)]
    public void GetByteLength_returns_expected_size(TextureFormat format, int width, int height, int expected)
    {
        Assert.Equal(expected, TextureCodec.GetByteLength(width, height, format));
    }

    [Theory]
    [InlineData(TextureFormat.I4, 8, 4)]
    [InlineData(TextureFormat.I8, 8, 4)]
    [InlineData(TextureFormat.IA4, 8, 4)]
    [InlineData(TextureFormat.IA8, 8, 4)]
    [InlineData(TextureFormat.IA16, 8, 4)]
    [InlineData(TextureFormat.Rgba16, 8, 4)]
    [InlineData(TextureFormat.Rgba32, 8, 4)]
    public void Decoded_texture_encodes_back_to_identical_bytes(TextureFormat format, int width, int height)
    {
        byte[] original = CreatePayload(TextureCodec.GetByteLength(width, height, format));

        using Bitmap bitmap = TextureCodec.Decode(original, width, height, format);
        byte[] encoded = TextureCodec.Encode(bitmap, width, height, format);

        Assert.Equal(original, encoded);
    }

    [Theory]
    [InlineData(TextureFormat.CI4, 8, 4, 16)]
    [InlineData(TextureFormat.CI8, 8, 4, 256)]
    public void Decoded_ci_texture_encodes_back_to_identical_bytes(TextureFormat format, int width, int height, int colorCount)
    {
        byte[] tlut = CreateTlut(colorCount);
        byte[] original = CreateIndexedPayload(format, TextureCodec.GetByteLength(width, height, format));

        using Bitmap bitmap = TextureCodec.Decode(original, width, height, format, tlut, colorCount);
        byte[] encoded = TextureCodec.Encode(bitmap, width, height, format, tlut, colorCount, original);

        Assert.Equal(original, encoded);
    }

    [Fact]
    public void Decode_rejects_wrong_payload_length()
    {
        Assert.Throws<InvalidDataException>(() => TextureCodec.Decode([0, 1, 2], 8, 4, TextureFormat.I4));
    }

    [Fact]
    public void Decode_ci_rejects_missing_tlut()
    {
        byte[] payload = CreateIndexedPayload(TextureFormat.CI4, TextureCodec.GetByteLength(8, 4, TextureFormat.CI4));

        Assert.Throws<InvalidDataException>(() => TextureCodec.Decode(payload, 8, 4, TextureFormat.CI4));
    }

    [Fact]
    public void Encode_rejects_wrong_image_size()
    {
        using var bitmap = new Bitmap(4, 4);

        Assert.Throws<InvalidDataException>(() => TextureCodec.Encode(bitmap, 8, 4, TextureFormat.I4));
    }

    [Fact]
    public void Encode_ci_rejects_color_outside_tlut()
    {
        byte[] tlut = CreateTlut(16);
        using var bitmap = new Bitmap(1, 1);
        bitmap.SetPixel(0, 0, Color.Magenta);

        Assert.Throws<InvalidDataException>(() => TextureCodec.Encode(bitmap, 1, 1, TextureFormat.CI4, tlut, 16));
    }

    [Fact]
    public void Encode_ci_rejects_duplicate_tlut_color()
    {
        byte[] tlut = CreateTlut(16);
        tlut[2] = tlut[0];
        tlut[3] = tlut[1];
        using Bitmap bitmap = TextureCodec.Decode([0x01], 2, 1, TextureFormat.CI4, tlut, 16);

        Assert.Throws<InvalidDataException>(() => TextureCodec.Encode(bitmap, 2, 1, TextureFormat.CI4, tlut, 16));
    }

    [Fact]
    public void Encode_ci_uses_original_indices_to_resolve_duplicate_tlut_colors()
    {
        byte[] tlut = CreateTlut(16);
        tlut[2] = tlut[0];
        tlut[3] = tlut[1];
        byte[] original = [0x23];

        using Bitmap bitmap = TextureCodec.Decode(original, 2, 1, TextureFormat.CI4, tlut, 16);
        byte[] encoded = TextureCodec.Encode(bitmap, 2, 1, TextureFormat.CI4, tlut, 16, original);

        Assert.Equal(original, encoded);
    }

    [Fact]
    public void Encode_ci4_changed_pixel_maps_to_unique_non_original_palette_index()
    {
        byte[] tlut = CreateTlut(16);
        byte[] original = [0x23];

        using Bitmap bitmap = TextureCodec.Decode(original, 2, 1, TextureFormat.CI4, tlut, 16);
        using Bitmap replacementColor = TextureCodec.Decode([0x50], 1, 1, TextureFormat.CI4, tlut, 16);
        bitmap.SetPixel(0, 0, replacementColor.GetPixel(0, 0));

        byte[] encoded = TextureCodec.Encode(bitmap, 2, 1, TextureFormat.CI4, tlut, 16, original);

        Assert.Equal([0x53], encoded);
    }

    [Fact]
    public void Encode_ci8_changed_pixel_maps_to_unique_non_original_palette_index()
    {
        byte[] tlut = CreateTlut(256);
        byte[] original = [2, 3];

        using Bitmap bitmap = TextureCodec.Decode(original, 2, 1, TextureFormat.CI8, tlut, 256);
        using Bitmap replacementColor = TextureCodec.Decode([5], 1, 1, TextureFormat.CI8, tlut, 256);
        bitmap.SetPixel(0, 0, replacementColor.GetPixel(0, 0));

        byte[] encoded = TextureCodec.Encode(bitmap, 2, 1, TextureFormat.CI8, tlut, 256, original);

        Assert.Equal([5, 3], encoded);
    }

    private static byte[] CreatePayload(int length)
    {
        byte[] payload = new byte[length];
        for (int i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)((i * 37 + 19) & 0xff);
        }

        return payload;
    }

    private static byte[] CreateIndexedPayload(TextureFormat format, int length)
    {
        byte[] payload = new byte[length];
        for (int i = 0; i < payload.Length; i++)
        {
            payload[i] = format == TextureFormat.CI4
                ? (byte)((i % 16 << 4) | ((i + 1) % 16))
                : (byte)i;
        }

        return payload;
    }

    private static byte[] CreateTlut(int colorCount)
    {
        byte[] tlut = new byte[TextureCodec.GetTlutByteLength(colorCount)];
        for (int i = 0; i < colorCount; i++)
        {
            int r = i & 0x1f;
            int g = (i >> 5) & 0x1f;
            int b = (i >> 10) & 0x1f;
            ushort value = (ushort)((r << 11) | (g << 6) | (b << 1) | 1);
            tlut[i * 2] = (byte)(value >> 8);
            tlut[i * 2 + 1] = (byte)value;
        }

        return tlut;
    }
}
