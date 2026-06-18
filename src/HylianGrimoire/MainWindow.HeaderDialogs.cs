using HylianGrimoire.Headers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
}
