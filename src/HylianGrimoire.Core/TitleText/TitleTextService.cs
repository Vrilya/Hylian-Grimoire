using HylianGrimoire.Games;
using HylianGrimoire.Rom;

namespace HylianGrimoire.TitleText;

public static class TitleTextService
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

    public static bool TryGetProfile(RomVersionProfile romProfile, out TitleTextPatchProfile profile) =>
        Profiles.TryGetValue(romProfile.Name, out profile!);

    public static (TitleTextLine? NoController, TitleTextLine PressStart) Read(
        ReadOnlySpan<byte> rom,
        TitleTextPatchProfile profile,
        int languageIndex = 0)
    {
        TitleTextLocalizedLineProfile? localizedPressStart = GetLocalizedPressStart(profile, languageIndex);
        return (
            profile.NoController is null ? null : ReadLine(rom, profile.NoController),
            localizedPressStart is null ? ReadLine(rom, profile.PressStart) : ReadLocalizedLine(rom, localizedPressStart));
    }

    public static void Write(
        Span<byte> rom,
        TitleTextPatchProfile profile,
        TitleTextLine? noController,
        TitleTextLine pressStart,
        int languageIndex = 0)
    {
        if (profile.NoController is not null && noController is not null)
        {
            WriteLine(rom, profile.NoController, noController);
        }

        TitleTextLocalizedLineProfile? localizedPressStart = GetLocalizedPressStart(profile, languageIndex);
        if (localizedPressStart is null)
        {
            WriteLine(rom, profile.PressStart, pressStart);
        }
        else
        {
            WriteLocalizedLine(rom, localizedPressStart, pressStart);
        }
    }

    public static void Reset(Span<byte> rom, TitleTextPatchProfile profile, TitleTextKind kind, int languageIndex = 0)
    {
        if (kind == TitleTextKind.PressStart &&
            GetLocalizedPressStart(profile, languageIndex) is TitleTextLocalizedLineProfile localizedPressStart)
        {
            WriteDefaultLocalizedLine(rom, localizedPressStart);
            return;
        }

        TitleTextLineProfile? lineProfile = kind == TitleTextKind.NoController
            ? profile.NoController
            : profile.PressStart;
        if (lineProfile is null)
        {
            return;
        }

        WriteDefaultLine(rom, lineProfile);
    }

    public static string GetDisplayName(TitleTextPatchProfile profile, int languageIndex)
    {
        TitleTextLocalizedLineProfile? localizedPressStart = GetLocalizedPressStart(profile, languageIndex);
        return localizedPressStart is null
            ? profile.DisplayName
            : $"{profile.DisplayName} - {localizedPressStart.LanguageName}";
    }

    public static int GetPressStartMaxCharacters(TitleTextPatchProfile profile, int languageIndex) =>
        GetLocalizedPressStart(profile, languageIndex)?.MaxVisibleCharacters ?? profile.PressStart.MaxCharacters;

    public static int GetPressStartMaxSpaces(TitleTextPatchProfile profile, int languageIndex) =>
        GetLocalizedPressStart(profile, languageIndex)?.DefaultGapAfterIndexes.Length ?? 1;

    public static bool AllowsLocalizedUDiaeresis(TitleTextPatchProfile profile, int languageIndex) =>
        GetLocalizedPressStart(profile, languageIndex)?.LanguageIndex == 1;

    public static IReadOnlyList<TitleTextPreviewGlyph>? GetLocalizedPreviewGlyphs(
        TitleTextPatchProfile profile,
        TitleTextLine line,
        int languageIndex)
    {
        TitleTextLocalizedLineProfile? localizedPressStart = GetLocalizedPressStart(profile, languageIndex);
        if (localizedPressStart is null)
        {
            return null;
        }

        (string cleanText, int[] gapAfterIndexes) = ParseLocalizedInput(line.Text, localizedPressStart);
        var output = new List<TitleTextPreviewGlyph>(cleanText.Length);
        for (int i = 0; i < cleanText.Length; i++)
        {
            byte encoded = EncodeLocalizedCharacter(cleanText[i]);
            int advance = GetLocalizedBaseWidth(localizedPressStart, encoded);
            if (gapAfterIndexes.Contains(i))
            {
                advance += GetLocalizedGapExtraWidth(localizedPressStart, encoded);
            }

            output.Add(new TitleTextPreviewGlyph(GetLocalizedPreviewGlyph(encoded), advance));
        }

        return output;
    }

    public static string NormalizeText(string text, int maxCharacters)
    {
        _ = ParseInput(text, maxCharacters, defaultGapAfterIndex: 0);
        return text.Trim().ToUpperInvariant();
    }

    private static TitleTextLine ReadLine(ReadOnlySpan<byte> rom, TitleTextLineProfile profile)
    {
        EnsureRange(rom, profile.StringOffset, profile.MaxCharacters);
        EnsureRange(rom, profile.LoopCounter1Offset, 1);
        foreach (TitleTextXPatch xPatch in profile.XOffsets)
        {
            EnsureRange(rom, xPatch.Offset, 1);
        }

        foreach (int gapOffset in profile.GapOffsets)
        {
            EnsureRange(rom, gapOffset, 1);
        }

        int count = rom[profile.LoopCounter1Offset];
        count = Math.Clamp(count, 0, profile.MaxCharacters);
        int start = GetNoControllerStorageStart(profile, count);
        ReadOnlySpan<byte> encoded = rom.Slice(profile.StringOffset + start, count);
        string text = DecodeText(encoded, profile.FontBase);
        int gapAfterIndex = rom[profile.GapOffsets[0]];
        if (gapAfterIndex >= 0 && gapAfterIndex < text.Length - 1)
        {
            text = text.Insert(gapAfterIndex + 1, " ");
        }

        TitleTextXPatch primaryX = profile.XOffsets[0];
        int x = rom[primaryX.Offset] - primaryX.Delta;
        return new TitleTextLine(profile.Kind, text, gapAfterIndex, x, profile.MaxCharacters);
    }

    private static void WriteLine(Span<byte> rom, TitleTextLineProfile profile, TitleTextLine line)
    {
        (string text, string textClean, int gapAfterIndex) = ParseInput(
            line.Text,
            profile.MaxCharacters,
            profile.DefaultGapAfterIndex);
        byte[] encoded = EncodeText(textClean, profile.FontBase);
        int count = encoded.Length;

        if (profile.PointerOffset is int pointerOffset && profile.DefaultPointer is byte defaultPointer)
        {
            EnsureRange(rom, pointerOffset, 1);
            int extra = Math.Max(0, count - profile.DefaultCharacters);
            rom[pointerOffset] = checked((byte)(defaultPointer - extra));
        }

        rom[profile.LoopCounter1Offset] = checked((byte)count);
        rom[profile.LoopCounter2Offset] = checked((byte)count);
        foreach (int gapOffset in profile.GapOffsets)
        {
            rom[gapOffset] = checked((byte)gapAfterIndex);
        }

        foreach (TitleTextXPatch xPatch in profile.XOffsets)
        {
            rom[xPatch.Offset] = checked((byte)(line.X + xPatch.Delta));
        }

        Span<byte> storage = rom.Slice(profile.StringOffset, profile.MaxCharacters);
        storage.Clear();
        int start = GetNoControllerStorageStart(profile, count);
        encoded.CopyTo(storage.Slice(start, encoded.Length));

        _ = text;
    }

    private static void WriteDefaultLine(Span<byte> rom, TitleTextLineProfile profile)
    {
        if (profile.PointerOffset is int pointerOffset && profile.DefaultPointer is byte defaultPointer)
        {
            rom[pointerOffset] = defaultPointer;
        }

        rom[profile.LoopCounter1Offset] = checked((byte)profile.DefaultCharacters);
        rom[profile.LoopCounter2Offset] = checked((byte)profile.DefaultCharacters);
        foreach (int gapOffset in profile.GapOffsets)
        {
            rom[gapOffset] = checked((byte)profile.DefaultGapAfterIndex);
        }

        foreach (TitleTextXPatch xPatch in profile.XOffsets)
        {
            rom[xPatch.Offset] = checked((byte)(profile.DefaultX + xPatch.Delta));
        }

        profile.DefaultString.CopyTo(rom.Slice(profile.StringOffset, profile.DefaultString.Length));
    }

    private static TitleTextLine ReadLocalizedLine(ReadOnlySpan<byte> rom, TitleTextLocalizedLineProfile profile)
    {
        EnsureRange(rom, profile.StringOffset, profile.MaxCharacters);
        EnsureRange(rom, profile.WidthOffset, profile.MaxCharacters);
        foreach (TitleTextXPatch xPatch in profile.XOffsets)
        {
            EnsureRange(rom, xPatch.Offset, 1);
        }

        ReadOnlySpan<byte> encoded = rom.Slice(profile.StringOffset, profile.MaxCharacters);
        ReadOnlySpan<byte> widths = rom.Slice(profile.WidthOffset, profile.MaxCharacters);
        var text = new System.Text.StringBuilder(profile.MaxVisibleCharacters + profile.DefaultGapAfterIndexes.Length);

        for (int i = 0; i < encoded.Length; i++)
        {
            byte value = encoded[i];
            if (value == 0x00 || value == profile.Terminator)
            {
                break;
            }

            char ch = DecodeLocalizedCharacter(value);
            text.Append(ch);

            int baseWidth = GetLocalizedBaseWidth(profile, value);
            if (widths[i] >= baseWidth + 3 && i < encoded.Length - 1)
            {
                text.Append(' ');
            }
        }

        TitleTextXPatch primaryX = profile.XOffsets[0];
        int x = rom[primaryX.Offset] - primaryX.Delta;
        return new TitleTextLine(TitleTextKind.PressStart, text.ToString(), GapAfterIndex: 0, x, profile.MaxVisibleCharacters);
    }

    private static void WriteLocalizedLine(Span<byte> rom, TitleTextLocalizedLineProfile profile, TitleTextLine line)
    {
        (string cleanText, int[] gapAfterIndexes) = ParseLocalizedInput(line.Text, profile);
        byte[] encoded = EncodeLocalizedText(cleanText);

        Span<byte> stringStorage = rom.Slice(profile.StringOffset, profile.MaxCharacters);
        Span<byte> widthStorage = rom.Slice(profile.WidthOffset, profile.MaxCharacters);
        stringStorage.Clear();
        widthStorage.Clear();
        encoded.CopyTo(stringStorage);

        for (int i = 0; i < encoded.Length; i++)
        {
            int width = GetLocalizedBaseWidth(profile, encoded[i]);
            if (gapAfterIndexes.Contains(i))
            {
                width += GetLocalizedGapExtraWidth(profile, encoded[i]);
            }

            widthStorage[i] = checked((byte)width);
        }

        if (encoded.Length < profile.MaxCharacters)
        {
            stringStorage[encoded.Length] = profile.Terminator;
            if (encoded.Length < profile.DefaultWidths.Length)
            {
                widthStorage[encoded.Length] = profile.DefaultWidths[encoded.Length];
            }
        }

        foreach (TitleTextXPatch xPatch in profile.XOffsets)
        {
            rom[xPatch.Offset] = checked((byte)(line.X + xPatch.Delta));
        }
    }

    private static void WriteDefaultLocalizedLine(Span<byte> rom, TitleTextLocalizedLineProfile profile)
    {
        profile.DefaultString.CopyTo(rom.Slice(profile.StringOffset, profile.DefaultString.Length));
        profile.DefaultWidths.CopyTo(rom.Slice(profile.WidthOffset, profile.DefaultWidths.Length));

        foreach (TitleTextXPatch xPatch in profile.XOffsets)
        {
            rom[xPatch.Offset] = checked((byte)(profile.DefaultX + xPatch.Delta));
        }
    }

    private static int GetNoControllerStorageStart(TitleTextLineProfile profile, int count)
    {
        if (profile.Kind != TitleTextKind.NoController)
        {
            return 0;
        }

        return Math.Max(0, profile.MaxCharacters - Math.Max(count, profile.DefaultCharacters));
    }

    private static (string Text, string TextClean, int GapAfterIndex) ParseInput(
        string text,
        int maxCharacters,
        int defaultGapAfterIndex)
    {
        text = text.Trim().ToUpperInvariant();
        int spaces = text.Count(char.IsWhiteSpace);
        if (spaces > 1)
        {
            throw new InvalidDataException("Use at most one space to control the title-text gap.");
        }

        int gapAfterIndex = defaultGapAfterIndex;
        string textClean = text;
        if (spaces == 1)
        {
            int gapIndex = text.IndexOf(' ');
            if (gapIndex <= 0)
            {
                throw new InvalidDataException("The gap must come after at least one character.");
            }

            gapAfterIndex = gapIndex - 1;
            textClean = text.Remove(gapIndex, 1);
        }

        if (textClean.Length == 0)
        {
            throw new InvalidDataException("Title text must contain at least one character.");
        }

        if (textClean.Length > maxCharacters)
        {
            throw new InvalidDataException($"Title text can contain at most {maxCharacters} visible characters.");
        }

        if (spaces == 0)
        {
            gapAfterIndex = textClean.Length - 1;
        }

        foreach (char ch in textClean)
        {
            if (ch is < 'A' or > 'Z')
            {
                throw new InvalidDataException("Title text supports A-Z characters only.");
            }
        }

        return (text, textClean, gapAfterIndex);
    }

    private static (string CleanText, int[] GapAfterIndexes) ParseLocalizedInput(
        string text,
        TitleTextLocalizedLineProfile profile)
    {
        text = text.Trim().ToUpperInvariant();
        var clean = new System.Text.StringBuilder(text.Length);
        var gapAfterIndexes = new List<int>(profile.DefaultGapAfterIndexes.Length);

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (char.IsWhiteSpace(ch))
            {
                if (clean.Length == 0 || gapAfterIndexes.Count >= profile.DefaultGapAfterIndexes.Length)
                {
                    throw new InvalidDataException(GetLocalizedGapError(profile));
                }

                int gapAfterIndex = clean.Length - 1;
                if (gapAfterIndexes.Count > 0 && gapAfterIndexes[^1] == gapAfterIndex)
                {
                    throw new InvalidDataException(GetLocalizedGapError(profile));
                }

                gapAfterIndexes.Add(gapAfterIndex);
                continue;
            }

            if (!CanEncodeLocalizedCharacter(ch, profile))
            {
                throw new InvalidDataException(profile.LanguageIndex == 1
                    ? "Title text supports A-Z and Ü for this language."
                    : "Title text supports A-Z characters only.");
            }

            clean.Append(ch);
            if (clean.Length > profile.MaxVisibleCharacters)
            {
                throw new InvalidDataException($"Title text can contain at most {profile.MaxVisibleCharacters} visible characters.");
            }
        }

        if (clean.Length == 0)
        {
            throw new InvalidDataException("Title text must contain at least one character.");
        }

        return (clean.ToString(), gapAfterIndexes.ToArray());
    }

    private static string GetLocalizedGapError(TitleTextLocalizedLineProfile profile)
    {
        int maxSpaces = profile.DefaultGapAfterIndexes.Length;
        return maxSpaces == 1
            ? "Use at most one space to control the title-text gap."
            : $"Use at most {maxSpaces} spaces to control the title-text gaps.";
    }

    private static string DecodeText(ReadOnlySpan<byte> encoded, int fontBase)
    {
        Span<char> chars = encoded.Length <= 128 ? stackalloc char[encoded.Length] : new char[encoded.Length];
        for (int i = 0; i < encoded.Length; i++)
        {
            int letter = encoded[i] - fontBase;
            chars[i] = letter is >= 0 and < 26 ? (char)('A' + letter) : '?';
        }

        return new string(chars);
    }

    private static byte[] EncodeText(string text, int fontBase)
    {
        byte[] output = new byte[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            output[i] = checked((byte)(text[i] - 'A' + fontBase));
        }

        return output;
    }

    private static char DecodeLocalizedCharacter(byte value)
    {
        if (value is >= 0x0A and <= 0x23)
        {
            return (char)('A' + value - 0x0A);
        }

        return value == 0x57 ? 'Ü' : '?';
    }

    private static byte[] EncodeLocalizedText(string text)
    {
        byte[] output = new byte[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            output[i] = EncodeLocalizedCharacter(text[i]);
        }

        return output;
    }

    private static byte EncodeLocalizedCharacter(char ch)
    {
        if (ch is >= 'A' and <= 'Z')
        {
            return checked((byte)(0x0A + ch - 'A'));
        }

        if (ch == 'Ü')
        {
            return 0x57;
        }

        throw new InvalidDataException("Title text contains a character that cannot be encoded.");
    }

    private static bool CanEncodeLocalizedCharacter(char ch, TitleTextLocalizedLineProfile profile) =>
        ch is >= 'A' and <= 'Z' || (profile.LanguageIndex == 1 && ch == 'Ü');

    private static byte GetLocalizedPreviewGlyph(byte encoded) =>
        encoded == 0x57
            ? (byte)0x95
            : checked((byte)('A' + encoded - 0x0A));

    private static int GetLocalizedBaseWidth(TitleTextLocalizedLineProfile profile, byte encoded)
    {
        int width = int.MaxValue;
        for (int i = 0; i < profile.DefaultString.Length && i < profile.DefaultWidths.Length; i++)
        {
            if (profile.DefaultString[i] != encoded)
            {
                continue;
            }

            int candidate = profile.DefaultWidths[i];
            if (profile.DefaultGapAfterIndexes.Contains(i))
            {
                candidate -= 5;
            }

            if (candidate > 0)
            {
                width = Math.Min(width, candidate);
            }
        }

        return width == int.MaxValue ? 7 : width;
    }

    private static int GetLocalizedGapExtraWidth(TitleTextLocalizedLineProfile profile, byte encoded)
    {
        int baseWidth = GetLocalizedBaseWidth(profile, encoded);
        int extra = 5;
        for (int i = 0; i < profile.DefaultString.Length && i < profile.DefaultWidths.Length; i++)
        {
            if (profile.DefaultString[i] == encoded && profile.DefaultGapAfterIndexes.Contains(i))
            {
                extra = Math.Max(extra, profile.DefaultWidths[i] - baseWidth);
            }
        }

        return extra;
    }

    private static void EnsureRange(ReadOnlySpan<byte> rom, int offset, int length)
    {
        if (offset < 0 || offset + length > rom.Length)
        {
            throw new InvalidDataException("The loaded ROM is too small for title-text editing.");
        }
    }

    private static TitleTextLocalizedLineProfile? GetLocalizedPressStart(TitleTextPatchProfile profile, int languageIndex)
    {
        if (profile.LocalizedPressStarts.Count == 0)
        {
            return null;
        }

        int index = Math.Clamp(languageIndex, 0, profile.LocalizedPressStarts.Count - 1);
        return profile.LocalizedPressStarts[index];
    }

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
