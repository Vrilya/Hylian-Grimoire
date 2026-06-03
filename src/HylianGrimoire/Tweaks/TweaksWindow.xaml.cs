using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using HylianGrimoire.Games;
using HylianGrimoire.Interop;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Tweaks;

public sealed partial class TweaksWindow : Window
{
    private RomMessageData? _romData;
    private readonly Action<string> _onChanged;
    private IReadOnlyList<TweakUi> _tweaks = [];
    private bool _updating;

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

    private async void OnBootLogoToggled(object sender, RoutedEventArgs e)
    {
        await ApplyTweakAsync(
            BootLogoSwitch,
            (romData, enabled) => GcBootLogoTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled Nintendo boot logo tweak.",
            "Disabled Nintendo boot logo tweak.");
    }

    private async void OnColorToggled(object sender, RoutedEventArgs e)
    {
        await ApplyTweakAsync(
            ColorSwitch,
            (romData, enabled) => GcColorTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled N64 interface colors tweak.",
            "Disabled N64 interface colors tweak.");
    }

    private async void OnViPalToggled(object sender, RoutedEventArgs e)
    {
        await ApplyTweakAsync(
            ViPalSwitch,
            (romData, enabled) => ViPalTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled N64 VI PAL timing tweak.",
            "Disabled N64 VI PAL timing tweak.");
    }

    private async void OnMmFpalToggled(object sender, RoutedEventArgs e)
    {
        await ApplyTweakAsync(
            MmFpalSwitch,
            (romData, enabled) => MmFpalTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled N64 VI PAL timing tweak.",
            "Disabled N64 VI PAL timing tweak.");
    }

    private async void OnMmViSelectorToggled(object sender, RoutedEventArgs e)
    {
        await ApplyTweakAsync(
            MmViSelectorSwitch,
            (romData, enabled) => MmViSelectorTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled MM FPAL/MPAL selector tweak.",
            "Disabled MM FPAL/MPAL selector tweak.");
    }

    private async void OnNoControllerToggled(object sender, RoutedEventArgs e)
    {
        await ApplyTweakAsync(
            NoControllerSwitch,
            (romData, enabled) => GcNoControllerTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled no-controller message tweak.",
            "Disabled no-controller message tweak.");
    }

    private async void OnViSelectorToggled(object sender, RoutedEventArgs e)
    {
        await ApplyTweakAsync(
            ViSelectorSwitch,
            (romData, enabled) => ViSelectorTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled FPAL/MPAL selector tweak.",
            "Disabled FPAL/MPAL selector tweak.");
    }

    private async void OnCreditsToggled(object sender, RoutedEventArgs e)
    {
        await ApplyTweakAsync(
            CreditsSwitch,
            (romData, enabled) => GcCreditsTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled GC credits N64 crash fix.",
            "Disabled GC credits N64 crash fix.");
    }

    private async void OnAntiPiracyToggled(object sender, RoutedEventArgs e)
    {
        await ApplyTweakAsync(
            AntiPiracySwitch,
            (romData, enabled) => AntiPiracyTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Disabled anti-piracy checks.",
            "Restored anti-piracy checks.");
    }

    private async Task ApplyTweakAsync(
        ToggleSwitch toggleSwitch,
        Action<RomMessageData, bool> setEnabled,
        string enabledMessage,
        string disabledMessage)
    {
        if (_updating || _romData is null)
        {
            return;
        }

        try
        {
            setEnabled(_romData, toggleSwitch.IsOn);
            _onChanged(toggleSwitch.IsOn ? enabledMessage : disabledMessage);
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
        _updating = true;
        try
        {
            foreach (TweakUi tweak in _tweaks)
            {
                RefreshTweak(tweak);
            }

            RefreshMajorasMaskMutualExclusion();
        }
        finally
        {
            _updating = false;
        }
    }

    private IReadOnlyList<TweakUi> CreateTweakDefinitions() =>
    [
        new(
            GameKind.OcarinaOfTime,
            BootLogoCard,
            BootLogoSwitch,
            BootLogoStatusText,
            romData => GcBootLogoTweak.GetStatus(romData.DecompressedRom, romData.Profile)),
        new(
            GameKind.OcarinaOfTime,
            ColorCard,
            ColorSwitch,
            ColorStatusText,
            romData => GcColorTweak.GetStatus(romData.DecompressedRom, romData.Profile)),
        new(
            GameKind.OcarinaOfTime,
            ViPalCard,
            ViPalSwitch,
            ViPalStatusText,
            romData => ViPalTweak.GetStatus(romData.DecompressedRom, romData.Profile)),
        new(
            GameKind.OcarinaOfTime,
            ViSelectorCard,
            ViSelectorSwitch,
            ViSelectorStatusText,
            romData => ViSelectorTweak.GetStatus(romData.DecompressedRom, romData.Profile)),
        new(
            GameKind.OcarinaOfTime,
            NoControllerCard,
            NoControllerSwitch,
            NoControllerStatusText,
            romData => GcNoControllerTweak.GetStatus(romData.DecompressedRom, romData.Profile)),
        new(
            GameKind.OcarinaOfTime,
            CreditsCard,
            CreditsSwitch,
            CreditsStatusText,
            romData => GcCreditsTweak.GetStatus(romData.DecompressedRom, romData.Profile)),
        new(
            GameKind.OcarinaOfTime,
            AntiPiracyCard,
            AntiPiracySwitch,
            AntiPiracyStatusText,
            romData => AntiPiracyTweak.GetStatus(romData.DecompressedRom, romData.Profile)),
        new(
            GameKind.MajorasMask,
            MmFpalCard,
            MmFpalSwitch,
            MmFpalStatusText,
            romData => MmFpalTweak.GetStatus(romData.DecompressedRom, romData.Profile),
            ShowMixedAsOn: false),
        new(
            GameKind.MajorasMask,
            MmViSelectorCard,
            MmViSelectorSwitch,
            MmViSelectorStatusText,
            romData => MmViSelectorTweak.GetStatus(romData.DecompressedRom, romData.Profile),
            ShowMixedAsOn: false),
    ];

    private void RefreshTweak(TweakUi tweak)
    {
        RomTweakStatus status = GetStatus(tweak);
        tweak.Card.Visibility = _romData?.Profile.Game == tweak.Game
            ? Visibility.Visible
            : Visibility.Collapsed;
        tweak.Switch.IsEnabled = status.CanToggle;
        tweak.Switch.IsOn = status.State == RomTweakState.On
            || tweak.ShowMixedAsOn && status.State == RomTweakState.Mixed;
        tweak.StatusText.Text = status.Detail;
    }

    private void RefreshMajorasMaskMutualExclusion()
    {
        if (_romData?.Profile.Game != GameKind.MajorasMask)
        {
            return;
        }

        RomTweakStatus fpalStatus = MmFpalTweak.GetStatus(_romData.DecompressedRom, _romData.Profile);
        bool selectorHasPayload = MmViSelectorTweak.HasPatchedPayload(_romData.DecompressedRom, _romData.Profile);
        bool fpalBlocksSelector = fpalStatus.State is RomTweakState.On
            || fpalStatus.State == RomTweakState.Mixed && !selectorHasPayload;

        if (fpalBlocksSelector)
        {
            MmViSelectorSwitch.IsEnabled = false;
        }

        if (selectorHasPayload)
        {
            MmFpalSwitch.IsEnabled = false;
        }
    }

    private RomTweakStatus GetStatus(TweakUi tweak) =>
        _romData is null
            ? new RomTweakStatus(RomTweakState.Unsupported, "Load a ROM.")
            : tweak.GetStatus(_romData);

    private sealed record TweakUi(
        GameKind Game,
        Border Card,
        ToggleSwitch Switch,
        TextBlock StatusText,
        Func<RomMessageData, RomTweakStatus> GetStatus,
        bool ShowMixedAsOn = true);

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
