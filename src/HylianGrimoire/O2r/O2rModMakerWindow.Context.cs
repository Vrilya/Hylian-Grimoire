using HylianGrimoire.Codecs;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow
{
    public void SetContext(
        O2rModPortProfile portProfile,
        RomMessageData? romData,
        MessageEncodingProfile? encodingProfile = null)
    {
        _portProfile = portProfile;
        Title = $"Hylian Grimoire - {_portProfile.ToolTitle}";
        ToolTitleText.Text = _portProfile.ToolTitle;
        SetRomData(romData, encodingProfile);
    }

    private void SetRomData(RomMessageData? romData, MessageEncodingProfile? encodingProfile = null)
    {
        _romData = romData;
        _encodingProfile = encodingProfile ?? romData?.Profile.GameProfile.EncodingProfile ?? _encodingProfile;
        ClearLoadedResources();

        if (_romData is null)
        {
            LoadCurrentDocumentContext();
            return;
        }

        LoadRomContext();
    }

    private void ClearLoadedResources()
    {
        _selectedResources.Clear();
        _selectedTextResources.Clear();
        _textures = [];
        _archiveTextureResources = [];
        _textResources = [];
        _existingModPath = null;
        _existingEntries = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        _resourceViewMode = _romData is null ? ResourceViewMode.Mod : ResourceViewMode.Rom;
        _hasWorkspaceChanges = false;
        TextureTree.RootNodes.Clear();
        TextResourcePanel.Children.Clear();
        ClearPreview();
    }

    private void LoadCurrentDocumentContext()
    {
        _textResources = _portProfile.GetCurrentDocumentTextResources(_getCurrentTextLanguages());
        PopulateTextResources();
        SetIncludeChecks(hasText: _textResources.Count > 0, hasTextures: false);
        ProfileText.Text = _textResources.Count > 0 ? "Current text document" : "No document loaded.";
        DetailsText.Text = "No ROM texture catalog is available.";
        StatusText.Text = _textResources.Count > 0
            ? $"Create a text-only .o2r for {_portProfile.DisplayName} or load an existing .o2r to preserve its textures."
            : $"Load text or a supported ROM to create an {_portProfile.DisplayName} mod.";
        UpdateResourceViewButtons();
        SetEnabled(_textResources.Count > 0, hasTextureResources: false);
        UpdateWorkspaceSummary();
    }

    private void LoadRomContext()
    {
        RomMessageData romData = _romData
            ?? throw new InvalidOperationException("ROM context was not initialized.");
        ProfileText.Text = romData.Profile.Name;
        if (!TextureCatalog.TryGetTextures(romData.Profile, out IReadOnlyList<TextureDefinition>? textures))
        {
            LoadRomContextWithoutTextures(romData);
            return;
        }

        _textures = textures
            .Where(_portProfile.SupportsTextureResource)
            .ToList();
        _textResources = _portProfile.GetRomTextResources(romData);
        PopulateTextureTree();
        PopulateTextResources();

        SetIncludeChecks(hasText: _textResources.Count > 0, hasTextures: _textures.Count > 0);
        UpdateResourceViewButtons();
        SetEnabled(true, hasTextureResources: _textures.Count > 0);
        StatusText.Text = "Edit Existing Mod opens an .o2r and checks matching resources automatically.";
        UpdateWorkspaceSummary();
    }

    private void LoadRomContextWithoutTextures(RomMessageData romData)
    {
        _textResources = _portProfile.GetRomTextResources(romData);
        PopulateTextResources();
        DetailsText.Text = "No texture catalog is available for this ROM.";
        StatusText.Text = "This ROM can create text resources, but has no texture catalog.";
        SetIncludeChecks(hasText: _textResources.Count > 0, hasTextures: false);
        UpdateResourceViewButtons();
        SetEnabled(_textResources.Count > 0, hasTextureResources: false);
        UpdateWorkspaceSummary();
    }

    private void SetEnabled(bool enabled, bool hasTextureResources)
    {
        LoadExistingButton.IsEnabled = enabled;
        CreateButton.IsEnabled = enabled;
        IncludeTextCheck.IsEnabled = _textResources.Count > 0;
        IncludeTexturesCheck.IsEnabled = hasTextureResources;
        TextureTree.IsEnabled = enabled && hasTextureResources && IncludeTexturesCheck.IsChecked == true;
        RomViewButton.IsEnabled = enabled && _textures.Count > 0 && IncludeTexturesCheck.IsChecked == true;
        ModViewButton.IsEnabled = enabled && IncludeTexturesCheck.IsChecked == true;
        foreach (CheckBox checkBox in TextResourcePanel.Children.OfType<CheckBox>())
        {
            checkBox.IsEnabled = enabled && IncludeTextCheck.IsChecked == true;
        }
    }
}
