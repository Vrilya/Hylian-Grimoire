namespace HylianGrimoire.Games;

public sealed record GameAssetPaths(
    string Root,
    string PreviewRoot,
    string TextureCatalogRoot,
    string TitleRoot)
{
    public string GetPath(params string[] parts) =>
        Path.Combine([AppContext.BaseDirectory, Root, .. parts]);
}
