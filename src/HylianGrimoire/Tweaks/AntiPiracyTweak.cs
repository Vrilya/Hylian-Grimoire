using HylianGrimoire.Rom;
namespace HylianGrimoire.Tweaks;

public static class AntiPiracyTweak
{
    private static readonly HashSet<string> SupportedBuildDates = new(StringComparer.Ordinal)
    {
        "98-10-21 04:56:31",
        "98-10-26 10:58:45",
        "98-11-12 18:17:03",
        "98-11-10 14:34:22",
        "98-11-18 17:36:49",
    };

    private static readonly Patch[] Patches =
    [
        new(
            "fishing pond",
            Convert.FromHexString("3C01AD0934210010004110262C4200012C420001"),
            [],
            [],
            PatchKind.StoreRegisterByte),
        new(
            "Zelda hair",
            Convert.FromHexString("240117D98FA20094"),
            Convert.FromHexString("13210008"),
            Convert.FromHexString("10000008")),
        new(
            "castle escape gates",
            Convert.FromHexString("3C01C86E34212000"),
            Convert.FromHexString("55E10009"),
            Convert.FromHexString("55EF0009")),
    ];

    public static RomTweakStatus GetStatus(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile)
    {
        if (!SupportedBuildDates.Contains(profile.BuildDate))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "Unavailable");
        }

        int originalCount = 0;
        int patchedCount = 0;
        int unknownCount = 0;
        foreach (Patch patch in Patches)
        {
            if (!TryLocatePatch(decompressedRom, patch, out int offset))
            {
                unknownCount++;
                continue;
            }

            PatchState state = GetPatchState(decompressedRom, offset, patch);
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
            return new RomTweakStatus(RomTweakState.Unknown, $"{unknownCount} anti-piracy patch locations contain unknown bytes.");
        }

        if (patchedCount == Patches.Length)
        {
            return new RomTweakStatus(RomTweakState.On, "On");
        }

        if (originalCount == Patches.Length)
        {
            return new RomTweakStatus(RomTweakState.Off, "Off");
        }

        return new RomTweakStatus(RomTweakState.Mixed, $"{patchedCount} of {Patches.Length} anti-piracy patches are disabled.");
    }

    public static void SetEnabled(Span<byte> decompressedRom, RomVersionProfile profile, bool enabled)
    {
        if (!SupportedBuildDates.Contains(profile.BuildDate))
        {
            throw new InvalidOperationException("This tweak is not supported for the loaded ROM.");
        }

        RomTweakStatus status = GetStatus(decompressedRom, profile);
        if (!status.CanToggle)
        {
            throw new InvalidOperationException(status.Detail);
        }

        foreach (Patch patch in Patches)
        {
            if (!TryLocatePatch(decompressedRom, patch, out int offset))
            {
                throw new InvalidOperationException($"Could not locate the {patch.Name} patch location.");
            }

            WritePatch(decompressedRom, offset, patch, enabled);
        }
    }

    private static bool TryLocatePatch(ReadOnlySpan<byte> rom, Patch patch, out int offset)
    {
        int signatureOffset = IndexOf(rom, patch.Signature);
        if (signatureOffset < 0)
        {
            offset = -1;
            return false;
        }

        if (IndexOf(rom.Slice(signatureOffset + 1), patch.Signature) >= 0)
        {
            offset = -1;
            return false;
        }

        offset = signatureOffset + patch.Signature.Length + GetTargetOffsetFromSignature(patch);
        return offset + GetPatchLength(patch) <= rom.Length;
    }

    private static PatchState GetPatchState(ReadOnlySpan<byte> rom, int offset, Patch patch)
    {
        if (patch.Kind == PatchKind.StoreRegisterByte)
        {
            ReadOnlySpan<byte> store = rom.Slice(offset, 4);
            if (store[0] != 0xA0)
            {
                return PatchState.Unknown;
            }

            return store[1] switch
            {
                0x22 => PatchState.Original,
                0x20 => PatchState.Patched,
                _ => PatchState.Unknown,
            };
        }

        ReadOnlySpan<byte> current = rom.Slice(offset, patch.Original.Length);
        if (current.SequenceEqual(patch.Original))
        {
            return PatchState.Original;
        }

        return current.SequenceEqual(patch.Patched) ? PatchState.Patched : PatchState.Unknown;
    }

    private static void WritePatch(Span<byte> rom, int offset, Patch patch, bool enabled)
    {
        if (patch.Kind == PatchKind.StoreRegisterByte)
        {
            rom[offset + 1] = enabled ? (byte)0x20 : (byte)0x22;
            return;
        }

        ReadOnlySpan<byte> target = enabled ? patch.Patched : patch.Original;
        target.CopyTo(rom.Slice(offset, target.Length));
    }

    private static int GetPatchLength(Patch patch)
    {
        return patch.Kind == PatchKind.StoreRegisterByte ? 4 : patch.Original.Length;
    }

    private static int GetTargetOffsetFromSignature(Patch patch)
    {
        return patch.Kind == PatchKind.StoreRegisterByte ? 4 : 0;
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

    private sealed record Patch(
        string Name,
        byte[] Signature,
        byte[] Original,
        byte[] Patched,
        PatchKind Kind = PatchKind.FixedBytes);

    private enum PatchKind
    {
        FixedBytes,
        StoreRegisterByte,
    }

    private enum PatchState
    {
        Original,
        Patched,
        Unknown,
    }
}
