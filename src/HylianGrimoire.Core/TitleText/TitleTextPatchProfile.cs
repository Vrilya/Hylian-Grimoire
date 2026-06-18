namespace HylianGrimoire.TitleText;

public sealed record TitleTextPatchProfile(
    string DisplayName,
    string BackgroundPath,
    TitleTextLineProfile? NoController,
    TitleTextLineProfile PressStart)
{
    public IReadOnlyList<TitleTextLocalizedLineProfile> LocalizedPressStarts { get; init; }
        = Array.Empty<TitleTextLocalizedLineProfile>();
}

public sealed record TitleTextLineProfile(
    TitleTextKind Kind,
    int FontBase,
    int MaxCharacters,
    int DefaultCharacters,
    int DefaultGapAfterIndex,
    int DefaultX,
    int PreviewY,
    int PreviewAdvance,
    int PreviewGapWidth,
    int PreviewColorArgb,
    byte[] DefaultString,
    int StringOffset,
    int LoopCounter1Offset,
    int LoopCounter2Offset,
    int[] GapOffsets,
    TitleTextXPatch[] XOffsets,
    int? PointerOffset = null,
    byte? DefaultPointer = null);

public sealed record TitleTextXPatch(int Offset, int Delta = 0);

public sealed record TitleTextLocalizedLineProfile(
    int LanguageIndex,
    string LanguageName,
    int MaxCharacters,
    int MaxVisibleCharacters,
    byte Terminator,
    int DefaultX,
    int PreviewY,
    int PreviewColorArgb,
    byte[] DefaultString,
    byte[] DefaultWidths,
    int StringOffset,
    int WidthOffset,
    int[] DefaultGapAfterIndexes,
    TitleTextXPatch[] XOffsets);

public readonly record struct TitleTextPreviewGlyph(byte GlyphValue, int Advance);
