using System.Collections.ObjectModel;
using HylianGrimoire.Games;
using HylianGrimoire.Interop;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileWindow : Window
{
    private readonly ObservableCollection<GlyphListItem> _glyphs = [];
    private readonly ObservableCollection<string> _profiles = [];
    private readonly CharacterProfileRuntime _characterProfileRuntime;
    private RomGlyphEditorSession? _romSession;
    private int _updateDepth;
    private bool _updating => _updateDepth > 0;
    private byte? _selectedValue;

    public event EventHandler? GlyphDataChanged;

    public CharacterProfileWindow(CharacterProfileRuntime characterProfileRuntime)
        : this(characterProfileRuntime, null)
    {
    }

    public CharacterProfileWindow(CharacterProfileRuntime characterProfileRuntime, RomGlyphEditorSession? romSession)
    {
        _characterProfileRuntime = characterProfileRuntime ?? throw new ArgumentNullException(nameof(characterProfileRuntime));
        _romSession = romSession;
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        RefreshWindowMode();

        AttachRomSession(_romSession);

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
        }

        AppWindow.Resize(new Windows.Graphics.SizeInt32(1100, 640));
        WindowSizeLimits.SetFixedWidth(this, 1100, 640);
        WindowIcon.Apply(this);
        WindowTheme.Register(this);
        Closed += (_, _) => DetachRomSession();

        ProfileCombo.ItemsSource = _profiles;
        GlyphList.ItemsSource = _glyphs;
        RefreshProfileAndGlyphViews();
        GlyphList.SelectedIndex = 0;
    }

    public bool IsRomMode => _romSession is not null;

    private GameKind GlyphGameKind => _romSession?.GameKind ?? _characterProfileRuntime.ActiveGameKind;

    private void RefreshWindowMode()
    {
        Title = IsRomMode
            ? "Hylian Grimoire - ROM Glyph Manager"
            : "Hylian Grimoire - Glyph Manager";
        WindowHeadingText.Text = IsRomMode ? "ROM Glyphs" : "Glyphs";
        HelpText.Text = IsRomMode
            ? "Glyph image and width changes are written to the loaded ROM session. Glyph profiles control which keyboard characters map to each byte."
            : "Glyph profiles keep editor characters, preview images, and preview widths together. The byte value stays the same.";
    }
}
