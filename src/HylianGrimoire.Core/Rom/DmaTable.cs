namespace HylianGrimoire.Rom;

public static class DmaTable
{
    private const int EntrySize = 16;

    public static List<DmaEntry> Parse(ReadOnlySpan<byte> rom, RomVersionProfile profile)
    {
        if (profile.DmaTableOffset < 0 || profile.DmaTableOffset + (profile.DmaEntryCount * EntrySize) > rom.Length)
        {
            throw new InvalidDataException("DMA table is outside the ROM buffer.");
        }

        var entries = new List<DmaEntry>(profile.DmaEntryCount);
        for (int i = 0; i < profile.DmaEntryCount; i++)
        {
            int offset = profile.DmaTableOffset + (i * EntrySize);
            entries.Add(new DmaEntry(
                i,
                ReadUInt32BigEndian(rom, offset),
                ReadUInt32BigEndian(rom, offset + 4),
                ReadUInt32BigEndian(rom, offset + 8),
                ReadUInt32BigEndian(rom, offset + 12)));
        }

        return entries;
    }

    public static void WriteDecompressedTable(Span<byte> rom, RomVersionProfile profile, IReadOnlyList<DmaEntry> entries)
    {
        if (entries.Count != profile.DmaEntryCount)
        {
            throw new InvalidDataException("DMA entry count does not match ROM profile.");
        }

        for (int i = 0; i < entries.Count; i++)
        {
            DmaEntry entry = entries[i];
            int offset = profile.DmaTableOffset + (i * EntrySize);

            if (entry.IsDeleted || entry.IsEmpty)
            {
                WriteUInt32BigEndian(rom, offset, entry.VirtualStart);
                WriteUInt32BigEndian(rom, offset + 4, entry.VirtualEnd);
                WriteUInt32BigEndian(rom, offset + 8, entry.PhysicalStart);
                WriteUInt32BigEndian(rom, offset + 12, entry.PhysicalEnd);
                continue;
            }

            WriteUInt32BigEndian(rom, offset, entry.VirtualStart);
            WriteUInt32BigEndian(rom, offset + 4, entry.VirtualEnd);
            WriteUInt32BigEndian(rom, offset + 8, entry.VirtualStart);
            WriteUInt32BigEndian(rom, offset + 12, 0);
        }
    }

    internal static uint ReadUInt32BigEndian(ReadOnlySpan<byte> data, int offset) =>
        ((uint)data[offset] << 24)
        | ((uint)data[offset + 1] << 16)
        | ((uint)data[offset + 2] << 8)
        | data[offset + 3];

    internal static void WriteUInt32BigEndian(Span<byte> data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }
}
