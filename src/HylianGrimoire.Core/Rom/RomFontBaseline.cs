namespace HylianGrimoire.Rom;

public enum RomFontBaseline
{
    Standard,
    PalGameCube,
    PalMultiLanguage,
    MajorasMask,
    MajorasMaskUsGameCube,
    MajorasMaskEu,
}

public static class RomFontBaselineMetrics
{
    public static double GetDefaultAdvance(RomFontBaseline baseline, byte value)
    {
        if (baseline is RomFontBaseline.MajorasMask or RomFontBaseline.MajorasMaskUsGameCube or RomFontBaseline.MajorasMaskEu)
        {
            return Glyphs.MmGlyphMetrics.GetDefaultAdvance(value);
        }

        if (value == 0x81)
        {
            return baseline is RomFontBaseline.PalGameCube or RomFontBaseline.PalMultiLanguage
                ? 6.0
                : 12.0;
        }

        return Glyphs.OotGlyphMetrics.GetDefaultAdvance(value);
    }
}
