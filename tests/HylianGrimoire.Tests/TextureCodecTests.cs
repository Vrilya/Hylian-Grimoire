using System.Drawing;
using HylianGrimoire.Textures;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class TextureCodecTests
{
    [Theory]
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

    [Fact]
    public void Decode_rejects_wrong_payload_length()
    {
        Assert.Throws<InvalidDataException>(() => TextureCodec.Decode([0, 1, 2], 8, 4, TextureFormat.I4));
    }

    [Fact]
    public void Encode_rejects_wrong_image_size()
    {
        using var bitmap = new Bitmap(4, 4);

        Assert.Throws<InvalidDataException>(() => TextureCodec.Encode(bitmap, 8, 4, TextureFormat.I4));
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
}
