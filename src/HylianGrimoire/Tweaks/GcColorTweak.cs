using HylianGrimoire.Rom;
namespace HylianGrimoire.Tweaks;

public static class GcColorTweak
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<Patch>> Profiles = new Dictionary<string, IReadOnlyList<Patch>>(StringComparer.Ordinal)
    {
        ["Retail NTSC GameCube"] =
        [
            Icon(0x00844750),
            ..ConstructUs(0x00B57944),
            Start(0x00AE8BD4),
            ..Quest(0x00B9DFF0),
            ..Pause([0x00BB4448, 0x00BB44A8, 0x00BB44BA], [(0x00BAB8C0, 0x00BAB8D4), (0x00BABAAC, 0x00BABAB0)]),
            ..TextboxPrompt(0x00B87568),
            ..MessageOcarina(0x00B87840, 0x00B4BA60, "A426CFFC", "A438CFFC", 0x00B4BA68, "A438CFFA", "A426CFFA"),
            ..Shop(0x00E1B468, 0x00E1B474, 0x00E1BEF8, 0x00E1BEFC),
        ],
        ["Retail NTSC Master Quest"] =
        [
            Icon(0x00844750),
            ..ConstructUs(0x00B57924),
            Start(0x00AE8BD4),
            ..Quest(0x00B9DFD0),
            ..Pause([0x00BB4428, 0x00BB4488, 0x00BB449A], [(0x00BAB8A0, 0x00BAB8B4), (0x00BABA8C, 0x00BABA90)]),
            ..TextboxPrompt(0x00B87548),
            ..MessageOcarina(0x00B87820, 0x00B4BA40, "A426CFDC", "A438CFDC", 0x00B4BA48, "A438CFDA", "A426CFDA"),
            ..Shop(0x00E1B4E8, 0x00E1B4F4, 0x00E1BF78, 0x00E1BF7C),
        ],
        ["Retail PAL GameCube"] =
        [
            Icon(0x007C9B50),
            ..ConstructEu(0x00B57144),
            Start(0x00AEAB78),
            ..Quest(0x00B9C490),
            ..Pause([0x00BB2AE0, 0x00BB2B54, 0x00BB2B66], [(0x00BA9CE0, 0x00BA9CF4), (0x00BA9ECC, 0x00BA9ED0)]),
            ..TextboxPrompt(0x00B86FB8),
            ..MessageOcarina(0x00B87298, 0x00B4DB20, "A426A80C", "A438A80C", 0x00B4DB28, "A438A80A", "A426A80A"),
            ..Shop(0x00E196B8, 0x00E196C4, 0x00E1A148, 0x00E1A14C),
        ],
        ["Retail PAL Master Quest"] =
        [
            Icon(0x007C9B50),
            ..ConstructEu(0x00B57124),
            Start(0x00AEAB78),
            ..Quest(0x00B9C470),
            ..Pause([0x00BB2AC0, 0x00BB2B34, 0x00BB2B46], [(0x00BA9CC0, 0x00BA9CD4), (0x00BA9EAC, 0x00BA9EB0)]),
            ..TextboxPrompt(0x00B86F98),
            ..MessageOcarina(0x00B87278, 0x00B4DB00, "A426A7EC", "A438A7EC", 0x00B4DB08, "A438A7EA", "A426A7EA"),
            ..Shop(0x00E19698, 0x00E196A4, 0x00E1A128, 0x00E1A12C),
        ],
    };

    public static RomTweakStatus GetStatus(ReadOnlySpan<byte> decompressedRom, RomVersionProfile profile)
    {
        if (!Profiles.TryGetValue(profile.Name, out IReadOnlyList<Patch>? patches))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "This tweak supports retail GameCube ROMs only.");
        }

        if (!HasPatchRanges(decompressedRom, patches))
        {
            return new RomTweakStatus(RomTweakState.Unsupported, "The ROM is too small for this tweak.");
        }

        int gcCount = 0;
        int n64Count = 0;
        int unknownCount = 0;
        foreach (Patch patch in patches)
        {
            PatchState state = GetPatchState(decompressedRom, patch);
            if (state == PatchState.Gc)
            {
                gcCount++;
            }
            else if (state == PatchState.N64)
            {
                n64Count++;
            }
            else
            {
                unknownCount++;
            }
        }

        if (unknownCount > 0)
        {
            return new RomTweakStatus(RomTweakState.Unknown, $"{unknownCount} color patch locations contain unknown bytes.");
        }

        if (n64Count == patches.Count)
        {
            return new RomTweakStatus(RomTweakState.On, "On");
        }

        if (gcCount == patches.Count)
        {
            return new RomTweakStatus(RomTweakState.Off, "Off");
        }

        return new RomTweakStatus(RomTweakState.Mixed, $"{n64Count} of {patches.Count} color patches are enabled.");
    }

    public static void SetEnabled(Span<byte> decompressedRom, RomVersionProfile profile, bool enabled)
    {
        if (!Profiles.TryGetValue(profile.Name, out IReadOnlyList<Patch>? patches) || !HasPatchRanges(decompressedRom, patches))
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
            ReadOnlySpan<byte> target = enabled ? patch.N64 : patch.Gc;
            target.CopyTo(decompressedRom.Slice(patch.Offset, target.Length));
        }
    }

    private static bool HasPatchRanges(ReadOnlySpan<byte> rom, IReadOnlyList<Patch> patches)
    {
        foreach (Patch patch in patches)
        {
            if (patch.Offset < 0 || patch.Offset + patch.Gc.Length > rom.Length)
            {
                return false;
            }
        }

        return true;
    }

    private static PatchState GetPatchState(ReadOnlySpan<byte> rom, Patch patch)
    {
        ReadOnlySpan<byte> current = rom.Slice(patch.Offset, patch.Gc.Length);
        if (current.SequenceEqual(patch.Gc))
        {
            return PatchState.Gc;
        }

        return current.SequenceEqual(patch.N64) ? PatchState.N64 : PatchState.Unknown;
    }

    private static Patch P(int offset, string gc, string n64)
    {
        return new Patch(offset, Convert.FromHexString(gc), Convert.FromHexString(n64));
    }

    private static Patch Icon(int offset) => P(offset, "FA00000000FF64FF", "FA0000000064FFFF");

    private static Patch Start(int offset) => P(offset, "3C01787834217800", "3C01C80034210000");

    private static IReadOnlyList<Patch> Quest(int overlayStart) =>
    [
        P(overlayStart + 0x17C0, "3C0A50FF", "3C0A5096"),
        P(overlayStart + 0x17C4, "354A9600", "354AFF00"),
        P(overlayStart + 0x1AA0, "3C0950FF", "3C095096"),
        P(overlayStart + 0x1AA4, "352996C8", "3529FFC8"),
        P(overlayStart + 0x1D8C, "3C0A50FF", "3C0A5096"),
        P(overlayStart + 0x1D94, "354A9600", "354AFF00"),
    ];

    private static IReadOnlyList<Patch> TextboxPrompt(int baseOffset) =>
    [
        P(baseOffset, "000000C80050003200FF0082", "0000005000C80032008200FF"),
        P(baseOffset + 0x0C, "000000000000000000FF0082", "0000000000000000008200FF"),
        P(baseOffset + 0x18, "0000000000C8000000500000000C0000", "000000000050000000C80000000C0000"),
    ];

    private static IReadOnlyList<Patch> MessageOcarina(
        int dataBase,
        int resetGreen,
        string resetGreenGc,
        string resetGreenN64,
        int resetBlue,
        string resetBlueGc,
        string resetBlueN64) =>
    [
        P(resetGreen, resetGreenGc, resetGreenN64),
        P(resetBlue, resetBlueGc, resetBlueN64),
        P(dataBase, "005000FF0096006400FF00C8", "0050009600FF006400C800FF"),
        P(dataBase + 0x0C, "000A000A000A003200FF0032", "000A000A000A0032003200FF"),
    ];

    private static IReadOnlyList<Patch> Pause(int[] colorOffsets, (int High, int Low)[] promptPairs)
    {
        var patches = new List<Patch>();
        patches.AddRange(colorOffsets.Select(offset => P(offset, "000000FF0032", "0000003200FF")));
        foreach ((int high, int low) in promptPairs)
        {
            patches.Add(P(high, "3C0164FF", "3C016464"));
            patches.Add(P(low, "34216400", "3421FF00"));
        }

        return patches;
    }

    private static IReadOnlyList<Patch> Shop(int animGreen, int animBlue, int initGreen, int initBlue) =>
    [
        P(animGreen, "006A5823", "01AA5823"),
        P(animBlue, "AC8F0234", "AC990234"),
        P(initGreen, "AE030230", "AE0F0230"),
        P(initBlue, "AE0F0234", "AE030234"),
    ];

    private static IReadOnlyList<Patch> ConstructUs(int baseOffset) =>
    [
        P(baseOffset + 0x04, "240E00FF", "240E0000"),
        P(baseOffset + 0x10, "2403001E", "24030096"),
        P(baseOffset + 0x24, "240F00C8", "240F005A"),
        P(baseOffset + 0x30, "A72307EE", "A72007EE"),
        P(baseOffset + 0x38, "24190032", "241900FF"),
        P(baseOffset + 0x5C, "A5C00AC0", "A5CF0AC0"),
    ];

    private static IReadOnlyList<Patch> ConstructEu(int baseOffset) =>
    [
        P(baseOffset + 0x04, "240C00FF", "240C0000"),
        P(baseOffset + 0x10, "2403001E", "24030096"),
        P(baseOffset + 0x24, "240D00C8", "240D005A"),
        P(baseOffset + 0x30, "A5E307EE", "A5E007EE"),
        P(baseOffset + 0x38, "240F0032", "240F00FF"),
        P(baseOffset + 0x5C, "A5800AC0", "A58D0AC0"),
    ];

    private sealed record Patch(int Offset, byte[] Gc, byte[] N64);

    private enum PatchState
    {
        Gc,
        N64,
        Unknown,
    }
}
