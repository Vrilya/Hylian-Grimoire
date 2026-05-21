using System.Drawing.Imaging;
using System.Security.Cryptography;
using HylianGrimoire.Glyphs;

namespace HylianGrimoire.Rom;

public sealed class RomGlyphSource : IOotGlyphSource
{
    private static readonly string CacheRoot = Path.Combine(Path.GetTempPath(), "HylianGrimoireRomGlyphCache");

    private readonly byte[] _rom;
    private readonly RomFontResources _resources;
    private readonly Dictionary<byte, string> _glyphPaths = [];

    public RomGlyphSource(byte[] decompressedRom, RomFontResources resources)
    {
        _rom = decompressedRom;
        _resources = resources;
        CacheKey = CreateCacheKey(decompressedRom, resources);
    }

    public string CacheKey { get; }

    public string GetGlyphPath(byte value)
    {
        value = NormalizeGlyphValue(value);
        if (_glyphPaths.TryGetValue(value, out string? cachedPath))
        {
            return cachedPath;
        }

        byte[] glyphBytes = RomFontService.ReadGlyph(_rom, _resources, value);
        string hash = Convert.ToHexString(SHA256.HashData(glyphBytes))[..16].ToLowerInvariant();
        string path = Path.Combine(CacheRoot, $"{CacheKey}-{value:x2}-{hash}.png");
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(CacheRoot);
            using var bitmap = RomGlyphCodec.DecodeI4Glyph(glyphBytes);
            bitmap.Save(path, ImageFormat.Png);
        }

        _glyphPaths[value] = path;
        return path;
    }

    public double GetAdvance(byte value)
    {
        return value == 0x20
            ? 6.0
            : RomFontService.ReadWidth(_rom, _resources, NormalizeGlyphValue(value));
    }

    private static byte NormalizeGlyphValue(byte value)
    {
        return value == 0x7f ? (byte)0x20 : value;
    }

    private static string CreateCacheKey(byte[] decompressedRom, RomFontResources resources)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(BitConverter.GetBytes(resources.GlyphDataOffset));
        hash.AppendData(BitConverter.GetBytes(resources.WidthTableOffset));
        hash.AppendData(decompressedRom.AsSpan(
            resources.WidthTableOffset,
            resources.WidthCount * sizeof(float)));

        int glyphBytes = resources.GlyphCount * RomFontResources.GlyphByteSize;
        hash.AppendData(decompressedRom.AsSpan(resources.GlyphDataOffset, glyphBytes));
        return $"rom-{Convert.ToHexString(hash.GetHashAndReset())[..16].ToLowerInvariant()}";
    }

}
