using HylianGrimoire.Interop;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HylianGrimoire.Soh;

public sealed partial class SohModMakerWindow : Window
{
    private readonly Func<List<MessageEntry>> _getCurrentEntries;
    private readonly Func<IReadOnlyDictionary<int, List<MessageEntry>>> _getCurrentTextLanguages;
    private readonly Action<string> _onChanged;
    private readonly HashSet<string> _selectedResources = new(StringComparer.Ordinal);
    private readonly HashSet<string> _selectedTextResources = new(StringComparer.Ordinal);

    private RomMessageData? _romData;
    private IReadOnlyList<TextureDefinition> _textures = [];
    private IReadOnlyList<SohArchiveTextureResource> _archiveTextureResources = [];
    private IReadOnlyList<SohTextResourceItem> _textResources = [];
    private IReadOnlyDictionary<string, byte[]> _existingEntries = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
    private string? _existingModPath;
    private ResourceViewMode _resourceViewMode = ResourceViewMode.Mod;
    private bool _hasWorkspaceChanges;
    private bool _updatingChecks;
    private bool _updatingTextChecks;
    private bool _updatingIncludeChecks;
    private bool _updatingResourceView;
    private int _previewCounter;

    public SohModMakerWindow(
        RomMessageData? romData,
        Func<List<MessageEntry>> getCurrentEntries,
        Func<IReadOnlyDictionary<int, List<MessageEntry>>> getCurrentTextLanguages,
        Action<string> onChanged)
    {
        InitializeComponent();
        _getCurrentEntries = getCurrentEntries;
        _getCurrentTextLanguages = getCurrentTextLanguages;
        _onChanged = onChanged;

        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1550, 1040));
        WindowSizeLimits.SetMinimumSize(this, 1220, 740);
        WindowIcon.Apply(this);
        AppWindow.TitleBar.ResetToDefault();
        WindowTheme.Register(this);

        SetRomData(romData);
    }

    public void SetRomData(RomMessageData? romData)
    {
        _romData = romData;
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
            _textResources = BuildCurrentDocumentTextResources(_getCurrentTextLanguages());
            PopulateTextResources();
            ProfileText.Text = _textResources.Count > 0 ? "Current text document" : "No document loaded.";
            DetailsText.Text = "No ROM texture catalog is available.";
            StatusText.Text = _textResources.Count > 0
                ? "Create a text-only .o2r or load an existing .o2r to preserve its textures."
                : "Load text or a ROM to create a SoH mod.";
            UpdateResourceViewButtons();
            SetEnabled(_textResources.Count > 0, hasTextureResources: false);
            UpdateWorkspaceSummary();
            return;
        }

        ProfileText.Text = _romData.Profile.Name;
        if (!TextureCatalog.TryGetTextures(_romData.Profile, out IReadOnlyList<TextureDefinition>? textures))
        {
            _textResources = BuildTextResources(_romData);
            PopulateTextResources();
            DetailsText.Text = "No texture catalog is available for this ROM.";
            StatusText.Text = "This ROM can create text resources, but has no texture catalog.";
            SetIncludeChecks(hasText: _textResources.Count > 0, hasTextures: false);
            UpdateResourceViewButtons();
            SetEnabled(_textResources.Count > 0, hasTextureResources: false);
            UpdateWorkspaceSummary();
            return;
        }

        _textures = textures;
        _textResources = BuildTextResources(_romData);
        PopulateTextureTree();
        PopulateTextResources();

        SetIncludeChecks(hasText: _textResources.Count > 0, hasTextures: true);
        UpdateResourceViewButtons();
        SetEnabled(true, hasTextureResources: true);
        DetailsText.Text = $"{textures.Count} textures available.";
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
