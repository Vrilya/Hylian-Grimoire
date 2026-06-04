using System.Buffers.Binary;
using HylianGrimoire.O2r;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class O2rResourcePackerTests
{
    [Fact]
    public void PackSohTextWritesOtxtPayload()
    {
        byte[] table =
        [
            0x12, 0x34, 0x21, 0x00, 0x07, 0x00, 0x00, 0x00,
            0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        ];
        byte[] message = [0x48, 0x69, 0x02, 0x00];

        byte[] resource = O2rResourcePacker.PackSohText(message, table, addFontOrder: true);
        ReadOnlySpan<byte> payload = resource.AsSpan(O2rResourcePacker.ResourceHeaderSize);

        Assert.Equal(O2rResourcePacker.SohTextResourceType, ReadResourceType(resource));
        Assert.Equal(1, BinaryPrimitives.ReadInt32LittleEndian(payload[..4]));
        Assert.Equal(0x1234, BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4, 2)));
        Assert.Equal(2, payload[6]);
        Assert.Equal(1, payload[7]);
        Assert.Equal(3, BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4)));
        Assert.Equal([0x48, 0x69, 0x02], payload.Slice(12, 3).ToArray());
    }

    [Fact]
    public void PackMajorasMaskTextWritesOtxmPayloadWithoutRomHeaderBytes()
    {
        byte[] table =
        [
            0x12, 0x34, 0x23, 0x00, 0x08, 0x00, 0x00, 0x00,
            0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        ];
        byte[] message =
        [
            0x02, 0x30, 0x04, 0x56, 0x78, 0x00, 0x0a, 0xff, 0xfe, 0x12, 0x34,
            0x00, 0x48, 0x69, 0xbf, 0x00,
        ];

        byte[] resource = O2rResourcePacker.PackMajorasMaskText(message, table, messageDataHasHeaders: true);
        ReadOnlySpan<byte> payload = resource.AsSpan(O2rResourcePacker.ResourceHeaderSize);

        Assert.Equal(O2rResourcePacker.MajorasMaskTextResourceType, ReadResourceType(resource));
        Assert.Equal(1, BinaryPrimitives.ReadInt32LittleEndian(payload[..4]));
        Assert.Equal(0x1234, BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4, 2)));
        Assert.Equal(2, payload[6]);
        Assert.Equal(3, payload[7]);
        Assert.Equal(0x0004, BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(8, 2)));
        Assert.Equal(0x5678, BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(10, 2)));
        Assert.Equal(0x000a, BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(12, 2)));
        Assert.Equal(0xfffe, BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(14, 2)));
        Assert.Equal(4, BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(16, 4)));
        Assert.Equal([0x00, 0x48, 0x69, 0xbf], payload.Slice(20, 4).ToArray());
    }

    [Fact]
    public void PackMajorasMaskCreditsWritesRawStaffMessageBytes()
    {
        byte[] table =
        [
            0x4e, 0x20, 0x45, 0x00, 0x07, 0x00, 0x00, 0x00,
            0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        ];
        byte[] message = [0x41, 0x02, 0x00];

        byte[] resource = O2rResourcePacker.PackMajorasMaskText(message, table, messageDataHasHeaders: false);
        ReadOnlySpan<byte> payload = resource.AsSpan(O2rResourcePacker.ResourceHeaderSize);

        Assert.Equal(O2rResourcePacker.MajorasMaskTextResourceType, ReadResourceType(resource));
        Assert.Equal(1, BinaryPrimitives.ReadInt32LittleEndian(payload[..4]));
        Assert.Equal(0x4e20, BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4, 2)));
        Assert.Equal(4, payload[6]);
        Assert.Equal(5, payload[7]);
        Assert.Equal(0x00fe, BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(8, 2)));
        Assert.Equal(0xffff, BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(10, 2)));
        Assert.Equal(0xffff, BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(12, 2)));
        Assert.Equal(0xffff, BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(14, 2)));
        Assert.Equal(2, BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(16, 4)));
        Assert.Equal([0x41, 0x02], payload.Slice(20, 2).ToArray());
    }

    private static uint ReadResourceType(byte[] resource)
        => BinaryPrimitives.ReadUInt32LittleEndian(resource.AsSpan(0x04, 4));
}
