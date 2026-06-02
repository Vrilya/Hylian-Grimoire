using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;
using HylianGrimoire.Services;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private async void OnOpenFontOrder(object sender, RoutedEventArgs e)
    {
        MessageEntry? entry = FontOrderService.FindEntry(_session.Entries, _session.RomData);
        if (entry is null)
        {
            return;
        }

        var editor = new TextBox
        {
            AcceptsReturn = true,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Mono"),
            MinWidth = 520,
            MinHeight = 220,
            TextWrapping = TextWrapping.NoWrap,
        };
        editor.Text = FontOrderService.GetEditorText(entry, _session.RomData);
        ScrollViewer.SetHorizontalScrollBarVisibility(editor, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(editor, ScrollBarVisibility.Auto);

        var resetStandardButton = new Button
        {
            Content = "Reset to standard",
        };
        resetStandardButton.Click += (_, _) =>
        {
            editor.Text = FontOrderCodec.GetStandardEditorText();
        };

        var resetLoadedButton = new Button
        {
            Content = "Reset to loaded",
        };
        resetLoadedButton.Click += (_, _) =>
        {
            editor.Text = FontOrderService.GetLoadedEditorText(entry, _session.RomData);
        };

        var resetButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
        };
        resetButtons.Children.Add(resetLoadedButton);
        resetButtons.Children.Add(resetStandardButton);

        var content = new StackPanel
        {
            Spacing = 12,
        };
        content.Children.Add(editor);
        content.Children.Add(resetButtons);

        var dialog = new ContentDialog
        {
            Title = "Font order (0xFFFC)",
            Content = content,
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ApplyFontOrderTextAsync(entry, editor.Text);
        }
    }

    private async Task ApplyFontOrderTextAsync(MessageEntry entry, string editorText)
    {
        FontOrderUpdateResult result = FontOrderService.ApplyEditorText(entry, editorText);
        if (result.ErrorMessage is not null)
        {
            await ShowErrorAsync("Invalid font order", result.ErrorMessage);
            return;
        }

        if (!result.Changed)
        {
            return;
        }

        MarkDirty();
        PopulateList();
        SetStatus("Updated font order (0xFFFC).");
    }
}
