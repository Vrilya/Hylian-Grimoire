using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using HylianGrimoire.Interop;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Tweaks;

public sealed partial class TweaksWindow : Window
{
    private RomMessageData? _romData;
    private readonly Action<string> _onChanged;
    private IReadOnlyList<TweakUi> _tweaks = [];
    private int _updateDepth;
    private bool _updating => _updateDepth > 0;

    public TweaksWindow(RomMessageData? romData, Action<string> onChanged)
    {
        InitializeComponent();
        _tweaks = CreateTweakDefinitions();
        _romData = romData;
        _onChanged = onChanged;

        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }

        AppWindow.Resize(new Windows.Graphics.SizeInt32(690, 860));
        WindowSizeLimits.SetFixedSize(this, 690, 860);
        WindowIcon.Apply(this);
        AppWindow.TitleBar.ResetToDefault();
        WindowTheme.Register(this);
        Refresh();
    }

    public void SetRomData(RomMessageData? romData)
    {
        _romData = romData;
        Refresh();
    }
}
