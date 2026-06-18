using System.Security.Cryptography;
using System.Text;

namespace HylianGrimoire.Preview;

internal sealed class PreviewBitmapCache
{
    private readonly string _cacheRoot;

    public PreviewBitmapCache(string gameKey)
    {
        _cacheRoot = Path.Combine(Path.GetTempPath(), "HylianGrimoirePreviewCache", gameKey);
    }

    public void EnsureDirectory()
    {
        Directory.CreateDirectory(_cacheRoot);
    }

    public string GetPath(string key)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        string name = Convert.ToHexString(hash)[..16].ToLowerInvariant();
        return Path.Combine(_cacheRoot, $"{name}.png");
    }

    public void ClearTemporaryFiles()
    {
        try
        {
            Directory.Delete(_cacheRoot, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
