using HylianGrimoire.Games;
using HylianGrimoire.Rom;

namespace HylianGrimoire.TitleText;

internal static class TitleTextProfileCatalog
{
    private static readonly byte[] NtscNoController =
    [
        0x00, 0x00, 0xB8, 0xB9, 0xAD, 0xB9, 0xB8, 0xBE,
        0xBC, 0xB9, 0xB6, 0xB6, 0xAF, 0xBC,
    ];

    private static readonly byte[] PalNoController =
    [
        0x00, 0x00, 0x17, 0x18, 0x0C, 0x18, 0x17, 0x1D,
        0x1B, 0x18, 0x15, 0x15, 0x0E, 0x1B,
    ];

    private static readonly byte[] NtscPressStart =
    [
        0xBA, 0xBC, 0xAF, 0xBD, 0xBD, 0xBD, 0xBE, 0xAB,
        0xBC, 0xBE, 0x00, 0x00,
    ];

    private static readonly byte[] PalPressStart =
    [
        0x19, 0x1B, 0x0E, 0x1C, 0x1C, 0x1C, 0x1D, 0x0A,
        0x1B, 0x1D, 0x00, 0x00,
    ];

    private static readonly byte[] MmEu10PressStartEnglish =
    [
        0x19, 0x1B, 0x0E, 0x1C, 0x1C, 0x1C, 0x1D, 0x0A,
        0x1B, 0x1D, 0x3E, 0x00, 0x00, 0x00, 0x00,
    ];

    private static readonly byte[] MmEu10PressStartGerman =
    [
        0x0D, 0x1B, 0x57, 0x0C, 0x14, 0x0E, 0x1C, 0x1D,
        0x0A, 0x1B, 0x1D, 0x3E, 0x00, 0x00, 0x00,
    ];

    private static readonly byte[] MmEu10PressStartFrench =
    [
        0x0A, 0x19, 0x19, 0x1E, 0x22, 0x0E, 0x23, 0x1C,
        0x1E, 0x1B, 0x1C, 0x1D, 0x0A, 0x1B, 0x1D,
    ];

    private static readonly byte[] MmEu10PressStartSpanish =
    [
        0x19, 0x1E, 0x15, 0x1C, 0x0A, 0x1C, 0x1D, 0x0A,
        0x1B, 0x1D, 0x3E, 0x00, 0x00, 0x00, 0x00,
    ];

    private static readonly byte[] MmEu10PressStartEnglishWidths =
    [
        0x06, 0x07, 0x05, 0x07, 0x0C, 0x07, 0x06, 0x08,
        0x07, 0x06, 0x05, 0x00, 0x00, 0x00, 0x00,
    ];

    private static readonly byte[] MmEu10PressStartGermanWidths =
    [
        0x07, 0x07, 0x07, 0x07, 0x07, 0x0A, 0x07, 0x06,
        0x08, 0x07, 0x06, 0x05, 0x00, 0x00, 0x00,
    ];

    private static readonly byte[] MmEu10PressStartFrenchWidths =
    [
        0x08, 0x06, 0x06, 0x07, 0x07, 0x06, 0x0C, 0x07,
        0x07, 0x0C, 0x07, 0x06, 0x08, 0x07, 0x06,
    ];

    private static readonly byte[] MmEu10PressStartSpanishWidths =
    [
        0x06, 0x07, 0x05, 0x07, 0x0D, 0x07, 0x06, 0x08,
        0x07, 0x06, 0x05, 0x00, 0x00, 0x00, 0x00,
    ];

    private static readonly IReadOnlyDictionary<string, TitleTextPatchProfile> Profiles =
        new Dictionary<string, TitleTextPatchProfile>(StringComparer.Ordinal)
        {
            ["Retail NTSC 1.0"] = Profile(
                GameKind.OcarinaOfTime,
                "Retail NTSC 1.0",
                NoController(0xAB, NtscNoController, 0xE6E97B, 0xE6E9DB, 0xE6EA8B, [0xE6E997], 0xE6EE66, 0xE6C34F, 0x88),
                PressStart(0xAB, NtscPressStart, 0xE6EBCB, 0xE6EC9F, [0xE6EB87], 0xE6EE74, 0xE6C2FF)),
            ["Retail NTSC 1.1"] = Profile(
                GameKind.OcarinaOfTime,
                "Retail NTSC 1.1",
                NoController(0xAB, NtscNoController, 0xE6ECBB, 0xE6ED1B, 0xE6EDCB, [0xE6ECD7], 0xE6F1A6, 0xE6C68F, 0x28),
                PressStart(0xAB, NtscPressStart, 0xE6EF0B, 0xE6EFDF, [0xE6EEC7], 0xE6F1B4, 0xE6C63F)),
            ["Retail NTSC 1.2"] = Profile(
                GameKind.OcarinaOfTime,
                "Retail NTSC 1.2",
                NoController(0xAB, NtscNoController, 0xE6EEBB, 0xE6EF1B, 0xE6EFCB, [0xE6EED7], 0xE6F3A6, 0xE6C88F, 0x98),
                PressStart(0xAB, NtscPressStart, 0xE6F10B, 0xE6F1DF, [0xE6F0C7], 0xE6F3B4, 0xE6C83F)),
            ["Retail PAL 1.0"] = Profile(
                GameKind.OcarinaOfTime,
                "Retail PAL 1.0",
                NoController(0x0A, PalNoController, 0xE6EC13, 0xE6EC73, 0xE6ED23, [0xE6EC2F], 0xE6F106, 0xE6C99F, 0xF8),
                PressStart(0x0A, PalPressStart, 0xE6EE63, 0xE6EF37, [0xE6EE1F], 0xE6F114, 0xE6C94F)),
            ["Retail PAL 1.1"] = Profile(
                GameKind.OcarinaOfTime,
                "Retail PAL 1.1",
                NoController(0x0A, PalNoController, 0xE6EDE3, 0xE6EE43, 0xE6EEF3, [0xE6EDFF], 0xE6F2D6, 0xE6CB6F, 0x88),
                PressStart(0x0A, PalPressStart, 0xE6F033, 0xE6F107, [0xE6EFEF], 0xE6F2E4, 0xE6CB1F)),
            ["Retail NTSC GameCube"] = Profile(
                GameKind.OcarinaOfTime,
                "Retail NTSC GameCube",
                NoController(0xAB, NtscNoController, 0xDFC397, 0xDFC407, 0xDFC4BB, [0xDFC3E3, 0xDFC497], 0xDFC85E, 0xDFA137, 0xC0),
                PressStart(0xAB, NtscPressStart, 0xDFC5FB, 0xDFC6D3, [0xDFC5D7, 0xDFC6AF], 0xDFC86C, 0xDFA0E7)),
            ["Retail NTSC Master Quest"] = Profile(
                GameKind.OcarinaOfTime,
                "Retail NTSC Master Quest",
                NoController(0xAB, NtscNoController, 0xDFC3FF, 0xDFC45F, 0xDFC517, [0xDFC413], 0xDFC8CE, 0xDFA097, 0x50),
                PressStart(0xAB, NtscPressStart, 0xDFC657, 0xDFC733, [0xDFC59B], 0xDFC8DC, 0xDFA047)),
            ["Retail PAL GameCube"] = Profile(
                GameKind.OcarinaOfTime,
                "Retail PAL GameCube",
                NoController(0x0A, PalNoController, 0xDFA5EB, 0xDFA65B, 0xDFA70F, [0xDFA637, 0xDFA6EB], 0xDFAAAE, 0xDF8817, 0x20),
                PressStart(0x0A, PalPressStart, 0xDFA84B, 0xDFA923, [0xDFA827, 0xDFA8FF], 0xDFAABC, 0xDF87C7)),
            ["Retail PAL Master Quest"] = Profile(
                GameKind.OcarinaOfTime,
                "Retail PAL Master Quest",
                NoController(0x0A, PalNoController, 0xDFA5CB, 0xDFA63B, 0xDFA6EF, [0xDFA617, 0xDFA6CB], 0xDFAA8E, 0xDF8777, 0x20),
                PressStart(0x0A, PalPressStart, 0xDFA82B, 0xDFA903, [0xDFA807, 0xDFA8DF], 0xDFAA9C, 0xDF8727)),
            ["Majora's Mask NTSC-U"] = Profile(
                GameKind.MajorasMask,
                "Majora's Mask NTSC-U",
                noController: null,
                PressStart(
                    0x0A,
                    PalPressStart,
                    0xDE14F7,
                    0xDE1583,
                    [0xDE14B7],
                    0xDE1724,
                    [new TitleTextXPatch(0xDE153F), new TitleTextXPatch(0xDE14A7, 1)],
                    previewY: 174)),
            ["Majora's Mask NTSC-U GameCube"] = Profile(
                GameKind.MajorasMask,
                "Majora's Mask NTSC-U GameCube",
                noController: null,
                PressStart(
                    0x0A,
                    PalPressStart,
                    0xDD6D03,
                    0xDD6D8F,
                    [0xDD6CC3],
                    0xDD6F34,
                    [new TitleTextXPatch(0xDD6D4B), new TitleTextXPatch(0xDD6CB3, 1)],
                    previewY: 174)),
            ["Majora's Mask EU 1.0"] = MajorasMaskEuProfile("Majora's Mask EU 1.0", dataOffsetDelta: 0, xOffsetDelta: 0),
            ["Majora's Mask EU 1.1"] = MajorasMaskEuProfile("Majora's Mask EU 1.1", dataOffsetDelta: 0x190, xOffsetDelta: 0x190),
            ["Majora's Mask EU GameCube"] = MajorasMaskEuProfile("Majora's Mask EU GameCube", dataOffsetDelta: 0xC950, xOffsetDelta: 0xC948),
        };

    internal static bool TryGetProfile(RomVersionProfile romProfile, out TitleTextPatchProfile profile) =>
        Profiles.TryGetValue(romProfile.Name, out profile!);

    private static TitleTextPatchProfile MajorasMaskEuProfile(string displayName, int dataOffsetDelta, int xOffsetDelta) =>
        Profile(
            GameKind.MajorasMask,
            displayName,
            noController: null,
            PressStart(
                0x0A,
                PalPressStart,
                0xF12664 + dataOffsetDelta,
                0xF12664 + dataOffsetDelta,
                [0xF126A0 + dataOffsetDelta],
                0xF12664 + dataOffsetDelta,
                [new TitleTextXPatch(0xF12433 + xOffsetDelta), new TitleTextXPatch(0xF12317 + xOffsetDelta, 1)],
                previewY: 174))
        with
        {
            LocalizedPressStarts =
            [
                LocalizedPressStart(
                    0,
                    "English",
                    MmEu10PressStartEnglish,
                    MmEu10PressStartEnglishWidths,
                    [4],
                    0xF12664 + dataOffsetDelta,
                    0xF126A0 + dataOffsetDelta,
                    119,
                    [new TitleTextXPatch(0xF12433 + xOffsetDelta), new TitleTextXPatch(0xF12317 + xOffsetDelta, 1)]),
                LocalizedPressStart(
                    1,
                    "German",
                    MmEu10PressStartGerman,
                    MmEu10PressStartGermanWidths,
                    [5],
                    0xF12673 + dataOffsetDelta,
                    0xF126AF + dataOffsetDelta,
                    116,
                    [new TitleTextXPatch(0xF1243B + xOffsetDelta), new TitleTextXPatch(0xF1231F + xOffsetDelta, 1)]),
                LocalizedPressStart(
                    2,
                    "French",
                    MmEu10PressStartFrench,
                    MmEu10PressStartFrenchWidths,
                    [6, 9],
                    0xF12682 + dataOffsetDelta,
                    0xF126BE + dataOffsetDelta,
                    110,
                    [new TitleTextXPatch(0xF12443 + xOffsetDelta), new TitleTextXPatch(0xF12327 + xOffsetDelta, 1)]),
                LocalizedPressStart(
                    3,
                    "Spanish",
                    MmEu10PressStartSpanish,
                    MmEu10PressStartSpanishWidths,
                    [4],
                    0xF12691 + dataOffsetDelta,
                    0xF126CD + dataOffsetDelta,
                    119,
                    [new TitleTextXPatch(0xF1242B + xOffsetDelta), new TitleTextXPatch(0xF1230F + xOffsetDelta, 1)]),
            ],
        };

    private static TitleTextPatchProfile Profile(
        GameKind game,
        string displayName,
        TitleTextLineProfile? noController,
        TitleTextLineProfile pressStart) =>
        new(displayName, GetBackgroundPath(game), noController, pressStart);

    private static string GetBackgroundPath(GameKind game) =>
        Path.Combine(AppContext.BaseDirectory, GameProfiles.Get(game).Assets.TitleRoot, "Titel.png");

    private static TitleTextLineProfile NoController(
        int fontBase,
        byte[] defaultString,
        int pointerOffset,
        int loopCounter1Offset,
        int loopCounter2Offset,
        int[] gapOffsets,
        int stringOffset,
        int xOffset,
        byte defaultPointer) =>
        new(
            TitleTextKind.NoController,
            fontBase,
            MaxCharacters: 14,
            DefaultCharacters: 12,
            DefaultGapAfterIndex: 1,
            DefaultX: 99,
            PreviewY: 174,
            PreviewAdvance: 9,
            PreviewGapWidth: 10,
            PreviewColorArgb: unchecked((int)0xff64ffff),
            defaultString,
            stringOffset,
            loopCounter1Offset,
            loopCounter2Offset,
            gapOffsets,
            [new TitleTextXPatch(xOffset)],
            pointerOffset,
            defaultPointer);

    private static TitleTextLineProfile PressStart(
        int fontBase,
        byte[] defaultString,
        int loopCounter1Offset,
        int loopCounter2Offset,
        int[] gapOffsets,
        int stringOffset,
        int xOffset) =>
        PressStart(fontBase, defaultString, loopCounter1Offset, loopCounter2Offset, gapOffsets, stringOffset, [new TitleTextXPatch(xOffset)]);

    private static TitleTextLineProfile PressStart(
        int fontBase,
        byte[] defaultString,
        int loopCounter1Offset,
        int loopCounter2Offset,
        int[] gapOffsets,
        int stringOffset,
        TitleTextXPatch[] xOffsets) =>
        PressStart(fontBase, defaultString, loopCounter1Offset, loopCounter2Offset, gapOffsets, stringOffset, xOffsets, previewY: 192);

    private static TitleTextLineProfile PressStart(
        int fontBase,
        byte[] defaultString,
        int loopCounter1Offset,
        int loopCounter2Offset,
        int[] gapOffsets,
        int stringOffset,
        TitleTextXPatch[] xOffsets,
        int previewY) =>
        new(
            TitleTextKind.PressStart,
            fontBase,
            MaxCharacters: 12,
            DefaultCharacters: 10,
            DefaultGapAfterIndex: 4,
            DefaultX: 119,
            PreviewY: previewY,
            PreviewAdvance: 7,
            PreviewGapWidth: 5,
            PreviewColorArgb: unchecked((int)0xffff1e1e),
            defaultString,
            stringOffset,
            loopCounter1Offset,
            loopCounter2Offset,
            gapOffsets,
            xOffsets);

    private static TitleTextLocalizedLineProfile LocalizedPressStart(
        int languageIndex,
        string languageName,
        byte[] defaultString,
        byte[] defaultWidths,
        int[] defaultGapAfterIndexes,
        int stringOffset,
        int widthOffset,
        int defaultX,
        TitleTextXPatch[] xOffsets) =>
        new(
            languageIndex,
            languageName,
            MaxCharacters: 15,
            MaxVisibleCharacters: GetDefaultVisibleCharacters(defaultString, terminator: 0x3E),
            Terminator: 0x3E,
            defaultX,
            PreviewY: 174,
            PreviewColorArgb: unchecked((int)0xffff1e1e),
            defaultString,
            defaultWidths,
            stringOffset,
            widthOffset,
            defaultGapAfterIndexes,
            xOffsets);

    private static int GetDefaultVisibleCharacters(byte[] defaultString, byte terminator)
    {
        int terminatorIndex = Array.IndexOf(defaultString, terminator);
        if (terminatorIndex >= 0)
        {
            return terminatorIndex;
        }

        int zeroIndex = Array.IndexOf(defaultString, (byte)0x00);
        return zeroIndex >= 0 ? zeroIndex : defaultString.Length;
    }
}
