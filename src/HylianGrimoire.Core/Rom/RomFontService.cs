using System.Buffers.Binary;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;

namespace HylianGrimoire.Rom;

public static class RomFontService
{
    private const int DmaEntrySize = 16;
    private const int MajorasMaskGlyphCount = 0x4e00 / RomFontResources.GlyphByteSize;

    private static readonly byte[] StandardFontLoadCharProlog =
    [
        0x27, 0xbd, 0xff, 0xe8,
        0xaf, 0xbf, 0x00, 0x14,
        0xaf, 0xa4, 0x00, 0x18,
        0xaf, 0xa5, 0x00, 0x1c,
        0xaf, 0xa6, 0x00, 0x20,
        0x30, 0xa7, 0x00, 0xff,
    ];

    private static readonly byte[] WidthTablePrefix =
    [
        0x41, 0x00, 0x00, 0x00,
        0x41, 0x00, 0x00, 0x00,
        0x40, 0xc0, 0x00, 0x00,
        0x41, 0x10, 0x00, 0x00,
        0x41, 0x10, 0x00, 0x00,
        0x41, 0x60, 0x00, 0x00,
        0x41, 0x40, 0x00, 0x00,
        0x40, 0x40, 0x00, 0x00,
        0x40, 0xe0, 0x00, 0x00,
        0x40, 0xe0, 0x00, 0x00,
    ];

    public static RomFontResources Locate(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile)
    {
        int glyphDataOffset = LocateGlyphData(decompressedRom, profile);
        int widthTableOffset = LocateWidthTable(decompressedRom, profile);
        if (widthTableOffset < 0)
        {
            throw new InvalidDataException("Could not locate the ROM font width table.");
        }

        return new RomFontResources(
            glyphDataOffset,
            GetGlyphCount(profile),
            widthTableOffset,
            GetWidthCount(profile));
    }

    public static byte[] ReadGlyph(ReadOnlySpan<byte> decompressedRom, RomFontResources resources, byte value)
    {
        int offset = GetGlyphOffset(resources, value);
        return decompressedRom.Slice(offset, RomFontResources.GlyphByteSize).ToArray();
    }

    public static void WriteGlyph(Span<byte> decompressedRom, RomFontResources resources, byte value, ReadOnlySpan<byte> glyphBytes)
    {
        if (glyphBytes.Length != RomFontResources.GlyphByteSize)
        {
            throw new InvalidDataException($"ROM glyph payload must be exactly {RomFontResources.GlyphByteSize} bytes.");
        }

        glyphBytes.CopyTo(decompressedRom.Slice(GetGlyphOffset(resources, value), RomFontResources.GlyphByteSize));
    }

    public static float ReadWidth(ReadOnlySpan<byte> decompressedRom, RomFontResources resources, byte value)
    {
        int offset = GetWidthOffset(resources, value);
        int raw = BinaryPrimitives.ReadInt32BigEndian(decompressedRom.Slice(offset, sizeof(float)));
        return BitConverter.Int32BitsToSingle(raw);
    }

    public static void WriteWidth(Span<byte> decompressedRom, RomFontResources resources, byte value, float width)
    {
        int raw = BitConverter.SingleToInt32Bits(width);
        BinaryPrimitives.WriteInt32BigEndian(decompressedRom.Slice(GetWidthOffset(resources, value), sizeof(float)), raw);
    }

    private static int LocateGlyphData(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile)
    {
        if (profile.FontDmaEntryIndex is int fontDmaEntryIndex)
        {
            int glyphDataOffset = LocateDmaVirtualStart(decompressedRom, profile, fontDmaEntryIndex);
            if (CanContainGlyphs(decompressedRom, glyphDataOffset, GetGlyphCount(profile)))
            {
                return glyphDataOffset;
            }
        }

        int prologOffset = FindBytes(decompressedRom, StandardFontLoadCharProlog);
        if (prologOffset >= 0)
        {
            int glyphDataOffset = ReadLuiAddiuAddress(decompressedRom, prologOffset + 0x20, prologOffset + 0x24);
            if (CanContainGlyphs(decompressedRom, glyphDataOffset, GetGlyphCount(profile)))
            {
                return glyphDataOffset;
            }
        }

        if (profile.Codec == RomCodecKind.RawDeflate)
        {
            const int iQueStandardGlyphOffset = 0x8f1000;
            if (CanContainGlyphs(decompressedRom, iQueStandardGlyphOffset, GetGlyphCount(profile)))
            {
                return iQueStandardGlyphOffset;
            }
        }

        throw new InvalidDataException("Could not locate ROM glyph data.");
    }

    private static int LocateWidthTable(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile)
    {
        if (profile.FontWidthTableOffset is int fixedOffset
            && CanContainWidthTable(decompressedRom, fixedOffset, GetWidthCount(profile)))
        {
            return fixedOffset;
        }

        if (profile.Game == GameKind.MajorasMask)
        {
            return FindFloatTable(decompressedRom, MmGlyphMetrics.DefaultWidths);
        }

        return FindBytes(decompressedRom, WidthTablePrefix);
    }

    private static int LocateDmaVirtualStart(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile, int entryIndex)
    {
        if (entryIndex < 0 || entryIndex >= profile.DmaEntryCount)
        {
            throw new InvalidDataException("ROM font DMA index is outside the DMA table.");
        }

        int entryOffset = profile.DmaTableOffset + (entryIndex * DmaEntrySize);
        if (entryOffset < 0 || entryOffset + DmaEntrySize > decompressedRom.Length)
        {
            throw new InvalidDataException("ROM font DMA entry is outside the ROM buffer.");
        }

        uint virtualStart = DmaTable.ReadUInt32BigEndian(decompressedRom, entryOffset);
        uint virtualEnd = DmaTable.ReadUInt32BigEndian(decompressedRom, entryOffset + 4);
        if (virtualEnd <= virtualStart || virtualStart > int.MaxValue)
        {
            throw new InvalidDataException("ROM font DMA entry is invalid.");
        }

        return checked((int)virtualStart);
    }

    private static int GetGlyphOffset(RomFontResources resources, byte value)
    {
        int index = value - RomFontResources.FirstGlyphValue;
        if (index < 0 || index >= resources.GlyphCount)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Glyph 0x{value:x2} is outside the ROM glyph table.");
        }

        return resources.GlyphDataOffset + (index * RomFontResources.GlyphByteSize);
    }

    private static int GetWidthOffset(RomFontResources resources, byte value)
    {
        int index = value - RomFontResources.FirstGlyphValue;
        if (index < 0 || index >= resources.WidthCount)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Glyph 0x{value:x2} is outside the ROM width table.");
        }

        return resources.WidthTableOffset + (index * sizeof(float));
    }

    private static int GetGlyphCount(RomVersionProfile profile)
    {
        if (profile.Game == GameKind.MajorasMask)
        {
            return MajorasMaskGlyphCount;
        }

        return profile.Codec == RomCodecKind.RawDeflate
            ? Math.Max(RomFontResources.StandardGlyphCount, 1909)
            : RomFontResources.StandardGlyphCount;
    }

    private static int GetWidthCount(RomVersionProfile profile)
    {
        return profile.Game == GameKind.MajorasMask
            ? MmGlyphMetrics.DefaultWidths.Length
            : RomFontResources.StandardWidthCount;
    }

    private static bool CanContainGlyphs(ReadOnlySpan<byte> data, int offset, int glyphCount)
    {
        long byteCount = (long)glyphCount * RomFontResources.GlyphByteSize;
        return offset >= 0 && offset + byteCount <= data.Length;
    }

    private static bool CanContainWidthTable(ReadOnlySpan<byte> data, int offset, int widthCount)
    {
        long byteCount = (long)widthCount * sizeof(float);
        return offset >= 0 && offset + byteCount <= data.Length;
    }

    private static int ReadLuiAddiuAddress(ReadOnlySpan<byte> rom, int luiOffset, int addiuOffset)
    {
        ushort high = BinaryPrimitives.ReadUInt16BigEndian(rom.Slice(luiOffset + 2, sizeof(ushort)));
        ushort low = BinaryPrimitives.ReadUInt16BigEndian(rom.Slice(addiuOffset + 2, sizeof(ushort)));
        return (high << 16) + (short)low;
    }

    private static int FindBytes(ReadOnlySpan<byte> data, ReadOnlySpan<byte> pattern)
    {
        for (int i = 0; i <= data.Length - pattern.Length; i++)
        {
            if (data.Slice(i, pattern.Length).SequenceEqual(pattern))
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindFloatTable(ReadOnlySpan<byte> data, IReadOnlyList<double> values)
    {
        byte[] pattern = new byte[values.Count * sizeof(float)];
        for (int i = 0; i < values.Count; i++)
        {
            int raw = BitConverter.SingleToInt32Bits((float)values[i]);
            BinaryPrimitives.WriteInt32BigEndian(pattern.AsSpan(i * sizeof(float), sizeof(float)), raw);
        }

        return FindBytes(data, pattern);
    }
}
