using HylianGrimoire.Textures;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow
{
    private void PopulateTextureTree()
    {
        TextureTree.SelectedNode = null;
        TextureTree.RootNodes.Clear();
        HashSet<string> selectedArchiveResourcePaths = _archiveTextureResources
            .Where(resource => _selectedResources.Contains(resource.ResourcePath))
            .Select(resource => resource.ResourcePath)
            .ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<TextureDefinition> visibleTextures = _resourceViewMode == ResourceViewMode.Rom
            ? _textures
            : _textures
                .Where(IsTextureSelected)
                .Where(texture => !selectedArchiveResourcePaths.Contains(_portProfile.GetTextureResourcePath(texture)))
                .ToList();
        IReadOnlyList<O2rArchiveTextureResource> visibleArchiveTextures = _resourceViewMode == ResourceViewMode.Mod
            ? _archiveTextureResources
                .Where(resource => _selectedResources.Contains(resource.ResourcePath))
                .ToList()
            : [];

        foreach (TextureFolderItem folder in BuildTextureTree(visibleTextures, visibleArchiveTextures, _portProfile))
        {
            TextureTree.RootNodes.Add(CreateFolderNode(folder, isExpanded: false));
        }

        RefreshVisibleCheckStates();
        RefreshSelectedDetails();
    }

    private void PopulateExpandedTextureFolder(TreeViewNode node, TextureFolderItem folder)
    {
        node.Children.Clear();
        foreach (TextureFolderItem childFolder in folder.Folders.Values.OrderBy(child => child.Name))
        {
            node.Children.Add(CreateFolderNode(childFolder, isExpanded: false));
        }

        foreach (TextureDefinition texture in folder.Textures.OrderBy(texture => texture.Name))
        {
            node.Children.Add(new TreeViewNode
            {
                Content = new TextureListItem(texture),
            });
        }

        foreach (O2rArchiveTextureResource texture in folder.ArchiveTextures.OrderBy(texture => texture.Name))
        {
            node.Children.Add(new TreeViewNode
            {
                Content = new ArchiveTextureListItem(texture),
            });
        }

        folder.IsPopulated = true;
        RefreshVisibleCheckStates();
    }

    private static TreeViewNode CreateFolderNode(TextureFolderItem folder, bool isExpanded)
        => new()
        {
            Content = folder,
            IsExpanded = isExpanded,
            HasUnrealizedChildren = folder.Folders.Count > 0 || folder.Textures.Count > 0 || folder.ArchiveTextures.Count > 0,
        };

    private static TextureFolderItem? GetFolderItem(TreeViewNode node)
        => node.Content as TextureFolderItem;

    private static IReadOnlyList<TextureFolderItem> BuildTextureTree(
        IReadOnlyList<TextureDefinition> textures,
        IReadOnlyList<O2rArchiveTextureResource> archiveTextures,
        O2rModPortProfile portProfile)
    {
        var root = new TextureFolderItem(string.Empty, portProfile);
        foreach (TextureDefinition texture in textures)
        {
            TextureFolderItem folder = root;
            foreach (string part in texture.Group.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries))
            {
                folder = folder.GetOrAddFolder(part);
            }

            folder.Textures.Add(texture);
        }

        foreach (O2rArchiveTextureResource texture in archiveTextures)
        {
            TextureFolderItem folder = root;
            string[] parts = texture.ResourcePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < Math.Max(0, parts.Length - 1); i++)
            {
                folder = folder.GetOrAddFolder(parts[i]);
            }

            folder.ArchiveTextures.Add(texture);
        }

        root.UpdateTotalCount();
        return [.. root.Folders.Values.OrderBy(folder => folder.Name)];
    }
}
