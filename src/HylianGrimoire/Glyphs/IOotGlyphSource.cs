namespace HylianGrimoire.Glyphs;

public interface IOotGlyphSource
{
    string CacheKey { get; }

    string GetGlyphPath(byte value);

    double GetAdvance(byte value);
}
