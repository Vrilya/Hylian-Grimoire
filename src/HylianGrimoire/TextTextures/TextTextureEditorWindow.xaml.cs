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
    private ItemNameTextureRenderSettings _itemSettings = new();
    private CompactTextTextureRenderSettings _promptSettings = new();
    private readonly Dictionary<string, CompactTextUiSettings> _pausePromptChoiceSettings = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DungeonMapNameUiSettings> _dungeonMapNameSettings = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FileSelectUiSettings> _fileSelectSettings = new(StringComparer.Ordinal);
    private PlaceTitleCardTextureRenderSettings _placeSettings = new();
    private BossTitleCardTextureRenderSettings _bossSettings = new();
    private GameOverTextureRenderSettings _gameOverSettings = new();
    private ContinuePlayingTextureRenderSettings _continuePlayingSettings = new();
    private PauseHeaderTextureRenderSettings _pauseSettings = new();
    private EndTitleTextureRenderSettings _endTitleSettings = new();
    private Bitmap? _lastGenerated;
    private int _previewCounter;
    private bool _showPauseOriginalColors = true;
    private bool _updatingControls;

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
        LoadTargets();
        UpdateEnabledState();
        RefreshPreview();
    }

    private void InitializeTextureKindControls()
    {
        _updatingControls = true;
        try
        {
            TextureKindCombo.ItemsSource = TextureKinds;
            TextureKindCombo.SelectedItem = TextureKinds[0];
            CenterCheck.IsChecked = true;
            PauseOriginalColorsCheck.IsChecked = true;
            XNudgeBox.Value = DefaultHorizontalPosition;
            ItemWidthScaleBox.Value = _itemSettings.HorizontalScale;
            UpdateTextControlVisibility();
        }
        finally
        {
            _updatingControls = false;
        }
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
                _updatingControls = true;
                try
                {
                    SetTextFromTarget(item);
                    UpdateTextControlVisibility();
                    SetControlsFromCurrentSettings();
                }
                finally
                {
                    _updatingControls = false;
                }
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
}
