using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using HylianGrimoire.Interop;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Tweaks;

public sealed partial class TweaksWindow : Window
{
    private RomMessageData? _romData;
    private readonly Action<string> _onChanged;
    private bool _updating;

    public TweaksWindow(RomMessageData? romData, Action<string> onChanged)
    {
        InitializeComponent();
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

    private async void OnBootLogoToggled(object sender, RoutedEventArgs e)
    {
        if (_updating || _romData is null)
        {
            return;
        }

        try
        {
            GcBootLogoTweak.SetEnabled(_romData.DecompressedRom, _romData.Profile, BootLogoSwitch.IsOn);
            _onChanged(BootLogoSwitch.IsOn
                ? "Enabled Nintendo boot logo tweak."
                : "Disabled Nintendo boot logo tweak.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to apply tweak", ex.Message);
        }
        finally
        {
            Refresh();
        }
    }

    private async void OnColorToggled(object sender, RoutedEventArgs e)
    {
        if (_updating || _romData is null)
        {
            return;
        }

        try
        {
            GcColorTweak.SetEnabled(_romData.DecompressedRom, _romData.Profile, ColorSwitch.IsOn);
            _onChanged(ColorSwitch.IsOn
                ? "Enabled N64 interface colors tweak."
                : "Disabled N64 interface colors tweak.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to apply tweak", ex.Message);
        }
        finally
        {
            Refresh();
        }
    }

    private async void OnViPalToggled(object sender, RoutedEventArgs e)
    {
        if (_updating || _romData is null)
        {
            return;
        }

        try
        {
            ViPalTweak.SetEnabled(_romData.DecompressedRom, _romData.Profile, ViPalSwitch.IsOn);
            _onChanged(ViPalSwitch.IsOn
                ? "Enabled N64 VI PAL timing tweak."
                : "Disabled N64 VI PAL timing tweak.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to apply tweak", ex.Message);
        }
        finally
        {
            Refresh();
        }
    }

    private async void OnNoControllerToggled(object sender, RoutedEventArgs e)
    {
        if (_updating || _romData is null)
        {
            return;
        }

        try
        {
            GcNoControllerTweak.SetEnabled(_romData.DecompressedRom, _romData.Profile, NoControllerSwitch.IsOn);
            _onChanged(NoControllerSwitch.IsOn
                ? "Enabled no-controller message tweak."
                : "Disabled no-controller message tweak.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to apply tweak", ex.Message);
        }
        finally
        {
            Refresh();
        }
    }

    private async void OnViSelectorToggled(object sender, RoutedEventArgs e)
    {
        if (_updating || _romData is null)
        {
            return;
        }

        try
        {
            ViSelectorTweak.SetEnabled(_romData.DecompressedRom, _romData.Profile, ViSelectorSwitch.IsOn);
            _onChanged(ViSelectorSwitch.IsOn
                ? "Enabled FPAL/MPAL selector tweak."
                : "Disabled FPAL/MPAL selector tweak.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to apply tweak", ex.Message);
        }
        finally
        {
            Refresh();
        }
    }

    private async void OnCreditsToggled(object sender, RoutedEventArgs e)
    {
        if (_updating || _romData is null)
        {
            return;
        }

        try
        {
            GcCreditsTweak.SetEnabled(_romData.DecompressedRom, _romData.Profile, CreditsSwitch.IsOn);
            _onChanged(CreditsSwitch.IsOn
                ? "Enabled GC credits N64 crash fix."
                : "Disabled GC credits N64 crash fix.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to apply tweak", ex.Message);
        }
        finally
        {
            Refresh();
        }
    }

    private async void OnAntiPiracyToggled(object sender, RoutedEventArgs e)
    {
        if (_updating || _romData is null)
        {
            return;
        }

        try
        {
            AntiPiracyTweak.SetEnabled(_romData.DecompressedRom, _romData.Profile, AntiPiracySwitch.IsOn);
            _onChanged(AntiPiracySwitch.IsOn
                ? "Disabled anti-piracy checks."
                : "Restored anti-piracy checks.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to apply tweak", ex.Message);
        }
        finally
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        RomTweakStatus bootLogoStatus = _romData is null
            ? new RomTweakStatus(RomTweakState.Unsupported, "Load a ROM.")
            : GcBootLogoTweak.GetStatus(_romData.DecompressedRom, _romData.Profile);
        RomTweakStatus colorStatus = _romData is null
            ? new RomTweakStatus(RomTweakState.Unsupported, "Load a ROM.")
            : GcColorTweak.GetStatus(_romData.DecompressedRom, _romData.Profile);
        RomTweakStatus viPalStatus = _romData is null
            ? new RomTweakStatus(RomTweakState.Unsupported, "Load a ROM.")
            : ViPalTweak.GetStatus(_romData.DecompressedRom, _romData.Profile);
        RomTweakStatus noControllerStatus = _romData is null
            ? new RomTweakStatus(RomTweakState.Unsupported, "Load a ROM.")
            : GcNoControllerTweak.GetStatus(_romData.DecompressedRom, _romData.Profile);
        RomTweakStatus viSelectorStatus = _romData is null
            ? new RomTweakStatus(RomTweakState.Unsupported, "Load a ROM.")
            : ViSelectorTweak.GetStatus(_romData.DecompressedRom, _romData.Profile);
        RomTweakStatus creditsStatus = _romData is null
            ? new RomTweakStatus(RomTweakState.Unsupported, "Load a ROM.")
            : GcCreditsTweak.GetStatus(_romData.DecompressedRom, _romData.Profile);
        RomTweakStatus antiPiracyStatus = _romData is null
            ? new RomTweakStatus(RomTweakState.Unsupported, "Load a ROM.")
            : AntiPiracyTweak.GetStatus(_romData.DecompressedRom, _romData.Profile);

        _updating = true;
        try
        {
            BootLogoSwitch.IsEnabled = bootLogoStatus.CanToggle;
            BootLogoSwitch.IsOn = bootLogoStatus.State is RomTweakState.On or RomTweakState.Mixed;
            BootLogoStatusText.Text = bootLogoStatus.Detail;

            ColorSwitch.IsEnabled = colorStatus.CanToggle;
            ColorSwitch.IsOn = colorStatus.State is RomTweakState.On or RomTweakState.Mixed;
            ColorStatusText.Text = colorStatus.Detail;

            ViPalSwitch.IsEnabled = viPalStatus.CanToggle;
            ViPalSwitch.IsOn = viPalStatus.State is RomTweakState.On or RomTweakState.Mixed;
            ViPalStatusText.Text = viPalStatus.Detail;

            NoControllerSwitch.IsEnabled = noControllerStatus.CanToggle;
            NoControllerSwitch.IsOn = noControllerStatus.State is RomTweakState.On or RomTweakState.Mixed;
            NoControllerStatusText.Text = noControllerStatus.Detail;

            ViSelectorSwitch.IsEnabled = viSelectorStatus.CanToggle;
            ViSelectorSwitch.IsOn = viSelectorStatus.State is RomTweakState.On or RomTweakState.Mixed;
            ViSelectorStatusText.Text = viSelectorStatus.Detail;

            CreditsSwitch.IsEnabled = creditsStatus.CanToggle;
            CreditsSwitch.IsOn = creditsStatus.State is RomTweakState.On or RomTweakState.Mixed;
            CreditsStatusText.Text = creditsStatus.Detail;

            AntiPiracySwitch.IsEnabled = antiPiracyStatus.CanToggle;
            AntiPiracySwitch.IsOn = antiPiracyStatus.State is RomTweakState.On or RomTweakState.Mixed;
            AntiPiracyStatusText.Text = antiPiracyStatus.Detail;
        }
        finally
        {
            _updating = false;
        }
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };

        await dialog.ShowAsync();
    }
}
