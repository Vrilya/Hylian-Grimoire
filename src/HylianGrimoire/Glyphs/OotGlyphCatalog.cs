using HylianGrimoire.Codecs;

using HylianGrimoire.Rom;

namespace HylianGrimoire.Glyphs;

public sealed record OotGlyphInfo(
    byte Value,
    string Hex,
    char DefaultChar,
    char CurrentChar,
    double DefaultWidth,
    double CurrentWidth,
    string OriginalPath,
    string CurrentPath,
    bool HasDisplayOverride,
    bool HasWidthOverride,
    bool HasImageOverride);

public static class OotGlyphCatalog
{
    private static readonly string AssetRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "Preview", "Oot");
    private const string FontRelativeRoot = @"nes_font_static";
    private const string RawFontRelativeRoot = @"nes_font_static_raw";

    public static IReadOnlyList<byte> GlyphValues { get; } =
        Enumerable.Range(0x20, OotGlyphMetrics.DefaultWidths.Length)
            .Select(value => (byte)value)
            .Where(value => value is < 0x9f or > 0xab)
            .ToArray();

    public static OotGlyphInfo GetGlyphInfo(byte value)
    {
        CharacterProfileStore profiles = CharacterProfileStore.Current;
        char defaultChar = MessageEncodingProfile.Original.GetDefaultEditorChar(value);
        char currentChar = profiles.TryGetDisplayChar(value, out char displayChar) ? displayChar : defaultChar;
        double defaultWidth = OotGlyphMetrics.GetDefaultAdvance(value);
        double currentWidth = defaultWidth;
        if (profiles.TryGetWidth(value, out double profileWidth))
        {
            currentWidth = profileWidth;
        }

        string originalPath = GetOriginalGlyphPath(value);
        string currentPath = originalPath;
        if (profiles.TryGetImagePath(value, out string? profileImagePath) && profileImagePath is not null)
        {
            currentPath = profileImagePath;
        }

        return new OotGlyphInfo(
            value,
            $"0x{value:X2}",
            defaultChar,
            currentChar,
            defaultWidth,
            currentWidth,
            originalPath,
            currentPath,
            profiles.TryGetDisplayChar(value, out _),
            profiles.TryGetWidth(value, out _),
            profiles.TryGetImagePath(value, out _));
    }

    public static string GetOriginalGlyphPath(byte value)
    {
        return GetOriginalGlyphPath(value, RomFontBaseline.Standard);
    }

    public static string GetOriginalGlyphPath(byte value, RomFontBaseline baseline)
    {
        string fontRoot = Path.Combine(AssetRoot, FontRelativeRoot);
        string[] matches = FindGlyphFiles(fontRoot, value, ".png", baseline);
        return matches.Length > 0
            ? matches[0]
            : Path.Combine(AssetRoot, "__missing__", $"gMsgChar{value:X2}Tex.png");
    }

    public static byte[] GetOriginalGlyphBytes(byte value)
    {
        return GetOriginalGlyphBytes(value, RomFontBaseline.Standard);
    }

    public static byte[] GetOriginalGlyphBytes(byte value, RomFontBaseline baseline)
    {
        string path = GetOriginalGlyphDataPath(value, baseline);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Original glyph data was not found for 0x{value:X2}.", path);
        }

        byte[] bytes = File.ReadAllBytes(path);
        if (bytes.Length != 128)
        {
            throw new InvalidDataException($"Original glyph data for 0x{value:X2} must be exactly 128 bytes.");
        }

        return bytes;
    }

    public static string GetOriginalGlyphDataPath(byte value)
    {
        return GetOriginalGlyphDataPath(value, RomFontBaseline.Standard);
    }

    public static string GetOriginalGlyphDataPath(byte value, RomFontBaseline baseline)
    {
        string fontRoot = Path.Combine(AssetRoot, RawFontRelativeRoot);
        string[] matches = FindGlyphFiles(fontRoot, value, ".bin", baseline);
        return matches.Length > 0
            ? matches[0]
            : Path.Combine(AssetRoot, "__missing__", $"gMsgChar{value:X2}Tex.bin");
    }

    public static string GetGlyphRelativePath(byte value)
    {
        string originalPath = GetOriginalGlyphPath(value);
        return Path.GetRelativePath(AssetRoot, originalPath);
    }

    private static string[] FindGlyphFiles(string fontRoot, byte value, string extension, RomFontBaseline baseline)
    {
        if (!Directory.Exists(fontRoot))
        {
            return [];
        }

        if (baseline == RomFontBaseline.PalMultiLanguage && value is 0x81 or 0x8f)
        {
            string[] multiLanguageMatches = Directory.GetFiles(fontRoot, $"gMsgChar{value:X2}*Tex_MLANG{extension}");
            if (multiLanguageMatches.Length > 0)
            {
                return multiLanguageMatches;
            }
        }

        return Directory.GetFiles(fontRoot, $"gMsgChar{value:X2}*Tex{extension}");
    }

}
