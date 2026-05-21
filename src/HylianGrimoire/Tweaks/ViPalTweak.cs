using HylianGrimoire.Rom;
namespace HylianGrimoire.Tweaks;

public static class ViPalTweak
{
    private static readonly HashSet<string> SupportedBuildDates = new(StringComparer.Ordinal)
    {
        "03-02-21 20:12:23",
        "03-02-21 20:37:19",
    };

    private static readonly byte[] OriginalSignature = Convert.FromHexString("020000000000311e0000014003e52239");
    private static readonly byte[] PatchedSignature = Convert.FromHexString("2c0000000000311e0000014004541e3a");

    private static readonly Patch[] Patches =
    [
        P(0x00, "02", "2c"),
        P(0x0C, "03e52239", "04541e3a"),
        P(0x10, "0000020d", "00000271"),
        P(0x14, "00000c15", "00170c69"),
        P(0x18, "0c150c15", "0c6f0c6d"),
        P(0x1C, "006c02ec", "00800300"),
        P(0x2C, "00000400", "00000354"),
        P(0x30, "002501ff", "002f0269"),
        P(0x34, "000e0204", "0009026b"),
        P(0x40, "00000400", "00000354"),
        P(0x44, "002501ff", "002f0269"),
        P(0x48, "000e0204", "0009026b"),
    ];

    public static RomTweakStatus GetStatus(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile)
    {
        if (!SupportedBuildDates.Contains(profile.BuildDate))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "This tweak supports retail PAL GameCube ROMs only.");
        }

        if (!TryLocateStruct(decompressedRom, out int structOffset))
        {
            return new RomTweakStatus(RomTweakState.Unknown, "Could not find a known VI timing struct.");
        }

        int originalCount = 0;
        int patchedCount = 0;
        int unknownCount = 0;
        foreach (Patch patch in Patches)
        {
            PatchState state = GetPatchState(decompressedRom, structOffset, patch);
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
            return new RomTweakStatus(RomTweakState.Unknown, $"{unknownCount} VI timing patch locations contain unknown bytes.");
        }

        if (patchedCount == Patches.Length)
        {
            return new RomTweakStatus(RomTweakState.On, "On");
        }

        if (originalCount == Patches.Length)
        {
            return new RomTweakStatus(RomTweakState.Off, "Off");
        }

        return new RomTweakStatus(RomTweakState.Mixed, $"{patchedCount} of {Patches.Length} VI timing patches are enabled.");
    }

    public static void SetEnabled(Span<byte> decompressedRom, RomVersionProfile profile, bool enabled)
    {
        if (!SupportedBuildDates.Contains(profile.BuildDate))
        {
            throw new InvalidOperationException("This tweak is not supported for the loaded ROM.");
        }

        if (!TryLocateStruct(decompressedRom, out int structOffset))
        {
            throw new InvalidOperationException("Could not find a known VI timing struct.");
        }

        RomTweakStatus status = GetStatus(decompressedRom, profile);
        if (!status.CanToggle)
        {
            throw new InvalidOperationException(status.Detail);
        }

        foreach (Patch patch in Patches)
        {
            ReadOnlySpan<byte> target = enabled ? patch.Patched : patch.Original;
            target.CopyTo(decompressedRom.Slice(structOffset + patch.RelativeOffset, target.Length));
        }

        if (!N64Checksum.TryUpdate(decompressedRom))
        {
            throw new InvalidOperationException("Could not recalculate the ROM checksum.");
        }
    }

    private static bool TryLocateStruct(ReadOnlySpan<byte> rom, out int offset)
    {
        offset = IndexOf(rom, OriginalSignature);
        if (offset >= 0)
        {
            return true;
        }

        offset = IndexOf(rom, PatchedSignature);
        return offset >= 0;
    }

    private static int IndexOf(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            if (haystack.Slice(i, needle.Length).SequenceEqual(needle))
            {
                return i;
            }
        }

        return -1;
    }

    private static PatchState GetPatchState(ReadOnlySpan<byte> rom, int structOffset, Patch patch)
    {
        ReadOnlySpan<byte> current = rom.Slice(structOffset + patch.RelativeOffset, patch.Original.Length);
        if (current.SequenceEqual(patch.Original))
        {
            return PatchState.Original;
        }

        return current.SequenceEqual(patch.Patched) ? PatchState.Patched : PatchState.Unknown;
    }

    private static Patch P(int relativeOffset, string original, string patched)
    {
        return new Patch(relativeOffset, Convert.FromHexString(original), Convert.FromHexString(patched));
    }

    private sealed record Patch(int RelativeOffset, byte[] Original, byte[] Patched);

    private enum PatchState
    {
        Original,
        Patched,
        Unknown,
    }
}
