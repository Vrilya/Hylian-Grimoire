using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using HylianGrimoire.Games;
using HylianGrimoire.Headers;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private readonly record struct HeaderExportChoice(CHeaderExportFormat Format, bool AllRomLanguages);
    private readonly record struct HeaderRomImportChoice(bool AllWesternLanguages, CHeaderMessageSlot SelectedSlot);
    private sealed record HeaderExportOption(string Name, string Description, CHeaderExportFormat Format, bool AllRomLanguages)
    {
        public override string ToString() => Name;
    }

    private sealed record HeaderLanguageSlotOption(string Name, CHeaderMessageSlot Slot)
    {
        public override string ToString() => Name;
    }

    private sealed record GameProjectOption(string Name, GameKind Kind)
    {
        public override string ToString() => Name;
    }

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

    private async Task<HeaderExportChoice?> PromptForHeaderExportFormatAsync(bool canExportAllRomLanguages)
    {
        var options = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 375,
            SelectedIndex = 0,
        };
        if (canExportAllRomLanguages)
        {
            options.Items.Add(new HeaderExportOption(
                "Modern, all ROM languages",
                "Modern decomp header with every ROM language slot.",
                CHeaderExportFormat.Modern,
                true));
        }

        options.Items.Add(new HeaderExportOption(
            "Modern, selected language",
            "Modern decomp header for the currently selected language.",
            CHeaderExportFormat.Modern,
            false));

        options.Items.Add(new HeaderExportOption(
            "Legacy",
            "Classic old decomp header format.",
            CHeaderExportFormat.Legacy,
            false));
        options.Items.Add(new HeaderExportOption(
            "OTRMod",
            "Legacy-like header format used by OTRMod.",
            CHeaderExportFormat.OTRMod,
            false));

        var description = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0),
        };
        options.SelectionChanged += (_, _) =>
        {
            description.Text = options.SelectedItem is HeaderExportOption option ? option.Description : string.Empty;
        };
        description.Text = ((HeaderExportOption)options.SelectedItem).Description;

        var panel = new StackPanel
        {
            Spacing = 0,
        };
        panel.Children.Add(options);
        panel.Children.Add(description);

        var dialog = new ContentDialog
        {
            Title = "Export to .h",
            Content = panel,
            PrimaryButtonText = "Export",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return null;
        }

        var selected = (HeaderExportOption)options.SelectedItem;
        return new HeaderExportChoice(selected.Format, selected.AllRomLanguages);
    }

    private async Task<HeaderRomImportChoice?> PromptForHeaderRomImportAsync(
        IReadOnlyList<CHeaderMessageSlot> availableWesternSlots,
        bool canImportAllWesternLanguages)
    {
        var modeOptions = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Width = 420,
            SelectedIndex = 0,
        };
        modeOptions.Items.Add("Import selected .h language into current ROM language");
        if (canImportAllWesternLanguages && availableWesternSlots.Count > 1)
        {
            modeOptions.Items.Add("Import all western .h languages into all ROM languages");
        }

        var slotOptions = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Width = 420,
            SelectedIndex = 0,
        };
        foreach (CHeaderMessageSlot slot in availableWesternSlots)
        {
            slotOptions.Items.Add(new HeaderLanguageSlotOption(GetHeaderSlotDisplayName(slot), slot));
        }
        slotOptions.IsEnabled = availableWesternSlots.Count > 1;

        modeOptions.SelectionChanged += (_, _) =>
        {
            slotOptions.IsEnabled = modeOptions.SelectedIndex == 0 && availableWesternSlots.Count > 1;
        };

        var panel = new StackPanel
        {
            Spacing = 12,
        };
        panel.Children.Add(new TextBlock { Text = "Import mode" });
        panel.Children.Add(modeOptions);
        panel.Children.Add(new TextBlock { Text = "Header language" });
        panel.Children.Add(slotOptions);

        var dialog = new ContentDialog
        {
            Title = "Import .h into ROM",
            Content = panel,
            PrimaryButtonText = "Import",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return null;
        }

        bool allWestern = modeOptions.SelectedIndex == 1 && canImportAllWesternLanguages && availableWesternSlots.Count > 1;
        var selectedSlot = (HeaderLanguageSlotOption)slotOptions.SelectedItem;
        return new HeaderRomImportChoice(allWestern, selectedSlot.Slot);
    }

    private static string GetHeaderSlotDisplayName(CHeaderMessageSlot slot)
        => slot switch
        {
            CHeaderMessageSlot.Ger => "Language 2",
            CHeaderMessageSlot.Fra => "Language 3",
            _ => "Language 1 / NES",
        };

    private IDisposable ShowProgress(string status, string progressText)
    {
        string previousStatus = StatusText.Text;
        SetStatus(progressText);
        ProgressTitleText.Text = status;
        ProgressPercentText.Text = "0%";
        ProgressModalBar.Value = 0;
        ProgressOverlay.Visibility = Visibility.Visible;
        AutomationProperties.SetName(ProgressModalBar, progressText);
        return new BusyScope(this, previousStatus);
    }

    private void UpdateBusyProgress(HylianGrimoire.Rom.RomFileOperationProgress progress)
    {
        int percent = (int)Math.Round(progress.Percent);
        ProgressModalBar.Value = percent;
        ProgressPercentText.Text = $"{percent}%";
    }

    private sealed class BusyScope : IDisposable
    {
        private readonly MainWindow _owner;
        private readonly string _previousStatus;
        private bool _disposed;

        public BusyScope(MainWindow owner, string previousStatus)
        {
            _owner = owner;
            _previousStatus = previousStatus;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _owner.ProgressOverlay.Visibility = Visibility.Collapsed;
            _owner.ProgressModalBar.Value = 0;
            _owner.ProgressPercentText.Text = "0%";
            _owner.SetStatus(_previousStatus);
        }
    }
}
