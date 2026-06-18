using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.Tweaks;

public sealed partial class TweaksWindow
{
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
