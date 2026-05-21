namespace HylianGrimoire.TitleText;

public sealed record TitleTextPatchProfile(
    string DisplayName,
    TitleTextLineProfile NoController,
    TitleTextLineProfile PressStart);

public sealed record TitleTextLineProfile(
    TitleTextKind Kind,
    int FontBase,
    int MaxCharacters,
    int DefaultCharacters,
    int DefaultGapAfterIndex,
    int DefaultX,
    byte[] DefaultString,
    int StringOffset,
    int LoopCounter1Offset,
    int LoopCounter2Offset,
    int[] GapOffsets,
    int XOffset,
    int? PointerOffset = null,
    byte? DefaultPointer = null);
