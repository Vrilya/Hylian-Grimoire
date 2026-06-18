using HylianGrimoire.Interop;
using HylianGrimoire.Rom;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace HylianGrimoire.TitleText;

public sealed partial class TitleTextWindow : Window
{
    private readonly Action<string> _onChanged;
    private RomMessageData? _romData;
    private TitleTextPatchProfile? _profile;
    private int _languageIndex;
    private int _updateDepth;
    private bool _updating => _updateDepth > 0;

    public TitleTextWindow(RomMessageData? romData, int languageIndex, Action<string> onChanged)
    {
        InitializeComponent();
        _onChanged = onChanged;
        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }

        AppWindow.Resize(new Windows.Graphics.SizeInt32(1389, 950));
        WindowTheme.Register(this);
        WindowIcon.Apply(this);
        WindowSizeLimits.SetFixedSize(this, 1389, 950);
        SetRomData(romData, languageIndex);
    }

    public void SetRomData(RomMessageData? romData, int languageIndex)
    {
        _romData = romData;
        _languageIndex = languageIndex;
        LoadFromRom();
    }
}
