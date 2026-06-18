namespace HylianGrimoire.Tests;

internal sealed class TempDirectory : IDisposable
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
