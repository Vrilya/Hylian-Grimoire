using HylianGrimoire.Games;

namespace HylianGrimoire.Preview;

internal sealed class PreviewAssetResolver
{
    private readonly string _assetRoot;

    public PreviewAssetResolver(GameKind gameKind)
    {
        _assetRoot = Path.Combine(AppContext.BaseDirectory, GameProfiles.Get(gameKind).Assets.PreviewRoot);
    }

    public string Resolve(string relativePath)
    {
        return Path.Combine(_assetRoot, relativePath.Replace('\\', Path.DirectorySeparatorChar));
    }

    public string ResolveMissing(string fileName)
    {
        return Path.Combine(_assetRoot, "__missing__", fileName);
    }
}
