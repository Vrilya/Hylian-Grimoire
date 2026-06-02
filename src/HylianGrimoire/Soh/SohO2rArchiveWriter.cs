using System.IO.Compression;
using HylianGrimoire.Services;

namespace HylianGrimoire.Soh;

internal sealed class SohO2rArchiveWriter
{
    private static readonly DateTimeOffset ZipEpoch = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly SortedDictionary<string, byte[]> _entries = new(StringComparer.Ordinal);

    public void Add(string path, byte[] data)
    {
        string normalized = path.Replace('\\', '/');
        _entries[normalized] = data;
    }

    public void Write(string outputPath)
    {
        if (_entries.Count == 0)
        {
            throw new InvalidOperationException("No resources were selected.");
        }

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach ((string path, byte[] data) in _entries)
            {
                ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.SmallestSize);
                entry.LastWriteTime = ZipEpoch;
                using Stream entryStream = entry.Open();
                entryStream.Write(data, 0, data.Length);
            }
        }

        AtomicFileWriter.WriteAllBytes(outputPath, stream.ToArray());
    }

    public static IReadOnlySet<string> ReadResourcePaths(string inputPath)
    {
        return ReadEntries(inputPath).Keys.ToHashSet(StringComparer.Ordinal);
    }

    public static IReadOnlyDictionary<string, byte[]> ReadEntries(string inputPath)
    {
        using var archive = ZipFile.OpenRead(inputPath);
        var entries = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (ZipArchiveEntry entry in archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)))
        {
            using Stream stream = entry.Open();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            entries[entry.FullName.Replace('\\', '/')] = memory.ToArray();
        }

        return entries;
    }
}
