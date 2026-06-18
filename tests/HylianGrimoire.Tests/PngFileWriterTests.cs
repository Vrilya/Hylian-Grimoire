using System.Drawing;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class PngFileWriterTests
{
    [Fact]
    public void SaveWritesPngBytesAtomically()
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        string path = Path.Combine(tempDirectory.Path, "texture.png");
        using var bitmap = new Bitmap(2, 1);
        bitmap.SetPixel(0, 0, Color.Red);
        bitmap.SetPixel(1, 0, Color.Blue);

        PngFileWriter.Save(bitmap, path);

        byte[] bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > PngSignature.Length);
        Assert.True(bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature));
        Assert.Empty(Directory.GetFiles(tempDirectory.Path, "*.hylian-write-*", SearchOption.AllDirectories));
    }

    [Fact]
    public void SaveDirectWritesPngBytesWithoutAtomicTempFiles()
    {
        using TempDirectory tempDirectory = TempDirectory.Create();
        string path = Path.Combine(tempDirectory.Path, "batch", "texture.png");
        using var bitmap = new Bitmap(1, 1);
        bitmap.SetPixel(0, 0, Color.Green);

        PngFileWriter.SaveDirect(bitmap, path);

        byte[] bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > PngSignature.Length);
        Assert.True(bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature));
        Assert.Empty(Directory.GetFiles(tempDirectory.Path, "*.hylian-write-*", SearchOption.AllDirectories));
    }

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
}
