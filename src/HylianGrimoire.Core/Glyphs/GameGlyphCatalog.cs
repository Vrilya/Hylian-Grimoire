using HylianGrimoire.Games;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Glyphs;

public static class GameGlyphCatalog
{
    public static IReadOnlyList<byte> GetGlyphValues(GameKind gameKind)
    {
        return gameKind switch
        {
            GameKind.OcarinaOfTime => OotGlyphCatalog.GlyphValues,
            GameKind.MajorasMask => MmGlyphCatalog.GlyphValues,
            _ => throw new NotSupportedException($"No glyph catalog is registered for {gameKind}.")
        };
    }

    public static GlyphInfo GetGlyphInfo(GameKind gameKind, byte value, CharacterProfileSnapshot snapshot)
    {
        EnsureSnapshotGame(gameKind, snapshot);

        bool hasDisplayOverride = snapshot.TryGetDisplayChar(value, out char displayChar);
        bool hasWidthOverride = snapshot.TryGetWidth(value, out double profileWidth);
        bool hasImageOverride = snapshot.TryGetImagePath(value, out string? profileImagePath);

        char defaultChar = GameProfiles.GetOriginalEncodingProfile(gameKind).GetDefaultEditorChar(value);
        char currentChar = hasDisplayOverride ? displayChar : defaultChar;
        double defaultWidth = GetDefaultAdvance(gameKind, value);
        double currentWidth = hasWidthOverride ? profileWidth : defaultWidth;
        string originalPath = GetOriginalGlyphPath(gameKind, value);
        string currentPath = hasImageOverride && profileImagePath is not null
            ? profileImagePath
            : originalPath;

        return new GlyphInfo(
            value,
            $"0x{value:X2}",
            defaultChar,
            currentChar,
            defaultWidth,
            currentWidth,
            originalPath,
            currentPath,
            hasDisplayOverride,
            hasWidthOverride,
            hasImageOverride);
    }

    public static string GetOriginalGlyphPath(GameKind gameKind, byte value)
    {
        return gameKind switch
        {
            GameKind.OcarinaOfTime => OotGlyphCatalog.GetOriginalGlyphPath(value),
            GameKind.MajorasMask => MmGlyphCatalog.GetOriginalGlyphPath(value),
            _ => throw new NotSupportedException($"No glyph catalog is registered for {gameKind}.")
        };
    }

    public static string GetOriginalGlyphPath(GameKind gameKind, byte value, RomFontBaseline baseline)
    {
        return gameKind switch
        {
            GameKind.OcarinaOfTime => OotGlyphCatalog.GetOriginalGlyphPath(value, baseline),
            GameKind.MajorasMask => MmGlyphCatalog.GetOriginalGlyphPath(value, baseline),
            _ => throw new NotSupportedException($"No glyph catalog is registered for {gameKind}.")
        };
    }

    public static byte[] GetOriginalGlyphBytes(GameKind gameKind, byte value, RomFontBaseline baseline)
    {
        return gameKind switch
        {
            GameKind.OcarinaOfTime => OotGlyphCatalog.GetOriginalGlyphBytes(value, baseline),
            GameKind.MajorasMask => MmGlyphCatalog.GetOriginalGlyphBytes(value, baseline),
            _ => throw new NotSupportedException($"No glyph catalog is registered for {gameKind}.")
        };
    }

    public static string GetGlyphRelativePath(GameKind gameKind, byte value)
    {
        return gameKind switch
        {
            GameKind.OcarinaOfTime => OotGlyphCatalog.GetGlyphRelativePath(value),
            GameKind.MajorasMask => MmGlyphCatalog.GetGlyphRelativePath(value),
            _ => throw new NotSupportedException($"No glyph catalog is registered for {gameKind}.")
        };
    }

    public static double GetDefaultAdvance(GameKind gameKind, byte value)
    {
        return gameKind switch
        {
            GameKind.OcarinaOfTime => OotGlyphMetrics.GetDefaultAdvance(value),
            GameKind.MajorasMask => MmGlyphMetrics.GetDefaultAdvance(value),
            _ => throw new NotSupportedException($"No glyph metrics are registered for {gameKind}.")
        };
    }

    private static void EnsureSnapshotGame(GameKind gameKind, CharacterProfileSnapshot snapshot)
    {
        if (snapshot.GameKind != gameKind)
        {
            throw new InvalidOperationException(
                $"Cannot use a {snapshot.GameKind} character profile snapshot with the {gameKind} glyph catalog.");
        }
    }
}
