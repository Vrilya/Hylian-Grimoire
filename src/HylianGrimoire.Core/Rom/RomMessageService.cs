namespace HylianGrimoire.Rom;

public static partial class RomMessageService
{
    private static byte[] Slice(byte[] data, int offset, int size)
    {
        if (offset < 0 || size < 0 || offset + size > data.Length)
        {
            throw new InvalidDataException("ROM message section is outside the decompressed ROM buffer.");
        }

        return data.AsSpan(offset, size).ToArray();
    }

    private static DmaEntry? FindDmaEntry(byte[] rom, RomVersionProfile profile, int virtualOffset)
    {
        return DmaTable.Parse(rom, profile)
            .FirstOrDefault(entry =>
                !entry.IsDeleted
                && !entry.IsEmpty
                && entry.VirtualStart <= virtualOffset
                && virtualOffset < entry.VirtualEnd);
    }

    private static uint FindNextVirtualStart(byte[] rom, RomVersionProfile profile, DmaEntry currentEntry)
    {
        return DmaTable.Parse(rom, profile)
            .Where(entry => !entry.IsDeleted && !entry.IsEmpty && entry.VirtualStart > currentEntry.VirtualStart)
            .Select(entry => entry.VirtualStart)
            .DefaultIfEmpty((uint)rom.Length)
            .Min();
    }

    private static int Align16(int value) => (value + 15) & ~15;

    private static int SignExtend16(ushort value) => value >= 0x8000 ? value - 0x10000 : value;

    private static ushort ReadUInt16BigEndian(byte[] data, int offset) =>
        (ushort)((data[offset] << 8) | data[offset + 1]);

    private static int Align4(int value) => (value + 3) & ~3;
}
