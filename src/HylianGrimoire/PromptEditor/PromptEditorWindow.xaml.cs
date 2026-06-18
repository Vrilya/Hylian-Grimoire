using System.Collections.ObjectModel;
using HylianGrimoire.Interop;
using HylianGrimoire.Rom;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace HylianGrimoire.PromptEditor;

public sealed partial class PromptEditorWindow : Window
{
    private readonly Action<string> _onChanged;
    private RomMessageData? _romData;
    private PromptEditorProfile? _profile;
    private readonly ObservableCollection<PromptEditorLine> _lines = [];
    private string _languageKey = "eng";
    private int _updateDepth;
    private bool _updating => _updateDepth > 0;

    public PromptEditorWindow(RomMessageData? romData, Action<string> onChanged)
    {
        InitializeComponent();
        _onChanged = onChanged;
        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }

        var windowSize = new Windows.Graphics.SizeInt32(1085, 820);
        AppWindow.Resize(windowSize);
        WindowSizeLimits.SetMinimumSize(this, windowSize.Width, windowSize.Height);
        WindowIcon.Apply(this);
        AppWindow.TitleBar.ResetToDefault();
        WindowTheme.Register(this);
        PromptList.ItemsSource = _lines;
        SetRomData(romData);
    }

    public void SetRomData(RomMessageData? romData)
    {
        _romData = romData;
        LoadFromRom();
    }
}
