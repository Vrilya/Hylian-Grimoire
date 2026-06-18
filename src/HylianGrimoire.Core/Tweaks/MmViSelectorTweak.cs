using System.Buffers.Binary;
using System.IO.Compression;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Tweaks;

public static class MmViSelectorTweak
{
    private const int RowSize = 0x30;
    private const int HeaderRegionOffset = 0x3E;
    private const byte OriginalHeaderRegion = (byte)'E';
    private const byte PatchedHeaderRegion = (byte)'P';

    private static readonly IReadOnlyDictionary<string, PatchProfile> ProfilesByName = new Dictionary<string, PatchProfile>(StringComparer.Ordinal)
    {
        ["Majora's Mask NTSC-U"] = new(
            rowOffset: 0xC53EB4,
            blockOffset: 0xC7A4E0,
            blockSize: 0x910,
            dmaEntryOffset: 0x1A700,
            viConfigurePalTableOffset: 0xC75600,
            regionCheckOffset: 0xC0F7F8,
            originalRowWords:
            [
                0x00C7A4E0, 0x00C7ADF0, 0x80800000, 0x80800910,
                0x00000000, 0x8080074C, 0x8080071C, 0x00000000,
                0x00000000, 0x00000000, 0x00000248, 0x00000000,
            ],
            patchedRowWords:
            [
                0x00C7A4E0, 0x00C7ADF0, 0x80800000, 0x80800910,
                0x00000000, 0x80800444, 0x80800414, 0x00000000,
                0x00000000, 0x00000000, 0x00000248, 0x00000000,
            ],
            originalDmaWords:
            [
                0x00C7A4E0, 0x00C7ADF0, 0x00C7A4E0, 0x00000000,
            ],
            originalBlockBase64:
            """
            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
            """,
            patchedBlockBase64:
            """
            eNrtlU9oHFUcx7/zdma7SZ/ZLSTNhBQd8E3+kZIE2rLFwV1jErY1Fj0IqaeKF2koBvSgEHG0jZRmkwnYQ0EPS21CSHZ2l+rBosGoIQSRBvQiUjSHqEUEcygS
            MDh+32YjpejZSx48fr/5vfd77/v5/d4m7UvR3fJnaITf4V4qibQyYHjCr2/6HH2q3095Mb9Ovfjmshf3670D9H8ZPaZWb2ZxLusiu+aqU8gFaaD9JKSbgAzt
            6F7QE22HyWg7SEc7zZ307WhH70H2nIvll9zQBoIemOoMcmES5tQk4OaZHzJ/Mbo3dYU5C9H21GS00zRNfzHa0XtUHR72DL8+XASmrjC/BSJcgFnovDnjGRl/
            4C0HXMfmJpAC+oonY9As5Nj4b44xciy7apgcSXLY1HGbOtbJkeDda+RIkkPSXydHUnOMkCPnhuvkSFDHWXKskeM8OUaZ38n8LnJcYM5Rcpwnx+v0u8jBPUqi
            u8rRRY4LMMOjZOjWDL6/0mY5VYafgaJD/VWurOaoU/2jvVxzNnNce/STy/mPeVYSBuC45bh4NV8CVk4DP30lNvPz9Lfop8Td/C1ACuO1D3rE9wF73b4EO7aB
            BEnQvhT57L8qV+CUb8APSvCnvgDyA6JSvo5xaX0zw/gAsh1uMIfx/IooePYPaK9gWAVIFNdFofg2zOI1QFCHFLZZnmUtOem36hj7FJVvIVX1LUQqjjtSHH5Z
            HUBHLfYQvycZl8E8fB3zLN+fqBPpJgcx9sxnP2Nq1YL2aU2efULNmlsP5DcEC/Crb5j7Jp7ie161jug9TS/A1GfWzjIYt3fPsBK0VR01/S3krupHIeV68kcU
            h1nvLPs8i0TAWrAm7+4xBiHGw1lRYV1VwBrqurK+Puu6rGsK33d1fadNoFxAI2eKeg1qNaT54ZdA2m0TaLx6hi3i/X3rSRzagKl7844t0o+1wJhrEWnJk6ir
            O+C8moPsq5vBsw4O6Xu5Vq0938gjqv+PMRVjjethFJ7MHOZsLp725q8D73kHKWY006ukSJUkUqWDrHdNNzVn73sT1b8J0mqdURMiJa2Lx3Vcini9tv/yhjbI
            atdYq/WW1te/ak4Zx8ju98UOvbfNxLw0/1yatgCvgf1IMul2bMT93WwsNcCU1rcrpSQSvSMwmoE4f/PGHtexlr/usHdW8axX+Ic1gaiY8wqFU5nM7j2t77dZ
            IjUHkabOI2SzqdXZ0+q+4gw988Swc7w397z+fpzT2Y0/reMnHohnnsu8gf2xP/bH/zTMrZqTrdnGQRj5IRgzgxA3hiCKtBXaj2iv0X5Ku0W7Tfsd7Qbtb0OI
            8Z9YbJ5zZRDm+BDMS7Q+7eX7Lhv7G0CthMw=
            """),
    };

    private static readonly byte[] OriginalHeader = [OriginalHeaderRegion];
    private static readonly byte[] PatchedHeader = [PatchedHeaderRegion];
    private static readonly byte[] OriginalRegionCheck = Convert.FromHexString("00001025");
    private static readonly byte[] PatchedRegionCheck = Convert.FromHexString("2c620003");

    private static readonly Patch[] ViConfigurePalTablePatches =
    [
        P(0x00, "0404233a", "04541e3a"),
        P(0x08, "00150c69", "00170c69"),
        P(0x0C, "0c6f0c6e", "0c6f0c6d"),
        P(0x14, "005f0239", "002f0269"),
    ];

    public static RomTweakStatus GetStatus(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile)
    {
        if (!ProfilesByName.TryGetValue(profile.Name, out PatchProfile? patchProfile))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "Unavailable");
        }

        if (!HasRequiredRanges(decompressedRom, patchProfile))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "The ROM is too small for this tweak.");
        }

        if (!WordsEqual(decompressedRom.Slice(patchProfile.DmaEntryOffset, sizeof(uint) * patchProfile.OriginalDmaWords.Length), patchProfile.OriginalDmaWords))
        {
            return new RomTweakStatus(RomTweakState.Unknown, "The MM title overlay DMA entry is not original.");
        }

        IReadOnlyList<ComponentState> states = GetComponentStates(decompressedRom, patchProfile);
        string[] unknownNames = states
            .Where(state => state.State == PatchState.Unknown)
            .Select(state => state.Name)
            .ToArray();
        if (unknownNames.Length > 0)
        {
            return new RomTweakStatus(
                RomTweakState.Unknown,
                $"The MM VI selector contains unexpected bytes at: {string.Join(", ", unknownNames)}.");
        }

        int patchedCount = states.Count(state => state.State == PatchState.Patched);
        if (patchedCount == states.Count)
        {
            return new RomTweakStatus(RomTweakState.On, "On");
        }

        if (patchedCount == 0)
        {
            return new RomTweakStatus(RomTweakState.Off, "Off");
        }

        return new RomTweakStatus(RomTweakState.Mixed, $"{patchedCount} of {states.Count} MM VI selector locations are enabled.");
    }

    public static bool HasPatchedPayload(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile)
    {
        if (!ProfilesByName.TryGetValue(profile.Name, out PatchProfile? patchProfile) || !HasRequiredRanges(decompressedRom, patchProfile))
        {
            return false;
        }

        ReadOnlySpan<byte> row = decompressedRom.Slice(patchProfile.RowOffset, RowSize);
        ReadOnlySpan<byte> block = decompressedRom.Slice(patchProfile.BlockOffset, patchProfile.BlockSize);
        return WordsEqual(row, patchProfile.PatchedRowWords) && block.SequenceEqual(patchProfile.PatchedBlock);
    }

    public static void SetEnabled(Span<byte> decompressedRom, RomVersionProfile profile, bool enabled)
    {
        if (!ProfilesByName.TryGetValue(profile.Name, out PatchProfile? patchProfile) || !HasRequiredRanges(decompressedRom, patchProfile))
        {
            throw new InvalidOperationException("This tweak is not supported for the loaded ROM.");
        }

        RomTweakStatus status = GetStatus(decompressedRom, profile);
        if (!status.CanToggle)
        {
            throw new InvalidOperationException(status.Detail);
        }

        bool hasPayload = HasAnyPayload(decompressedRom, patchProfile);
        if (enabled && status.State == RomTweakState.On || !enabled && (status.State == RomTweakState.Off || !hasPayload))
        {
            return;
        }

        if (enabled && IsBlockedByMmFpal(decompressedRom, profile, hasPayload))
        {
            throw new InvalidOperationException("Disable the MM N64 VI PAL timing tweak before enabling the MM VI selector.");
        }

        Span<byte> row = decompressedRom.Slice(patchProfile.RowOffset, RowSize);
        Span<byte> block = decompressedRom.Slice(patchProfile.BlockOffset, patchProfile.BlockSize);
        if (enabled)
        {
            WriteWords(row, patchProfile.PatchedRowWords);
            patchProfile.PatchedBlock.CopyTo(block);
            CopyPatch(decompressedRom, HeaderRegionOffset, PatchedHeader);
            CopyPatch(decompressedRom, patchProfile.RegionCheckOffset, PatchedRegionCheck);
            foreach (Patch patch in ViConfigurePalTablePatches)
            {
                CopyPatch(decompressedRom, patchProfile.ViConfigurePalTableOffset + patch.RelativeOffset, patch.Patched);
            }
        }
        else
        {
            WriteWords(row, patchProfile.OriginalRowWords);
            patchProfile.OriginalBlock.CopyTo(block);
            CopyPatch(decompressedRom, HeaderRegionOffset, OriginalHeader);
            CopyPatch(decompressedRom, patchProfile.RegionCheckOffset, OriginalRegionCheck);
            foreach (Patch patch in ViConfigurePalTablePatches)
            {
                CopyPatch(decompressedRom, patchProfile.ViConfigurePalTableOffset + patch.RelativeOffset, patch.Original);
            }
        }

        if (!N64Checksum.TryUpdate(decompressedRom))
        {
            throw new InvalidOperationException("Could not recalculate the ROM checksum.");
        }
    }

    private static bool IsBlockedByMmFpal(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile, bool hasPayload)
    {
        RomTweakStatus fpalStatus = MmFpalTweak.GetStatus(decompressedRom, profile);
        return fpalStatus.State == RomTweakState.On || fpalStatus.State == RomTweakState.Mixed && !hasPayload;
    }

    private static bool HasAnyPayload(ReadOnlySpan<byte> decompressedRom, PatchProfile patchProfile)
    {
        ReadOnlySpan<byte> row = decompressedRom.Slice(patchProfile.RowOffset, RowSize);
        ReadOnlySpan<byte> block = decompressedRom.Slice(patchProfile.BlockOffset, patchProfile.BlockSize);
        return WordsEqual(row, patchProfile.PatchedRowWords) || block.SequenceEqual(patchProfile.PatchedBlock);
    }

    private static IReadOnlyList<ComponentState> GetComponentStates(ReadOnlySpan<byte> rom, PatchProfile patchProfile)
    {
        List<ComponentState> states =
        [
            new("ROM region code", GetPatchState(rom, HeaderRegionOffset, OriginalHeader, PatchedHeader)),
            new(
                "title game-state row",
                GetWordPatchState(rom.Slice(patchProfile.RowOffset, RowSize), patchProfile.OriginalRowWords, patchProfile.PatchedRowWords)),
            new(
                "title overlay payload",
                GetBlockPatchState(rom.Slice(patchProfile.BlockOffset, patchProfile.BlockSize), patchProfile.OriginalBlock, patchProfile.PatchedBlock)),
            new("startup region check", GetPatchState(rom, patchProfile.RegionCheckOffset, OriginalRegionCheck, PatchedRegionCheck)),
        ];

        foreach (Patch patch in ViConfigurePalTablePatches)
        {
            states.Add(new ComponentState(
                $"dynamic PAL VI timing field 0x{patch.RelativeOffset:X2}",
                GetPatchState(
                    rom,
                    patchProfile.ViConfigurePalTableOffset + patch.RelativeOffset,
                    patch.Original,
                    patch.Patched)));
        }

        return states;
    }

    private static PatchState GetPatchState(ReadOnlySpan<byte> rom, int offset, ReadOnlySpan<byte> original, ReadOnlySpan<byte> patched)
    {
        ReadOnlySpan<byte> current = rom.Slice(offset, original.Length);
        if (current.SequenceEqual(original))
        {
            return PatchState.Original;
        }

        return current.SequenceEqual(patched) ? PatchState.Patched : PatchState.Unknown;
    }

    private static PatchState GetWordPatchState(ReadOnlySpan<byte> bytes, IReadOnlyList<uint> original, IReadOnlyList<uint> patched)
    {
        if (WordsEqual(bytes, original))
        {
            return PatchState.Original;
        }

        return WordsEqual(bytes, patched) ? PatchState.Patched : PatchState.Unknown;
    }

    private static PatchState GetBlockPatchState(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> original, ReadOnlySpan<byte> patched)
    {
        if (bytes.SequenceEqual(original))
        {
            return PatchState.Original;
        }

        return bytes.SequenceEqual(patched) ? PatchState.Patched : PatchState.Unknown;
    }

    private static bool HasRequiredRanges(ReadOnlySpan<byte> rom, PatchProfile patchProfile)
    {
        if (!HasPatchRange(rom, HeaderRegionOffset, OriginalHeader.Length)
            || !HasPatchRange(rom, patchProfile.RowOffset, RowSize)
            || !HasPatchRange(rom, patchProfile.BlockOffset, patchProfile.BlockSize)
            || !HasPatchRange(rom, patchProfile.DmaEntryOffset, sizeof(uint) * patchProfile.OriginalDmaWords.Length)
            || !HasPatchRange(rom, patchProfile.RegionCheckOffset, OriginalRegionCheck.Length))
        {
            return false;
        }

        foreach (Patch patch in ViConfigurePalTablePatches)
        {
            if (!HasPatchRange(rom, patchProfile.ViConfigurePalTableOffset + patch.RelativeOffset, patch.Original.Length))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasPatchRange(ReadOnlySpan<byte> rom, int offset, int length) =>
        offset >= 0 && offset + length <= rom.Length;

    private static void CopyPatch(Span<byte> rom, int offset, ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(rom.Slice(offset, bytes.Length));
    }

    private static bool WordsEqual(ReadOnlySpan<byte> bytes, IReadOnlyList<uint> words)
    {
        if (bytes.Length != words.Count * sizeof(uint))
        {
            return false;
        }

        for (int i = 0; i < words.Count; i++)
        {
            uint value = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(i * sizeof(uint), sizeof(uint)));
            if (value != words[i])
            {
                return false;
            }
        }

        return true;
    }

    private static void WriteWords(Span<byte> destination, IReadOnlyList<uint> words)
    {
        for (int i = 0; i < words.Count; i++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(i * sizeof(uint), sizeof(uint)), words[i]);
        }
    }

    private static byte[] DecodeZlibBase64(string data)
    {
        byte[] packed = Convert.FromBase64String(string.Concat(data.Where(c => !char.IsWhiteSpace(c))));
        using var input = new MemoryStream(packed);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    private static Patch P(int relativeOffset, string original, string patched)
    {
        return new Patch(relativeOffset, Convert.FromHexString(original), Convert.FromHexString(patched));
    }

    private sealed class PatchProfile(
        int rowOffset,
        int blockOffset,
        int blockSize,
        int dmaEntryOffset,
        int viConfigurePalTableOffset,
        int regionCheckOffset,
        uint[] originalRowWords,
        uint[] patchedRowWords,
        uint[] originalDmaWords,
        string originalBlockBase64,
        string patchedBlockBase64)
    {
        private readonly Lazy<byte[]> _originalBlock = new(() => DecodeZlibBase64(originalBlockBase64));
        private readonly Lazy<byte[]> _patchedBlock = new(() => DecodeZlibBase64(patchedBlockBase64));

        public int RowOffset { get; } = rowOffset;
        public int BlockOffset { get; } = blockOffset;
        public int BlockSize { get; } = blockSize;
        public int DmaEntryOffset { get; } = dmaEntryOffset;
        public int ViConfigurePalTableOffset { get; } = viConfigurePalTableOffset;
        public int RegionCheckOffset { get; } = regionCheckOffset;
        public uint[] OriginalRowWords { get; } = originalRowWords;
        public uint[] PatchedRowWords { get; } = patchedRowWords;
        public uint[] OriginalDmaWords { get; } = originalDmaWords;
        public byte[] OriginalBlock => _originalBlock.Value;
        public byte[] PatchedBlock => _patchedBlock.Value;
    }

    private sealed record Patch(int RelativeOffset, byte[] Original, byte[] Patched);

    private sealed record ComponentState(string Name, PatchState State);

    private enum PatchState
    {
        Original,
        Patched,
        Unknown,
    }
}
