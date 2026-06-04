using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow
{
    private async Task<string?> PickOpenO2rAsync()
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.FileTypeFilter.Add(".o2r");
        StorageFile? file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    private async Task<string?> PickSaveO2rAsync()
    {
        var picker = new FileSavePicker
        {
            SuggestedFileName = _portProfile.SuggestedFileName,
        };
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.FileTypeChoices.Add(_portProfile.FileTypeDescription, [".o2r"]);
        StorageFile? file = await picker.PickSaveFileAsync();
        return file?.Path;
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = RootGrid.XamlRoot,
        };

        await dialog.ShowAsync();
    }

    private async Task<bool?> AskOverwriteTextureOverridesAsync(IReadOnlyList<string> resourcePaths)
    {
        var dialog = new ContentDialog
        {
            Title = "Texture overrides found",
            Content = CreateConflictDialogContent(
                "These selected ROM textures already exist differently in the loaded .o2r:",
                resourcePaths,
                "Keep the .o2r overrides or use the loaded ROM textures?"),
            PrimaryButtonText = "Keep .o2r overrides",
            SecondaryButtonText = "Overwrite with ROM",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = RootGrid.XamlRoot,
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result switch
        {
            ContentDialogResult.Primary => false,
            ContentDialogResult.Secondary => true,
            _ => null,
        };
    }

    private async Task<bool?> AskOverwriteTextResourcesAsync(IReadOnlyList<string> resourceNames)
    {
        var dialog = new ContentDialog
        {
            Title = "Text resources found",
            Content = CreateConflictDialogContent(
                "These selected text resources differ from the loaded .o2r:",
                resourceNames,
                "Keep the .o2r text or use the current Hylian Grimoire text?"),
            PrimaryButtonText = "Keep .o2r text",
            SecondaryButtonText = "Use editor text",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = RootGrid.XamlRoot,
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result switch
        {
            ContentDialogResult.Primary => false,
            ContentDialogResult.Secondary => true,
            _ => null,
        };
    }

    private UIElement CreateConflictDialogContent(
        string intro,
        IReadOnlyList<string> items,
        string question)
    {
        const int previewCount = 5;
        var panel = new StackPanel
        {
            Spacing = 12,
            MaxWidth = 560,
        };

        panel.Children.Add(new TextBlock
        {
            Text = intro,
            TextWrapping = TextWrapping.Wrap,
        });

        panel.Children.Add(new TextBlock
        {
            Text = string.Join(Environment.NewLine, items.Take(previewCount)),
            FontFamily = new FontFamily("Cascadia Mono"),
            TextWrapping = TextWrapping.NoWrap,
        });

        int remaining = items.Count - previewCount;
        if (remaining > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"and {remaining} more",
                TextWrapping = TextWrapping.Wrap,
            });
        }

        panel.Children.Add(new TextBlock
        {
            Text = question,
            TextWrapping = TextWrapping.Wrap,
        });

        return panel;
    }

    private IDisposable ShowProgress(string title)
    {
        ProgressTitleText.Text = title;
        ProgressPercentText.Text = "0%";
        ProgressBar.Value = 0;
        ProgressOverlay.Visibility = Visibility.Visible;
        return new ProgressScope(this);
    }

    private void UpdateProgress(int percent)
    {
        ProgressBar.Value = percent;
        ProgressPercentText.Text = $"{percent}%";
    }

    private static int GetPercent(int completed, int total)
        => total <= 0 ? 100 : Math.Clamp((int)Math.Round(completed * 100d / total), 0, 100);
}
