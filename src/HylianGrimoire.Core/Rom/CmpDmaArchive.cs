using System.Buffers.Binary;
using HylianGrimoire.Compression;

namespace HylianGrimoire.Rom;

public static class CmpDmaArchive
{
    private const int TableEntrySize = 4;
    private const int Alignment = 0x10;

    public static byte[] DecodeAll(ReadOnlySpan<byte> rom, int archiveRomAddress, int? archiveLength = null)
    {
        CmpDmaArchiveTable table = ReadTable(rom, archiveRomAddress, archiveLength);
        using var output = new MemoryStream();

        foreach (CmpDmaArchiveEntry entry in table.Entries)
        {
            if (entry.CompressedLength == 0)
            {
                continue;
            }

            ReadOnlySpan<byte> compressed = rom.Slice(entry.CompressedRomAddress, entry.CompressedLength);
            if (!Yaz0Codec.IsYaz0(compressed))
            {
                throw new InvalidDataException($"CmpDma file at 0x{entry.CompressedRomAddress:x8} is not Yaz0-compressed.");
            }

            byte[] file = Yaz0Codec.Decode(compressed);
            output.Write(file);
        }

        return output.ToArray();
    }

    public static void WriteSlice(
        Span<byte> rom,
        int archiveRomAddress,
        int archiveLength,
        int localOffset,
        ReadOnlySpan<byte> data)
    {
        if (archiveLength <= 0)
        {
            throw new InvalidDataException("CmpDma archive length must be positive.");
        }

        CmpDmaArchiveTable table = ReadTable(rom, archiveRomAddress, archiveLength);
        byte[][] decodedFiles = DecodeFiles(rom, table);
        int totalDecodedLength = decodedFiles.Sum(file => file.Length);
        if (localOffset < 0 || data.Length < 0 || localOffset > totalDecodedLength - data.Length)
        {
            throw new InvalidDataException(
                $"CmpDma write at local offset 0x{localOffset:x} with length {data.Length} is outside the decoded archive.");
        }

        if (!PatchDecodedFiles(decodedFiles, localOffset, data))
        {
            return;
        }

        byte[][] compressedFiles = RepackFiles(rom, table, decodedFiles);
        byte[] rebuilt = BuildArchive(table.DataStart, compressedFiles);
        if (rebuilt.Length > archiveLength)
        {
            throw new InvalidDataException(
                $"Repacked CmpDma archive grew to 0x{rebuilt.Length:x} bytes, but the ROM file only has 0x{archiveLength:x} bytes available.");
        }

        Span<byte> destination = rom.Slice(archiveRomAddress, archiveLength);
        destination.Clear();
        rebuilt.CopyTo(destination);
    }

    private static CmpDmaArchiveTable ReadTable(ReadOnlySpan<byte> rom, int archiveRomAddress, int? archiveLength)
    {
        if (archiveRomAddress < 0 || archiveRomAddress > rom.Length - 4)
        {
            throw new InvalidDataException($"CmpDma archive starts outside the ROM at 0x{archiveRomAddress:x8}.");
        }

        int availableLength = archiveLength ?? rom.Length - archiveRomAddress;
        if (availableLength < 8 || archiveRomAddress > rom.Length - availableLength)
        {
            throw new InvalidDataException($"CmpDma archive at 0x{archiveRomAddress:x8} has an invalid length.");
        }

        int dataStart = checked((int)BinaryPrimitives.ReadUInt32BigEndian(rom.Slice(archiveRomAddress, 4)));
        if (dataStart < 8 || (dataStart & 3) != 0 || dataStart > availableLength)
        {
            throw new InvalidDataException($"Invalid CmpDma archive table at 0x{archiveRomAddress:x8}.");
        }

        int tableWordCount = dataStart / TableEntrySize;
        if (tableWordCount < 2)
        {
            throw new InvalidDataException($"CmpDma archive has no file entries at 0x{archiveRomAddress:x8}.");
        }

        int fileCount = tableWordCount - 1;
        int previousOffset = 0;
        var entries = new List<CmpDmaArchiveEntry>(fileCount);
        for (int i = 0; i < fileCount; i++)
        {
            int nextOffset = checked((int)BinaryPrimitives.ReadUInt32BigEndian(
                rom.Slice(archiveRomAddress + (i + 1) * TableEntrySize, TableEntrySize)));

            if (nextOffset < previousOffset)
            {
                throw new InvalidDataException($"CmpDma archive offsets are not monotonic at 0x{archiveRomAddress:x8}.");
            }

            if (dataStart + nextOffset > availableLength)
            {
                throw new InvalidDataException($"CmpDma file {i} extends past archive EOF at 0x{archiveRomAddress:x8}.");
            }

            entries.Add(new CmpDmaArchiveEntry(
                archiveRomAddress + dataStart + previousOffset,
                nextOffset - previousOffset));
            previousOffset = nextOffset;
        }

        return new CmpDmaArchiveTable(dataStart, entries);
    }

    private static byte[][] DecodeFiles(ReadOnlySpan<byte> rom, CmpDmaArchiveTable table)
    {
        var files = new byte[table.Entries.Count][];
        for (int i = 0; i < table.Entries.Count; i++)
        {
            CmpDmaArchiveEntry entry = table.Entries[i];
            if (entry.CompressedLength == 0)
            {
                files[i] = [];
                continue;
            }

            ReadOnlySpan<byte> compressed = rom.Slice(entry.CompressedRomAddress, entry.CompressedLength);
            if (!Yaz0Codec.IsYaz0(compressed))
            {
                throw new InvalidDataException($"CmpDma file {i} at 0x{entry.CompressedRomAddress:x8} is not Yaz0-compressed.");
            }

            files[i] = Yaz0Codec.Decode(compressed);
        }

        return files;
    }

    private static bool PatchDecodedFiles(byte[][] decodedFiles, int localOffset, ReadOnlySpan<byte> data)
    {
        bool changed = false;
        int cursor = 0;
        int remainingOffset = localOffset;
        int dataOffset = 0;

        foreach (byte[] decodedFile in decodedFiles)
        {
            if (remainingOffset >= decodedFile.Length)
            {
                remainingOffset -= decodedFile.Length;
                cursor += decodedFile.Length;
                continue;
            }

            int writable = Math.Min(data.Length - dataOffset, decodedFile.Length - remainingOffset);
            if (writable <= 0)
            {
                break;
            }

            Span<byte> target = decodedFile.AsSpan(remainingOffset, writable);
            ReadOnlySpan<byte> source = data.Slice(dataOffset, writable);
            if (!target.SequenceEqual(source))
            {
                source.CopyTo(target);
                changed = true;
            }

            dataOffset += writable;
            remainingOffset = 0;
            cursor += decodedFile.Length;

            if (dataOffset == data.Length)
            {
                break;
            }
        }

        if (dataOffset != data.Length)
        {
            throw new InvalidDataException(
                $"CmpDma write at local offset 0x{localOffset:x} reached only 0x{cursor:x} decoded bytes.");
        }

        return changed;
    }

    private static byte[][] RepackFiles(ReadOnlySpan<byte> rom, CmpDmaArchiveTable table, byte[][] decodedFiles)
    {
        var compressedFiles = new byte[decodedFiles.Length][];
        for (int i = 0; i < decodedFiles.Length; i++)
        {
            CmpDmaArchiveEntry entry = table.Entries[i];
            if (entry.CompressedLength == 0 && decodedFiles[i].Length == 0)
            {
                compressedFiles[i] = [];
                continue;
            }

            byte[] original = rom.Slice(entry.CompressedRomAddress, entry.CompressedLength).ToArray();
            if (entry.CompressedLength != 0
                && Yaz0Codec.IsYaz0(original)
                && Yaz0Codec.Decode(original).AsSpan().SequenceEqual(decodedFiles[i]))
            {
                compressedFiles[i] = original;
                continue;
            }

            compressedFiles[i] = Pad(Yaz0Codec.Encode(decodedFiles[i]));
        }

        return compressedFiles;
    }

    private static byte[] BuildArchive(int dataStart, IReadOnlyList<byte[]> compressedFiles)
    {
        int tableLength = (compressedFiles.Count + 1) * TableEntrySize;
        if (dataStart < tableLength)
        {
            throw new InvalidDataException("CmpDma archive table is too small for its file count.");
        }

        int dataLength = compressedFiles.Sum(file => file.Length);
        var output = new byte[dataStart + dataLength];
        BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(0, TableEntrySize), checked((uint)dataStart));

        int cursor = 0;
        for (int i = 0; i < compressedFiles.Count; i++)
        {
            cursor += compressedFiles[i].Length;
            BinaryPrimitives.WriteUInt32BigEndian(
                output.AsSpan((i + 1) * TableEntrySize, TableEntrySize),
                checked((uint)cursor));
        }

        cursor = dataStart;
        foreach (byte[] compressedFile in compressedFiles)
        {
            compressedFile.CopyTo(output.AsSpan(cursor));
            cursor += compressedFile.Length;
        }

        return output;
    }

    private static byte[] Pad(byte[] data)
    {
        int paddedLength = Align(data.Length, Alignment);
        if (paddedLength == data.Length)
        {
            return data;
        }

        var padded = new byte[paddedLength];
        data.CopyTo(padded.AsSpan());
        return padded;
    }

    private static int Align(int value, int alignment)
        => checked((value + alignment - 1) / alignment * alignment);

    private sealed record CmpDmaArchiveTable(int DataStart, IReadOnlyList<CmpDmaArchiveEntry> Entries);

    private sealed record CmpDmaArchiveEntry(int CompressedRomAddress, int CompressedLength);
}
