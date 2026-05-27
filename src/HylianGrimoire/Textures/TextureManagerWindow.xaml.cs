using HylianGrimoire.Interop;
using HylianGrimoire.Rom;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace HylianGrimoire.Textures;

public sealed partial class TextureManagerWindow : Window
{
    private const int PreviewPadding = 32;
    private readonly Action<string> _onChanged;
    private RomMessageData? _romData;
    private int _previewCounter;

    public TextureManagerWindow(RomMessageData? romData, Action<string> onChanged)
    {
        InitializeComponent();
        _onChanged = onChanged;

        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(980, 680));
        WindowSizeLimits.SetMinimumSize(this, 980, 680);
        WindowIcon.Apply(this);
        AppWindow.TitleBar.ResetToDefault();
        WindowTheme.Register(this);

        SetRomData(romData);
    }
}
