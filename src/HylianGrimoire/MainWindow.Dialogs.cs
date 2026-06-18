using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private async Task ShowErrorAsync(string title, string message) => await ShowDialogAsync(title, message);

    private async Task ShowExceptionAsync(string title, Exception exception)
    {
        AppDiagnostics.LogHandledException(title, exception);
        await ShowDialogAsync(title, exception.Message);
    }

    private async Task ShowOperationExceptionAsync(
        string title,
        Exception exception,
        string? recoveryMessage = null,
        string? statusMessage = null)
    {
        await UiOperationExceptionHandler.ShowAsync(
            title,
            exception,
            ShowErrorAsync,
            recoveryMessage,
            SetStatus,
            statusMessage);
    }

    private async Task ShowInfoAsync(string title, string message) => await ShowDialogAsync(title, message);

    private async Task<int?> PromptForMessageIdAsync(string title, string primaryButtonText, string initialValue = "")
    {
        while (true)
        {
            var input = new TextBox
            {
                Text = initialValue,
                PlaceholderText = "0x0000",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Mono"),
                SelectionStart = initialValue.Length,
            };

            var dialog = new ContentDialog
            {
                Title = title,
                Content = input,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot,
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            {
                return null;
            }

            if (TryParseMessageId(input.Text, out int id))
            {
                return id;
            }

            await ShowErrorAsync("Invalid message ID", "Enter a value from 0x0000 to 0xffff.");
            initialValue = input.Text;
        }
    }

    private static bool TryParseMessageId(string text, out int id)
    {
        id = 0;
        text = text.Trim();
        if (text.Length == 0)
        {
            return false;
        }

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            text = text[2..];
        }

        return int.TryParse(
                text,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out id)
            && id is >= 0x0000 and <= 0xffff;
    }

    private async Task ShowDialogAsync(string title, string message)
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

    private async Task<bool> ConfirmCloseWithUnsavedChangesAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Save changes before closing?",
            Content = "There are unsaved changes in the current message data.",
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Don't Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result switch
        {
            ContentDialogResult.Primary => await SaveCurrentFilesForCloseAsync(),
            ContentDialogResult.Secondary => true,
            _ => false,
        };
    }
}
