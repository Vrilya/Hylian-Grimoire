using HylianGrimoire.Games;
using Microsoft.UI.Xaml;

namespace HylianGrimoire.Tweaks;

public sealed partial class TweaksWindow
{
    private void Refresh()
    {
        using IDisposable update = BeginUpdate();

        foreach (TweakUi tweak in _tweaks)
        {
            RefreshTweak(tweak);
        }

        RefreshMajorasMaskMutualExclusion();
    }

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
}
