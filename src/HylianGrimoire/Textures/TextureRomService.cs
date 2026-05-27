using System.Drawing;

namespace HylianGrimoire.Textures;

public static class TextureRomService
{
    public static byte[] ReadRaw(ReadOnlySpan<byte> rom, TextureDefinition texture)
    {
        int length = TextureCodec.GetByteLength(texture.Width, texture.Height, texture.Format);
        ValidateRange(rom.Length, texture.RomAddress, length, texture.Name);

        return rom.Slice(texture.RomAddress, length).ToArray();
    }

    public static byte[] ReadTlutRaw(ReadOnlySpan<byte> rom, TextureDefinition texture)
    {
        if (!texture.UsesTlut)
        {
            return [];
        }

        if (texture.TlutRomAddress is not { } tlutAddress)
        {
            throw new InvalidDataException($"Texture {texture.Name} uses {texture.Format} but has no TLUT address.");
        }

        int length = TextureCodec.GetTlutByteLength(texture.EffectiveTlutColorCount);
        ValidateRange(rom.Length, tlutAddress, length, $"{texture.Name} TLUT");

        return rom.Slice(tlutAddress, length).ToArray();
    }

    public static Bitmap Decode(ReadOnlySpan<byte> rom, TextureDefinition texture)
    {
        byte[] raw = ReadRaw(rom, texture);
        if (!texture.UsesTlut)
        {
            return TextureCodec.Decode(raw, texture.Width, texture.Height, texture.Format);
        }

        byte[] tlut = ReadTlutRaw(rom, texture);
        return TextureCodec.Decode(raw, texture.Width, texture.Height, texture.Format, tlut, texture.EffectiveTlutColorCount);
    }

    public static void WriteRaw(Span<byte> rom, TextureDefinition texture, ReadOnlySpan<byte> data)
    {
        int length = TextureCodec.GetByteLength(texture.Width, texture.Height, texture.Format);
        if (data.Length != length)
        {
            throw new InvalidDataException($"Texture payload for {texture.Name} must be exactly {length} bytes.");
        }

        ValidateRange(rom.Length, texture.RomAddress, length, texture.Name);
        data.CopyTo(rom.Slice(texture.RomAddress, length));
    }

    public static void EncodeAndWrite(Span<byte> rom, TextureDefinition texture, Bitmap bitmap)
    {
        byte[] encoded = texture.UsesTlut
            ? TextureCodec.Encode(bitmap, texture.Width, texture.Height, texture.Format, ReadTlutRaw(rom, texture), texture.EffectiveTlutColorCount, ReadRaw(rom, texture))
            : TextureCodec.Encode(bitmap, texture.Width, texture.Height, texture.Format);
        WriteRaw(rom, texture, encoded);
    }

    public static void EncodeAndWrite(Span<byte> rom, TextureDefinition texture, string imagePath)
    {
        byte[] encoded = texture.UsesTlut
            ? TextureCodec.Encode(imagePath, texture.Width, texture.Height, texture.Format, ReadTlutRaw(rom, texture), texture.EffectiveTlutColorCount, ReadRaw(rom, texture))
            : TextureCodec.Encode(imagePath, texture.Width, texture.Height, texture.Format);
        WriteRaw(rom, texture, encoded);
    }

    private static void ValidateRange(int romLength, int address, int length, string textureName)
    {
        if (address < 0 || length < 0 || address > romLength - length)
        {
            throw new InvalidDataException($"Texture {textureName} at 0x{address:x} with length {length} is outside the loaded ROM.");
        }
    }
}
