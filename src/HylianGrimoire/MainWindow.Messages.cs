using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private async void OnAddMessageId(object sender, RoutedEventArgs e)
    {
        CommitCurrent();

        int? id = await PromptForMessageIdAsync("Add message ID", "Add");
        if (id is null)
        {
            return;
        }

        await ApplyMessageListOperationAsync(MessageListService.AddAfterSelected(_entries, _currentIdx, id.Value));
    }

    private async void OnChangeMessageId(object sender, RoutedEventArgs e)
    {
        if (_currentIdx < 0 || _currentIdx >= _entries.Count)
        {
            await ShowInfoAsync("No message selected", "Select a message ID first.");
            return;
        }

        CommitCurrent();

        var entry = _entries[_currentIdx];
        int? id = await PromptForMessageIdAsync("Change message ID", "Change", $"0x{entry.Id:x4}");
        if (id is null || id.Value == entry.Id)
        {
            return;
        }

        await ApplyMessageListOperationAsync(MessageListService.ChangeId(_entries, _currentIdx, id.Value));
    }

    private async void OnDeleteMessageId(object sender, RoutedEventArgs e)
    {
        if (_currentIdx < 0 || _currentIdx >= _entries.Count)
        {
            await ShowInfoAsync("No message selected", "Select a message ID first.");
            return;
        }

        var entry = _entries[_currentIdx];
        var dialog = new ContentDialog
        {
            Title = "Delete message ID",
            Content = $"Delete message 0x{entry.Id:x4}?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        int deletedId = entry.Id;
        MessageListOperationResult result = MessageListService.Delete(_entries, _currentIdx);
        _currentIdx = -1;
        if (!result.Succeeded)
        {
            await ShowErrorAsync(result.ErrorTitle ?? "Delete failed", result.ErrorMessage ?? "The message could not be deleted.");
            return;
        }

        if (result.SelectedIndex < 0)
        {
            ClearEditor();
        }
        else
        {
            SelectEntry(result.SelectedIndex);
        }

        MarkDirty();
        SetStatus(string.IsNullOrEmpty(result.Status) ? $"Deleted message 0x{deletedId:x4}." : result.Status);
    }

    private void PopulateList()
    {
        ApplySearchFilter();
    }

    private void ClearSearch()
    {
        _searchText = string.Empty;
        SearchBox.Text = string.Empty;
    }

    private void SelectEntry(int entryIndex)
    {
        ClearSearch();
        PopulateList();

        var item = _items.FirstOrDefault(item => item.Index == entryIndex);
        if (item is not null)
        {
            MessageList.SelectedItem = item;
            ScrollMessageItemIntoView(item);
            ShowEntry(entryIndex);
        }
    }

    private void RefreshListItem(int idx)
    {
        var item = _items.FirstOrDefault(i => i.Index == idx);
        if (item is null)
        {
            return;
        }

        item.Refresh();
    }

    private void OnListSelect(object sender, SelectionChangedEventArgs e)
    {
        int idx = MessageList.SelectedItem is MessageItem item ? item.Index : -1;
        if (idx < 0 || idx == _currentIdx)
        {
            return;
        }

        CommitCurrent();
        ShowEntry(idx);
    }

    private void OnMessageItemRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not MessageItem item)
        {
            return;
        }

        var flyout = new MenuFlyout();
        flyout.Opened += (_, _) => ResetCursorToArrow();

        var moveUpItem = new MenuFlyoutItem
        {
            Text = "Move up",
            Icon = new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"), Glyph = "\uE74A" },
            IsEnabled = item.Index > 0,
        };
        moveUpItem.Click += (_, _) => MoveMessageEntry(item.Index, item.Index - 1);
        flyout.Items.Add(moveUpItem);

        var moveDownItem = new MenuFlyoutItem
        {
            Text = "Move down",
            Icon = new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"), Glyph = "\uE74B" },
            IsEnabled = item.Index >= 0 && item.Index < _entries.Count - 1,
        };
        moveDownItem.Click += (_, _) => MoveMessageEntry(item.Index, item.Index + 1);
        flyout.Items.Add(moveDownItem);

        flyout.Items.Add(new MenuFlyoutSeparator());

        var moveUnderItem = new MenuFlyoutItem
        {
            Text = "Move under message ID...",
            Icon = new SymbolIcon(Symbol.GoToToday),
        };
        moveUnderItem.Click += async (_, _) => await MoveMessageEntryUnderIdAsync(item.Index);
        flyout.Items.Add(moveUnderItem);

        flyout.ShowAt(element, new FlyoutShowOptions
        {
            Position = e.GetPosition(element),
        });
        ResetCursorToArrow();
        e.Handled = true;
    }

    private void ShowEntry(int idx)
    {
        _updating = true;
        try
        {
            var entry = _entries[idx];
            _currentIdx = idx;

            IdBox.Text = $"0x{entry.Id:x4}";
            TypeCombo.SelectedItem = MessageTypeCatalog.Items.FirstOrDefault(item => item.Value == entry.Type);
            PositionCombo.SelectedIndex = entry.Position < PositionNames.Length ? entry.Position : -1;
            TextEditor.Text = MessageTextSyntax.ToDisplay(entry.Text);
            UpdatePreview();

            SetStatus($"Editing message 0x{entry.Id:x4}  ({GetVisibleMessageOrdinal(idx) + 1} / {CountVisibleMessageEntries()})");
        }
        finally
        {
            _updating = false;
        }
    }

    private void CommitCurrent()
    {
        if (_currentIdx < 0 || _currentIdx >= _entries.Count)
        {
            return;
        }

        string editorText = MessageTextSyntax.FromDisplay(GetEditorText());
        if (MessageTextSyntax.TryNormalizeEditorText(editorText, out string normalized))
        {
            editorText = normalized;
        }

        _entries[_currentIdx].Text = editorText;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text.Trim();
        ApplySearchFilter();
    }

    private void MoveMessageEntry(int fromIndex, int toIndex)
    {
        CommitCurrent();
        ApplyMessageListOperation(MessageListService.Move(_entries, fromIndex, toIndex));
    }

    private async Task MoveMessageEntryUnderIdAsync(int fromIndex)
    {
        if (fromIndex < 0 || fromIndex >= _entries.Count)
        {
            return;
        }

        int? targetId = await PromptForMessageIdAsync("Move under message ID", "Move");
        if (targetId is null)
        {
            return;
        }

        CommitCurrent();
        await ApplyMessageListOperationAsync(MessageListService.MoveUnderId(_entries, fromIndex, targetId.Value));
    }

    private void ApplyMessageListOperation(MessageListOperationResult result)
    {
        if (!result.Succeeded)
        {
            return;
        }

        if (!string.IsNullOrEmpty(result.Status))
        {
            MarkDirty();
        }

        _currentIdx = -1;
        if (result.SelectedIndex >= 0)
        {
            SelectEntry(result.SelectedIndex);
        }
        else
        {
            ClearEditor();
        }

        if (!string.IsNullOrEmpty(result.Status))
        {
            SetStatus(result.Status);
        }

        UpdateRomToolState();
    }

    private async Task ApplyMessageListOperationAsync(MessageListOperationResult result)
    {
        if (!result.Succeeded)
        {
            await ShowErrorAsync(result.ErrorTitle ?? "Message operation failed", result.ErrorMessage ?? "The message operation could not be completed.");
            return;
        }

        ApplyMessageListOperation(result);
    }

    private void ApplySearchFilter()
    {
        int selectedEntryIndex = MessageList.SelectedItem is MessageItem selectedItem ? selectedItem.Index : _currentIdx;

        _items.Clear();
        for (int idx = 0; idx < _entries.Count; idx++)
        {
            if (IsVisibleMessageEntry(_entries[idx])
                && MessageSearch.Matches(_entries[idx], _searchText))
            {
                _items.Add(new MessageItem(_entries[idx], idx));
            }
        }

        SearchStatusText.Text = string.IsNullOrWhiteSpace(_searchText)
            ? $"{CountVisibleMessageEntries()} messages"
            : $"{_items.Count} matches";

        if (selectedEntryIndex >= 0)
        {
            var visibleItem = _items.FirstOrDefault(i => i.Index == selectedEntryIndex);
            if (visibleItem is not null)
            {
                MessageList.SelectedItem = visibleItem;
                ScrollMessageItemIntoView(visibleItem);
            }
        }
    }

    private void ScrollMessageItemIntoView(MessageItem item)
    {
        DispatcherQueue.TryEnqueue(() => MessageList.ScrollIntoView(item));
    }

    private void ClearEditor()
    {
        PopulateList();
        IdBox.Text = string.Empty;
        TypeCombo.SelectedIndex = -1;
        PositionCombo.SelectedIndex = -1;
        TextEditor.Text = string.Empty;
        UpdatePreview();
    }

    private bool IsVisibleMessageEntry(MessageEntry entry)
        => !ShouldHideFontOrderEntry() || entry.Id != FontOrderCodec.MessageId;

    private int CountVisibleMessageEntries()
        => ShouldHideFontOrderEntry()
            ? _entries.Count(entry => entry.Id != FontOrderCodec.MessageId)
            : _entries.Count;

    private int GetVisibleMessageOrdinal(int entryIndex)
    {
        int ordinal = 0;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (!IsVisibleMessageEntry(_entries[i]))
            {
                continue;
            }

            if (i == entryIndex)
            {
                return ordinal;
            }

            ordinal++;
        }

        return Math.Max(0, ordinal - 1);
    }

    private int GetSelectedVisibleOrdinal()
        => _currentIdx >= 0 ? GetVisibleMessageOrdinal(_currentIdx) : 0;

    private void SelectVisibleOrdinal(int ordinal)
    {
        if (_items.Count == 0)
        {
            ClearEditor();
            return;
        }

        MessageItem item = _items[Math.Clamp(ordinal, 0, _items.Count - 1)];
        MessageList.SelectedItem = item;
        ScrollMessageItemIntoView(item);
        ShowEntry(item.Index);
    }

    private bool ShouldHideFontOrderEntry()
        => MessageExportService.ShouldHideFontOrderEntry(_entries, _romData);

    private static void ResetCursorToArrow()
    {
        IntPtr cursor = LoadCursor(IntPtr.Zero, 32512);
        if (cursor != IntPtr.Zero)
        {
            _ = SetCursor(cursor);
        }
    }

}
