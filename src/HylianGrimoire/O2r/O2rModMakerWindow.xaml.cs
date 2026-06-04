using HylianGrimoire.Codecs;
using HylianGrimoire.Interop;
using HylianGrimoire.Models;
using HylianGrimoire.O2r;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow : Window
{
    private O2rModPortProfile _portProfile;
    private readonly Func<List<MessageEntry>> _getCurrentEntries;
    private readonly Func<IReadOnlyDictionary<int, List<MessageEntry>>> _getCurrentTextLanguages;
    private readonly Action<string> _onChanged;
    private readonly HashSet<string> _selectedResources = new(StringComparer.Ordinal);
    private readonly HashSet<string> _selectedTextResources = new(StringComparer.Ordinal);

    private RomMessageData? _romData;
    private IReadOnlyList<TextureDefinition> _textures = [];
    private IReadOnlyList<O2rArchiveTextureResource> _archiveTextureResources = [];
    private IReadOnlyList<O2rTextResourceDefinition> _textResources = [];
    private IReadOnlyDictionary<string, byte[]> _existingEntries = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
    private string? _existingModPath;
    private ResourceViewMode _resourceViewMode = ResourceViewMode.Mod;
    private bool _hasWorkspaceChanges;
    private bool _updatingChecks;
    private bool _updatingTextChecks;
    private bool _updatingIncludeChecks;
    private bool _updatingResourceView;
    private int _previewCounter;
    private MessageEncodingProfile _encodingProfile;

    public O2rModMakerWindow(
        O2rModPortProfile portProfile,
        RomMessageData? romData,
        Func<List<MessageEntry>> getCurrentEntries,
        Func<IReadOnlyDictionary<int, List<MessageEntry>>> getCurrentTextLanguages,
        MessageEncodingProfile encodingProfile,
        Action<string> onChanged)
    {
        _portProfile = portProfile;
        InitializeComponent();
        _getCurrentEntries = getCurrentEntries;
        _getCurrentTextLanguages = getCurrentTextLanguages;
        _encodingProfile = encodingProfile;
        _onChanged = onChanged;

        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1550, 1040));
        WindowSizeLimits.SetMinimumSize(this, 1220, 740);
        WindowIcon.Apply(this);
        AppWindow.TitleBar.ResetToDefault();
        WindowTheme.Register(this);

        SetContext(portProfile, romData, encodingProfile);
    }

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
        SetIncludeChecks(hasText: _getCurrentEntries().Count > 0, hasTextures: false);

        if (_romData is null)
        {
            _textResources = _portProfile.GetCurrentDocumentTextResources(_getCurrentTextLanguages());
            PopulateTextResources();
            ProfileText.Text = _textResources.Count > 0 ? "Current text document" : "No document loaded.";
            DetailsText.Text = "No ROM texture catalog is available.";
            StatusText.Text = _textResources.Count > 0
                ? $"Create a text-only .o2r for {_portProfile.DisplayName} or load an existing .o2r to preserve its textures."
                : $"Load text or a supported ROM to create an {_portProfile.DisplayName} mod.";
            UpdateResourceViewButtons();
            SetEnabled(_textResources.Count > 0, hasTextureResources: false);
            UpdateWorkspaceSummary();
            return;
        }

        ProfileText.Text = _romData.Profile.Name;
        if (!TextureCatalog.TryGetTextures(_romData.Profile, out IReadOnlyList<TextureDefinition>? textures))
        {
            _textResources = _portProfile.GetRomTextResources(_romData);
            PopulateTextResources();
            DetailsText.Text = "No texture catalog is available for this ROM.";
            StatusText.Text = "This ROM can create text resources, but has no texture catalog.";
            SetIncludeChecks(hasText: _textResources.Count > 0, hasTextures: false);
            UpdateResourceViewButtons();
            SetEnabled(_textResources.Count > 0, hasTextureResources: false);
            UpdateWorkspaceSummary();
            return;
        }

        _textures = textures
            .Where(_portProfile.SupportsTextureResource)
            .ToList();
        _textResources = _portProfile.GetRomTextResources(_romData);
        PopulateTextureTree();
        PopulateTextResources();

        SetIncludeChecks(hasText: _textResources.Count > 0, hasTextures: _textures.Count > 0);
        UpdateResourceViewButtons();
        SetEnabled(true, hasTextureResources: _textures.Count > 0);
        DetailsText.Text = $"{_textures.Count} textures available.";
        StatusText.Text = "Edit Existing Mod opens an .o2r and checks matching resources automatically.";
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
