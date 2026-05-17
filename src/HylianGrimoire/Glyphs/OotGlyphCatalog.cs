using HylianGrimoire.Codecs;

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

    public static IReadOnlyList<byte> GlyphValues { get; } =
        Enumerable.Range(0x20, OotGlyphMetrics.DefaultWidths.Length)
            .Select(value => (byte)value)
            .Where(value => value is < 0x9f or > 0xab)
            .ToArray();

    public static OotGlyphInfo GetGlyphInfo(byte value)
    {
        GlyphOverrideStore store = GlyphOverrideStore.Current;
        char defaultChar = MessageEncodingProfile.Default.GetDefaultEditorChar(value);
        char currentChar = store.TryGetDisplayChar(value, out char displayChar) ? displayChar : defaultChar;
        double defaultWidth = OotGlyphMetrics.GetDefaultAdvance(value);
        double currentWidth = store.TryGetWidth(value, out double width) ? width : defaultWidth;
        string originalPath = GetOriginalGlyphPath(value);
        string currentPath = store.TryGetImagePath(value, out string? imagePath) && imagePath is not null
            ? imagePath
            : originalPath;

        return new OotGlyphInfo(
            value,
            $"0x{value:X2}",
            defaultChar,
            currentChar,
            defaultWidth,
            currentWidth,
            originalPath,
            currentPath,
            store.TryGetDisplayChar(value, out _),
            store.TryGetWidth(value, out _),
            store.TryGetImagePath(value, out _));
    }

    public static string GetOriginalGlyphPath(byte value)
    {
        if (value == 0x7f)
        {
            value = 0x20;
        }

        string fontRoot = Path.Combine(AssetRoot, FontRelativeRoot);
        string[] matches = Directory.GetFiles(fontRoot, $"gMsgChar{value:X2}*Tex.png");
        return matches.Length > 0
            ? matches[0]
            : Path.Combine(AssetRoot, "__missing__", $"gMsgChar{value:X2}Tex.png");
    }

    public static string GetGlyphRelativePath(byte value)
    {
        string originalPath = GetOriginalGlyphPath(value);
        return Path.GetRelativePath(AssetRoot, originalPath);
    }
}
