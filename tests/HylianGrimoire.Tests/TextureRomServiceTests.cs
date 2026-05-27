using System.Drawing;
using HylianGrimoire.Textures;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class TextureRomServiceTests
{
    private const int TexturePngRoundtripPasses = 10;

    [Fact]
    public void ReadRaw_returns_exact_texture_slice()
    {
        byte[] rom = CreateRom(96);
        var texture = new TextureDefinition("test", "Texture", 16, 8, 4, TextureFormat.I4);

        byte[] raw = TextureRomService.ReadRaw(rom, texture);

        Assert.Equal(rom.Skip(16).Take(16).ToArray(), raw);
    }

    [Fact]
    public void Decode_reads_texture_from_rom_slice()
    {
        byte[] rom = new byte[64];
        var texture = new TextureDefinition("test", "Texture", 8, 4, 2, TextureFormat.I4);
        rom[8] = 0x1f;
        rom[9] = 0x80;
        rom[10] = 0x37;
        rom[11] = 0xc4;

        using Bitmap bitmap = TextureRomService.Decode(rom, texture);
        byte[] encoded = TextureCodec.Encode(bitmap, texture.Width, texture.Height, texture.Format);

        Assert.Equal(rom.Skip(8).Take(4).ToArray(), encoded);
    }

    [Fact]
    public void Decode_ci_reads_texture_and_tlut_from_rom()
    {
        byte[] rom = CreateRom(96);
        var texture = new TextureDefinition("test", "Texture", 8, 4, 2, TextureFormat.CI4, 32, 16);
        byte[] source = [0x01, 0x23, 0x45, 0x67];
        byte[] tlut = CreateTlut(16);
        Array.Copy(source, 0, rom, texture.RomAddress, source.Length);
        Array.Copy(tlut, 0, rom, texture.TlutRomAddress!.Value, tlut.Length);

        using Bitmap bitmap = TextureRomService.Decode(rom, texture);
        byte[] encoded = TextureCodec.Encode(bitmap, texture.Width, texture.Height, texture.Format, tlut, texture.EffectiveTlutColorCount, source);

        Assert.Equal(source, encoded);
    }

    [Fact]
    public void WriteRaw_replaces_only_texture_range()
    {
        byte[] rom = CreateRom(96);
        byte[] before = rom.ToArray();
        var texture = new TextureDefinition("test", "Texture", 20, 4, 4, TextureFormat.I8);
        byte[] replacement = Enumerable.Range(0, 16).Select(i => (byte)(0xf0 + i)).ToArray();

        TextureRomService.WriteRaw(rom, texture, replacement);

        Assert.Equal(before.Take(20), rom.Take(20));
        Assert.Equal(replacement, rom.Skip(20).Take(16));
        Assert.Equal(before.Skip(36), rom.Skip(36));
    }

    [Fact]
    public void EncodeAndWrite_writes_encoded_bitmap_bytes()
    {
        byte[] rom = CreateRom(96);
        var texture = new TextureDefinition("test", "Texture", 12, 4, 4, TextureFormat.Rgba32);
        byte[] source = Enumerable.Range(0, 64).Select(i => (byte)(i * 3)).ToArray();

        using Bitmap bitmap = TextureCodec.Decode(source, texture.Width, texture.Height, texture.Format);
        TextureRomService.EncodeAndWrite(rom, texture, bitmap);

        Assert.Equal(source, rom.Skip(12).Take(64));
    }

    [Fact]
    public void EncodeAndWrite_ci_writes_only_texture_range()
    {
        byte[] rom = CreateRom(128);
        var texture = new TextureDefinition("test", "Texture", 12, 8, 4, TextureFormat.CI4, 64, 16);
        byte[] source = CreateTexturePayload(texture.Format, TextureCodec.GetByteLength(texture.Width, texture.Height, texture.Format));
        byte[] tlut = CreateTlut(texture.EffectiveTlutColorCount);
        Array.Copy(source, 0, rom, texture.RomAddress, source.Length);
        Array.Copy(tlut, 0, rom, texture.TlutRomAddress!.Value, tlut.Length);
        byte[] before = rom.ToArray();

        using Bitmap bitmap = TextureRomService.Decode(rom, texture);
        TextureRomService.EncodeAndWrite(rom, texture, bitmap);

        Assert.Equal(before, rom);
    }

    [Fact]
    public void EncodeAndWrite_ci_preserves_duplicate_tlut_indices_for_unchanged_png()
    {
        byte[] rom = CreateRom(128);
        var texture = new TextureDefinition("test", "Texture", 12, 2, 1, TextureFormat.CI4, 64, 16);
        byte[] source = [0x23];
        byte[] tlut = CreateTlut(texture.EffectiveTlutColorCount);
        tlut[4] = tlut[0];
        tlut[5] = tlut[1];
        tlut[6] = tlut[2];
        tlut[7] = tlut[3];
        Array.Copy(source, 0, rom, texture.RomAddress, source.Length);
        Array.Copy(tlut, 0, rom, texture.TlutRomAddress!.Value, tlut.Length);
        byte[] before = rom.ToArray();

        using Bitmap bitmap = TextureRomService.Decode(rom, texture);
        TextureRomService.EncodeAndWrite(rom, texture, bitmap);

        Assert.Equal(before, rom);
    }

    [Theory]
    [InlineData(TextureFormat.I4, 8, 4)]
    [InlineData(TextureFormat.I8, 8, 4)]
    [InlineData(TextureFormat.IA4, 8, 4)]
    [InlineData(TextureFormat.IA8, 8, 4)]
    [InlineData(TextureFormat.IA16, 8, 4)]
    [InlineData(TextureFormat.Rgba16, 8, 4)]
    [InlineData(TextureFormat.Rgba32, 8, 4)]
    public void Exported_png_can_replace_texture_without_changing_bytes_after_repeated_roundtrips(TextureFormat format, int width, int height)
    {
        int textureLength = TextureCodec.GetByteLength(width, height, format);
        var texture = new TextureDefinition("test/group", "Texture", 24, width, height, format);
        byte[] rom = CreateRom(24 + textureLength + 16);
        byte[] source = CreateTexturePayload(format, textureLength);
        Array.Copy(source, 0, rom, texture.RomAddress, source.Length);
        byte[] before = rom.ToArray();
        string path = Path.Combine(Path.GetTempPath(), $"hylian-grimoire-texture-roundtrip-{Guid.NewGuid():N}.png");

        try
        {
            using (Bitmap bitmap = TextureRomService.Decode(rom, texture))
            {
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }

            for (int pass = 0; pass < TexturePngRoundtripPasses; pass++)
            {
                TextureRomService.EncodeAndWrite(rom, texture, path);
                Assert.Equal(before, rom);

                using Bitmap bitmap = TextureRomService.Decode(rom, texture);
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Theory]
    [InlineData(TextureFormat.CI4, 8, 4, 16)]
    [InlineData(TextureFormat.CI8, 8, 4, 256)]
    public void Exported_ci_png_can_replace_texture_without_changing_bytes_after_repeated_roundtrips(TextureFormat format, int width, int height, int colorCount)
    {
        int textureLength = TextureCodec.GetByteLength(width, height, format);
        int tlutLength = TextureCodec.GetTlutByteLength(colorCount);
        var texture = new TextureDefinition("test/group", "Texture", 24, width, height, format, 24 + textureLength + 8, colorCount);
        byte[] rom = CreateRom(texture.TlutRomAddress!.Value + tlutLength + 16);
        byte[] source = CreateTexturePayload(format, textureLength);
        byte[] tlut = CreateTlut(colorCount);
        Array.Copy(source, 0, rom, texture.RomAddress, source.Length);
        Array.Copy(tlut, 0, rom, texture.TlutRomAddress.Value, tlut.Length);
        byte[] before = rom.ToArray();
        string path = Path.Combine(Path.GetTempPath(), $"hylian-grimoire-ci-texture-roundtrip-{Guid.NewGuid():N}.png");

        try
        {
            using (Bitmap bitmap = TextureRomService.Decode(rom, texture))
            {
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }

            for (int pass = 0; pass < TexturePngRoundtripPasses; pass++)
            {
                TextureRomService.EncodeAndWrite(rom, texture, path);
                Assert.Equal(before, rom);

                using Bitmap bitmap = TextureRomService.Decode(rom, texture);
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void WriteRaw_rejects_wrong_payload_length()
    {
        byte[] rom = CreateRom(64);
        var texture = new TextureDefinition("test", "Texture", 0, 4, 4, TextureFormat.I8);

        Assert.Throws<InvalidDataException>(() => TextureRomService.WriteRaw(rom, texture, [1, 2, 3]));
    }

    [Fact]
    public void ReadRaw_rejects_out_of_range_texture()
    {
        byte[] rom = CreateRom(16);
        var texture = new TextureDefinition("test", "Texture", 12, 8, 4, TextureFormat.I4);

        Assert.Throws<InvalidDataException>(() => TextureRomService.ReadRaw(rom, texture));
    }

    private static byte[] CreateRom(int length)
        => Enumerable.Range(0, length).Select(i => (byte)((i * 17 + 3) & 0xff)).ToArray();

    private static byte[] CreateTexturePayload(TextureFormat format, int length)
    {
        byte[] payload = new byte[length];
        for (int i = 0; i < payload.Length; i++)
        {
            payload[i] = format switch
            {
                TextureFormat.CI4 => (byte)((i % 16 << 4) | ((i + 1) % 16)),
                TextureFormat.CI8 => (byte)i,
                TextureFormat.Rgba32 => (byte)((i % 4) == 3 ? 255 : ((i * 37 + 19) & 0xff)),
                TextureFormat.IA16 => (byte)((i % 2) == 1 ? 255 : ((i * 37 + 19) & 0xff)),
                _ => (byte)((i * 37 + 19) & 0xff),
            };
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
