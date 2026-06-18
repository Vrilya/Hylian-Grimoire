using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.Tweaks;

public sealed partial class TweaksWindow
{
    private async void OnTweakToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        TweakUi? tweak = _tweaks.FirstOrDefault(tweak => ReferenceEquals(tweak.Switch, toggleSwitch));
        if (tweak is null)
        {
            return;
        }

        await ApplyTweakAsync(tweak);
    }

    private async Task ApplyTweakAsync(TweakUi tweak)
    {
        if (_updating || _romData is null)
        {
            return;
        }

        try
        {
            tweak.SetEnabled(_romData, tweak.Switch.IsOn);
            _onChanged(tweak.Switch.IsOn ? tweak.EnabledMessage : tweak.DisabledMessage);
        }
        catch (Exception ex)
        {
            await UiOperationExceptionHandler.ShowAsync("Failed to apply tweak", ex, ShowErrorAsync);
        }
        finally
        {
            Refresh();
        }
    }
}
