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

    private static readonly IReadOnlyDictionary<string, TitleTextPatchProfile> Profiles =
        new Dictionary<string, TitleTextPatchProfile>(StringComparer.Ordinal)
        {
            ["Retail NTSC 1.0"] = Profile(
                "Retail NTSC 1.0",
                NoController(0xAB, NtscNoController, 0xE6E97B, 0xE6E9DB, 0xE6EA8B, [0xE6E997], 0xE6EE66, 0xE6C34F, 0x88),
                PressStart(0xAB, NtscPressStart, 0xE6EBCB, 0xE6EC9F, [0xE6EB87], 0xE6EE74, 0xE6C2FF)),
            ["Retail NTSC 1.1"] = Profile(
                "Retail NTSC 1.1",
                NoController(0xAB, NtscNoController, 0xE6ECBB, 0xE6ED1B, 0xE6EDCB, [0xE6ECD7], 0xE6F1A6, 0xE6C68F, 0x28),
                PressStart(0xAB, NtscPressStart, 0xE6EF0B, 0xE6EFDF, [0xE6EEC7], 0xE6F1B4, 0xE6C63F)),
            ["Retail NTSC 1.2"] = Profile(
                "Retail NTSC 1.2",
                NoController(0xAB, NtscNoController, 0xE6EEBB, 0xE6EF1B, 0xE6EFCB, [0xE6EED7], 0xE6F3A6, 0xE6C88F, 0x98),
                PressStart(0xAB, NtscPressStart, 0xE6F10B, 0xE6F1DF, [0xE6F0C7], 0xE6F3B4, 0xE6C83F)),
            ["Retail PAL 1.0"] = Profile(
                "Retail PAL 1.0",
                NoController(0x0A, PalNoController, 0xE6EC13, 0xE6EC73, 0xE6ED23, [0xE6EC2F], 0xE6F106, 0xE6C99F, 0xF8),
                PressStart(0x0A, PalPressStart, 0xE6EE63, 0xE6EF37, [0xE6EE1F], 0xE6F114, 0xE6C94F)),
            ["Retail PAL 1.1"] = Profile(
                "Retail PAL 1.1",
                NoController(0x0A, PalNoController, 0xE6EDE3, 0xE6EE43, 0xE6EEF3, [0xE6EDFF], 0xE6F2D6, 0xE6CB6F, 0x88),
                PressStart(0x0A, PalPressStart, 0xE6F033, 0xE6F107, [0xE6EFEF], 0xE6F2E4, 0xE6CB1F)),
            ["Retail NTSC GameCube"] = Profile(
                "Retail NTSC GameCube",
                NoController(0xAB, NtscNoController, 0xDFC397, 0xDFC407, 0xDFC4BB, [0xDFC3E3, 0xDFC497], 0xDFC85E, 0xDFA137, 0xC0),
                PressStart(0xAB, NtscPressStart, 0xDFC5FB, 0xDFC6D3, [0xDFC5D7, 0xDFC6AF], 0xDFC86C, 0xDFA0E7)),
            ["Retail NTSC Master Quest"] = Profile(
                "Retail NTSC Master Quest",
                NoController(0xAB, NtscNoController, 0xDFC3FF, 0xDFC45F, 0xDFC517, [0xDFC413], 0xDFC8CE, 0xDFA097, 0x50),
                PressStart(0xAB, NtscPressStart, 0xDFC657, 0xDFC733, [0xDFC59B], 0xDFC8DC, 0xDFA047)),
            ["Retail PAL GameCube"] = Profile(
                "Retail PAL GameCube",
                NoController(0x0A, PalNoController, 0xDFA5EB, 0xDFA65B, 0xDFA70F, [0xDFA637, 0xDFA6EB], 0xDFAAAE, 0xDF8817, 0x20),
                PressStart(0x0A, PalPressStart, 0xDFA84B, 0xDFA923, [0xDFA827, 0xDFA8FF], 0xDFAABC, 0xDF87C7)),
            ["Retail PAL Master Quest"] = Profile(
                "Retail PAL Master Quest",
                NoController(0x0A, PalNoController, 0xDFA5CB, 0xDFA63B, 0xDFA6EF, [0xDFA617, 0xDFA6CB], 0xDFAA8E, 0xDF8777, 0x20),
                PressStart(0x0A, PalPressStart, 0xDFA82B, 0xDFA903, [0xDFA807, 0xDFA8DF], 0xDFAA9C, 0xDF8727)),
        };

    public static bool TryGetProfile(RomVersionProfile romProfile, out TitleTextPatchProfile profile) =>
        Profiles.TryGetValue(romProfile.Name, out profile!);

    public static (TitleTextLine NoController, TitleTextLine PressStart) Read(ReadOnlySpan<byte> rom, TitleTextPatchProfile profile)
    {
        return (
            ReadLine(rom, profile.NoController),
            ReadLine(rom, profile.PressStart));
    }

    public static void Write(Span<byte> rom, TitleTextPatchProfile profile, TitleTextLine noController, TitleTextLine pressStart)
    {
        WriteLine(rom, profile.NoController, noController);
        WriteLine(rom, profile.PressStart, pressStart);
    }

    public static void Reset(Span<byte> rom, TitleTextPatchProfile profile, TitleTextKind kind)
    {
        TitleTextLineProfile lineProfile = kind == TitleTextKind.NoController
            ? profile.NoController
            : profile.PressStart;
        WriteDefaultLine(rom, lineProfile);
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
        EnsureRange(rom, profile.XOffset, 1);
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

        return new TitleTextLine(profile.Kind, text, gapAfterIndex, rom[profile.XOffset], profile.MaxCharacters);
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

        rom[profile.XOffset] = checked((byte)line.X);

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

        rom[profile.XOffset] = checked((byte)profile.DefaultX);
        profile.DefaultString.CopyTo(rom.Slice(profile.StringOffset, profile.DefaultString.Length));
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

    private static void EnsureRange(ReadOnlySpan<byte> rom, int offset, int length)
    {
        if (offset < 0 || offset + length > rom.Length)
        {
            throw new InvalidDataException("The loaded ROM is too small for title-text editing.");
        }
    }

    private static TitleTextPatchProfile Profile(
        string displayName,
        TitleTextLineProfile noController,
        TitleTextLineProfile pressStart) =>
        new(displayName, noController, pressStart);

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
            defaultString,
            stringOffset,
            loopCounter1Offset,
            loopCounter2Offset,
            gapOffsets,
            xOffset,
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
        new(
            TitleTextKind.PressStart,
            fontBase,
            MaxCharacters: 12,
            DefaultCharacters: 10,
            DefaultGapAfterIndex: 4,
            DefaultX: 119,
            defaultString,
            stringOffset,
            loopCounter1Offset,
            loopCounter2Offset,
            gapOffsets,
            xOffset);
}
