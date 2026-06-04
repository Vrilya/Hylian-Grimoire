using HylianGrimoire.Glyphs;

namespace HylianGrimoire.Tests;

internal sealed class CharacterProfileStoreTestScope : IDisposable
{
    private CharacterProfileStoreTestScope(string root)
    {
        Root = root;
        Store = CharacterProfileStore.Load(CharacterProfileStoreStorage.CreateIsolated(root));
    }

    public string Root { get; }

    public CharacterProfileStore Store { get; }

    public static CharacterProfileStoreTestScope Create()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "HylianGrimoireTests",
            Guid.NewGuid().ToString("N"));
        return new CharacterProfileStoreTestScope(root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Temp cleanup must not hide the test result.
        }
        catch (UnauthorizedAccessException)
        {
            // Temp cleanup must not hide the test result.
        }
    }
}
