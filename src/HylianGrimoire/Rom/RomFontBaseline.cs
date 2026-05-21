namespace HylianGrimoire.Rom;

public enum RomFontBaseline
{
    Standard,
    PalGameCube,
    PalMultiLanguage,
}

public static class RomFontBaselineMetrics
{
    public static double GetDefaultAdvance(RomFontBaseline baseline, byte value)
    {
        if (value == 0x81)
        {
            return baseline is RomFontBaseline.PalGameCube or RomFontBaseline.PalMultiLanguage
                ? 6.0
                : 12.0;
        }

        return Glyphs.OotGlyphMetrics.GetDefaultAdvance(value);
    }
}
