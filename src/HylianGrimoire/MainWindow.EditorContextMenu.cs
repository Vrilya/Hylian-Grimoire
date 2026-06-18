using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private const string ContextMenuIconFontFamily = "Segoe MDL2 Assets";
    private const string CutIconGlyph = "\uE8C6";
    private const string CopyIconGlyph = "\uE8C8";
    private const string PasteIconGlyph = "\uE77F";
    private const string SelectAllIconGlyph = "\uE8B3";

    private void AddEditorCommandItems(MenuFlyout flyout)
    {
        string text = TextEditor.Text ?? string.Empty;
        bool hasSelection = TextEditor.SelectionLength > 0;
        bool canEdit = _session.CurrentIndex >= 0;

        var cutItem = new MenuFlyoutItem
        {
            Text = "Cut",
            Icon = CreateContextMenuIcon(CutIconGlyph),
            IsEnabled = canEdit && hasSelection,
        };
        cutItem.Click += OnEditorContextCutClick;
        flyout.Items.Add(cutItem);

        var copyItem = new MenuFlyoutItem
        {
            Text = "Copy",
            Icon = CreateContextMenuIcon(CopyIconGlyph),
            IsEnabled = hasSelection,
        };
        copyItem.Click += OnEditorContextCopyClick;
        flyout.Items.Add(copyItem);

        var pasteItem = new MenuFlyoutItem
        {
            Text = "Paste",
            Icon = CreateContextMenuIcon(PasteIconGlyph),
            IsEnabled = canEdit && ClipboardContainsText(),
        };
        pasteItem.Click += OnEditorContextPasteClick;
        flyout.Items.Add(pasteItem);

        var selectAllItem = new MenuFlyoutItem
        {
            Text = "Select All",
            Icon = CreateContextMenuIcon(SelectAllIconGlyph),
            IsEnabled = text.Length > 0,
        };
        selectAllItem.Click += OnEditorContextSelectAllClick;
        flyout.Items.Add(selectAllItem);

        flyout.Items.Add(new MenuFlyoutSeparator());
    }

    private void OnEditorContextCutClick(object sender, RoutedEventArgs e)
    {
        string selectedText = TextEditor.SelectedText ?? string.Empty;
        if (_session.CurrentIndex < 0 || selectedText.Length == 0)
        {
            return;
        }

        if (!TryCopyTextToClipboard(selectedText))
        {
            return;
        }

        ReplaceEditorSelection(string.Empty);
        SetStatus("Cut selected text");
    }

    private void OnEditorContextCopyClick(object sender, RoutedEventArgs e)
    {
        string selectedText = TextEditor.SelectedText ?? string.Empty;
        if (selectedText.Length == 0)
        {
            return;
        }

        if (!TryCopyTextToClipboard(selectedText))
        {
            return;
        }

        SetStatus("Copied selected text");
    }

    private async void OnEditorContextPasteClick(object sender, RoutedEventArgs e)
    {
        if (_session.CurrentIndex < 0)
        {
            return;
        }

        DataPackageView content;
        try
        {
            content = Clipboard.GetContent();
        }
        catch (Exception ex) when (IsClipboardException(ex))
        {
            SetClipboardFailureStatus("Clipboard paste failed", ex);
            return;
        }

        if (!content.Contains(StandardDataFormats.Text))
        {
            return;
        }

        string pastedText;
        try
        {
            pastedText = await content.GetTextAsync();
        }
        catch (Exception ex) when (IsClipboardException(ex))
        {
            SetClipboardFailureStatus("Clipboard paste failed", ex);
            return;
        }

        if (pastedText.Length == 0)
        {
            return;
        }

        ReplaceEditorSelection(pastedText.Replace("\r\n", "\n").Replace('\r', '\n'));
        SetStatus("Pasted text");
    }

    private void OnEditorContextSelectAllClick(object sender, RoutedEventArgs e)
    {
        TextEditor.SelectAll();
        TextEditor.Focus(FocusState.Programmatic);
    }

    private static FontIcon CreateContextMenuIcon(string glyph)
        => new()
        {
            FontFamily = new FontFamily(ContextMenuIconFontFamily),
            Glyph = glyph,
        };
}
