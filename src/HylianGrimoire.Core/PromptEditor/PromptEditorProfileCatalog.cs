using System.Drawing;
using HylianGrimoire.Rom;

namespace HylianGrimoire.PromptEditor;

public static class PromptEditorProfileCatalog
{
    public const int N64Width = 320;

    private static readonly IReadOnlyList<PromptEditorLineDefinition> OotLines =
    [
        new(PromptEditorKind.File, "File panel", "ButtonA", "Confirm", "FileIcon", "FileText"),
        new(PromptEditorKind.Melody, "Melody panel", "ButtonA", "Melody", "MelodyIcon", "MelodyText"),
        new(PromptEditorKind.Gear, "Gear panel", "ButtonA", "Use", "GearIcon", "GearText"),
        new(PromptEditorKind.Item, "Item panel", "ButtonC", "Use", "ItemIcon", "ItemText"),
    ];

    private static readonly IReadOnlyDictionary<string, PromptEditorLanguage> OotLanguages =
        new Dictionary<string, PromptEditorLanguage>(StringComparer.Ordinal)
        {
            ["eng"] = new(
                "eng",
                "English",
                new Dictionary<PromptEditorKind, PromptEditorDefaults>
                {
                    [PromptEditorKind.File] = new(-42, -20),
                    [PromptEditorKind.Melody] = new(-53, -31),
                    [PromptEditorKind.Gear] = new(-37, -15),
                    [PromptEditorKind.Item] = new(-48, -1),
                }),
            ["ger"] = new(
                "ger",
                "Deutsch",
                new Dictionary<PromptEditorKind, PromptEditorDefaults>
                {
                    [PromptEditorKind.File] = new(32, -52),
                    [PromptEditorKind.Melody] = new(40, -59),
                    [PromptEditorKind.Gear] = new(30, -54),
                    [PromptEditorKind.Item] = new(16, -68),
                }),
            ["fra"] = new(
                "fra",
                "Francais",
                new Dictionary<PromptEditorKind, PromptEditorDefaults>
                {
                    [PromptEditorKind.File] = new(-45, -25),
                    [PromptEditorKind.Melody] = new(-64, -44),
                    [PromptEditorKind.Gear] = new(-50, -30),
                    [PromptEditorKind.Item] = new(-62, -18),
                }),
        };

    private static readonly IReadOnlyList<PromptEditorLineDefinition> MajorasMaskLines =
    [
        new(PromptEditorKind.Equip, "Equip panel", "ButtonC", "Equip", "EquipIcon", "EquipText"),
        new(PromptEditorKind.Notebook, "Notebook panel", "ButtonA", "Notebook", "NotebookIcon", "NotebookText"),
        new(PromptEditorKind.Melody, "Melody panel", "ButtonA", "Melody", "MelodyIcon", "MelodyText"),
    ];

    private static readonly IReadOnlyDictionary<string, PromptEditorLanguage> MajorasMaskLanguages =
        new Dictionary<string, PromptEditorLanguage>(StringComparer.Ordinal)
        {
            ["eng"] = new(
                "eng",
                "English",
                new Dictionary<PromptEditorKind, PromptEditorDefaults>
                {
                    [PromptEditorKind.Equip] = new(-49, -2),
                    [PromptEditorKind.Notebook] = new(-58, -38),
                    [PromptEditorKind.Melody] = new(-55, -35),
                }),
        };

    private static readonly IReadOnlyDictionary<string, PromptEditorProfile> Profiles =
        new Dictionary<string, PromptEditorProfile>(StringComparer.Ordinal)
        {
            ["Retail NTSC 1.0"] = OotProfile("Retail NTSC 1.0", ["eng"], 0x007bd000, 0x00872000, null, null, NtscIconAssets(), OotTextAssets(), NtscSites(0x00bb11e0), NtscFixed(0x00bb11e0)),
            ["Retail NTSC 1.1"] = OotProfile("Retail NTSC 1.1", ["eng"], 0x007bd000, 0x00872000, null, null, NtscIconAssets(), OotTextAssets(), NtscSites(0x00bb1380), NtscFixed(0x00bb1380)),
            ["Retail NTSC 1.2"] = OotProfile("Retail NTSC 1.2", ["eng"], 0x007bd000, 0x00872000, null, null, NtscIconAssets(), OotTextAssets(), NtscSites(0x00bb1210), NtscFixed(0x00bb1210)),
            ["Retail NTSC GameCube"] = OotProfile("Retail NTSC GameCube", ["eng"], 0x007bc000, 0x00871000, null, null, NtscIconAssets(), OotTextAssets(), GcUsSites(0x00b9dff0), GcUsFixed(0x00b9dff0)),
            ["Retail NTSC Master Quest"] = OotProfile("Retail NTSC Master Quest", ["eng"], 0x007bc000, 0x00871000, null, null, NtscIconAssets(), OotTextAssets(), GcUsSites(0x00b9dfd0), GcUsFixed(0x00b9dfd0)),
            ["Retail PAL 1.0"] = OotProfile("Retail PAL 1.0", ["eng", "ger", "fra"], 0x00741000, 0x007e9000, 0x007f7000, 0x00806000, PalIconAssets(), OotTextAssets(), PalSites(0x00bb10e0), PalFixed(0x00bb10e0)),
            ["Retail PAL 1.1"] = OotProfile("Retail PAL 1.1", ["eng", "ger", "fra"], 0x00741000, 0x007e9000, 0x007f7000, 0x00806000, PalIconAssets(), OotTextAssets(), PalSites(0x00bb1230), PalFixed(0x00bb1230)),
            ["Retail PAL GameCube"] = OotProfile("Retail PAL GameCube", ["eng"], 0x00740000, 0x007e8000, null, null, PalIconAssets(), OotTextAssets(), GcEuSites(0x00b9c490), GcEuFixed(0x00b9c490)),
            ["Retail PAL Master Quest"] = OotProfile("Retail PAL Master Quest", ["eng"], 0x00740000, 0x007e8000, null, null, PalIconAssets(), OotTextAssets(), GcEuSites(0x00b9c470), GcEuFixed(0x00b9c470)),
            ["Majora's Mask NTSC-U"] = MajorasMaskUsProfile(),
            ["Majora's Mask NTSC-U GameCube"] = MajorasMaskUsGameCubeProfile(),
        };

    public static bool TryGetProfile(RomVersionProfile romProfile, out PromptEditorProfile profile) =>
        Profiles.TryGetValue(romProfile.Name, out profile!);

    public static string GetDefaultLanguageKey(RomVersionProfile romProfile, int activeMessageBankIndex)
    {
        if (!TryGetProfile(romProfile, out PromptEditorProfile? profile))
        {
            return "eng";
        }

        return profile.LanguageKeys.Count > activeMessageBankIndex
            ? profile.LanguageKeys[activeMessageBankIndex]
            : profile.LanguageKeys[0];
    }

    private static PromptEditorProfile OotProfile(
        string displayName,
        string[] languages,
        int iconBase,
        int englishTextBase,
        int? germanTextBase,
        int? frenchTextBase,
        IReadOnlyDictionary<string, PromptEditorAsset> iconAssets,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, PromptEditorAsset>> textAssets,
        IReadOnlyDictionary<string, PromptEditorPatchSite> patchSites,
        IReadOnlyDictionary<string, PromptEditorFixedPatch> fixedPatches) =>
        new(
            displayName,
            languages,
            OotLanguages,
            OotLines,
            RawSegment(iconBase),
            OotTextBases(englishTextBase, germanTextBase, frenchTextBase),
            iconAssets,
            textAssets,
            patchSites,
            fixedPatches);

    private static PromptEditorProfile MajorasMaskUsProfile() =>
        new(
            "Majora's Mask NTSC-U",
            ["eng"],
            MajorasMaskLanguages,
            MajorasMaskLines,
            CmpDmaArchiveSegment(0x00a36c10),
            new Dictionary<string, PromptEditorSegment>(StringComparer.Ordinal)
            {
                ["eng"] = RawSegment(0x00a13000),
            },
            MajorasMaskIconAssets(),
            MajorasMaskTextAssets(0x2000, 0x2400, 0x3700),
            MajorasMaskUsSites(0x00c9bdb0),
            new Dictionary<string, PromptEditorFixedPatch>(StringComparer.Ordinal));

    private static PromptEditorProfile MajorasMaskUsGameCubeProfile() =>
        new(
            "Majora's Mask NTSC-U GameCube",
            ["eng"],
            MajorasMaskLanguages,
            MajorasMaskLines,
            CmpDmaArchiveSegment(0x00a45c10),
            new Dictionary<string, PromptEditorSegment>(StringComparer.Ordinal)
            {
                ["eng"] = RawSegment(0x00a23000),
            },
            MajorasMaskGameCubeIconAssets(),
            MajorasMaskTextAssets(0x1000, 0x1400, 0x2700),
            MajorasMaskUsSites(0x00ca59a0),
            new Dictionary<string, PromptEditorFixedPatch>(StringComparer.Ordinal));

    private static Dictionary<string, PromptEditorSegment> OotTextBases(int english, int? german, int? french)
    {
        var bases = new Dictionary<string, PromptEditorSegment>(StringComparer.Ordinal)
        {
            ["eng"] = RawSegment(english),
        };

        if (german is int germanBase)
        {
            bases["ger"] = RawSegment(germanBase);
        }

        if (french is int frenchBase)
        {
            bases["fra"] = RawSegment(frenchBase);
        }

        return bases;
    }

    private static PromptEditorSegment RawSegment(int romBase) =>
        new(romBase, PromptEditorSegmentFormat.Raw);

    private static PromptEditorSegment CmpDmaArchiveSegment(int romBase) =>
        new(romBase, PromptEditorSegmentFormat.CmpDmaArchive);

    private static Dictionary<string, PromptEditorAsset> NtscIconAssets() => new(StringComparer.Ordinal)
    {
        ["ButtonA"] = Ia8Asset(0x861c0, 24, 16, 0x88748),
        ["ButtonC"] = Ia8Asset(0x864c0, 48, 16, 0x886f0),
        ["PanelLeft"] = Ia8Asset(0x867c0, 72, 24, Color.FromArgb(90, 100, 130)),
        ["PanelRight"] = Ia8Asset(0x86e80, 72, 24, Color.FromArgb(90, 100, 130)),
    };

    private static Dictionary<string, PromptEditorAsset> PalIconAssets() => new(StringComparer.Ordinal)
    {
        ["ButtonA"] = Ia8Asset(0x875c0, 24, 16, 0x89b48),
        ["ButtonC"] = Ia8Asset(0x878c0, 48, 16, 0x89af0),
        ["PanelLeft"] = Ia8Asset(0x87bc0, 72, 24, Color.FromArgb(90, 100, 130)),
        ["PanelRight"] = Ia8Asset(0x88280, 72, 24, Color.FromArgb(90, 100, 130)),
    };

    private static Dictionary<string, PromptEditorAsset> MajorasMaskIconAssets() => new(StringComparer.Ordinal)
    {
        ["ButtonA"] = Ia8Asset(0x93c40, 24, 16, Color.FromArgb(0, 95, 255)),
        ["ButtonC"] = Ia8Asset(0x93f40, 48, 16, Color.FromArgb(255, 165, 0)),
        ["PanelLeft"] = Ia8Asset(0x94240, 72, 24, Color.FromArgb(150, 140, 90)),
        ["PanelRight"] = Ia8Asset(0x94900, 72, 24, Color.FromArgb(150, 140, 90)),
    };

    private static Dictionary<string, PromptEditorAsset> MajorasMaskGameCubeIconAssets() => new(StringComparer.Ordinal)
    {
        ["ButtonA"] = Ia8Asset(0x93c40, 24, 16, Color.FromArgb(0, 255, 100)),
        ["ButtonC"] = Ia8Asset(0x93f40, 48, 16, Color.FromArgb(255, 165, 0)),
        ["PanelLeft"] = Ia8Asset(0x94240, 72, 24, Color.FromArgb(150, 140, 90)),
        ["PanelRight"] = Ia8Asset(0x94900, 72, 24, Color.FromArgb(150, 140, 90)),
    };

    private static Dictionary<string, IReadOnlyDictionary<string, PromptEditorAsset>> OotTextAssets() => new(StringComparer.Ordinal)
    {
        ["eng"] = new Dictionary<string, PromptEditorAsset>(StringComparer.Ordinal)
        {
            ["Confirm"] = Ia8TextAsset(0x3f80, 64, 16),
            ["Melody"] = Ia8TextAsset(0x4380, 80, 16),
            ["Use"] = Ia8TextAsset(0x3c00, 56, 16),
        },
        ["ger"] = new Dictionary<string, PromptEditorAsset>(StringComparer.Ordinal)
        {
            ["Confirm"] = Ia8TextAsset(0x4180, 88, 16),
            ["Melody"] = Ia8TextAsset(0x4700, 104, 16),
            ["Use"] = Ia8TextAsset(0x3c00, 88, 16),
        },
        ["fra"] = new Dictionary<string, PromptEditorAsset>(StringComparer.Ordinal)
        {
            ["Confirm"] = Ia8TextAsset(0x4100, 72, 16),
            ["Melody"] = Ia8TextAsset(0x4580, 112, 16),
            ["Use"] = Ia8TextAsset(0x3c00, 80, 16),
        },
    };

    private static Dictionary<string, IReadOnlyDictionary<string, PromptEditorAsset>> MajorasMaskTextAssets(
        int equipOffset,
        int melodyOffset,
        int notebookOffset) => new(StringComparer.Ordinal)
        {
            ["eng"] = new Dictionary<string, PromptEditorAsset>(StringComparer.Ordinal)
            {
                ["Equip"] = Ia4TextAsset(equipOffset, 64, 16),
                ["Melody"] = Ia4TextAsset(melodyOffset, 96, 16),
                ["Notebook"] = Ia4TextAsset(notebookOffset, 96, 16),
            },
        };

    private static PromptEditorAsset Ia8Asset(int local, int width, int height, int colorDisplayListLocal) =>
        new(local, width, height, colorDisplayListLocal, Color.White, PromptEditorAssetFormat.Ia8, width);

    private static PromptEditorAsset Ia8Asset(int local, int width, int height, Color color) =>
        new(local, width, height, null, color, PromptEditorAssetFormat.Ia8, width);

    private static PromptEditorAsset Ia8TextAsset(int local, int width, int height) =>
        new(local, width, height, null, Color.White, PromptEditorAssetFormat.Ia8, width);

    private static PromptEditorAsset Ia4TextAsset(int local, int width, int height, int? drawWidth = null) =>
        new(local, width, height, null, Color.White, PromptEditorAssetFormat.Ia4, drawWidth ?? width);

    private static Dictionary<string, PromptEditorPatchSite> NtscSites(int baseOffset) => OotSites(baseOffset, new Dictionary<string, (int Local, uint Expected, int Rt)>
    {
        ["FileIcon"] = (0xeda0, 0x87220504, 2),
        ["MelodyIcon"] = (0xf268, 0x85e204fc, 2),
        ["GearIcon"] = (0xf408, 0x85c20508, 2),
        ["ItemIcon"] = (0xf08c, 0x85e204f4, 2),
        ["FileText"] = (0xedf0, 0x85cf04f8, 15),
        ["MelodyText"] = (0xf2bc, 0x85d804f8, 24),
        ["GearText"] = (0xf45c, 0x872f04f8, 15),
        ["ItemText"] = (0xf0e0, 0x870e0500, 14),
    });

    private static Dictionary<string, PromptEditorPatchSite> PalSites(int baseOffset) => OotSites(baseOffset, new Dictionary<string, (int Local, uint Expected, int Rt)>
    {
        ["FileIcon"] = (0xed38, 0x85e20e0e, 2),
        ["MelodyIcon"] = (0xf214, 0x87020e02, 2),
        ["GearIcon"] = (0xf3e8, 0x85e20e14, 2),
        ["ItemIcon"] = (0xf034, 0x85c20df6, 2),
        ["FileText"] = (0xed88, 0x87190dfc, 25),
        ["MelodyText"] = (0xf26c, 0x85cf0dfc, 15),
        ["GearText"] = (0xf43c, 0x85d90dfc, 25),
        ["ItemText"] = (0xf088, 0x872f0e08, 15),
    });

    private static Dictionary<string, PromptEditorPatchSite> GcUsSites(int baseOffset) => OotSites(baseOffset, new Dictionary<string, (int Local, uint Expected, int Rt)>
    {
        ["FileIcon"] = (0xec60, 0x87220504, 2),
        ["MelodyIcon"] = (0xf114, 0x85c204fc, 2),
        ["GearIcon"] = (0xf2ac, 0x85c20508, 2),
        ["ItemIcon"] = (0xef40, 0x872204f4, 2),
        ["FileText"] = (0xecb0, 0x85cf04f8, 15),
        ["MelodyText"] = (0xf168, 0x871904f8, 25),
        ["GearText"] = (0xf300, 0x873804f8, 24),
        ["ItemText"] = (0xef94, 0x85cf0500, 15),
    });

    private static Dictionary<string, PromptEditorPatchSite> GcEuSites(int baseOffset) => OotSites(baseOffset, new Dictionary<string, (int Local, uint Expected, int Rt)>
    {
        ["FileIcon"] = (0xebe0, 0x85e20e0e, 2),
        ["MelodyIcon"] = (0xf0b0, 0x87220e02, 2),
        ["GearIcon"] = (0xf27c, 0x85c20e14, 2),
        ["ItemIcon"] = (0xeed8, 0x85c20df6, 2),
        ["FileText"] = (0xec30, 0x85d80dfc, 24),
        ["MelodyText"] = (0xf108, 0x85f80dfc, 24),
        ["GearText"] = (0xf2d0, 0x87380dfc, 24),
        ["ItemText"] = (0xef2c, 0x85f90e08, 25),
    });

    private static Dictionary<string, PromptEditorPatchSite> MajorasMaskUsSites(int baseOffset) =>
        new Dictionary<string, PromptEditorPatchSite>(StringComparer.Ordinal)
        {
            ["EquipIcon"] = DirectSite(baseOffset + 0x2a48, 0x2418ffcf, 24),
            ["EquipText"] = RelativeToDirectSite(baseOffset + 0x2a8c, 0x2463002f, 3, 3, 0),
            ["NotebookIcon"] = DirectSite(baseOffset + 0x2bb0, 0x240fffc6, 15),
            ["NotebookText"] = RelativeToDirectSite(baseOffset + 0x2bf4, 0x24630014, 3, 3, 0),
            ["MelodyIcon"] = DirectSite(baseOffset + 0x2d08, 0x240fffc9, 15),
            ["MelodyText"] = RelativeToDirectSite(baseOffset + 0x2d4c, 0x24630014, 3, 3, 0),
        };

    private static Dictionary<string, PromptEditorPatchSite> OotSites(int baseOffset, IReadOnlyDictionary<string, (int Local, uint Expected, int Rt)> sites) =>
        sites.ToDictionary(
            pair => pair.Key,
            pair => DirectSite(baseOffset + pair.Value.Local, pair.Value.Expected, pair.Value.Rt),
            StringComparer.Ordinal);

    private static PromptEditorPatchSite DirectSite(int offset, uint expected, int rt) =>
        new(offset, expected, rt, 0, rt, 0);

    private static PromptEditorPatchSite RelativeToDirectSite(int offset, uint expected, int expectedRt, int expectedRs, int writeRs) =>
        new(offset, expected, expectedRt, expectedRs, expectedRt, writeRs);

    private static Dictionary<string, PromptEditorFixedPatch> NtscFixed(int baseOffset) => Fixed(baseOffset, new Dictionary<string, (int Local, uint Expected, uint Value)>
    {
        ["FileTextDirect"] = (0xedf8, 0x01f91021, 0x01e01025),
        ["MelodyTextDirect"] = (0xf2c4, 0x030f1021, 0x03001025),
        ["GearTextDirect"] = (0xf464, 0x01ee1021, 0x01e01025),
        ["ItemTextDirect"] = (0xf0e8, 0x01cf1021, 0x01c01025),
    });

    private static Dictionary<string, PromptEditorFixedPatch> PalFixed(int baseOffset) => Fixed(baseOffset, new Dictionary<string, (int Local, uint Expected, uint Value)>
    {
        ["FileTextDirect"] = (0xed90, 0x032f1021, 0x03201025),
        ["MelodyTextDirect"] = (0xf274, 0x01f81021, 0x01e01025),
        ["GearTextDirect"] = (0xf444, 0x032f1021, 0x03201025),
        ["ItemTextDirect"] = (0xf090, 0x01ee1021, 0x01e01025),
        ["GermanMelodyDirect"] = (0xf29c, 0x84620100, 0x01e01025),
        ["GermanMelodyNoAdjust"] = (0xf2a0, 0x2442ff9d, 0x00000000),
    });

    private static Dictionary<string, PromptEditorFixedPatch> GcUsFixed(int baseOffset) => Fixed(baseOffset, new Dictionary<string, (int Local, uint Expected, uint Value)>
    {
        ["FileTextDirect"] = (0xecb8, 0x01f91021, 0x01e01025),
        ["MelodyTextDirect"] = (0xf170, 0x032e1021, 0x03201025),
        ["GearTextDirect"] = (0xf308, 0x030e1021, 0x03001025),
        ["ItemTextDirect"] = (0xef9c, 0x01f91021, 0x01e01025),
    });

    private static Dictionary<string, PromptEditorFixedPatch> GcEuFixed(int baseOffset) => Fixed(baseOffset, new Dictionary<string, (int Local, uint Expected, uint Value)>
    {
        ["FileTextDirect"] = (0xec38, 0x030f1021, 0x03001025),
        ["MelodyTextDirect"] = (0xf110, 0x03191021, 0x03001025),
        ["GearTextDirect"] = (0xf2d8, 0x030e1021, 0x03001025),
        ["ItemTextDirect"] = (0xef34, 0x032e1021, 0x03201025),
    });

    private static Dictionary<string, PromptEditorFixedPatch> Fixed(int baseOffset, IReadOnlyDictionary<string, (int Local, uint Expected, uint Value)> fixedPatches) =>
        fixedPatches.ToDictionary(
            pair => pair.Key,
            pair => new PromptEditorFixedPatch(baseOffset + pair.Value.Local, pair.Value.Expected, pair.Value.Value),
            StringComparer.Ordinal);
}
