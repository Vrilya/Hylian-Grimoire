using HylianGrimoire.Rom;

namespace HylianGrimoire.TitleText;

public static class TitleTextService
{
    public static bool TryGetProfile(RomVersionProfile romProfile, out TitleTextPatchProfile profile) =>
        TitleTextProfileCatalog.TryGetProfile(romProfile, out profile);

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

        (string cleanText, int[] gapAfterIndexes) = TitleTextCodec.ParseLocalizedInput(line.Text, localizedPressStart);
        var output = new List<TitleTextPreviewGlyph>(cleanText.Length);
        for (int i = 0; i < cleanText.Length; i++)
        {
            byte encoded = TitleTextCodec.EncodeLocalizedCharacter(cleanText[i]);
            int advance = TitleTextCodec.GetLocalizedBaseWidth(localizedPressStart, encoded);
            if (gapAfterIndexes.Contains(i))
            {
                advance += TitleTextCodec.GetLocalizedGapExtraWidth(localizedPressStart, encoded);
            }

            output.Add(new TitleTextPreviewGlyph(TitleTextCodec.GetLocalizedPreviewGlyph(encoded), advance));
        }

        return output;
    }

    public static string NormalizeText(string text, int maxCharacters)
    {
        _ = TitleTextCodec.ParseInput(text, maxCharacters, defaultGapAfterIndex: 0);
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
        string text = TitleTextCodec.DecodeText(encoded, profile.FontBase);
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
        (string text, string textClean, int gapAfterIndex) = TitleTextCodec.ParseInput(
            line.Text,
            profile.MaxCharacters,
            profile.DefaultGapAfterIndex);
        byte[] encoded = TitleTextCodec.EncodeText(textClean, profile.FontBase);
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

            char ch = TitleTextCodec.DecodeLocalizedCharacter(value);
            text.Append(ch);

            int baseWidth = TitleTextCodec.GetLocalizedBaseWidth(profile, value);
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
        (string cleanText, int[] gapAfterIndexes) = TitleTextCodec.ParseLocalizedInput(line.Text, profile);
        byte[] encoded = TitleTextCodec.EncodeLocalizedText(cleanText);

        Span<byte> stringStorage = rom.Slice(profile.StringOffset, profile.MaxCharacters);
        Span<byte> widthStorage = rom.Slice(profile.WidthOffset, profile.MaxCharacters);
        stringStorage.Clear();
        widthStorage.Clear();
        encoded.CopyTo(stringStorage);

        for (int i = 0; i < encoded.Length; i++)
        {
            int width = TitleTextCodec.GetLocalizedBaseWidth(profile, encoded[i]);
            if (gapAfterIndexes.Contains(i))
            {
                width += TitleTextCodec.GetLocalizedGapExtraWidth(profile, encoded[i]);
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

}
