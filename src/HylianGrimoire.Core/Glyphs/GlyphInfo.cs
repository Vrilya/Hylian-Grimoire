namespace HylianGrimoire.Glyphs;

public sealed record GlyphInfo(
    byte Value,
    string Hex,
    char DefaultChar,
    char CurrentChar,
    double DefaultWidth,
    double CurrentWidth,
    string OriginalPath,
    string CurrentPath,
    bool HasDisplayOverride,
    bool HasWidthOverride,
    bool HasImageOverride);
