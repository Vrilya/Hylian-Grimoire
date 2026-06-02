namespace HylianGrimoire.Glyphs;

public static class OotGlyphSources
{
    public static IGlyphSource OriginalAssets { get; } = new OriginalAssetGlyphSource();

    public static IGlyphSource FromSnapshot(CharacterProfileSnapshot snapshot)
        => new SnapshotGlyphSource(snapshot);

    private sealed class OriginalAssetGlyphSource : IGlyphSource
    {
        public string CacheKey => "assets";

        public string GetGlyphPath(byte value)
        {
            return OotGlyphCatalog.GetOriginalGlyphPath(NormalizeGlyphValue(value));
        }

        public double GetAdvance(byte value)
        {
            return OotGlyphMetrics.GetDefaultAdvance(NormalizeGlyphValue(value));
        }
    }

    private sealed class SnapshotGlyphSource(CharacterProfileSnapshot snapshot) : IGlyphSource
    {
        public string CacheKey => $"profiles-snapshot-{snapshot.CacheKey}";

        public string GetGlyphPath(byte value)
        {
            value = NormalizeGlyphValue(value);
            if (snapshot.TryGetImagePath(value, out string? profilePath) && profilePath is not null)
            {
                return profilePath;
            }

            return OotGlyphCatalog.GetOriginalGlyphPath(value);
        }

        public double GetAdvance(byte value)
        {
            value = NormalizeGlyphValue(value);
            if (snapshot.TryGetWidth(value, out double profileWidth))
            {
                return profileWidth;
            }

            return OotGlyphMetrics.GetDefaultAdvance(value);
        }
    }

    private static byte NormalizeGlyphValue(byte value)
    {
        return value == 0x7f ? (byte)0x20 : value;
    }
}
