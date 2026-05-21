namespace HylianGrimoire.Glyphs;

public static class OotGlyphSources
{
    public static IOotGlyphSource OriginalAssets { get; } = new OriginalAssetGlyphSource();

    public static IOotGlyphSource ActiveProfile { get; } = new ActiveProfileGlyphSource();

    private sealed class OriginalAssetGlyphSource : IOotGlyphSource
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

    private sealed class ActiveProfileGlyphSource : IOotGlyphSource
    {
        public string CacheKey => $"profiles-{CharacterProfileStore.Current.Version}";

        public string GetGlyphPath(byte value)
        {
            value = NormalizeGlyphValue(value);
            if (CharacterProfileStore.Current.TryGetImagePath(value, out string? profilePath) && profilePath is not null)
            {
                return profilePath;
            }

            return OotGlyphCatalog.GetOriginalGlyphPath(value);
        }

        public double GetAdvance(byte value)
        {
            value = NormalizeGlyphValue(value);
            if (CharacterProfileStore.Current.TryGetWidth(value, out double profileWidth))
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
