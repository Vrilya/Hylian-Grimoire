using System.Collections.ObjectModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using HylianGrimoire.Games;
using HylianGrimoire.Interop;
using HylianGrimoire.Models;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Preview;
using HylianGrimoire.PromptEditor;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using HylianGrimoire.Sessions;
using HylianGrimoire.Soh;
using HylianGrimoire.Textures;
using HylianGrimoire.TitleText;
using HylianGrimoire.Tweaks;

namespace HylianGrimoire;

public sealed partial class MainWindow : Window
{
    private readonly DocumentSession _session = new();
    private readonly CharacterProfileRuntime _characterProfileRuntime = new(CharacterProfileStore.Current);
    private readonly HeaderDocumentWorkflow _headerDocumentWorkflow = new();
    private readonly RomDocumentWorkflow _romDocumentWorkflow = new();
    private readonly TableFileWorkflow _tableFileWorkflow = new();
    private readonly ObservableCollection<MessageItem> _items = new();
    private bool _updating;
    private bool _closeConfirmed;
    private bool _suppressCharacterProfileTextRemap;
    private string _activeCharacterProfileName = CharacterProfileStore.DefaultProfileName;
    private IMessagePreviewWindow? _previewWindow;
    private CharacterProfileWindow? _characterProfileWindow;
    private GlyphRemapWindow? _glyphRemapWindow;
    private TweaksWindow? _tweaksWindow;
    private TitleTextWindow? _titleTextWindow;
    private PromptEditorWindow? _promptEditorWindow;
    private TextureManagerWindow? _textureManagerWindow;
    private SohModMakerWindow? _sohModMakerWindow;

    public MainWindow()
    {
        InitializeComponent();

        UpdateWindowTitle();
        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1260, 700));
        WindowSizeLimits.SetMinimumSize(this, 1025, 700);
        WindowIcon.Apply(this);
        AppWindow.TitleBar.ResetToDefault();
        WindowTheme.Register(this);

        MessageList.ItemsSource = _items;
        TypeCombo.DisplayMemberPath = nameof(MessageTypeItem.Name);
        PositionCombo.DisplayMemberPath = nameof(MessagePositionItem.Name);
        InitializeMajorasMaskMetadataControls();
        SyncActiveCharacterProfileName();
        RefreshAuxiliaryWindowsForLoadedDocument();
        UpdateLanguageMenuState();
        UpdateGlyphProfileMenu();
        _characterProfileRuntime.AutomaticProfileChanged += OnCharacterProfileMenuChanged;
        _characterProfileRuntime.ProfilesChanged += OnCharacterProfileMenuChanged;
        _characterProfileRuntime.SelectionChanged += OnCharacterProfileSelectionChanged;
        _characterProfileRuntime.MappingsChanged += OnCharacterProfileMappingsChanged;
        AppWindow.Closing += OnAppWindowClosing;
        Closed += OnMainWindowClosed;
    }

    private async void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_closeConfirmed || !_session.HasUnsavedChanges)
        {
            return;
        }

        args.Cancel = true;
        if (!await ConfirmCloseWithUnsavedChangesAsync())
        {
            return;
        }

        _closeConfirmed = true;
        Close();
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
        => CloseAuxiliaryWindows();

    private void CloseAuxiliaryWindows()
    {
        _previewWindow?.Close();
        _previewWindow = null;
        _characterProfileWindow?.Close();
        _characterProfileWindow = null;
        _glyphRemapWindow?.Close();
        _glyphRemapWindow = null;
        _tweaksWindow?.Close();
        _tweaksWindow = null;
        _titleTextWindow?.Close();
        _titleTextWindow = null;
        _promptEditorWindow?.Close();
        _promptEditorWindow = null;
        _textureManagerWindow?.Close();
        _textureManagerWindow = null;
        _sohModMakerWindow?.Close();
        _sohModMakerWindow = null;
    }

    private void UpdateWindowTitle()
    {
        Title = _session.Kind switch
        {
            DocumentKind.Project => $"{AppMetadata.MainWindowTitle} - {CurrentGameProfile.DisplayName} Project",
            DocumentKind.DataFiles => $"{AppMetadata.MainWindowTitle} - Data Files",
            DocumentKind.Header => $"{AppMetadata.MainWindowTitle} - Header{GetHeaderTitleSuffix()}",
            DocumentKind.Rom => $"{AppMetadata.MainWindowTitle} - {GetRomTitle()}",
            _ => AppMetadata.MainWindowTitle,
        };
    }

    private string GetRomTitle()
    {
        if (_session.RomData is null)
        {
            return "ROM";
        }

        string romFormat = _session.RomData.WasCompressed ? "Compressed ROM" : "Decompressed ROM";
        string title = $"{_session.RomData.Profile.Name} - {romFormat}";
        if (_session.RomData.ActiveSection == RomMessageSection.Credits)
        {
            title += " - Credits";
        }
        else if (_session.RomData.Profile.Capabilities.SupportsMultipleMessageBanks)
        {
            IReadOnlyList<MessageBankProfile> banks = _session.RomData.Profile.GameProfile.MessageBankLayout.GetEditableBanks(_session.RomData.Profile);
            title += _session.RomData.ActiveMessageBankIndex >= 0 && _session.RomData.ActiveMessageBankIndex < banks.Count
                ? $" - {banks[_session.RomData.ActiveMessageBankIndex].Name}"
                : $" - Language {_session.RomData.ActiveMessageBankIndex + 1}";
        }

        return title;
    }

    private void OnCharacterProfileMenuChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateGlyphProfileMenu();
        });
    }

    private string GetHeaderTitleSuffix()
        => _session.HeaderLanguageEntries is not null && _session.HeaderLanguageEntries.Count > 1
            ? $" - {GetHeaderLanguageName(_session.ActiveHeaderLanguageIndex)}"
            : string.Empty;

    private void OnCharacterProfileSelectionChanged(object? sender, CharacterProfileSelectionChangedEventArgs e)
    {
        bool suppressTextRemap = _suppressCharacterProfileTextRemap;
        DispatcherQueue.TryEnqueue(() =>
        {
            if (suppressTextRemap)
            {
                _activeCharacterProfileName = e.SelectedProfileName;
            }
            else
            {
                RemapEditorTextForCharacterProfileChange(e);
            }

            UpdatePreview();
        });
    }

    private void OnCharacterProfileMappingsChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdatePreview();
        });
    }

    private void UpdateGlyphProfileMenu()
    {
        GlyphProfileMenu.Items.Clear();
        AddGlyphProfileMenuItem("Auto", CharacterProfileStore.AutomaticProfileName);
        GlyphProfileMenu.Items.Add(new MenuFlyoutSeparator());
        AddGlyphProfileMenuItem("Default", CharacterProfileStore.DefaultProfileName);

        foreach (string profileName in _characterProfileRuntime.NamedProfileNames)
        {
            AddGlyphProfileMenuItem(profileName, profileName);
        }
    }

    private void AddGlyphProfileMenuItem(string text, string profileName)
    {
        var item = new ToggleMenuFlyoutItem
        {
            Text = text,
            IsChecked = _characterProfileRuntime.AutomaticProfileNameSetting == profileName,
        };
        item.Click += (_, _) =>
        {
            _characterProfileRuntime.SetAutomaticProfile(profileName);
            UpdateGlyphProfileMenu();
        };
        GlyphProfileMenu.Items.Add(item);
    }

    private void SetActiveCharacterProfileGame(GameKind gameKind)
    {
        _suppressCharacterProfileTextRemap = true;
        try
        {
            _characterProfileRuntime.SetActiveGame(gameKind);
            SyncActiveCharacterProfileName();
        }
        finally
        {
            _suppressCharacterProfileTextRemap = false;
        }
    }

    private void ClearCustomGlyphProfileSelection()
    {
        _characterProfileRuntime.ClearCustomGlyphs();
        SyncActiveCharacterProfileName();
    }

    private void SyncActiveCharacterProfileName()
    {
        _activeCharacterProfileName = _characterProfileRuntime.SelectedProfileName;
    }

    private GameProfile? ActiveGameProfile => _session.ActiveGameProfile;

    private bool HasActiveProject => _session.HasActiveProject;

    private GameProfile CurrentGameProfile => _session.CurrentGameProfile;

}
