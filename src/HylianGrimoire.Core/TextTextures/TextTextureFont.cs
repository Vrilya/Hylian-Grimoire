using System.Drawing;
using System.Drawing.Text;

namespace HylianGrimoire.TextTextures;

public sealed record TextTextureFont(string Path, string? FamilyName = null, FontStyle Style = FontStyle.Regular)
{
    public static TextTextureFont FromPath(string path)
        => new(path);
}

internal sealed class TextTextureDrawingFont : IDisposable
{
    private readonly PrivateFontCollection _collection = new();

    public TextTextureDrawingFont(TextTextureFont font)
    {
        _collection.AddFontFile(font.Path);
        Family = TextTextureFontResolver.ResolveDrawingFamily(_collection, font);
        Style = TextTextureFontResolver.ResolveDrawingStyle(Family, font);
    }

    public FontFamily Family { get; }

    public FontStyle Style { get; }

    public void Dispose()
    {
        Family.Dispose();
        _collection.Dispose();
    }
}

internal static class TextTextureFontResolver
{
    public static FontFamily ResolveDrawingFamily(PrivateFontCollection collection, TextTextureFont font)
    {
        if (string.IsNullOrWhiteSpace(font.FamilyName))
        {
            return collection.Families[0];
        }

        foreach (FontFamily family in collection.Families)
        {
            if (string.Equals(family.Name, font.FamilyName, StringComparison.OrdinalIgnoreCase))
            {
                return family;
            }
        }

        string availableFamilies = string.Join(", ", collection.Families.Select(family => family.Name));
        throw new InvalidOperationException(
            $"Font family '{font.FamilyName}' was not found in {Path.GetFileName(font.Path)}. Available families: {availableFamilies}.");
    }

    public static FontStyle ResolveDrawingStyle(FontFamily family, TextTextureFont font)
    {
        if (family.IsStyleAvailable(font.Style))
        {
            return font.Style;
        }

        if (font.Style != FontStyle.Regular && family.IsStyleAvailable(FontStyle.Regular))
        {
            return FontStyle.Regular;
        }

        throw new InvalidOperationException($"Font family '{family.Name}' does not support {font.Style} style.");
    }

    public static SixLabors.Fonts.FontStyle ToImageSharpStyle(FontStyle style)
    {
        bool bold = (style & FontStyle.Bold) != 0;
        bool italic = (style & FontStyle.Italic) != 0;
        return (bold, italic) switch
        {
            (true, true) => SixLabors.Fonts.FontStyle.BoldItalic,
            (true, false) => SixLabors.Fonts.FontStyle.Bold,
            (false, true) => SixLabors.Fonts.FontStyle.Italic,
            _ => SixLabors.Fonts.FontStyle.Regular,
        };
    }
}
