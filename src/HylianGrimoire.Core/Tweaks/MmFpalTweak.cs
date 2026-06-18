using HylianGrimoire.Rom;

namespace HylianGrimoire.Tweaks;

public static class MmFpalTweak
{
    private const int HeaderRegionOffset = 0x3E;
    private const byte OriginalHeaderRegion = (byte)'E';
    private const byte PatchedHeaderRegion = (byte)'P';

    private static readonly IReadOnlyDictionary<string, PatchProfile> ProfilesByName = new Dictionary<string, PatchProfile>(StringComparer.Ordinal)
    {
        ["Majora's Mask NTSC-U"] = new(
            ViModeOffset: 0x18FC0,
            IdleVideoNtscBranchOffset: 0x13E0,
            NotebookTvTypeOffset: 0xC0EE64,
            ViConfigurePalTableOffset: 0xC75600,
            RegionCheckOffset: 0xC0F7F8),
    };

    private static readonly byte[] OriginalHeader = [OriginalHeaderRegion];
    private static readonly byte[] PatchedHeader = [PatchedHeaderRegion];
    private static readonly byte[] OriginalIdleVideoNtscBranch = Convert.FromHexString("10410006");
    private static readonly byte[] PatchedIdleVideoNtscBranch = Convert.FromHexString("10410030");
    private static readonly byte[] OriginalNotebookTvType = Convert.FromHexString("8cc60300");
    private static readonly byte[] PatchedNotebookTvType = Convert.FromHexString("24060000");
    private static readonly byte[] OriginalRegionCheck = Convert.FromHexString("00001025");
    private static readonly byte[] PatchedRegionCheck = Convert.FromHexString("2c620003");

    private static readonly Patch[] ViTimingPatches =
    [
        P(0x00, "02", "2c"),
        P(0x0C, "03e52239", "04541e3a"),
        P(0x10, "0000020d", "00000271"),
        P(0x14, "00000c15", "00170c69"),
        P(0x18, "0c150c15", "0c6f0c6d"),
        P(0x1C, "006c02ec", "00800300"),
        P(0x30, "002501ff", "002f0269"),
        P(0x34, "000e0204", "0009026b"),
        P(0x44, "002501ff", "002f0269"),
        P(0x48, "000e0204", "0009026b"),
    ];

    private static readonly Patch[] ViYScaleResetPatches =
    [
        P(0x2C, "00000400", "00000400"),
        P(0x40, "00000400", "00000400"),
    ];

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

        IReadOnlyList<ComponentState> states = GetComponentStates(decompressedRom, patchProfile);
        string[] unknownNames = states
            .Where(state => state.State == PatchState.Unknown)
            .Select(state => state.Name)
            .ToArray();
        if (unknownNames.Length > 0)
        {
            return new RomTweakStatus(
                RomTweakState.Unknown,
                $"The MM VI PAL patch contains unexpected bytes at: {string.Join(", ", unknownNames)}.");
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

        return new RomTweakStatus(RomTweakState.Mixed, $"{patchedCount} of {states.Count} MM VI PAL patch locations are enabled.");
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

        if (enabled && status.State == RomTweakState.On
            || !enabled && status.State == RomTweakState.Off)
        {
            return;
        }

        if (enabled && MmViSelectorTweak.HasPatchedPayload(decompressedRom, profile))
        {
            throw new InvalidOperationException("Disable the MM VI selector before enabling the MM N64 VI PAL timing tweak.");
        }

        foreach (Patch patch in ViTimingPatches)
        {
            CopyPatch(decompressedRom, patchProfile.ViModeOffset + patch.RelativeOffset, enabled ? patch.Patched : patch.Original);
        }

        foreach (Patch patch in ViYScaleResetPatches)
        {
            CopyPatch(decompressedRom, patchProfile.ViModeOffset + patch.RelativeOffset, patch.Original);
        }

        foreach (Patch patch in ViConfigurePalTablePatches)
        {
            CopyPatch(
                decompressedRom,
                patchProfile.ViConfigurePalTableOffset + patch.RelativeOffset,
                enabled ? patch.Patched : patch.Original);
        }

        CopyPatch(decompressedRom, HeaderRegionOffset, enabled ? PatchedHeader : OriginalHeader);
        CopyPatch(
            decompressedRom,
            patchProfile.IdleVideoNtscBranchOffset,
            enabled ? PatchedIdleVideoNtscBranch : OriginalIdleVideoNtscBranch);
        CopyPatch(decompressedRom, patchProfile.NotebookTvTypeOffset, enabled ? PatchedNotebookTvType : OriginalNotebookTvType);

        if (patchProfile.RegionCheckOffset is int regionCheckOffset)
        {
            CopyPatch(decompressedRom, regionCheckOffset, enabled ? PatchedRegionCheck : OriginalRegionCheck);
        }

        if (!N64Checksum.TryUpdate(decompressedRom))
        {
            throw new InvalidOperationException("Could not recalculate the ROM checksum.");
        }
    }

    private static IReadOnlyList<ComponentState> GetComponentStates(ReadOnlySpan<byte> rom, PatchProfile patchProfile)
    {
        List<ComponentState> states = [];

        foreach (Patch patch in ViTimingPatches)
        {
            states.Add(new ComponentState(
                $"VI timing field 0x{patch.RelativeOffset:X2}",
                GetPatchState(rom, patchProfile.ViModeOffset + patch.RelativeOffset, patch.Original, patch.Patched)));
        }

        states.Add(new ComponentState("ROM region code", GetPatchState(rom, HeaderRegionOffset, OriginalHeader, PatchedHeader)));
        states.Add(new ComponentState(
            "PAL video init path",
            GetPatchState(rom, patchProfile.IdleVideoNtscBranchOffset, OriginalIdleVideoNtscBranch, PatchedIdleVideoNtscBranch)));
        states.Add(new ComponentState(
            "Bombers' Notebook VI timing source",
            GetPatchState(rom, patchProfile.NotebookTvTypeOffset, OriginalNotebookTvType, PatchedNotebookTvType)));
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

        if (patchProfile.RegionCheckOffset is int regionCheckOffset)
        {
            states.Add(new ComponentState(
                "startup region check",
                GetPatchState(rom, regionCheckOffset, OriginalRegionCheck, PatchedRegionCheck)));
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

    private static bool HasRequiredRanges(ReadOnlySpan<byte> rom, PatchProfile patchProfile)
    {
        foreach (Patch patch in ViTimingPatches)
        {
            if (!HasPatchRange(rom, patchProfile.ViModeOffset + patch.RelativeOffset, patch.Original.Length))
            {
                return false;
            }
        }

        foreach (Patch patch in ViYScaleResetPatches)
        {
            if (!HasPatchRange(rom, patchProfile.ViModeOffset + patch.RelativeOffset, patch.Original.Length))
            {
                return false;
            }
        }

        if (!HasPatchRange(rom, HeaderRegionOffset, OriginalHeader.Length))
        {
            return false;
        }

        if (!HasPatchRange(rom, patchProfile.IdleVideoNtscBranchOffset, OriginalIdleVideoNtscBranch.Length))
        {
            return false;
        }

        if (!HasPatchRange(rom, patchProfile.NotebookTvTypeOffset, OriginalNotebookTvType.Length))
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

        return patchProfile.RegionCheckOffset is not int regionCheckOffset
            || HasPatchRange(rom, regionCheckOffset, OriginalRegionCheck.Length);
    }

    private static void CopyPatch(Span<byte> rom, int offset, ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(rom.Slice(offset, bytes.Length));
    }

    private static bool HasPatchRange(ReadOnlySpan<byte> rom, int offset, int length) =>
        offset >= 0 && offset + length <= rom.Length;

    private static Patch P(int relativeOffset, string original, string patched)
    {
        return new Patch(relativeOffset, Convert.FromHexString(original), Convert.FromHexString(patched));
    }

    private sealed record PatchProfile(
        int ViModeOffset,
        int IdleVideoNtscBranchOffset,
        int NotebookTvTypeOffset,
        int ViConfigurePalTableOffset,
        int? RegionCheckOffset);

    private sealed record Patch(int RelativeOffset, byte[] Original, byte[] Patched);

    private sealed record ComponentState(string Name, PatchState State);

    private enum PatchState
    {
        Original,
        Patched,
        Unknown,
    }
}
