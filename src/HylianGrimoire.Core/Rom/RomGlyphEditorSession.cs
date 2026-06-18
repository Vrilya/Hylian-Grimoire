using System.Security.Cryptography;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Services;

namespace HylianGrimoire.Rom;

public sealed class RomGlyphEditorSession(
    byte[] decompressedRom,
    RomFontResources resources,
    RomFontBaseline baseline,
    GameKind gameKind)
{
    private static readonly string CacheRoot = Path.Combine(Path.GetTempPath(), "HylianGrimoireRomGlyphEditorCache");
    private readonly IReadOnlyList<byte> _glyphValues = GameGlyphCatalog.GetGlyphValues(gameKind);
    private readonly Dictionary<byte, byte[]> _loadedGlyphs = CreateLoadedGlyphs(decompressedRom, resources, gameKind);
    private readonly Dictionary<byte, double> _loadedWidths = CreateLoadedWidths(decompressedRom, resources, gameKind);

    public event EventHandler? Changed;

    public GameKind GameKind => gameKind;

    public GlyphInfo GetGlyphInfo(byte value, CharacterProfileSnapshot snapshot)
    {
        if (snapshot.GameKind != gameKind)
        {
            throw new InvalidOperationException(
                $"Cannot use a {snapshot.GameKind} character profile snapshot with a {gameKind} ROM glyph session.");
        }

        char defaultChar = GameProfiles.GetOriginalEncodingProfile(gameKind).GetDefaultEditorChar(value);
        char currentChar = snapshot.TryGetDisplayChar(value, out char displayChar) ? displayChar : defaultChar;
        double defaultWidth = RomFontBaselineMetrics.GetDefaultAdvance(baseline, value);
        double currentWidth = RomFontService.ReadWidth(decompressedRom, resources, value);
        string originalPath = GameGlyphCatalog.GetOriginalGlyphPath(gameKind, value, baseline);
        string currentPath = GetCurrentGlyphPath(value);

        return new GlyphInfo(
            value,
            $"0x{value:X2}",
            defaultChar,
            currentChar,
            defaultWidth,
            currentWidth,
            originalPath,
            currentPath,
            HasDisplayOverride: snapshot.TryGetDisplayChar(value, out _),
            HasWidthOverride: Math.Abs(currentWidth - defaultWidth) > 0.001,
            HasImageOverride: !IsOriginalGlyphImage(value));
    }

    public bool HasAnyCustomGlyphOrWidth()
    {
        foreach (byte value in _glyphValues)
        {
            double defaultWidth = RomFontBaselineMetrics.GetDefaultAdvance(baseline, value);
            double currentWidth = RomFontService.ReadWidth(decompressedRom, resources, value);
            if (Math.Abs(currentWidth - defaultWidth) > 0.001 || !IsOriginalGlyphImage(value))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasLoadedCustomGlyphOrWidth()
    {
        foreach (byte value in _glyphValues)
        {
            double defaultWidth = RomFontBaselineMetrics.GetDefaultAdvance(baseline, value);
            if (_loadedWidths.TryGetValue(value, out double loadedWidth)
                && Math.Abs(loadedWidth - defaultWidth) > 0.001)
            {
                return true;
            }

            if (_loadedGlyphs.TryGetValue(value, out byte[]? loadedGlyph)
                && !IsOriginalGlyphImage(value, loadedGlyph))
            {
                return true;
            }
        }

        return false;
    }

    public void SetWidth(byte value, double width)
    {
        RomFontService.WriteWidth(decompressedRom, resources, value, (float)width);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ResetWidth(byte value)
    {
        SetWidth(value, RomFontBaselineMetrics.GetDefaultAdvance(baseline, value));
    }

    public void ResetAllToDefault()
    {
        WriteDefaultGlyphsAndWidths();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void RestoreLoadedRomGlyphs()
    {
        foreach (byte value in _glyphValues)
        {
            if (_loadedGlyphs.TryGetValue(value, out byte[]? glyphBytes))
            {
                RomFontService.WriteGlyph(decompressedRom, resources, value, glyphBytes);
            }

            if (_loadedWidths.TryGetValue(value, out double width))
            {
                RomFontService.WriteWidth(decompressedRom, resources, value, (float)width);
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyCharacterProfile(CharacterProfileSnapshot snapshot)
    {
        if (snapshot.GameKind != gameKind)
        {
            throw new InvalidOperationException(
                $"Cannot apply a {snapshot.GameKind} character profile snapshot to a {gameKind} ROM glyph session.");
        }

        WriteDefaultGlyphsAndWidths();

        foreach ((byte value, double width) in snapshot.Widths)
        {
            RomFontService.WriteWidth(decompressedRom, resources, value, (float)width);
        }

        foreach ((byte value, string imagePath) in snapshot.ImagePaths)
        {
            byte[] glyphBytes = RomGlyphCodec.EncodeI4Glyph(imagePath);
            RomFontService.WriteGlyph(decompressedRom, resources, value, glyphBytes);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void WriteDefaultGlyphsAndWidths()
    {
        foreach (byte value in _glyphValues)
        {
            byte[] glyphBytes = GameGlyphCatalog.GetOriginalGlyphBytes(gameKind, value, baseline);
            RomFontService.WriteGlyph(decompressedRom, resources, value, glyphBytes);
            RomFontService.WriteWidth(
                decompressedRom,
                resources,
                value,
                (float)RomFontBaselineMetrics.GetDefaultAdvance(baseline, value));
        }
    }

    public void SetImage(byte value, string imagePath)
    {
        byte[] glyphBytes = RomGlyphCodec.EncodeI4Glyph(imagePath);
        RomFontService.WriteGlyph(decompressedRom, resources, value, glyphBytes);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ResetImage(byte value)
    {
        byte[] glyphBytes = GameGlyphCatalog.GetOriginalGlyphBytes(gameKind, value, baseline);
        RomFontService.WriteGlyph(decompressedRom, resources, value, glyphBytes);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private string GetCurrentGlyphPath(byte value)
    {
        byte[] glyphBytes = RomFontService.ReadGlyph(decompressedRom, resources, value);
        string hash = Convert.ToHexString(SHA256.HashData(glyphBytes))[..16].ToLowerInvariant();
        string path = Path.Combine(CacheRoot, $"{value:x2}-{hash}.png");
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(CacheRoot);
            using var bitmap = RomGlyphCodec.DecodeI4Glyph(glyphBytes);
            PngFileWriter.Save(bitmap, path);
        }

        return path;
    }

    private static Dictionary<byte, byte[]> CreateLoadedGlyphs(
        byte[] rom,
        RomFontResources fontResources,
        GameKind gameKind)
    {
        return GameGlyphCatalog.GetGlyphValues(gameKind).ToDictionary(
            value => value,
            value => RomFontService.ReadGlyph(rom, fontResources, value));
    }

    private static Dictionary<byte, double> CreateLoadedWidths(
        byte[] rom,
        RomFontResources fontResources,
        GameKind gameKind)
    {
        return GameGlyphCatalog.GetGlyphValues(gameKind).ToDictionary(
            value => value,
            value => (double)RomFontService.ReadWidth(rom, fontResources, value));
    }

    private bool IsOriginalGlyphImage(byte value)
    {
        try
        {
            return IsOriginalGlyphImage(value, RomFontService.ReadGlyph(decompressedRom, resources, value));
        }
        catch (IOException)
        {
            return false;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private bool IsOriginalGlyphImage(byte value, byte[] glyphBytes)
    {
        byte[] original = GameGlyphCatalog.GetOriginalGlyphBytes(gameKind, value, baseline);
        return glyphBytes.AsSpan().SequenceEqual(original);
    }
}
