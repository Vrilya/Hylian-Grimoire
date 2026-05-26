using HylianGrimoire.Rom;
using System.Buffers.Binary;

namespace HylianGrimoire.Tweaks;

public static class GcCreditsTweak
{
    private static readonly uint[] OriginalWords =
    [
        0x8E4A1360,
        0x24010006,
        0x3C198100,
        0x15410003,
        0x00000000,
        0x0320F809,
    ];

    private static readonly uint[] PatchedWords =
    [
        0x8E4A1360,
        0x24010006,
        0x3C198100,
        0x15410003,
        0x00000000,
        0x00000000,
    ];

    private static readonly IReadOnlyDictionary<string, int> OffsetsByBuildDate = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["03-02-21 20:12:23"] = 0x00B119D0,
        ["03-02-21 20:37:19"] = 0x00B119B0,
        ["02-12-19 13:28:09"] = 0x00B0F920,
        ["02-12-19 14:05:42"] = 0x00B0F900,
    };

    public static RomTweakStatus GetStatus(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile)
    {
        if (!OffsetsByBuildDate.TryGetValue(profile.BuildDate, out int offset))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "Unavailable");
        }

        if (!HasPatchRange(decompressedRom, offset))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "The ROM is too small for this tweak.");
        }

        ReadOnlySpan<byte> window = decompressedRom.Slice(offset, OriginalWords.Length * sizeof(uint));
        if (WordsEqual(window, PatchedWords))
        {
            return new RomTweakStatus(RomTweakState.On, "On");
        }

        if (WordsEqual(window, OriginalWords))
        {
            return new RomTweakStatus(RomTweakState.Off, "Off");
        }

        return new RomTweakStatus(RomTweakState.Unknown, "The credits patch location contains unknown bytes.");
    }

    public static void SetEnabled(Span<byte> decompressedRom, RomVersionProfile profile, bool enabled)
    {
        if (!OffsetsByBuildDate.TryGetValue(profile.BuildDate, out int offset) || !HasPatchRange(decompressedRom, offset))
        {
            throw new InvalidOperationException("This tweak is not supported for the loaded ROM.");
        }

        RomTweakStatus status = GetStatus(decompressedRom, profile);
        if (!status.CanToggle)
        {
            throw new InvalidOperationException(status.Detail);
        }

        WriteWords(
            decompressedRom.Slice(offset, OriginalWords.Length * sizeof(uint)),
            enabled ? PatchedWords : OriginalWords);
    }

    private static bool HasPatchRange(ReadOnlySpan<byte> rom, int offset)
    {
        return offset >= 0 && offset + OriginalWords.Length * sizeof(uint) <= rom.Length;
    }

    private static bool WordsEqual(ReadOnlySpan<byte> bytes, IReadOnlyList<uint> words)
    {
        for (int i = 0; i < words.Count; i++)
        {
            if (BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(i * sizeof(uint), sizeof(uint))) != words[i])
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
}
