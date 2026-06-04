using System.Diagnostics.CodeAnalysis;
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

    public static string GetRequiredRoot()
    {
        string? root = GetRoot();
        return IsValidFixtureRoot(root)
            ? root!
            : Missing<string>(GetRootSkipReason()!);
    }

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
        return TryGetMajorasMaskPath(fileName, out string path)
            ? path
            : Missing<string>(GetMajorasMaskPathSkipReason(fileName)!);
    }

    public static string GetRequiredRetailDecompressedPath(string fileName)
    {
        return TryGetRetailDecompressedPath(fileName, out string path)
            ? path
            : Missing<string>(GetRetailDecompressedPathSkipReason(fileName)!);
    }

    public static (string CompressedPath, string DecompressedPath) GetRequiredMajorasMaskPair(
        string compressedFileName,
        string decompressedFileName)
    {
        return TryGetMajorasMaskPair(compressedFileName, decompressedFileName, out string compressedPath, out string decompressedPath)
            ? (compressedPath, decompressedPath)
            : Missing<(string, string)>(GetMajorasMaskPairSkipReason(compressedFileName, decompressedFileName)!);
    }

    public static void RequirePath(string path)
    {
        if (!File.Exists(path))
        {
            Missing($"Missing local ROM fixture: {path}. Set {RootEnvironmentVariable} or create .local/rom-fixtures.");
        }
    }

    public static string GetRequiredPath(string path)
    {
        RequirePath(path);
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

    public static string? GetRootSkipReason()
    {
        string? root = GetRoot();
        if (root is null)
        {
            return $"Missing local ROM fixture root. Set {RootEnvironmentVariable} or create .local/rom-fixtures.";
        }

        return IsValidFixtureRoot(root)
            ? null
            : $"Local ROM fixture root does not contain recognized fixtures: {root}. Check {RootEnvironmentVariable} or .local/rom-fixtures.";
    }

    public static string? GetLegacyRootSkipReason()
    {
        string? rootReason = GetRootSkipReason();
        if (rootReason is not null)
        {
            return rootReason;
        }

        string root = GetRoot()!;
        return Directory.Exists(Path.Combine(root, "compressed"))
            || Directory.Exists(Path.Combine(root, "decompressed"))
            || Directory.Exists(Path.Combine(root, "retailcompressed"))
            || Directory.Exists(Path.Combine(root, LegacyRetailDecompressedFixtureDirectory))
                ? null
                : $"Missing legacy OoT ROM fixture directories under {root}.";
    }

    public static string? GetMajorasMaskPathSkipReason(string fileName)
        => TryGetMajorasMaskPath(fileName, out string _)
            ? null
            : $"Missing local ROM fixture: {Combine(GetMajorasMaskRoot(), fileName) ?? fileName}. Set {MajorasMaskEnvironmentVariable} or {RootEnvironmentVariable}.";

    public static string? GetMajorasMaskPairSkipReason(string compressedFileName, string decompressedFileName)
        => TryGetMajorasMaskPair(compressedFileName, decompressedFileName, out string _, out string _)
            ? null
            : $"Missing local ROM fixture pair: {Combine(GetMajorasMaskRoot(), compressedFileName) ?? compressedFileName}, {Combine(GetMajorasMaskRoot(), decompressedFileName) ?? decompressedFileName}. Set {MajorasMaskEnvironmentVariable} or {RootEnvironmentVariable}.";

    public static string? GetRetailDecompressedRootSkipReason()
    {
        string? root = GetRetailDecompressedRoot();
        return root is not null && Directory.Exists(root)
            ? null
            : $"Missing local retail decompressed ROM fixture root. Set {RetailDecompressedEnvironmentVariable} or {RootEnvironmentVariable}.";
    }

    public static string? GetRetailDecompressedPathSkipReason(string fileName)
        => TryGetRetailDecompressedPath(fileName, out string _)
            ? null
            : $"Missing local ROM fixture: {Combine(GetRetailDecompressedRoot(), fileName) ?? fileName}. Set {RetailDecompressedEnvironmentVariable} or {RootEnvironmentVariable}.";

    [DoesNotReturn]
    private static T Missing<T>(string reason)
        => throw new InvalidOperationException(reason);

    [DoesNotReturn]
    private static void Missing(string reason)
        => throw new InvalidOperationException(reason);

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

    private static bool IsValidFixtureRoot([NotNullWhen(true)] string? path)
        => path is not null && LooksLikeFixtureRoot(path);
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class LegacyOotRomFixtureFactAttribute : FactAttribute
{
    public LegacyOotRomFixtureFactAttribute()
    {
        Skip = LocalRomFixtures.GetLegacyRootSkipReason();
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class RetailDecompressedRomFixtureFactAttribute : FactAttribute
{
    public RetailDecompressedRomFixtureFactAttribute(string fileName)
    {
        Skip = LocalRomFixtures.GetRetailDecompressedPathSkipReason(fileName);
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class RetailDecompressedRomFixtureTheoryAttribute : TheoryAttribute
{
    public RetailDecompressedRomFixtureTheoryAttribute()
    {
        Skip = LocalRomFixtures.GetRetailDecompressedRootSkipReason();
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class MajorasMaskRomFixtureFactAttribute : FactAttribute
{
    public MajorasMaskRomFixtureFactAttribute(string fileName)
    {
        Skip = LocalRomFixtures.GetMajorasMaskPathSkipReason(fileName);
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class MajorasMaskRomFixtureTheoryAttribute : TheoryAttribute
{
    public MajorasMaskRomFixtureTheoryAttribute(string fileName)
    {
        Skip = LocalRomFixtures.GetMajorasMaskPathSkipReason(fileName);
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class MajorasMaskRomFixturePairFactAttribute : FactAttribute
{
    public MajorasMaskRomFixturePairFactAttribute(string compressedFileName, string decompressedFileName)
    {
        Skip = LocalRomFixtures.GetMajorasMaskPairSkipReason(compressedFileName, decompressedFileName);
    }
}
