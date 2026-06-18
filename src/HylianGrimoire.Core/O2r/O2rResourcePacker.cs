using System.Buffers.Binary;
using HylianGrimoire.Codecs.MajorasMask;
using HylianGrimoire.Textures;

namespace HylianGrimoire.O2r;

public static class O2rResourcePacker
{
    public const int ResourceHeaderSize = 0x40;
    public const ulong ResourceMagic = 0xDEADBEEFDEADBEEF;
    public const uint TextureResourceType = 0x4F544558; // OTEX
    public const uint SohTextResourceType = 0x4F545854; // OTXT
    public const uint MajorasMaskTextResourceType = 0x4F54584D; // OTXM

    private const byte OotMessageEnd = 0x02;
    private const byte MajorasMaskMessageEnd = 0xbf;
    private const int MajorasMaskHeaderSize = 11;

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

    public static byte[] PackSohText(ReadOnlySpan<byte> messageData, ReadOnlySpan<byte> tableData, bool addFontOrder)
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
            entries.Add((id, type, position, ReadOotMessageContent(messageData, offset)));
            index += 8;
        }

        return MakeResource(SohTextResourceType, version: 0, WriteSohTextPayload(entries));
    }

    public static byte[] PackMajorasMaskText(
        ReadOnlySpan<byte> messageData,
        ReadOnlySpan<byte> tableData,
        bool messageDataHasHeaders)
    {
        var entries = new List<MajorasMaskTextEntry>();
        int index = 0;

        while (index + 7 < tableData.Length)
        {
            ushort id = BinaryPrimitives.ReadUInt16BigEndian(tableData[index..(index + 2)]);
            if (id == 0xffff)
            {
                break;
            }

            int rawOffset = BinaryPrimitives.ReadInt32BigEndian(tableData[(index + 4)..(index + 8)]);
            int offset = rawOffset & 0x00ff_ffff;
            entries.Add(messageDataHasHeaders
                ? ReadMajorasMaskMessageEntry(id, messageData, offset)
                : ReadMajorasMaskCreditsEntry(id, tableData[index + 2], messageData, offset));
            index += 8;
        }

        return MakeResource(MajorasMaskTextResourceType, version: 0, WriteMajorasMaskTextPayload(entries));
    }

    public static byte[] MakeResource(uint resourceType, int version, ReadOnlySpan<byte> payload)
    {
        byte[] resource = new byte[ResourceHeaderSize + payload.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(resource.AsSpan(0x04, 4), resourceType);
        BinaryPrimitives.WriteInt32LittleEndian(resource.AsSpan(0x08, 4), version);
        BinaryPrimitives.WriteUInt64LittleEndian(resource.AsSpan(0x0C, 8), ResourceMagic);
        payload.CopyTo(resource.AsSpan(ResourceHeaderSize));
        return resource;
    }

    private static byte[] WriteSohTextPayload(IReadOnlyList<(ushort Id, byte Type, byte Position, byte[] Content)> entries)
    {
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
            WriteLengthPrefixedBytes(stream, scratch, content);
        }

        return stream.ToArray();
    }

    private static byte[] WriteMajorasMaskTextPayload(IReadOnlyList<MajorasMaskTextEntry> entries)
    {
        using var stream = new MemoryStream();
        Span<byte> scratch = stackalloc byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(scratch[..4], entries.Count);
        stream.Write(scratch[..4]);

        foreach (MajorasMaskTextEntry entry in entries)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(scratch[..2], entry.Id);
            stream.Write(scratch[..2]);
            stream.WriteByte(entry.Type);
            stream.WriteByte(entry.Position);
            BinaryPrimitives.WriteUInt16LittleEndian(scratch[..2], entry.Icon);
            stream.Write(scratch[..2]);
            BinaryPrimitives.WriteUInt16LittleEndian(scratch[..2], entry.NextMessageId);
            stream.Write(scratch[..2]);
            BinaryPrimitives.WriteUInt16LittleEndian(scratch[..2], entry.FirstItemCost);
            stream.Write(scratch[..2]);
            BinaryPrimitives.WriteUInt16LittleEndian(scratch[..2], entry.SecondItemCost);
            stream.Write(scratch[..2]);
            WriteLengthPrefixedBytes(stream, scratch, entry.Message);
        }

        return stream.ToArray();
    }

    private static MajorasMaskTextEntry ReadMajorasMaskMessageEntry(ushort id, ReadOnlySpan<byte> messageData, int offset)
    {
        if (offset < 0 || offset + MajorasMaskHeaderSize > messageData.Length)
        {
            throw new InvalidDataException($"Message 0x{id:x4} is too short for a Majora's Mask header.");
        }

        ushort properties = BinaryPrimitives.ReadUInt16BigEndian(messageData[offset..(offset + 2)]);
        byte type = (byte)((properties >> 8) & 0x0f);
        byte position = (byte)((properties >> 4) & 0x0f);
        ushort icon = messageData[offset + 2];
        ushort nextMessageId = BinaryPrimitives.ReadUInt16BigEndian(messageData[(offset + 3)..(offset + 5)]);
        ushort firstItemCost = BinaryPrimitives.ReadUInt16BigEndian(messageData[(offset + 5)..(offset + 7)]);
        ushort secondItemCost = BinaryPrimitives.ReadUInt16BigEndian(messageData[(offset + 7)..(offset + 9)]);
        byte[] message = ReadMajorasMaskMessageContent(messageData, offset + MajorasMaskHeaderSize);

        return new MajorasMaskTextEntry(id, type, position, icon, nextMessageId, firstItemCost, secondItemCost, message);
    }

    private static MajorasMaskTextEntry ReadMajorasMaskCreditsEntry(
        ushort id,
        byte typePosition,
        ReadOnlySpan<byte> messageData,
        int offset)
    {
        byte type = (byte)((typePosition >> 4) & 0x0f);
        byte position = (byte)(typePosition & 0x0f);
        byte[] message = ReadOotMessageContent(messageData, offset);

        return new MajorasMaskTextEntry(
            id,
            type,
            position,
            Icon: 0xfe,
            NextMessageId: 0xffff,
            FirstItemCost: 0xffff,
            SecondItemCost: 0xffff,
            Message: message);
    }

    private static void WriteLengthPrefixedBytes(Stream stream, Span<byte> scratch, byte[] content)
    {
        BinaryPrimitives.WriteInt32LittleEndian(scratch[..4], content.Length);
        stream.Write(scratch[..4]);
        stream.Write(content);
    }

    private static byte[] ReadOotMessageContent(ReadOnlySpan<byte> messageData, int offset)
        => ReadMessageContent(messageData, offset, OotMessageEnd, stopAtZeroPadding: true, GetOotArgumentByteCount);

    private static byte[] ReadMajorasMaskMessageContent(ReadOnlySpan<byte> messageData, int offset)
        => ReadMessageContent(messageData, offset, MajorasMaskMessageEnd, stopAtZeroPadding: false, GetMajorasMaskArgumentByteCount);

    private static byte[] ReadMessageContent(
        ReadOnlySpan<byte> messageData,
        int offset,
        byte endByte,
        bool stopAtZeroPadding,
        Func<byte, int> getArgumentByteCount)
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
            if (stopAtZeroPadding && value == 0 && extra == 0 && !done)
            {
                break;
            }

            content.Add(value);
            position++;

            if (extra == 0)
            {
                if (value == endByte)
                {
                    done = true;
                }
                else
                {
                    extra = getArgumentByteCount(value);
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

    private static int GetOotArgumentByteCount(byte value) =>
        value switch
        {
            0x05 or 0x06 or 0x0c or 0x0e or 0x13 or 0x14 or 0x1e => 1,
            0x07 or 0x11 or 0x12 => 2,
            0x15 => 3,
            _ => 0,
        };

    private static int GetMajorasMaskArgumentByteCount(byte value)
    {
        if (MmMessageTokenMaps.OneByteArgumentTags.ContainsKey(value))
        {
            return 1;
        }

        return MmMessageTokenMaps.TwoByteArgumentTags.ContainsKey(value) ? 2 : 0;
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
            _ => throw new InvalidDataException($"Unsupported O2R texture format: {format}."),
        };

    private sealed record MajorasMaskTextEntry(
        ushort Id,
        byte Type,
        byte Position,
        ushort Icon,
        ushort NextMessageId,
        ushort FirstItemCost,
        ushort SecondItemCost,
        byte[] Message);
}
