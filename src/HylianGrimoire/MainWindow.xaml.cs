using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using HylianGrimoire.Interop;
using HylianGrimoire.Models;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Preview;
using HylianGrimoire.PromptEditor;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using HylianGrimoire.TitleText;
using HylianGrimoire.Tweaks;

namespace HylianGrimoire;

public sealed partial class MainWindow : Window
{
    private static readonly string[] PositionNames =
        ["Auto", "Top", "Middle", "Bottom"];

    private readonly ObservableCollection<MessageItem> _items = new();
    private List<MessageEntry> _entries = new();
    private int _currentIdx = -1;
    private bool _updating;
    private DocumentKind _documentKind = DocumentKind.None;
    private string? _tblPath;
    private string? _binPath;
    private string? _headerPath;
    private Dictionary<int, List<MessageEntry>>? _headerLanguageEntries;
    private int _activeHeaderLanguageIndex;
    private string? _romPath;
    private RomMessageData? _romData;
    private string _searchText = string.Empty;
    private string _cleanDocumentFingerprint = string.Empty;
    private bool _hasUnsavedChanges;
    private bool _hasUnsavedRomBankChanges;
    private bool _hasUnsavedHeaderLanguageChanges;
    private bool _closeConfirmed;
    private string _activeCharacterProfileName = CharacterProfileStore.Current.SelectedProfileName;
    private OotPreviewWindow? _previewWindow;
    private CharacterProfileWindow? _characterProfileWindow;
    private GlyphRemapWindow? _glyphRemapWindow;
    private TweaksWindow? _tweaksWindow;
    private TitleTextWindow? _titleTextWindow;
    private PromptEditorWindow? _promptEditorWindow;

    public MainWindow()
    {
        InitializeComponent();

        UpdateWindowTitle();
        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1025, 700));
        WindowSizeLimits.SetMinimumSize(this, 1025, 700);
        WindowIcon.Apply(this);
        AppWindow.TitleBar.ResetToDefault();
        WindowTheme.Register(this);

        MessageList.ItemsSource = _items;
        TypeCombo.ItemsSource = MessageTypeCatalog.Items;
        TypeCombo.DisplayMemberPath = nameof(MessageTypeItem.Name);
        PositionCombo.ItemsSource = PositionNames;
        UpdateLanguageMenuState();
        UpdateRomToolState();
        UpdateGlyphProfileMenu();
        CharacterProfileStore.Current.AutomaticProfileChanged += OnCharacterProfileMenuChanged;
        CharacterProfileStore.Current.ProfilesChanged += OnCharacterProfileMenuChanged;
        CharacterProfileStore.Current.SelectionChanged += OnCharacterProfileSelectionChanged;
        CharacterProfileStore.Current.MappingsChanged += OnCharacterProfileMappingsChanged;
        AppWindow.Closing += OnAppWindowClosing;
        Closed += OnMainWindowClosed;
    }

    private async void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_closeConfirmed || !_hasUnsavedChanges)
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
    }

    private void UpdateWindowTitle()
    {
        Title = _documentKind switch
        {
            DocumentKind.DataFiles => $"{AppMetadata.MainWindowTitle} - Data Files",
            DocumentKind.Header => $"{AppMetadata.MainWindowTitle} - Header{GetHeaderTitleSuffix()}",
            DocumentKind.Rom => $"{AppMetadata.MainWindowTitle} - {GetRomTitle()}",
            _ => AppMetadata.MainWindowTitle,
        };
    }

    private string GetRomTitle()
    {
        if (_romData is null)
        {
            return "ROM";
        }

        string romFormat = _romData.WasCompressed ? "Compressed ROM" : "Decompressed ROM";
        string title = $"{_romData.Profile.Name} - {romFormat}";
        if (_romData.ActiveSection == RomMessageSection.Credits)
        {
            title += " - Credits";
        }
        else if (_romData.Profile.MessageBanks.Count > 1)
        {
            title += $" - {_romData.Profile.MessageBanks[_romData.ActiveMessageBankIndex].Name}";
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
        => _headerLanguageEntries is not null && _headerLanguageEntries.Count > 1
            ? $" - {GetHeaderLanguageName(_activeHeaderLanguageIndex)}"
            : string.Empty;

    private void OnCharacterProfileSelectionChanged(object? sender, CharacterProfileSelectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RemapEditorTextForCharacterProfileChange(e);
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

        foreach (string profileName in CharacterProfileStore.Current.NamedProfileNames)
        {
            AddGlyphProfileMenuItem(profileName, profileName);
        }
    }

    private void AddGlyphProfileMenuItem(string text, string profileName)
    {
        var item = new ToggleMenuFlyoutItem
        {
            Text = text,
            IsChecked = CharacterProfileStore.Current.AutomaticProfileNameSetting == profileName,
        };
        item.Click += (_, _) =>
        {
            CharacterProfileStore.Current.SetAutomaticProfile(profileName);
            UpdateGlyphProfileMenu();
        };
        GlyphProfileMenu.Items.Add(item);
    }

    [LibraryImport("user32.dll", EntryPoint = "LoadCursorW")]
    private static partial IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    [LibraryImport("user32.dll")]
    private static partial IntPtr SetCursor(IntPtr hCursor);
}
