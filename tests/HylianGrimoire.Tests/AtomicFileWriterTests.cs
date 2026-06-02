using System.Text;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class AtomicFileWriterTests
{
    [Fact]
    public void WriteAllBytesCreatesNewFile()
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        string path = Path.Combine(tempDirectory.Path, "output.bin");

        AtomicFileWriter.WriteAllBytes(path, [0x01, 0x02, 0x03]);

        Assert.Equal([0x01, 0x02, 0x03], File.ReadAllBytes(path));
        Assert.Empty(FindTempFiles(tempDirectory.Path));
    }

    [Fact]
    public void WriteAllTextReplacesExistingFileWithoutBom()
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        string path = Path.Combine(tempDirectory.Path, "message.h");
        File.WriteAllText(path, "old");

        AtomicFileWriter.WriteAllText(path, "new text", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        byte[] bytes = File.ReadAllBytes(path);
        Assert.Equal("new text", File.ReadAllText(path));
        Assert.False(bytes.Length >= 3 && bytes.AsSpan(0, 3).SequenceEqual(new byte[] { 0xef, 0xbb, 0xbf }));
        Assert.Empty(FindTempFiles(tempDirectory.Path));
    }

    [Fact]
    public void WriteAllBytesBatchRollsBackEarlierReplacementWhenLaterWriteFails()
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        string firstPath = Path.Combine(tempDirectory.Path, "first.bin");
        string blockedPath = Path.Combine(tempDirectory.Path, "blocked.bin");
        File.WriteAllBytes(firstPath, [0xaa]);
        Directory.CreateDirectory(blockedPath);

        Assert.ThrowsAny<IOException>(() => AtomicFileWriter.WriteAllBytesBatch(
        [
            new AtomicFileWrite(firstPath, [0xbb]),
            new AtomicFileWrite(blockedPath, [0xcc]),
        ]));

        Assert.Equal([0xaa], File.ReadAllBytes(firstPath));
        Assert.True(Directory.Exists(blockedPath));
        Assert.Empty(FindTempFiles(tempDirectory.Path));
    }

    private static IReadOnlyList<string> FindTempFiles(string path)
        => Directory.GetFiles(path, "*.hylian-write-*", SearchOption.AllDirectories);

    private sealed class TempDirectory : IDisposable
    {
        private TempDirectory(string path)
        {
            Path = path;
            Directory.CreateDirectory(path);
        }

        public string Path { get; }

        public static TempDirectory Create()
            => new(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HylianGrimoireTests", Guid.NewGuid().ToString("N")));

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
