using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using HylianGrimoire.Codecs;
using HylianGrimoire.Interop;
using HylianGrimoire.Models;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Preview;
using HylianGrimoire.Services;

namespace HylianGrimoire;

public sealed partial class MainWindow : Window
{
    private static readonly string[] PositionNames =
        ["Auto", "Top", "Middle", "Bottom"];

    private readonly ObservableCollection<MessageItem> _items = new();
    private List<MessageEntry> _entries = new();
    private int _currentIdx = -1;
    private bool _updating;
    private string? _tblPath;
    private string? _binPath;
    private string _searchText = string.Empty;
    private string _cleanDocumentFingerprint = string.Empty;
    private bool _hasUnsavedChanges;
    private bool _closeConfirmed;
    private OotPreviewWindow? _previewWindow;
    private GlyphOverrideWindow? _glyphOverrideWindow;

    public MainWindow()
    {
        InitializeComponent();

        Title = AppMetadata.MainWindowTitle;
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
        GlyphOverrideStore.Current.Changed += OnGlyphOverridesChanged;
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
        _glyphOverrideWindow?.Close();
        _glyphOverrideWindow = null;
    }

    private void OnGlyphOverridesChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RefreshEntriesForGlyphOverrides();
            UpdatePreview();
        });
    }

    private void RefreshEntriesForGlyphOverrides()
    {
        int current = _currentIdx;
        int failedCount = 0;
        bool wasDirty = _hasUnsavedChanges;
        CommitCurrent();

        foreach (MessageEntry entry in _entries)
        {
            try
            {
                entry.Text = MessageTextSyntax.ApplyCurrentEncodingProfile(entry.Text);
            }
            catch (InvalidDataException)
            {
                failedCount++;
            }
        }

        ApplySearchFilter();
        if (current >= 0 && current < _entries.Count)
        {
            ShowEntry(current);
        }

        if (failedCount > 0)
        {
            SetStatus($"Character overrides applied, but {failedCount} message(s) could not be normalized.");
        }

        if (wasDirty)
        {
            MarkDirty();
        }
        else
        {
            MarkClean();
        }
    }

    [LibraryImport("user32.dll", EntryPoint = "LoadCursorW")]
    private static partial IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    [LibraryImport("user32.dll")]
    private static partial IntPtr SetCursor(IntPtr hCursor);
}
