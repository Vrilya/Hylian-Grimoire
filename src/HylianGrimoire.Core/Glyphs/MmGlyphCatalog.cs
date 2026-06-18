using HylianGrimoire.Games;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Glyphs;

public static class MmGlyphCatalog
{
    private static readonly string AssetRoot = Path.Combine(
        AppContext.BaseDirectory,
        GameProfiles.Get(GameKind.MajorasMask).Assets.PreviewRoot);

    private const string FontRelativeRoot = "nes_font_static";
    private const string RawFontRelativeRoot = "nes_font_static_raw";

    public static IReadOnlyList<byte> GlyphValues { get; } =
        Enumerable.Range(0x20, MmGlyphMetrics.DefaultWidths.Length)
            .Select(value => (byte)value)
            .Where(value => value < 0xb0)
            .ToArray();

    public static string GetOriginalGlyphPath(byte value)
        => GetOriginalGlyphPath(value, RomFontBaseline.MajorasMask);

    public static string GetOriginalGlyphPath(byte value, RomFontBaseline baseline)
    {
        string fontRoot = Path.Combine(AssetRoot, FontRelativeRoot);
        if (!Directory.Exists(fontRoot))
        {
            return GetMissingGlyphPath(value);
        }

        string[] matches = FindGlyphFiles(fontRoot, value, ".png", baseline);
        return matches.Length > 0 ? matches[0] : GetMissingGlyphPath(value);
    }

    public static string GetGlyphRelativePath(byte value)
    {
        string originalPath = GetOriginalGlyphPath(value);
        return Path.GetRelativePath(AssetRoot, originalPath);
    }

    public static byte[] GetOriginalGlyphBytes(byte value)
        => GetOriginalGlyphBytes(value, RomFontBaseline.MajorasMask);

    public static byte[] GetOriginalGlyphBytes(byte value, RomFontBaseline baseline)
    {
        string path = GetOriginalGlyphDataPath(value, baseline);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Original MM glyph data was not found for 0x{value:X2}.", path);
        }

        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length != 128)
        {
            throw new InvalidDataException($"Original MM glyph data for 0x{value:X2} must be exactly 128 bytes.");
        }

        return bytes;
    }

    public static string GetOriginalGlyphDataPath(byte value)
        => GetOriginalGlyphDataPath(value, RomFontBaseline.MajorasMask);

    public static string GetOriginalGlyphDataPath(byte value, RomFontBaseline baseline)
    {
        string fontRoot = Path.Combine(AssetRoot, RawFontRelativeRoot);
        if (!Directory.Exists(fontRoot))
        {
            return Path.Combine(AssetRoot, "__missing__", $"gMsgChar{value:X2}Tex.bin");
        }

        string[] matches = FindGlyphFiles(fontRoot, value, ".bin", baseline);
        return matches.Length > 0 ? matches[0] : Path.Combine(AssetRoot, "__missing__", $"gMsgChar{value:X2}Tex.bin");
    }

    private static string[] FindGlyphFiles(string fontRoot, byte value, string extension, RomFontBaseline baseline)
    {
        if (value == 0x2c && baseline is (RomFontBaseline.MajorasMaskUsGameCube or RomFontBaseline.MajorasMaskEu))
        {
            string[] euMatches = Directory.GetFiles(fontRoot, $"gMsgChar{value:X2}*Tex_EU{extension}");
            if (euMatches.Length > 0)
            {
                return euMatches;
            }
        }

        return Directory.GetFiles(fontRoot, $"gMsgChar{value:X2}*Tex{extension}");
    }

    private static string GetMissingGlyphPath(byte value)
        => Path.Combine(AssetRoot, "__missing__", $"gMsgChar{value:X2}Tex.png");
}
