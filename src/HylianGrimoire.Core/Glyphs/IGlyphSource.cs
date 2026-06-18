namespace HylianGrimoire.Glyphs;

public interface IGlyphSource
{
    string CacheKey { get; }

    string GetGlyphPath(byte value);

    double GetAdvance(byte value);
}
