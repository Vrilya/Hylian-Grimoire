using System.Collections.Concurrent;
using System.Globalization;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Textures;

public static class TextureCatalog
{
    private const string CatalogRoot = "Assets/TextureCatalog";

    private static readonly ConcurrentDictionary<string, IReadOnlyList<TextureDefinition>> Cache = new(StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, string> CatalogFileByProfileName = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Retail NTSC 1.0"] = "retail_ntsc_10.txt",
        ["Retail NTSC 1.1"] = "retail_ntsc_11.txt",
        ["Retail NTSC 1.2"] = "retail_ntsc_12.txt",
        ["Retail NTSC Master Quest"] = "retail_ntsc_mq.txt",
        ["Retail NTSC GameCube"] = "retail_ntsc_gc.txt",
        ["Retail PAL 1.0"] = "retail_pal_10.txt",
        ["Retail PAL 1.1"] = "retail_pal_11.txt",
        ["Retail PAL Master Quest"] = "retail_pal_mq.txt",
        ["Retail PAL GameCube"] = "retail_pal_gc.txt",
    };

    public static bool TryGetTextures(RomVersionProfile profile, out IReadOnlyList<TextureDefinition> textures)
    {
        if (!CatalogFileByProfileName.TryGetValue(profile.Name, out string? fileName))
        {
            textures = [];
            return false;
        }

        string path = GetCatalogPath(fileName);
        if (!File.Exists(path))
        {
            textures = [];
            return false;
        }

        textures = Cache.GetOrAdd(profile.Name, _ => Parse(File.ReadAllLines(path), fileName));
        return true;
    }

    public static IReadOnlyList<TextureDefinition> GetTextures(RomVersionProfile profile)
        => TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? textures)
            ? textures
            : throw new NotSupportedException($"Texture catalog is not available for {profile.Name}.");

    private static string GetCatalogPath(string fileName)
        => Path.Combine(AppContext.BaseDirectory, CatalogRoot, fileName);

    private static IReadOnlyList<TextureDefinition> Parse(IReadOnlyList<string> rawDefinitions, string fileName)
    {
        var definitions = new List<TextureDefinition>(rawDefinitions.Count);
        for (int i = 0; i < rawDefinitions.Count; i++)
        {
            string raw = rawDefinitions[i].Trim();
            if (raw.Length == 0)
            {
                continue;
            }

            string[] parts = raw.Split('|');
            definitions.Add(ParseDefinition(parts, fileName, i + 1));
        }

        return definitions;
    }

    private static TextureDefinition ParseDefinition(string[] parts, string fileName, int lineNumber)
    {
        if (parts.Length is not 7 and not 9)
        {
            throw new InvalidDataException($"Invalid texture definition in {fileName} line {lineNumber}.");
        }

        TextureFormat format = ParseFormat(parts[4]);
        bool usesTlut = format is TextureFormat.CI4 or TextureFormat.CI8;
        if (usesTlut && parts.Length != 9)
        {
            throw new InvalidDataException($"Texture definition in {fileName} line {lineNumber} uses {format} but has no TLUT fields.");
        }

        return new TextureDefinition(
            parts[0],
            parts[1],
            int.Parse(parts[3], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            int.Parse(parts[5], CultureInfo.InvariantCulture),
            int.Parse(parts[6], CultureInfo.InvariantCulture),
            format,
            parts.Length == 9 ? int.Parse(parts[7], NumberStyles.HexNumber, CultureInfo.InvariantCulture) : null,
            parts.Length == 9 ? int.Parse(parts[8], CultureInfo.InvariantCulture) : null,
            parts[2]);
    }

    private static TextureFormat ParseFormat(string value)
        => value.ToUpperInvariant() switch
        {
            "CI4" => TextureFormat.CI4,
            "CI8" => TextureFormat.CI8,
            "I4" => TextureFormat.I4,
            "I8" => TextureFormat.I8,
            "IA4" => TextureFormat.IA4,
            "IA8" => TextureFormat.IA8,
            "IA16" => TextureFormat.IA16,
            "RGBA16" => TextureFormat.Rgba16,
            "RGBA32" => TextureFormat.Rgba32,
            _ => throw new InvalidDataException($"Unsupported texture format: {value}."),
        };
}
