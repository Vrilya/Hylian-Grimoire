using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using HylianGrimoire.Interop;
using HylianGrimoire.Models;
using HylianGrimoire.Services;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void PopulateList()
    {
        ApplySearchFilter();
    }

    private void ClearSearch()
    {
        _session.SearchText = string.Empty;
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
        if (idx < 0 || idx == _session.CurrentIndex)
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
        flyout.Opened += (_, _) => MouseCursor.ResetToArrow();

        var copyIdItem = new MenuFlyoutItem
        {
            Text = "Copy ID",
            Icon = new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"), Glyph = "\uE8C8" },
        };
        copyIdItem.Click += (_, _) => CopyMessageIdToClipboard(item.Entry.Id);
        flyout.Items.Add(copyIdItem);
        flyout.Items.Add(new MenuFlyoutSeparator());

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
            IsEnabled = item.Index >= 0 && item.Index < _session.Entries.Count - 1,
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
        MouseCursor.ResetToArrow();
        e.Handled = true;
    }

    private void CopyMessageIdToClipboard(int id)
    {
        var package = new DataPackage();
        string text = $"0x{id:x4}";
        package.SetText(text);
        Clipboard.SetContent(package);
        SetStatus($"Copied message {text}");
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _session.SearchText = SearchBox.Text.Trim();
        ApplySearchFilter();
    }

    private void ApplySearchFilter()
    {
        if (!HasActiveProject)
        {
            MessageList.SelectedItem = null;
            _items.Clear();
            SearchStatusText.Text = string.Empty;
            return;
        }

        int selectedEntryIndex = MessageList.SelectedItem is MessageItem selectedItem ? selectedItem.Index : _session.CurrentIndex;
        MessageListViewResult view = MessageListViewService.Build(
            _session.Entries,
            _session.SearchText,
            CurrentGameProfile.Kind,
            _session.RomData,
            CurrentGameProfile.EditorTextSyntax,
            selectedEntryIndex);

        _items.Clear();
        foreach (int idx in view.VisibleEntryIndices)
        {
            _items.Add(new MessageItem(_session.Entries[idx], idx));
        }

        SearchStatusText.Text = view.StatusText;

        if (view.SelectedEntryIndex is int visibleEntryIndex)
        {
            var visibleItem = _items.FirstOrDefault(i => i.Index == visibleEntryIndex);
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
        MessageList.SelectedItem = null;
        TypeCombo.SelectedIndex = -1;
        PositionCombo.SelectedIndex = -1;
        UpdateMajorasMaskMetadataPanel(null);
        TextEditor.Text = string.Empty;
        UpdatePreview();
        RefreshMessageByteInspector();
    }

    private int CountVisibleMessageEntries()
        => MessageListViewService.CountVisible(
            _session.Entries,
            CurrentGameProfile.Kind,
            _session.RomData);

    private int GetVisibleMessageOrdinal(int entryIndex)
        => MessageListViewService.GetVisibleOrdinal(
            _session.Entries,
            CurrentGameProfile.Kind,
            _session.RomData,
            entryIndex);

    private int GetSelectedVisibleOrdinal()
        => MessageListViewService.GetSelectedVisibleOrdinal(
            _session.Entries,
            CurrentGameProfile.Kind,
            _session.RomData,
            _session.CurrentIndex);

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
}
