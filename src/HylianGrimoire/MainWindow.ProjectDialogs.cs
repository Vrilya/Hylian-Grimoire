using HylianGrimoire.Games;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private sealed record GameProjectOption(string Name, GameKind Kind)
    {
        public override string ToString() => Name;
    }

    private async Task<GameProfile?> PromptForNewProjectAsync()
    {
        var options = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 320,
            SelectedIndex = 0,
        };
        options.Items.Add(new GameProjectOption("Ocarina of Time", GameKind.OcarinaOfTime));
        options.Items.Add(new GameProjectOption("Majora's Mask", GameKind.MajorasMask));

        var dialog = new ContentDialog
        {
            Title = "New Project",
            Content = options,
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return null;
        }

        var selected = (GameProjectOption)options.SelectedItem;
        return GameProfiles.Get(selected.Kind);
    }

    private async Task<bool?> PromptForRomCompressionAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Save as ROM",
            Content = "Compress the ROM before saving?",
            PrimaryButtonText = "Compress",
            SecondaryButtonText = "Don't compress",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result switch
        {
            ContentDialogResult.Primary => true,
            ContentDialogResult.Secondary => false,
            _ => null,
        };
    }
}
