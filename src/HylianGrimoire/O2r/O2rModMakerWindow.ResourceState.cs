namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow
{
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

    private int GetTextureResourceCount()
    {
        if (_textures.Count == 0)
        {
            return _archiveTextureResources.Count;
        }

        return _textures
            .Select(_portProfile.GetTextureResourcePath)
            .Concat(_archiveTextureResources.Select(resource => resource.ResourcePath))
            .ToHashSet(StringComparer.Ordinal)
            .Count;
    }

    private void UpdateResourceViewButtons()
    {
        using IDisposable update = BeginResourceViewUpdate();

        if (_textures.Count == 0)
        {
            _resourceViewMode = ResourceViewMode.Mod;
        }

        RomViewButton.IsChecked = _resourceViewMode == ResourceViewMode.Rom;
        ModViewButton.IsChecked = _resourceViewMode == ResourceViewMode.Mod;
        RomViewButton.IsEnabled = _textures.Count > 0 && IncludeTexturesCheck.IsChecked == true;
        ModViewButton.IsEnabled = IncludeTexturesCheck.IsChecked == true;
    }
}
