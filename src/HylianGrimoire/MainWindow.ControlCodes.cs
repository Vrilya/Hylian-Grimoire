using HylianGrimoire.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void OnControlCodeFlyoutOpening(object sender, object e)
    {
        if (sender is not MenuFlyout flyout)
        {
            return;
        }

        BuildControlCodeFlyout(flyout);
    }

    private void BuildControlCodeFlyout(MenuFlyout flyout)
    {
        flyout.Items.Clear();
        AddEditorCommandItems(flyout);

        if (_session.CurrentIndex < 0)
        {
            flyout.Items.Add(new MenuFlyoutItem
            {
                Text = "Select a message first",
                IsEnabled = false,
            });
            return;
        }

        IReadOnlyList<MessageControlCodeGroup> groups = MessageControlCodeCatalog.GetGroups(CurrentGameProfile.Kind);
        if (groups.Count == 0)
        {
            flyout.Items.Add(new MenuFlyoutItem
            {
                Text = "No control codes available",
                IsEnabled = false,
            });
            return;
        }

        foreach (MessageControlCodeGroup group in groups)
        {
            var subItem = new MenuFlyoutSubItem
            {
                Text = group.Name,
            };

            foreach (MessageControlCodeEntry entry in group.Entries)
            {
                var item = new MenuFlyoutItem
                {
                    Text = entry.Label,
                    Tag = entry,
                };
                ToolTipService.SetToolTip(item, GetControlCodeToolTip(entry));
                item.Click += OnControlCodeMenuItemClick;
                subItem.Items.Add(item);
            }

            flyout.Items.Add(subItem);
        }
    }

    private void AddEditorCommandItems(MenuFlyout flyout)
    {
        string text = TextEditor.Text ?? string.Empty;
        bool hasSelection = TextEditor.SelectionLength > 0;
        bool canEdit = _session.CurrentIndex >= 0;

        var cutItem = new MenuFlyoutItem
        {
            Text = "Cut",
            Icon = CreateContextMenuIcon("\uE8C6"),
            IsEnabled = canEdit && hasSelection,
        };
        cutItem.Click += OnEditorContextCutClick;
        flyout.Items.Add(cutItem);

        var copyItem = new MenuFlyoutItem
        {
            Text = "Copy",
            Icon = CreateContextMenuIcon("\uE8C8"),
            IsEnabled = hasSelection,
        };
        copyItem.Click += OnEditorContextCopyClick;
        flyout.Items.Add(copyItem);

        var pasteItem = new MenuFlyoutItem
        {
            Text = "Paste",
            Icon = CreateContextMenuIcon("\uE77F"),
            IsEnabled = canEdit && ClipboardContainsText(),
        };
        pasteItem.Click += OnEditorContextPasteClick;
        flyout.Items.Add(pasteItem);

        var selectAllItem = new MenuFlyoutItem
        {
            Text = "Select All",
            Icon = CreateContextMenuIcon("\uE8B3"),
            IsEnabled = text.Length > 0,
        };
        selectAllItem.Click += OnEditorContextSelectAllClick;
        flyout.Items.Add(selectAllItem);

        flyout.Items.Add(new MenuFlyoutSeparator());
    }

    private void OnControlCodeMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: MessageControlCodeEntry entry })
        {
            InsertControlCode(entry);
        }
    }

    private void OnEditorContextCutClick(object sender, RoutedEventArgs e)
    {
        string selectedText = TextEditor.SelectedText ?? string.Empty;
        if (_session.CurrentIndex < 0 || selectedText.Length == 0)
        {
            return;
        }

        CopyTextToClipboard(selectedText);
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

        CopyTextToClipboard(selectedText);
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
        catch (Exception)
        {
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
        catch (Exception)
        {
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

    private void InsertControlCode(MessageControlCodeEntry entry)
    {
        if (_session.CurrentIndex < 0)
        {
            return;
        }

        int start = ReplaceEditorSelection(entry.InsertText);

        int insertedStart = FindInsertedControlCode(entry.InsertText, start);
        int selectionStart = insertedStart + entry.InsertText.Length;
        int selectionLength = 0;
        if (entry.SelectionStartOffset >= 0)
        {
            selectionStart = Math.Clamp(insertedStart + entry.SelectionStartOffset, 0, TextEditor.Text.Length);
            selectionLength = Math.Clamp(entry.SelectionLength, 0, TextEditor.Text.Length - selectionStart);
        }

        TextEditor.SelectionStart = selectionStart;
        TextEditor.SelectionLength = selectionLength;
        TextEditor.Focus(FocusState.Programmatic);
    }

    private int ReplaceEditorSelection(string replacement)
    {
        string text = TextEditor.Text ?? string.Empty;
        int start = Math.Clamp(TextEditor.SelectionStart, 0, text.Length);
        int length = Math.Clamp(TextEditor.SelectionLength, 0, text.Length - start);
        TextEditor.Text = text.Remove(start, length).Insert(start, replacement);

        TextEditor.SelectionStart = Math.Clamp(start + replacement.Length, 0, TextEditor.Text.Length);
        TextEditor.SelectionLength = 0;
        TextEditor.Focus(FocusState.Programmatic);
        return start;
    }

    private static void CopyTextToClipboard(string text)
    {
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }

    private static bool ClipboardContainsText()
    {
        try
        {
            return Clipboard.GetContent().Contains(StandardDataFormats.Text);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string GetControlCodeToolTip(MessageControlCodeEntry entry)
    {
        string insertText = entry.InsertText == "\n" ? "Line break" : entry.InsertText;
        return string.IsNullOrWhiteSpace(entry.Description)
            ? insertText
            : $"{insertText}\n{entry.Description}";
    }

    private int FindInsertedControlCode(string insertText, int originalStart)
    {
        string text = TextEditor.Text ?? string.Empty;
        if (insertText.Length == 0)
        {
            return Math.Clamp(originalStart, 0, text.Length);
        }

        int searchStart = Math.Clamp(originalStart - 1, 0, text.Length);
        int found = text.IndexOf(insertText, searchStart, StringComparison.Ordinal);
        return found >= 0 ? found : Math.Clamp(originalStart, 0, text.Length);
    }

    private static FontIcon CreateContextMenuIcon(string glyph)
        => new()
        {
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            Glyph = glyph,
        };
}
