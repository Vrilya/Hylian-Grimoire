using System.Buffers.Binary;
using HylianGrimoire.Textures;

namespace HylianGrimoire.Soh;

internal static class SohResourcePacker
{
    private const int ResourceHeaderSize = 0x40;
    private const ulong ResourceMagic = 0xDEADBEEFDEADBEEF;
    private const uint TextureResourceType = 0x4F544558; // OTEX
    private const uint TextResourceType = 0x4F545854; // OTXT
    private const byte MessageEnd = 0x02;

    private static readonly byte[] PalFontOrder =
    [
        0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x01,
        0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x01,
        0x4f, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5a, 0x01,
        0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x01,
        0x6f, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x01,
        0x20, 0x2d, 0x2e, 0x01,
        0x02, 0x02,
    ];

    public static byte[] PackTexture(TextureDefinition texture, ReadOnlySpan<byte> rawPixels)
    {
        int format = GetTextureFormatId(texture.Format);
        int expectedLength = TextureCodec.GetByteLength(texture.Width, texture.Height, texture.Format);
        if (rawPixels.Length != expectedLength)
        {
            throw new InvalidDataException(
                $"{texture.Group}/{texture.Name} must be {expectedLength} bytes, but was {rawPixels.Length} bytes.");
        }

        byte[] payload = new byte[16 + rawPixels.Length];
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), format);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4, 4), texture.Width);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(8, 4), texture.Height);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(12, 4), rawPixels.Length);
        rawPixels.CopyTo(payload.AsSpan(16));

        return MakeResource(TextureResourceType, version: 0, payload);
    }

    public static string GetTextureResourcePath(TextureDefinition texture)
        => $"{texture.Group.Replace('\\', '/')}/{texture.Name}";

    public static byte[] PackText(ReadOnlySpan<byte> messageData, ReadOnlySpan<byte> tableData, bool addFontOrder)
    {
        var entries = new List<(ushort Id, byte Type, byte Position, byte[] Content)>();
        int index = 0;

        while (index + 7 < tableData.Length)
        {
            ushort id = BinaryPrimitives.ReadUInt16BigEndian(tableData[index..(index + 2)]);
            if (id == 0xffff)
            {
                break;
            }

            if (id is 0xfffc or 0xfffd && addFontOrder)
            {
                entries.Add((0xfffc, 0, 0, PalFontOrder));
                break;
            }

            int rawOffset = BinaryPrimitives.ReadInt32BigEndian(tableData[(index + 4)..(index + 8)]);
            int offset = rawOffset & 0x00ff_ffff;
            byte box = tableData[index + 2];
            byte type = (byte)((box >> 4) & 0x0f);
            byte position = (byte)(box & 0x0f);
            entries.Add((id, type, position, ReadMessageContent(messageData, offset)));
            index += 8;
        }

        using var stream = new MemoryStream();
        Span<byte> scratch = stackalloc byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(scratch[..4], entries.Count);
        stream.Write(scratch[..4]);

        foreach ((ushort id, byte type, byte position, byte[] content) in entries)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(scratch[..2], id);
            stream.Write(scratch[..2]);
            stream.WriteByte(type);
            stream.WriteByte(position);
            BinaryPrimitives.WriteInt32LittleEndian(scratch[..4], content.Length);
            stream.Write(scratch[..4]);
            stream.Write(content);
        }

        return MakeResource(TextResourceType, version: 0, stream.ToArray());
    }

    public static string GetMessageResourcePath(int bankIndex)
        => bankIndex switch
        {
            0 => "text/nes_message_data_static/nes_message_data_static",
            1 => "text/ger_message_data_static/ger_message_data_static",
            2 => "text/fra_message_data_static/fra_message_data_static",
            _ => throw new ArgumentOutOfRangeException(nameof(bankIndex), bankIndex, "Unsupported SoH message bank."),
        };

    public static string JapaneseMessageResourcePath => "text/jpn_message_data_static/jpn_message_data_static";

    public static string CreditsResourcePath => "text/staff_message_data_static/staff_message_data_static";

    private static byte[] ReadMessageContent(ReadOnlySpan<byte> messageData, int offset)
    {
        if (offset < 0 || offset >= messageData.Length)
        {
            return [];
        }

        var content = new List<byte>();
        int position = offset;
        int extra = 0;
        bool done = false;

        while (position < messageData.Length)
        {
            byte value = messageData[position];
            if (value == 0 && extra == 0 && !done)
            {
                break;
            }

            content.Add(value);
            position++;

            if (extra == 0)
            {
                if (value is >= 0x02 and < 0x20)
                {
                    if (value == MessageEnd)
                    {
                        done = true;
                    }
                    else if (value is 0x05 or 0x06 or 0x0c or 0x0e or 0x13 or 0x14 or 0x1e)
                    {
                        extra = 1;
                    }
                    else if (value is 0x07 or 0x11 or 0x12)
                    {
                        extra = 2;
                        if (value == 0x07)
                        {
                            done = true;
                        }
                    }
                    else if (value == 0x15)
                    {
                        extra = 3;
                    }
                }
            }
            else
            {
                extra--;
            }

            if (done && extra == 0)
            {
                break;
            }
        }

        return [.. content];
    }

    private static byte[] MakeResource(uint resourceType, int version, ReadOnlySpan<byte> payload)
    {
        byte[] resource = new byte[ResourceHeaderSize + payload.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(resource.AsSpan(0x04, 4), resourceType);
        BinaryPrimitives.WriteInt32LittleEndian(resource.AsSpan(0x08, 4), version);
        BinaryPrimitives.WriteUInt64LittleEndian(resource.AsSpan(0x0C, 8), ResourceMagic);
        payload.CopyTo(resource.AsSpan(ResourceHeaderSize));
        return resource;
    }

    private static int GetTextureFormatId(TextureFormat format)
        => format switch
        {
            TextureFormat.Rgba32 => 1,
            TextureFormat.Rgba16 => 2,
            TextureFormat.CI4 => 3,
            TextureFormat.CI8 => 4,
            TextureFormat.I4 => 5,
            TextureFormat.I8 => 6,
            TextureFormat.IA4 => 7,
            TextureFormat.IA8 => 8,
            TextureFormat.IA16 => 9,
            _ => throw new InvalidDataException($"Unsupported SoH texture format: {format}."),
        };
}
