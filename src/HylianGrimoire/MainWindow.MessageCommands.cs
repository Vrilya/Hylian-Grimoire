using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

        await ApplyMessageListOperationAsync(MessageListService.AddAfterSelected(_session.Entries, _session.CurrentIndex, id.Value));
    }

    private async void OnChangeMessageId(object sender, RoutedEventArgs e)
    {
        if (_session.CurrentIndex < 0 || _session.CurrentIndex >= _session.Entries.Count)
        {
            await ShowInfoAsync("No message selected", "Select a message ID first.");
            return;
        }

        CommitCurrent();

        var entry = _session.Entries[_session.CurrentIndex];
        int? id = await PromptForMessageIdAsync("Change message ID", "Change", $"0x{entry.Id:x4}");
        if (id is null || id.Value == entry.Id)
        {
            return;
        }

        await ApplyMessageListOperationAsync(MessageListService.ChangeId(_session.Entries, _session.CurrentIndex, id.Value));
    }

    private async void OnDeleteMessageId(object sender, RoutedEventArgs e)
    {
        if (_session.CurrentIndex < 0 || _session.CurrentIndex >= _session.Entries.Count)
        {
            await ShowInfoAsync("No message selected", "Select a message ID first.");
            return;
        }

        var entry = _session.Entries[_session.CurrentIndex];
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
        MessageListOperationResult result = MessageListService.Delete(_session.Entries, _session.CurrentIndex);
        _session.CurrentIndex = -1;
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

    private void MoveMessageEntry(int fromIndex, int toIndex)
    {
        CommitCurrent();
        ApplyMessageListOperation(MessageListService.Move(_session.Entries, fromIndex, toIndex));
    }

    private async Task MoveMessageEntryUnderIdAsync(int fromIndex)
    {
        if (fromIndex < 0 || fromIndex >= _session.Entries.Count)
        {
            return;
        }

        int? targetId = await PromptForMessageIdAsync("Move under message ID", "Move");
        if (targetId is null)
        {
            return;
        }

        CommitCurrent();
        await ApplyMessageListOperationAsync(MessageListService.MoveUnderId(_session.Entries, fromIndex, targetId.Value));
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

        _session.CurrentIndex = -1;
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
}
