using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Text;

namespace HylianGrimoire.TextTextures;

public static partial class EndTitleTextureRenderer
{
    private static readonly ConcurrentDictionary<string, Lazy<CachedDrawingFontFace>> DrawingFontFaces = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, Lazy<CachedImageSharpFontFace>> ImageSharpFontFaces = new(StringComparer.OrdinalIgnoreCase);

    private static CachedDrawingFontFace GetDrawingFontFace(TextTextureFont font)
        => DrawingFontFaces.GetOrAdd(
            GetFontCacheKey(font),
            _ => new Lazy<CachedDrawingFontFace>(() => new CachedDrawingFontFace(font)))
            .Value;

    private static CachedImageSharpFontFace GetImageSharpFontFace(TextTextureFont font)
        => ImageSharpFontFaces.GetOrAdd(
            GetFontCacheKey(font),
            _ => new Lazy<CachedImageSharpFontFace>(() => new CachedImageSharpFontFace(font)))
            .Value;

    private static string GetFontCacheKey(TextTextureFont font)
        => string.Join("|", Path.GetFullPath(font.Path), font.FamilyName ?? string.Empty, font.Style);

    private sealed class CachedDrawingFontFace
    {
        private readonly PrivateFontCollection _collection = new();

        public CachedDrawingFontFace(TextTextureFont font)
        {
            _collection.AddFontFile(font.Path);
            Family = TextTextureFontResolver.ResolveDrawingFamily(_collection, font);
            Style = TextTextureFontResolver.ResolveDrawingStyle(Family, font);
        }

        public FontFamily Family { get; }

        public FontStyle Style { get; }
    }

    private sealed class CachedImageSharpFontFace
    {
        private readonly SixLabors.Fonts.FontCollection _collection = new();

        public CachedImageSharpFontFace(TextTextureFont font)
        {
            IReadOnlyCollection<SixLabors.Fonts.FontFamily> families = Path.GetExtension(font.Path).Equals(".ttc", StringComparison.OrdinalIgnoreCase)
                ? _collection.AddCollection(font.Path).ToArray()
                : [_collection.Add(font.Path)];

            Family = ResolveFamily(families, font);
            Style = TextTextureFontResolver.ToImageSharpStyle(font.Style);
        }

        public SixLabors.Fonts.FontFamily Family { get; }

        public SixLabors.Fonts.FontStyle Style { get; }

        private static SixLabors.Fonts.FontFamily ResolveFamily(
            IReadOnlyCollection<SixLabors.Fonts.FontFamily> families,
            TextTextureFont font)
        {
            if (string.IsNullOrWhiteSpace(font.FamilyName))
            {
                return families.First();
            }

            foreach (SixLabors.Fonts.FontFamily family in families)
            {
                if (string.Equals(family.Name, font.FamilyName, StringComparison.OrdinalIgnoreCase))
                {
                    return family;
                }
            }

            string availableFamilies = string.Join(", ", families.Select(family => family.Name));
            throw new InvalidOperationException(
                $"Font family '{font.FamilyName}' was not found in {Path.GetFileName(font.Path)}. Available families: {availableFamilies}.");
        }
    }
}
