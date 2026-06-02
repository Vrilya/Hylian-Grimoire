namespace HylianGrimoire.Rom;

public sealed record RomFontResources(
    int GlyphDataOffset,
    int GlyphCount,
    int WidthTableOffset,
    int WidthCount)
{
    public static RomFontResources Empty { get; } = new(0, 0, 0, 0);

    public const int FirstGlyphValue = 0x20;
    public const int GlyphByteSize = 128;
    public const int StandardGlyphCount = 139;
    public const int StandardWidthCount = 144;
}
