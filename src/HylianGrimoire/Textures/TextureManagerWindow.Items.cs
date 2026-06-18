namespace HylianGrimoire.Textures;

public sealed partial class TextureManagerWindow
{
    private sealed class TextureListItem(TextureDefinition texture)
    {
        public TextureDefinition Texture { get; } = texture;

        public string Name => Texture.Name;

        public string Summary => $"{Texture.Group}  {Texture.Width}x{Texture.Height}  {Texture.Format}";

        public override string ToString() => $"{Texture.Name}  {Texture.Width}x{Texture.Height}  {Texture.Format}";
    }

    private sealed class TextureFolderItem(string name)
    {
        public string Name { get; } = name;

        public SortedDictionary<string, TextureFolderItem> Folders { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<TextureDefinition> Textures { get; } = [];

        public int TotalCount { get; private set; }

        public bool IsPopulated { get; set; }

        public TextureFolderItem GetOrAddFolder(string childName)
        {
            if (!Folders.TryGetValue(childName, out TextureFolderItem? folder))
            {
                folder = new TextureFolderItem(childName);
                Folders.Add(childName, folder);
            }

            return folder;
        }

        public int UpdateTotalCount()
        {
            TotalCount = Textures.Count + Folders.Values.Sum(folder => folder.UpdateTotalCount());
            return TotalCount;
        }

        public override string ToString() => $"{Name} ({TotalCount})";
    }
}
