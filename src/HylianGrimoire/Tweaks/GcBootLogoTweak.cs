using HylianGrimoire.Rom;
using System.Buffers.Binary;

namespace HylianGrimoire.Tweaks;

public static class GcBootLogoTweak
{
    private static readonly uint[] OriginalStub =
    [
        0x240E0001,
        0xA08E01E1,
        0x03E00008,
        0x00000000,
    ];

    private static readonly uint[] CaveCode =
    [
        0x848201D6, 0x54400010, 0x848901D8, 0x848301DA,
        0x5060000D, 0x848901D8, 0x848E01D4, 0x2478FFFF,
        0xA49801DA, 0x25CFFFFF, 0xA48F01D4, 0x849901D4,
        0x24080190, 0x57200013, 0x848201DC, 0x10000010,
        0xA48801D4, 0x848901D8, 0x240B0003, 0x00495021,
        0xA48A01D6, 0x848201D6, 0x1C400004, 0x284100FF,
        0xA48001D6, 0x10000006, 0xA48B01D8, 0x14200004,
        0x240C00FF, 0x240D0001, 0xA48C01D6, 0xA08D01E1,
        0x848201DC, 0x304E007F, 0x244F0001, 0xA48E01DE,
        0xA48F01DC, 0x03E00008, 0x00000000,
    ];

    private static readonly IReadOnlyDictionary<string, PatchProfile> Profiles = new Dictionary<string, PatchProfile>(StringComparer.Ordinal)
    {
        ["Retail PAL GameCube"] = new(0x00B8A250, 0x00B59FEC, 0x08038BBB),
        ["Retail PAL Master Quest"] = new(0x00B8A230, 0x00B59FCC, 0x08038BB3),
        ["Retail NTSC GameCube"] = new(0x00B8AA60, 0x00B5A68C, 0x0803955B),
        ["Retail NTSC Master Quest"] = new(0x00B8AA40, 0x00B5A66C, 0x08039553),
    };

    public static RomTweakStatus GetStatus(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile)
    {
        if (!Profiles.TryGetValue(profile.Name, out PatchProfile? patch))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "This tweak supports retail GameCube ROMs only.");
        }

        if (!HasPatchRange(decompressedRom, patch))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "The ROM is too small for this tweak.");
        }

        ReadOnlySpan<byte> stub = decompressedRom.Slice(patch.StubOffset, OriginalStub.Length * sizeof(uint));
        ReadOnlySpan<byte> cave = decompressedRom.Slice(patch.CaveOffset, CaveCode.Length * sizeof(uint));
        if (WordsEqual(stub, OriginalStub) && IsZeroFilled(cave))
        {
            return new RomTweakStatus(RomTweakState.Off, "Off");
        }

        if (WordsEqual(stub, patch.TrampolineWords) && WordsEqual(cave, CaveCode))
        {
            return new RomTweakStatus(RomTweakState.On, "On");
        }

        return new RomTweakStatus(RomTweakState.Unknown, "This ROM already has different code in the boot-logo patch area.");
    }

    public static void SetEnabled(Span<byte> decompressedRom, RomVersionProfile profile, bool enabled)
    {
        if (!Profiles.TryGetValue(profile.Name, out PatchProfile? patch) || !HasPatchRange(decompressedRom, patch))
        {
            throw new InvalidOperationException("This tweak is not supported for the loaded ROM.");
        }

        RomTweakStatus status = GetStatus(decompressedRom, profile);
        if (!status.CanToggle)
        {
            throw new InvalidOperationException(status.Detail);
        }

        if (enabled && status.State == RomTweakState.On
            || !enabled && status.State == RomTweakState.Off)
        {
            return;
        }

        Span<byte> stub = decompressedRom.Slice(patch.StubOffset, OriginalStub.Length * sizeof(uint));
        Span<byte> cave = decompressedRom.Slice(patch.CaveOffset, CaveCode.Length * sizeof(uint));
        if (enabled)
        {
            WriteWords(stub, patch.TrampolineWords);
            WriteWords(cave, CaveCode);
            return;
        }

        WriteWords(stub, OriginalStub);
        cave.Clear();
    }

    private static bool HasPatchRange(ReadOnlySpan<byte> rom, PatchProfile patch)
    {
        return patch.StubOffset >= 0
            && patch.CaveOffset >= 0
            && patch.StubOffset + OriginalStub.Length * sizeof(uint) <= rom.Length
            && patch.CaveOffset + CaveCode.Length * sizeof(uint) <= rom.Length;
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

    private static bool IsZeroFilled(ReadOnlySpan<byte> bytes)
    {
        foreach (byte value in bytes)
        {
            if (value != 0)
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

    private sealed class PatchProfile(int stubOffset, int caveOffset, uint jumpToCave)
    {
        public int StubOffset { get; } = stubOffset;

        public int CaveOffset { get; } = caveOffset;

        public uint[] TrampolineWords { get; } = [jumpToCave, 0, 0, 0];
    }
}
