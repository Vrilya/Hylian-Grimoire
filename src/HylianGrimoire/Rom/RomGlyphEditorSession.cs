using System.Drawing.Imaging;
using System.Security.Cryptography;
using HylianGrimoire.Codecs;
using HylianGrimoire.Glyphs;

namespace HylianGrimoire.Rom;

public sealed class RomGlyphEditorSession(byte[] decompressedRom, RomFontResources resources, RomFontBaseline baseline)
{
    private static readonly string CacheRoot = Path.Combine(Path.GetTempPath(), "HylianGrimoireRomGlyphEditorCache");
    private readonly Dictionary<byte, byte[]> _loadedGlyphs = CreateLoadedGlyphs(decompressedRom, resources);
    private readonly Dictionary<byte, double> _loadedWidths = CreateLoadedWidths(decompressedRom, resources);

    public event EventHandler? Changed;

    public OotGlyphInfo GetGlyphInfo(byte value)
    {
        char defaultChar = MessageEncodingProfile.Original.GetDefaultEditorChar(value);
        char currentChar = CharacterProfileStore.Current.TryGetDisplayChar(value, out char displayChar) ? displayChar : defaultChar;
        double defaultWidth = RomFontBaselineMetrics.GetDefaultAdvance(baseline, value);
        double currentWidth = RomFontService.ReadWidth(decompressedRom, resources, value);
        string originalPath = OotGlyphCatalog.GetOriginalGlyphPath(value, baseline);
        string currentPath = GetCurrentGlyphPath(value);

        return new OotGlyphInfo(
            value,
            $"0x{value:X2}",
            defaultChar,
            currentChar,
            defaultWidth,
            currentWidth,
            originalPath,
            currentPath,
            HasDisplayOverride: CharacterProfileStore.Current.TryGetDisplayChar(value, out _),
            HasWidthOverride: Math.Abs(currentWidth - defaultWidth) > 0.001,
            HasImageOverride: !IsOriginalGlyphImage(value));
    }

    public bool HasAnyCustomGlyphOrWidth()
    {
        foreach (byte value in OotGlyphCatalog.GlyphValues)
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
        foreach (byte value in OotGlyphCatalog.GlyphValues)
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
        foreach (byte value in OotGlyphCatalog.GlyphValues)
        {
            byte[] glyphBytes = OotGlyphCatalog.GetOriginalGlyphBytes(value, baseline);
            RomFontService.WriteGlyph(decompressedRom, resources, value, glyphBytes);
            RomFontService.WriteWidth(
                decompressedRom,
                resources,
                value,
                (float)RomFontBaselineMetrics.GetDefaultAdvance(baseline, value));
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void RestoreLoadedRomGlyphs()
    {
        foreach (byte value in OotGlyphCatalog.GlyphValues)
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

    public void ApplySelectedCharacterProfile()
    {
        CharacterProfileStore profiles = CharacterProfileStore.Current;
        foreach ((byte value, double width) in profiles.GetSelectedProfileWidths())
        {
            RomFontService.WriteWidth(decompressedRom, resources, value, (float)width);
        }

        foreach ((byte value, string imagePath) in profiles.GetSelectedProfileImagePaths())
        {
            byte[] glyphBytes = RomGlyphCodec.EncodeI4Glyph(imagePath);
            RomFontService.WriteGlyph(decompressedRom, resources, value, glyphBytes);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void CaptureIntoSelectedCharacterProfile()
    {
        CharacterProfileStore profiles = CharacterProfileStore.Current;
        foreach (byte value in OotGlyphCatalog.GlyphValues)
        {
            OotGlyphInfo info = GetGlyphInfo(value);
            if (info.HasWidthOverride)
            {
                profiles.SetWidth(value, info.CurrentWidth, info.DefaultWidth);
            }

            if (info.HasImageOverride)
            {
                profiles.SetImage(value, info.CurrentPath);
            }
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
        byte[] glyphBytes = OotGlyphCatalog.GetOriginalGlyphBytes(value, baseline);
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
            bitmap.Save(path, ImageFormat.Png);
        }

        return path;
    }

    private static Dictionary<byte, byte[]> CreateLoadedGlyphs(byte[] rom, RomFontResources fontResources)
    {
        return OotGlyphCatalog.GlyphValues.ToDictionary(
            value => value,
            value => RomFontService.ReadGlyph(rom, fontResources, value));
    }

    private static Dictionary<byte, double> CreateLoadedWidths(byte[] rom, RomFontResources fontResources)
    {
        return OotGlyphCatalog.GlyphValues.ToDictionary(
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
        byte[] original = OotGlyphCatalog.GetOriginalGlyphBytes(value, baseline);
        return glyphBytes.AsSpan().SequenceEqual(original);
    }
}
