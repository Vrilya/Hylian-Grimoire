using HylianGrimoire.Textures;
using Microsoft.UI.Xaml;

namespace HylianGrimoire.Soh;

public sealed partial class SohModMakerWindow
{
    private async void OnLoadExistingMod(object sender, RoutedEventArgs e)
    {
        string? path = await PickOpenO2rAsync();
        if (path is null)
        {
            return;
        }

        try
        {
            _existingEntries = SohO2rArchiveWriter.ReadEntries(path);
            IReadOnlySet<string> resources = _existingEntries.Keys.ToHashSet(StringComparer.Ordinal);
            _selectedResources.Clear();
            _selectedTextResources.Clear();
            _archiveTextureResources = BuildArchiveTextureResources(_existingEntries, _textures, _romData?.DecompressedRom);
            foreach (SohArchiveTextureResource resource in _archiveTextureResources)
            {
                _selectedResources.Add(resource.ResourcePath);
            }

            foreach (TextureDefinition texture in _textures)
            {
                string resourcePath = SohResourcePacker.GetTextureResourcePath(texture);
                if (resources.Contains(resourcePath))
                {
                    _selectedResources.Add(resourcePath);
                }
            }

            foreach (SohTextResourceItem textResource in _textResources)
            {
                if (resources.Contains(textResource.ResourcePath))
                {
                    _selectedTextResources.Add(textResource.ResourcePath);
                }
            }

            _resourceViewMode = _textures.Count > 0 ? ResourceViewMode.Rom : ResourceViewMode.Mod;
            SetIncludeChecks(hasText: _textResources.Count > 0, hasTextures: _selectedResources.Count > 0);
            PopulateTextureTree();
            RefreshVisibleCheckStates();
            RefreshTextCheckStates();
            _existingModPath = path;
            _hasWorkspaceChanges = false;
            UpdateResourceViewButtons();
            SetEnabled(true, hasTextureResources: GetTextureResourceCount() > 0);
            UpdateWorkspaceSummary();
            StatusText.Text = $"Loaded {Path.GetFileName(path)}. {_selectedResources.Count} textures and {_selectedTextResources.Count} text resources selected.";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to load .o2r", ex.Message);
        }
    }

    private void SetIncludeChecks(bool hasText, bool hasTextures)
    {
        _updatingIncludeChecks = true;
        try
        {
            IncludeTextCheck.IsChecked = hasText;
            IncludeTexturesCheck.IsChecked = hasTextures;
        }
        finally
        {
            _updatingIncludeChecks = false;
        }
    }
}
