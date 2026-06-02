using Xunit;

namespace HylianGrimoire.Tests;

internal static class LocalRomFixtures
{
    public const string RootEnvironmentVariable = "HYLIAN_GRIMOIRE_ROM_FIXTURE_ROOT";
    public const string MajorasMaskEnvironmentVariable = "HYLIAN_GRIMOIRE_MAJORAS_MASK_FIXTURE_ROOT";
    public const string RetailDecompressedEnvironmentVariable = "HYLIAN_GRIMOIRE_RETAIL_DECOMPRESSED_ROOT";

    private const string LocalFixtureRootDirectory = ".local";
    private const string LocalFixtureRootName = "rom-fixtures";
    private const string OcarinaOfTimeFixtureDirectory = "oot";
    private const string MajorasMaskFixtureDirectory = "mm";
    private const string LegacyRetailDecompressedFixtureDirectory = "retaildecompressed";
    private const string LegacyMajorasMaskFixtureDirectory = "majorasmask";

    private static readonly IReadOnlyDictionary<string, string[]> LegacyFileNames =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["oot_retail_ntsc_1.0_decompressed.z64"] = ["ntsc10_orig.z64"],
            ["oot_retail_ntsc_1.1_decompressed.z64"] = ["ntsc11_orig.z64"],
            ["oot_retail_ntsc_1.2_decompressed.z64"] = ["ntsc12_orig.z64"],
            ["oot_retail_ntsc_gc_decompressed.z64"] = ["ntscgc_orig.z64"],
            ["oot_retail_ntsc_mq_decompressed.z64"] = ["ntscmq_orig.z64"],
            ["oot_retail_pal_1.0_decompressed.z64"] = ["pal10_orig.z64"],
            ["oot_retail_pal_1.1_decompressed.z64"] = ["pal11_orig.z64"],
            ["oot_retail_pal_gc_decompressed.z64"] = ["palgc_orig.z64"],
            ["oot_retail_pal_mq_decompressed.z64"] = ["palmq_orig.z64"],
            ["mm_us_n64_compressed.z64"] = ["majora_us_comp.z64"],
            ["mm_us_n64_decompressed.z64"] = ["majora_us_dec.z64"],
            ["mm_us_gc_compressed.z64"] = ["majora_us_comp_gc.z64"],
            ["mm_us_gc_decompressed.z64"] = ["majora_us_comp_gc.decompressed.z64"],
            ["mm_eu_1.0_n64_compressed.z64"] = ["majora_eu_comp_v1.0.z64"],
            ["mm_eu_1.0_n64_decompressed.z64"] = ["majorasmaskmajora_eu_dec_v1.0.z64"],
            ["mm_eu_1.1_n64_compressed.z64"] = ["majorasmask_eu_comp_v1.1.z64"],
            ["mm_eu_1.1_n64_decompressed.z64"] = ["majorasmask_eu_dec_v1.1.z64"],
            ["mm_eu_gc_compressed.z64"] = ["majora_eu_comp_gc.z64"],
            ["mm_eu_gc_decompressed.z64"] = ["majora_eu_dec_gc.z64"],
        };

    private static readonly Lazy<string?> DiscoveredRoot = new(FindFixtureRoot);

    public static string? GetRoot()
        => Normalize(Environment.GetEnvironmentVariable(RootEnvironmentVariable)) ?? DiscoveredRoot.Value;

    public static string? GetMajorasMaskRoot()
        => Normalize(Environment.GetEnvironmentVariable(MajorasMaskEnvironmentVariable))
            ?? GetGameFixtureRoot(MajorasMaskFixtureDirectory, LegacyMajorasMaskFixtureDirectory);

    public static string? GetRetailDecompressedRoot()
        => Normalize(Environment.GetEnvironmentVariable(RetailDecompressedEnvironmentVariable))
            ?? GetGameFixtureRoot(OcarinaOfTimeFixtureDirectory, LegacyRetailDecompressedFixtureDirectory);

    public static bool TryGetMajorasMaskPath(string fileName, out string path)
        => TryGetExistingPath(GetMajorasMaskRoot(), fileName, out path);

    public static bool TryGetMajorasMaskPair(
        string compressedFileName,
        string decompressedFileName,
        out string compressedPath,
        out string decompressedPath)
    {
        bool hasCompressed = TryGetMajorasMaskPath(compressedFileName, out compressedPath);
        bool hasDecompressed = TryGetMajorasMaskPath(decompressedFileName, out decompressedPath);
        return hasCompressed && hasDecompressed;
    }

    public static bool TryGetRetailDecompressedPath(string fileName, out string path)
        => TryGetExistingPath(GetRetailDecompressedRoot(), fileName, out path);

    public static string GetRequiredMajorasMaskPath(string fileName)
    {
        string? root = GetMajorasMaskRoot();
        string path = Combine(root, fileName) ?? fileName;
        Assert.True(
            File.Exists(path),
            $"Missing local ROM fixture: {path}. Set {MajorasMaskEnvironmentVariable} or {RootEnvironmentVariable}.");
        return path;
    }

    public static string GetRequiredRetailDecompressedPath(string fileName)
    {
        string? root = GetRetailDecompressedRoot();
        string path = Combine(root, fileName) ?? fileName;
        Assert.True(
            File.Exists(path),
            $"Missing local ROM fixture: {path}. Set {RetailDecompressedEnvironmentVariable} or {RootEnvironmentVariable}.");
        return path;
    }

    private static bool TryGetExistingPath(string? root, string fileName, out string path)
    {
        foreach (string candidateFileName in EnumerateFileNameCandidates(fileName))
        {
            path = Combine(root, candidateFileName) ?? string.Empty;
            if (path.Length > 0 && File.Exists(path))
            {
                return true;
            }
        }

        path = Combine(root, fileName) ?? string.Empty;
        return false;
    }

    private static IEnumerable<string> EnumerateFileNameCandidates(string fileName)
    {
        yield return fileName;
        if (LegacyFileNames.TryGetValue(fileName, out string[]? legacyFileNames))
        {
            foreach (string legacyFileName in legacyFileNames)
            {
                yield return legacyFileName;
            }
        }
    }

    private static string? GetGameFixtureRoot(string directoryName, string legacyDirectoryName)
    {
        string? root = GetRoot();
        if (root is null)
        {
            return null;
        }

        string currentPath = Path.Combine(root, directoryName);
        if (Directory.Exists(currentPath))
        {
            return currentPath;
        }

        string legacyPath = Path.Combine(root, legacyDirectoryName);
        if (Directory.Exists(legacyPath))
        {
            return legacyPath;
        }

        return currentPath;
    }

    private static string? Combine(string? root, string fileName)
        => root is null ? null : Path.Combine(root, fileName);

    private static string? Normalize(string? path)
        => string.IsNullOrWhiteSpace(path) ? null : path;

    private static string? FindFixtureRoot()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string startPath in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            DirectoryInfo? directory = Directory.Exists(startPath)
                ? new DirectoryInfo(startPath)
                : Directory.GetParent(startPath);

            while (directory is not null)
            {
                string localFixtureRoot = Path.Combine(
                    directory.FullName,
                    LocalFixtureRootDirectory,
                    LocalFixtureRootName);
                if (seen.Add(localFixtureRoot) && LooksLikeFixtureRoot(localFixtureRoot))
                {
                    return localFixtureRoot;
                }

                if (seen.Add(directory.FullName) && LooksLikeFixtureRoot(directory.FullName))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }

    private static bool LooksLikeFixtureRoot(string path)
        => Directory.Exists(Path.Combine(path, "compressed"))
            || Directory.Exists(Path.Combine(path, OcarinaOfTimeFixtureDirectory))
            || Directory.Exists(Path.Combine(path, MajorasMaskFixtureDirectory))
            || Directory.Exists(Path.Combine(path, LegacyMajorasMaskFixtureDirectory))
            || Directory.Exists(Path.Combine(path, LegacyRetailDecompressedFixtureDirectory));
}
