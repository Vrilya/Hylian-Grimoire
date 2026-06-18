using HylianGrimoire.Rom;
using System.Buffers.Binary;

namespace HylianGrimoire.Tweaks;

public static class GcNoControllerTweak
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<Patch>> Profiles = new Dictionary<string, IReadOnlyList<Patch>>(StringComparer.Ordinal)
    {
        ["03-02-21 20:12:23"] =
        [
            P(0x00DF8A0C, [0x3C0E8012, 0x8DCE9CAC, 0x3401FEDC, 0x11C10089, 0x34018000],
                          [0x3C0E8012, 0x91CEBBA8, 0x31CE0001, 0x11C00089, 0x34018000]),
            P(0x00DFA52C, [0x3C188012, 0x8F189CAC, 0x3401FEDC, 0x3C05E700, 0x17010078, 0x8FA90080],
                          [0x3C188012, 0x9318BBA8, 0x33180001, 0x3C05E700, 0x17000078, 0x8FA90080]),
        ],
        ["03-02-21 20:37:19"] =
        [
            P(0x00DF8974, [0x3C0E8012, 0x8DCE9C8C, 0x3401FEDC, 0x11C1008B, 0x34018000],
                          [0x3C0E8012, 0x91CEBB88, 0x31CE0001, 0x11C0008B, 0x34018000]),
            P(0x00DFA50C, [0x3C088012, 0x8D089C8C, 0x3401FEDC, 0x3C05E700, 0x15010078, 0x8FAA0080],
                          [0x3C088012, 0x9108BB88, 0x31080001, 0x3C05E700, 0x15000078, 0x8FAA0080]),
        ],
        ["02-12-19 13:28:09"] =
        [
            P(0x00DFA32C, [0x3C0E8012, 0x8DCEC49C, 0x3401FEDC, 0x11C10089, 0x34018000],
                          [0x3C0E8012, 0x91CEE39A, 0x31CE0001, 0x11C00089, 0x34018000]),
            P(0x00DFC2E0, [0x8E781354, 0x3401FEDC, 0x8FAF01C0, 0x17010077, 0x8FAA0080],
                          [0x92783252, 0x33180001, 0x8FAF01C0, 0x17000077, 0x8FAA0080]),
            P(0x00DFC0E0, [0x8E781354], [0x92783252]),
        ],
        ["02-12-19 14:05:42"] =
        [
            P(0x00DFA294, [0x3C0E8012, 0x8DCEC47C, 0x3401FEDC, 0x11C1008B, 0x34018000],
                          [0x3C0E8012, 0x91CEE37A, 0x31CE0001, 0x11C0008B, 0x34018000]),
            P(0x00DFC350, [0x8E6B1354, 0x3401FEDC, 0x8FAF01C8, 0x15610078, 0x8FAA0080],
                          [0x926B3252, 0x316B0001, 0x8FAF01C8, 0x15600078, 0x8FAA0080]),
            P(0x00DFC150, [0x8E6B1354], [0x926B3252]),
        ],
    };

    public static RomTweakStatus GetStatus(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile)
    {
        if (!Profiles.TryGetValue(profile.BuildDate, out IReadOnlyList<Patch>? patches))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "Unavailable");
        }

        if (!HasPatchRanges(decompressedRom, patches))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "The ROM is too small for this tweak.");
        }

        int originalCount = 0;
        int patchedCount = 0;
        int unknownCount = 0;
        foreach (Patch patch in patches)
        {
            PatchState state = GetPatchState(decompressedRom, patch);
            if (state == PatchState.Original)
            {
                originalCount++;
            }
            else if (state == PatchState.Patched)
            {
                patchedCount++;
            }
            else
            {
                unknownCount++;
            }
        }

        if (unknownCount > 0)
        {
            return new RomTweakStatus(RomTweakState.Unknown, $"{unknownCount} no-controller patch locations contain unknown bytes.");
        }

        if (patchedCount == patches.Count)
        {
            return new RomTweakStatus(RomTweakState.On, "On");
        }

        if (originalCount == patches.Count)
        {
            return new RomTweakStatus(RomTweakState.Off, "Off");
        }

        return new RomTweakStatus(RomTweakState.Mixed, $"{patchedCount} of {patches.Count} no-controller patches are enabled.");
    }

    public static void SetEnabled(Span<byte> decompressedRom, RomVersionProfile profile, bool enabled)
    {
        if (!Profiles.TryGetValue(profile.BuildDate, out IReadOnlyList<Patch>? patches) || !HasPatchRanges(decompressedRom, patches))
        {
            throw new InvalidOperationException("This tweak is not supported for the loaded ROM.");
        }

        RomTweakStatus status = GetStatus(decompressedRom, profile);
        if (!status.CanToggle)
        {
            throw new InvalidOperationException(status.Detail);
        }

        foreach (Patch patch in patches)
        {
            WriteWords(
                decompressedRom.Slice(patch.Offset, patch.OriginalWords.Count * sizeof(uint)),
                enabled ? patch.PatchedWords : patch.OriginalWords);
        }
    }

    private static bool HasPatchRanges(ReadOnlySpan<byte> rom, IReadOnlyList<Patch> patches)
    {
        foreach (Patch patch in patches)
        {
            if (patch.Offset < 0 || patch.Offset + patch.OriginalWords.Count * sizeof(uint) > rom.Length)
            {
                return false;
            }
        }

        return true;
    }

    private static PatchState GetPatchState(ReadOnlySpan<byte> rom, Patch patch)
    {
        ReadOnlySpan<byte> current = rom.Slice(patch.Offset, patch.OriginalWords.Count * sizeof(uint));
        if (WordsEqual(current, patch.OriginalWords))
        {
            return PatchState.Original;
        }

        return WordsEqual(current, patch.PatchedWords) ? PatchState.Patched : PatchState.Unknown;
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

    private static Patch P(int offset, uint[] originalWords, uint[] patchedWords)
    {
        return new Patch(offset, originalWords, patchedWords);
    }

    private sealed record Patch(int Offset, IReadOnlyList<uint> OriginalWords, IReadOnlyList<uint> PatchedWords);

    private enum PatchState
    {
        Original,
        Patched,
        Unknown,
    }
}
