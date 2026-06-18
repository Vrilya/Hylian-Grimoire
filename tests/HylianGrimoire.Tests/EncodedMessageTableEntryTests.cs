using HylianGrimoire.Codecs;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class EncodedMessageTableEntryTests
{
    [Fact]
    public void ReadsAndWritesOcarinaTableEntryFields()
    {
        byte[] raw = [0x12, 0x34, 0x23, 0x00, 0x07, 0xab, 0xcd, 0xef];

        EncodedMessageTableEntry entry = EncodedMessageTableEntry.Read(raw);
        var output = new List<byte>();
        entry.WriteTo(output);

        Assert.Equal(0x1234, entry.Id);
        Assert.Equal(2, entry.Type);
        Assert.Equal(3, entry.Position);
        Assert.Equal(0x07, entry.Bank);
        Assert.Equal(0xabcdef, entry.Offset);
        Assert.Equal(raw, output);
    }

    [Fact]
    public void BuildsEntryFromSeparateTypePositionAndPointerFields()
    {
        EncodedMessageTableEntry ocarina = EncodedMessageTableEntry.FromFields(0x2000, type: 4, position: 5, bank: 7, offset: 0x123456);
        EncodedMessageTableEntry majorasMask = EncodedMessageTableEntry.FromTypePosition(0x3000, 0xab, bank: 8, offset: 0x654321);

        Assert.Equal(0x45, ocarina.TypePosition);
        Assert.Equal(4, ocarina.Type);
        Assert.Equal(5, ocarina.Position);
        Assert.Equal(0x07, ocarina.Bank);
        Assert.Equal(0x123456, ocarina.Offset);

        Assert.Equal(0xab, majorasMask.TypePosition);
        Assert.Equal(0x0a, majorasMask.Type);
        Assert.Equal(0x0b, majorasMask.Position);
        Assert.Equal(0x08, majorasMask.Bank);
        Assert.Equal(0x654321, majorasMask.Offset);
    }
}
