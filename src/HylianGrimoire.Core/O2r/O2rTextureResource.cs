using System.Buffers.Binary;
using HylianGrimoire.Textures;

namespace HylianGrimoire.O2r;

public sealed record O2rTextureResource(
    TextureFormat Format,
    int Width,
    int Height,
    byte[] RawPixels)
{
    public static O2rTextureResource Read(ReadOnlySpan<byte> data)
    {
        if (data.Length < O2rResourcePacker.ResourceHeaderSize + 16)
        {
            throw new InvalidDataException("OTEX resource is too small.");
        }

        uint type = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(0x04, 4));
        if (type != O2rResourcePacker.TextureResourceType)
        {
            throw new InvalidDataException("Resource is not an OTEX texture.");
        }

        ulong magic = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(0x0C, 8));
        if (magic != O2rResourcePacker.ResourceMagic)
        {
            throw new InvalidDataException("OTEX resource has an invalid header.");
        }

        ReadOnlySpan<byte> payload = data[O2rResourcePacker.ResourceHeaderSize..];
        TextureFormat format = ReadFormat(BinaryPrimitives.ReadInt32LittleEndian(payload[..4]));
        int width = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4));
        int height = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4));
        int rawLength = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(12, 4));

        int expectedLength = TextureCodec.GetByteLength(width, height, format);
        if (rawLength != expectedLength)
        {
            throw new InvalidDataException($"OTEX payload must be exactly {expectedLength} bytes for {width}x{height} {format}.");
        }

        if (payload.Length < 16 + rawLength)
        {
            throw new InvalidDataException("OTEX resource is truncated.");
        }

        return new O2rTextureResource(format, width, height, payload.Slice(16, rawLength).ToArray());
    }

    private static TextureFormat ReadFormat(int format)
        => format switch
        {
            1 => TextureFormat.Rgba32,
            2 => TextureFormat.Rgba16,
            3 => TextureFormat.CI4,
            4 => TextureFormat.CI8,
            5 => TextureFormat.I4,
            6 => TextureFormat.I8,
            7 => TextureFormat.IA4,
            8 => TextureFormat.IA8,
            9 => TextureFormat.IA16,
            _ => throw new InvalidDataException($"Unsupported OTEX texture format id: {format}."),
        };
}
