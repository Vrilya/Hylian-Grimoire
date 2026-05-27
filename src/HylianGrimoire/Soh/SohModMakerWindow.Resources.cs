using HylianGrimoire.Textures;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.Soh;

public sealed partial class SohModMakerWindow
{
    private void OnTextureTreeExpanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        if (GetFolderItem(args.Node) is not { } folder || folder.IsPopulated)
        {
            return;
        }

        args.Node.Children.Clear();
        foreach (TextureFolderItem childFolder in folder.Folders.Values.OrderBy(child => child.Name))
        {
            args.Node.Children.Add(CreateFolderNode(childFolder, isExpanded: false));
        }

        foreach (TextureDefinition texture in folder.Textures.OrderBy(texture => texture.Name))
        {
            args.Node.Children.Add(new TreeViewNode
            {
                Content = new TextureListItem(texture),
            });
        }

        foreach (SohArchiveTextureResource texture in folder.ArchiveTextures.OrderBy(texture => texture.Name))
        {
            args.Node.Children.Add(new TreeViewNode
            {
                Content = new ArchiveTextureListItem(texture),
            });
        }

        folder.IsPopulated = true;
        RefreshVisibleCheckStates();
    }

    private void OnTextureSelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs e)
    {
        if (GetSelectedTextureItem() is not { } item)
        {
            if (GetSelectedArchiveTextureItem() is { } archiveItem)
            {
                UpdateArchiveTexturePreview(archiveItem.Resource);
            }
            else
            {
                ClearPreview();
            }

            return;
        }

        if (_romData is null)
        {
            ClearPreview();
            string resourcePath = SohResourcePacker.GetTextureResourcePath(item.Texture);
            DetailsText.Text =
                $"{resourcePath}\n" +
                "Texture preview requires a loaded ROM.\n" +
                (_selectedResources.Contains(resourcePath) ? "Included in mod." : "Not included.");
            return;
        }

        try
        {
            UpdatePreviewImage(item.Texture);
            int byteLength = TextureCodec.GetByteLength(item.Texture.Width, item.Texture.Height, item.Texture.Format);
            string resourcePath = SohResourcePacker.GetTextureResourcePath(item.Texture);
            DetailsText.Text =
                $"{resourcePath}\n" +
                $"0x{item.Texture.RomAddress:X}  {item.Texture.Width}x{item.Texture.Height}  {item.Texture.Format}  {byteLength} bytes\n" +
                (_selectedResources.Contains(resourcePath) ? "Included in mod." : "Not included.");
        }
        catch (Exception ex)
        {
            ClearPreview();
            DetailsText.Text = ex.Message;
        }
    }

    private void OnResourceViewToggled(object sender, RoutedEventArgs e)
    {
        if (_updatingResourceView)
        {
            return;
        }

        _resourceViewMode = ReferenceEquals(sender, RomViewButton) ? ResourceViewMode.Rom : ResourceViewMode.Mod;
        UpdateResourceViewButtons();
        PopulateTextureTree();
        DetailsText.Text = _resourceViewMode == ResourceViewMode.Rom
            ? $"{_textures.Count} ROM textures available."
            : $"{_selectedResources.Count} .o2r texture resources.";
    }

    private void OnIncludeTextChanged(object sender, RoutedEventArgs e)
    {
        if (_updatingIncludeChecks)
        {
            return;
        }

        RefreshTextCheckStates();
        SetEnabled(
            _textResources.Count > 0 || GetTextureResourceCount() > 0 || _existingEntries.Count > 0,
            hasTextureResources: GetTextureResourceCount() > 0);
    }

    private void OnIncludeTexturesChanged(object sender, RoutedEventArgs e)
    {
        if (_updatingIncludeChecks)
        {
            return;
        }

        TextureTree.IsEnabled = GetTextureResourceCount() > 0 && IncludeTexturesCheck.IsChecked == true;
        UpdateResourceViewButtons();
    }

    private void OnTreeItemCheckClicked(object sender, RoutedEventArgs e)
    {
        if (_updatingChecks || sender is not CheckBox checkBox)
        {
            return;
        }

        object? itemContext = GetTreeItemContext(checkBox);
        bool shouldSelect = checkBox.IsChecked == true;
        if (itemContext is TextureFolderItem folder)
        {
            foreach (string resourcePath in folder.GetAllResourcePaths())
            {
                if (shouldSelect)
                {
                    _selectedResources.Add(resourcePath);
                }
                else
                {
                    _selectedResources.Remove(resourcePath);
                }
            }
        }
        else if (itemContext is TextureListItem item)
        {
            string resourcePath = SohResourcePacker.GetTextureResourcePath(item.Texture);
            if (shouldSelect)
            {
                _selectedResources.Add(resourcePath);
            }
            else
            {
                _selectedResources.Remove(resourcePath);
            }
        }
        else if (itemContext is ArchiveTextureListItem archiveItem)
        {
            if (shouldSelect)
            {
                _selectedResources.Add(archiveItem.ResourcePath);
            }
            else
            {
                _selectedResources.Remove(archiveItem.ResourcePath);
            }
        }

        if (_resourceViewMode == ResourceViewMode.Mod && !shouldSelect)
        {
            PopulateTextureTree();
        }
        else
        {
            RefreshVisibleCheckStates();
        }

        MarkWorkspaceChanged();
        UpdateWorkspaceSummary();
        RefreshSelectedDetails();
    }

    private void RefreshVisibleCheckStates()
    {
        _updatingChecks = true;
        try
        {
            foreach (TreeViewNode node in TextureTree.RootNodes)
            {
                RefreshNodeCheckState(node);
            }
        }
        finally
        {
            _updatingChecks = false;
        }
    }

    private void PopulateTextureTree()
    {
        TextureTree.RootNodes.Clear();
        HashSet<string> selectedArchiveResourcePaths = _archiveTextureResources
            .Where(resource => _selectedResources.Contains(resource.ResourcePath))
            .Select(resource => resource.ResourcePath)
            .ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<TextureDefinition> visibleTextures = _resourceViewMode == ResourceViewMode.Rom
            ? _textures
            : _textures
                .Where(IsTextureSelected)
                .Where(texture => !selectedArchiveResourcePaths.Contains(SohResourcePacker.GetTextureResourcePath(texture)))
                .ToList();
        IReadOnlyList<SohArchiveTextureResource> visibleArchiveTextures = _resourceViewMode == ResourceViewMode.Mod
            ? _archiveTextureResources
                .Where(resource => _selectedResources.Contains(resource.ResourcePath))
                .ToList()
            : [];

        foreach (TextureFolderItem folder in BuildTextureTree(visibleTextures, visibleArchiveTextures))
        {
            TextureTree.RootNodes.Add(CreateFolderNode(folder, isExpanded: false));
        }

        RefreshVisibleCheckStates();
    }

    private static TreeViewNode CreateFolderNode(TextureFolderItem folder, bool isExpanded)
        => new()
        {
            Content = folder,
            IsExpanded = isExpanded,
            HasUnrealizedChildren = folder.Folders.Count > 0 || folder.Textures.Count > 0 || folder.ArchiveTextures.Count > 0,
        };

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

    private void RefreshSelectedDetails()
    {
        if (GetSelectedTextureItem() is not null)
        {
            OnTextureSelectionChanged(TextureTree, null!);
        }
        else if (GetSelectedArchiveTextureItem() is not null)
        {
            OnTextureSelectionChanged(TextureTree, null!);
        }
    }

    private void UpdateWorkspaceSummary()
    {
        if (ExistingModText is null || SelectionCountText is null)
        {
            return;
        }

        ExistingModText.Text = _existingModPath is null
            ? "No existing mod loaded."
            : Path.GetFileName(_existingModPath);
        ModStateText.Text = GetWorkspaceStateText();
        CreateButton.Content = _existingModPath is null ? "Create .o2r" : "Save .o2r as";
        SelectionCountText.Text =
            $"{_selectedResources.Count} of {GetTextureResourceCount()} textures selected.\n" +
            $"{_selectedTextResources.Count} of {_textResources.Count} text resources selected.";
    }

    private string GetWorkspaceStateText()
    {
        if (_existingModPath is null)
        {
            return _hasWorkspaceChanges ? "New mod workspace with unsaved changes." : "New mod workspace.";
        }

        return _hasWorkspaceChanges ? "Loaded mod with unsaved changes." : "Loaded mod.";
    }

    private void MarkWorkspaceChanged()
    {
        _hasWorkspaceChanges = true;
    }

    private bool IsTextureSelected(TextureDefinition texture)
        => _selectedResources.Contains(SohResourcePacker.GetTextureResourcePath(texture));

    private static TextureFolderItem? GetFolderItem(TreeViewNode node)
        => node.Content as TextureFolderItem;

    private TextureListItem? GetSelectedTextureItem()
        => TextureTree.SelectedNode?.Content as TextureListItem;

    private ArchiveTextureListItem? GetSelectedArchiveTextureItem()
        => TextureTree.SelectedNode?.Content as ArchiveTextureListItem;

    private static object? GetTreeItemContext(CheckBox checkBox)
        => checkBox.DataContext is TreeViewNode node ? node.Content : checkBox.DataContext;

    private int GetTextureResourceCount()
    {
        if (_textures.Count == 0)
        {
            return _archiveTextureResources.Count;
        }

        return _textures
            .Select(SohResourcePacker.GetTextureResourcePath)
            .Concat(_archiveTextureResources.Select(resource => resource.ResourcePath))
            .ToHashSet(StringComparer.Ordinal)
            .Count;
    }

    private void UpdateResourceViewButtons()
    {
        _updatingResourceView = true;
        try
        {
            if (_textures.Count == 0)
            {
                _resourceViewMode = ResourceViewMode.Mod;
            }

            RomViewButton.IsChecked = _resourceViewMode == ResourceViewMode.Rom;
            ModViewButton.IsChecked = _resourceViewMode == ResourceViewMode.Mod;
            RomViewButton.IsEnabled = _textures.Count > 0 && IncludeTexturesCheck.IsChecked == true;
            ModViewButton.IsEnabled = IncludeTexturesCheck.IsChecked == true;
        }
        finally
        {
            _updatingResourceView = false;
        }
    }

    private static IReadOnlyList<TextureFolderItem> BuildTextureTree(
        IReadOnlyList<TextureDefinition> textures,
        IReadOnlyList<SohArchiveTextureResource> archiveTextures)
    {
        var root = new TextureFolderItem(string.Empty);
        foreach (TextureDefinition texture in textures)
        {
            TextureFolderItem folder = root;
            foreach (string part in texture.Group.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries))
            {
                folder = folder.GetOrAddFolder(part);
            }

            folder.Textures.Add(texture);
        }

        foreach (SohArchiveTextureResource texture in archiveTextures)
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
