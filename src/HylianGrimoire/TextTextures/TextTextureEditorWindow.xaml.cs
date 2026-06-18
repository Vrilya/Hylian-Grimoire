using System.Drawing;
using HylianGrimoire.Interop;
using HylianGrimoire.Preview;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow : Window
{
    private const int InitialWindowWidth = 1551;
    private const int InitialWindowHeight = 874;
    private const int MinimumWindowWidth = 1551;
    private const int MinimumWindowHeight = 874;

    private readonly Action<TextureManagerChange> _onChanged;
    private readonly PreviewImageSlot _generatedSlot = new();
    private readonly PreviewImageSlot _romSlot = new();
    private readonly string _previewRoot;

    private RomMessageData? _romData;
    private TextTextureKind _selectedTextureKind = TextTextureKind.ItemNames;
    private IReadOnlyList<TextTextureKindDescriptor> _availableTextureKinds = TextureKinds;
    private ItemNameTextureRenderSettings _itemSettings = new();
    private ItemNameTextureRenderSettings _mapNameSettings = new(HorizontalScale: DefaultMapNameScale);
    private MapPositionNameTextureRenderSettings _mapPositionNameSettings = new();
    private readonly Dictionary<string, MapPositionNameTextureRenderSettings> _mapPositionNameSettingsByTexture = new(StringComparer.Ordinal);
    private ItemNameTextureRenderSettings _majorasMaskItemSettings = new();
    private ItemNameTextureRenderSettings _majorasMaskMapNameSettings = new();
    private ItemNameTextureRenderSettings _majorasMaskPausePromptBaseSettings = new();
    private CompactTextTextureRenderSettings _promptSettings = new();
    private readonly Dictionary<string, CompactTextUiSettings> _pausePromptChoiceSettings = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DungeonMapNameUiSettings> _dungeonMapNameSettings = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MajorasMaskDungeonMapNameUiSettings> _majorasMaskDungeonMapNameSettings = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FileSelectUiSettings> _fileSelectSettings = new(StringComparer.Ordinal);
    private PlaceTitleCardTextureRenderSettings _placeSettings = new();
    private BossTitleCardTextureRenderSettings _bossSettings = new();
    private GameOverTextureRenderSettings _gameOverSettings = new();
    private ContinuePlayingTextureRenderSettings _continuePlayingSettings = new();
    private GameOverTextureRenderSettings _majorasMaskGameOverSettings = new();
    private ContinuePlayingTextureRenderSettings _majorasMaskContinuePlayingSettings = new();
    private PauseHeaderTextureRenderSettings _pauseSettings = new();
    private EndTitleTextureRenderSettings _endTitleSettings = new();
    private Bitmap? _lastGenerated;
    private TextTexturePreviewSourceSignature? _lastGeneratedSourceSignature;
    private int _previewCounter;
    private bool _showPauseOriginalColors = true;
    private int _controlUpdateDepth;
    private bool _updatingControls => _controlUpdateDepth > 0;

    public TextTextureEditorWindow(RomMessageData? romData, Action<TextureManagerChange> onChanged)
    {
        InitializeComponent();
        _onChanged = onChanged;
        _previewRoot = Path.Combine(Path.GetTempPath(), "HylianGrimoireTextTexturePreview", Guid.NewGuid().ToString("N"));

        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(InitialWindowWidth, InitialWindowHeight));
        WindowSizeLimits.SetMinimumSize(this, MinimumWindowWidth, MinimumWindowHeight);
        WindowIcon.Apply(this);
        AppWindow.TitleBar.ResetToDefault();
        WindowTheme.Register(this);

        InitializeTextureKindControls();
        AddPreviewSlots();
        SetRomData(romData);
        Closed += OnClosed;
    }

    public void SetRomData(RomMessageData? romData)
    {
        _romData = romData;
        RefreshTextureKindOptions();
        LoadTargets();
        if (GetSelectedTarget() is not null)
        {
            SetControlsFromCurrentSettings();
        }

        UpdateTextControlVisibility();
        UpdateEnabledState();
        RefreshPreview();
    }

    private void InitializeTextureKindControls()
    {
        using IDisposable controlUpdate = BeginControlUpdate();
        RefreshTextureKindOptions();
        CenterCheck.IsChecked = true;
        PauseOriginalColorsCheck.IsChecked = true;
        XNudgeBox.Value = DefaultHorizontalPosition;
        ItemWidthScaleBox.Value = _itemSettings.HorizontalScale;
        UpdateTextControlVisibility();
    }

    private void AddPreviewSlots()
    {
        SetPreviewSlotSize();
        GeneratedPreviewHost.Children.Add(_generatedSlot.Root);
        RomPreviewHost.Children.Add(_romSlot.Root);
    }

    private void SetPreviewSlotSize()
    {
        double width = GetCanvasWidth() * GetPreviewScale();
        double height = GetCanvasHeight() * GetPreviewScale();
        _generatedSlot.SetSize(width, height);
        _romSlot.SetSize(width, height);
    }

    private void OnTextureKindChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingControls)
        {
            return;
        }

        if (TextureKindCombo.SelectedItem is not TextTextureKindDescriptor option)
        {
            return;
        }

        _selectedTextureKind = option.Kind;
        LoadTargets();
        SetControlsFromCurrentSettings();
        UpdateTextControlVisibility();
        UpdateEnabledState();
        SetPreviewSlotSize();
        RefreshPreview();
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_updatingControls)
        {
            RefreshPreview();
        }
    }

    private void OnBossTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_updatingControls)
        {
            RefreshPreview();
        }
    }

    private void OnTargetChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updatingControls)
        {
            if (GetSelectedTarget() is TextTextureTargetItem item)
            {
                using IDisposable controlUpdate = BeginControlUpdate();
                SetTextFromTarget(item);
                UpdateTextControlVisibility();
                SetControlsFromCurrentSettings();
            }

            UpdateEnabledState();
            SetPreviewSlotSize();
            RefreshPreview();
        }
    }

    private void OnRenderSettingChanged(object sender, NumberBoxValueChangedEventArgs e)
        => UpdateRenderSettings();

    private void OnRenderSettingChanged(object sender, RoutedEventArgs e)
        => UpdateRenderSettings();

    private IDisposable BeginControlUpdate()
        => new ControlUpdateScope(this);

    private sealed class ControlUpdateScope : IDisposable
    {
        private TextTextureEditorWindow? _owner;

        public ControlUpdateScope(TextTextureEditorWindow owner)
        {
            _owner = owner;
            owner._controlUpdateDepth++;
        }

        public void Dispose()
        {
            if (_owner is null)
            {
                return;
            }

            _owner._controlUpdateDepth = Math.Max(0, _owner._controlUpdateDepth - 1);
            _owner = null;
        }
    }
}
