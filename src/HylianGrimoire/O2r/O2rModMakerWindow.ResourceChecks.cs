using HylianGrimoire.Textures;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow
{
    private bool TryApplyTreeItemSelection(CheckBox checkBox, out bool shouldSelect)
    {
        shouldSelect = checkBox.IsChecked == true;
        object? itemContext = GetTreeItemContext(checkBox);

        if (itemContext is TextureFolderItem folder)
        {
            foreach (string resourcePath in folder.GetAllResourcePaths())
            {
                SetTextureResourceSelected(resourcePath, shouldSelect);
            }

            return true;
        }

        if (itemContext is TextureListItem item)
        {
            SetTextureResourceSelected(_portProfile.GetTextureResourcePath(item.Texture), shouldSelect);
            return true;
        }

        if (itemContext is ArchiveTextureListItem archiveItem)
        {
            SetTextureResourceSelected(archiveItem.ResourcePath, shouldSelect);
            return true;
        }

        return false;
    }

    private void SetTextureResourceSelected(string resourcePath, bool selected)
    {
        if (selected)
        {
            _selectedResources.Add(resourcePath);
        }
        else
        {
            _selectedResources.Remove(resourcePath);
        }
    }

    private void RefreshVisibleCheckStates()
    {
        using IDisposable update = BeginCheckUpdate();

        foreach (TreeViewNode node in TextureTree.RootNodes)
        {
            RefreshNodeCheckState(node);
        }
    }

    private void RefreshNodeCheckState(TreeViewNode node)
    {
        if (node.Content is TextureListItem item)
        {
            item.IsChecked = IsTextureSelected(item.Texture);
        }
        else if (node.Content is ArchiveTextureListItem archiveItem)
        {
            archiveItem.IsChecked = _selectedResources.Contains(archiveItem.ResourcePath);
        }
        else if (node.Content is TextureFolderItem folder)
        {
            int selectedCount = folder.GetAllResourcePaths().Count(_selectedResources.Contains);
            folder.IsChecked = selectedCount > 0;
        }

        foreach (TreeViewNode child in node.Children)
        {
            RefreshNodeCheckState(child);
        }
    }

    private bool IsTextureSelected(TextureDefinition texture)
        => _selectedResources.Contains(_portProfile.GetTextureResourcePath(texture));

    private static object? GetTreeItemContext(CheckBox checkBox)
        => checkBox.DataContext is TreeViewNode node ? node.Content : checkBox.DataContext;
}
