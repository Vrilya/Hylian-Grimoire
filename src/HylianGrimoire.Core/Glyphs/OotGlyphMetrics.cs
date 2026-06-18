namespace HylianGrimoire.Glyphs;

public static class OotGlyphMetrics
{
    public static readonly double[] DefaultWidths =
    [
        8, 8, 6, 9, 9, 14, 12, 3, 7, 7, 7, 9, 4, 6, 4, 9,
        10, 5, 9, 9, 10, 9, 9, 9, 9, 9, 6, 6, 9, 11, 9, 11,
        13, 12, 9, 11, 11, 8, 8, 12, 10, 4, 8, 10, 8, 13, 11, 13,
        9, 13, 10, 10, 9, 10, 11, 15, 11, 10, 10, 7, 10, 7, 10, 9,
        5, 8, 9, 8, 9, 9, 6, 9, 8, 4, 6, 8, 4, 12, 9, 9,
        9, 9, 7, 8, 7, 8, 9, 12, 8, 9, 8, 7, 5, 7, 10, 10,
        12, 6, 12, 12, 11, 8, 8, 8, 6, 6, 13, 13, 10, 10, 10, 9,
        8, 8, 8, 8, 8, 9, 9, 9, 9, 6, 9, 9, 9, 9, 9, 14,
        14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14
    ];

    public static double GetDefaultAdvance(byte glyph)
    {
        int index = glyph - 0x20;
        return index >= 0 && index < DefaultWidths.Length ? DefaultWidths[index] : 10;
    }
}
