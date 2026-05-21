using System.IO.Compression;
using HylianGrimoire.Compression;

namespace HylianGrimoire.Rom;

public sealed record RomCompressionResult(byte[] Data, RomVersionProfile Profile);

public static class RomCompressionService
{
    public static RomCompressionResult DecompressRom(ReadOnlySpan<byte> source)
    {
        RomVersionProfile profile = RomVersionDatabase.Detect(source);
        List<DmaEntry> entries = DmaTable.Parse(source, profile);
        int outputSize = source.Length;
        foreach (DmaEntry entry in entries)
        {
            if (!entry.IsDeleted && entry.VirtualEnd > outputSize)
            {
                outputSize *= 2;
            }
        }

        var output = new byte[outputSize];

        foreach (DmaEntry entry in entries)
        {
            if (entry.IsDeleted || entry.IsEmpty)
            {
                continue;
            }

            int virtualStart = checked((int)entry.VirtualStart);
            int physicalStart = checked((int)entry.PhysicalStart);
            if (entry.IsCompressed)
            {
                int physicalSize = entry.PhysicalSize;
                ReadOnlySpan<byte> compressed = source.Slice(physicalStart, physicalSize);
                if (profile.Codec == RomCodecKind.Yaz0)
                {
                    Yaz0Codec.DecodeInto(compressed, output.AsSpan(virtualStart, entry.VirtualSize));
                }
                else
                {
                    int skip = profile.RawDeflateHasNoHeader ? 0 : 8;
                    byte[] decoded = RawDeflateCodec.Decode(compressed[skip..], entry.VirtualSize);
                    decoded.CopyTo(output.AsSpan(virtualStart));
                }
            }
            else
            {
                source.Slice(physicalStart, entry.VirtualSize).CopyTo(output.AsSpan(virtualStart));
            }
        }

        DmaTable.WriteDecompressedTable(output, profile, entries);
        N64Checksum.TryUpdate(output);
        return new RomCompressionResult(output, profile);
    }

    public static RomCompressionResult CompressRom(
        ReadOnlySpan<byte> source,
        int targetSizeMiB = 0,
        IProgress<RomCompressionProgress>? progress = null)
    {
        RomVersionProfile profile = RomVersionDatabase.Detect(source);
        List<DmaEntry> entries = DmaTable.Parse(source, profile);
        var packedEntries = new List<PackedDmaEntry>(entries.Count);
        int totalFiles = entries.Count(entry => !entry.IsDeleted && !entry.IsEmpty);
        int completedFiles = 0;
        progress?.Report(new RomCompressionProgress(completedFiles, totalFiles));

        foreach (DmaEntry entry in entries)
        {
            if (entry.IsDeleted || entry.IsEmpty)
            {
                packedEntries.Add(new PackedDmaEntry(entry, [], Compress: false));
                continue;
            }

            ReadOnlySpan<byte> fileData = source.Slice(checked((int)entry.VirtualStart), entry.VirtualSize);
            bool shouldCompress = !profile.UncompressedEntryIndices.Contains(entry.Index);
            byte[] payload;
            bool compressed = false;

            if (shouldCompress)
            {
                payload = profile.Codec == RomCodecKind.Yaz0
                    ? Yaz0Codec.Encode(fileData)
                    : EncodeRawDeflateFile(fileData, profile.RawDeflateHasNoHeader);
                compressed = true;
            }
            else
            {
                payload = fileData.ToArray();
            }

            packedEntries.Add(new PackedDmaEntry(entry, payload, compressed));
            completedFiles++;
            progress?.Report(new RomCompressionProgress(completedFiles, totalFiles));
        }

        packedEntries.Sort((left, right) => left.Entry.VirtualStart.CompareTo(right.Entry.VirtualStart));

        int cursor = 0;
        foreach (PackedDmaEntry packed in packedEntries)
        {
            if (packed.Entry.IsDeleted || packed.Entry.IsEmpty || packed.Data.Length == 0)
            {
                continue;
            }

            packed.PhysicalStart = checked((uint)cursor);
            int alignedSize = Align16(packed.Data.Length);
            packed.PhysicalEnd = packed.Compress ? checked((uint)(cursor + alignedSize)) : 0;
            cursor += alignedSize;
        }

        int targetBytes = targetSizeMiB > 0
            ? targetSizeMiB * 0x100000
            : profile.TargetCompressedSizeMiB > 0
                ? profile.TargetCompressedSizeMiB * 0x100000
                : profile.Codec == RomCodecKind.RawDeflate
                    ? Align1KiB(cursor)
                    : Align8MiB(cursor);
        if (cursor > targetBytes)
        {
            throw new InvalidDataException("Compressed ROM data exceeds the target ROM size.");
        }

        var output = new byte[targetBytes];
        foreach (PackedDmaEntry packed in packedEntries)
        {
            if (packed.Entry.IsDeleted || packed.Entry.IsEmpty || packed.Data.Length == 0)
            {
                continue;
            }

            packed.Data.CopyTo(output.AsSpan(checked((int)packed.PhysicalStart)));
        }

        if (profile.Codec == RomCodecKind.Yaz0)
        {
            for (int i = cursor; i < output.Length; i++)
            {
                output[i] = (byte)i;
            }
        }

        WriteCompressedDmaTable(output, profile, packedEntries);
        N64Checksum.TryUpdate(output);
        return new RomCompressionResult(output, profile);
    }

    private static byte[] EncodeRawDeflateFile(ReadOnlySpan<byte> data, bool noHeader)
    {
        byte[] deflated = RawDeflateCodec.Encode(data, CompressionLevel.SmallestSize);
        if (noHeader)
        {
            var output = new byte[deflated.Length + 8];
            deflated.CopyTo(output, 0);
            WriteUInt32LittleEndian(output, deflated.Length, Crc32(data));
            WriteUInt32LittleEndian(output, deflated.Length + 4, (uint)data.Length);
            return output;
        }

        var withHeader = new byte[deflated.Length + 8];
        withHeader[0] = (byte)'Z';
        withHeader[1] = (byte)'L';
        withHeader[2] = (byte)'I';
        withHeader[3] = (byte)'B';
        DmaTable.WriteUInt32BigEndian(withHeader, 4, (uint)data.Length);
        deflated.CopyTo(withHeader, 8);
        return withHeader;
    }

    private static void WriteCompressedDmaTable(Span<byte> output, RomVersionProfile profile, IReadOnlyList<PackedDmaEntry> packedEntries)
    {
        output.Slice(profile.DmaTableOffset, profile.DmaEntryCount * 16).Clear();

        PackedDmaEntry[] usedEntries = packedEntries
            .Where(entry => !entry.Entry.IsDeleted && !entry.Entry.IsEmpty)
            .OrderBy(entry => entry.Entry.VirtualStart)
            .ToArray();

        int tableIndex = 0;
        foreach (PackedDmaEntry packed in usedEntries)
        {
            DmaEntry entry = packed.Entry;
            int offset = profile.DmaTableOffset + (tableIndex * 16);

            DmaTable.WriteUInt32BigEndian(output, offset, entry.VirtualStart);
            DmaTable.WriteUInt32BigEndian(output, offset + 4, entry.VirtualEnd);
            DmaTable.WriteUInt32BigEndian(output, offset + 8, packed.PhysicalStart);
            DmaTable.WriteUInt32BigEndian(output, offset + 12, packed.PhysicalEnd);
            tableIndex++;
        }
    }

    private static int Align16(int value) => (value + 15) & ~15;

    private static int Align1KiB(int value) => (value + 0x3ff) & ~0x3ff;

    private static int Align8MiB(int value)
    {
        const int alignment = 8 * 0x100000;
        return ((value + alignment - 1) / alignment) * alignment;
    }

    private static void WriteUInt32LittleEndian(Span<byte> data, int offset, uint value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xffffffff;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc >> 1) ^ (0xedb88320u & (uint)-(int)(crc & 1));
            }
        }

        return crc ^ 0xffffffff;
    }

    private sealed record PackedDmaEntry(DmaEntry Entry, byte[] Data, bool Compress)
    {
        public uint PhysicalStart { get; set; }

        public uint PhysicalEnd { get; set; }
    }
}
