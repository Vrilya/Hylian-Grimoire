using HylianGrimoire.Codecs;
using HylianGrimoire.Interop;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;
using Microsoft.UI.Xaml;
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
    private int _checkUpdateDepth;
    private int _textCheckUpdateDepth;
    private int _includeCheckUpdateDepth;
    private int _resourceViewUpdateDepth;
    private bool _updatingChecks => _checkUpdateDepth > 0;
    private bool _updatingTextChecks => _textCheckUpdateDepth > 0;
    private bool _updatingIncludeChecks => _includeCheckUpdateDepth > 0;
    private bool _updatingResourceView => _resourceViewUpdateDepth > 0;
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
}
