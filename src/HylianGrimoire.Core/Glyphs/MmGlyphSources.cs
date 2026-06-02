namespace HylianGrimoire.Glyphs;

public static class MmGlyphSources
{
    public static IGlyphSource Assets { get; } = new AssetGlyphSource();

    public static IGlyphSource FromSnapshot(CharacterProfileSnapshot snapshot)
        => new SnapshotGlyphSource(snapshot);

    private sealed class AssetGlyphSource : IGlyphSource
    {
        public string CacheKey => "mm-assets";

        public string GetGlyphPath(byte value) => MmGlyphCatalog.GetOriginalGlyphPath(value);

        public double GetAdvance(byte value) => MmGlyphMetrics.GetDefaultAdvance(value);
    }

    private sealed class SnapshotGlyphSource(CharacterProfileSnapshot snapshot) : IGlyphSource
    {
        public string CacheKey => $"mm-profiles-snapshot-{snapshot.CacheKey}";

        public string GetGlyphPath(byte value)
        {
            if (snapshot.TryGetImagePath(value, out string? profilePath) && profilePath is not null)
            {
                return profilePath;
            }

            return MmGlyphCatalog.GetOriginalGlyphPath(value);
        }

        public double GetAdvance(byte value)
        {
            if (snapshot.TryGetWidth(value, out double profileWidth))
            {
                return profileWidth;
            }

            return MmGlyphMetrics.GetDefaultAdvance(value);
        }
    }
}
