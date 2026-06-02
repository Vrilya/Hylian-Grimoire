using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public sealed record MessageListOperationResult(
    bool Succeeded,
    int SelectedIndex,
    string Status,
    string? ErrorTitle = null,
    string? ErrorMessage = null)
{
    public static MessageListOperationResult Ok(int selectedIndex, string status) =>
        new(true, selectedIndex, status);

    public static MessageListOperationResult Fail(string title, string message) =>
        new(false, -1, string.Empty, title, message);
}

public static class MessageListService
{
    public static MessageListOperationResult AddAfterSelected(List<MessageEntry> entries, int selectedIndex, int id)
    {
        if (entries.Any(entry => entry.Id == id))
        {
            return MessageListOperationResult.Fail("Message ID already exists", $"Message 0x{id:x4} already exists.");
        }

        var template = IsValidIndex(entries, selectedIndex)
            ? entries[selectedIndex]
            : null;
        int insertIndex = IsValidIndex(entries, selectedIndex)
            ? selectedIndex + 1
            : entries.Count;

        var entry = new MessageEntry(
            id,
            template?.Type ?? 0,
            template?.Position ?? 0,
            template?.Bank ?? 0x07,
            0)
        {
            TableEndMarkerId = template?.TableEndMarkerId ?? (entries.FirstOrDefault()?.TableEndMarkerId ?? 0xfffd),
            TableHasFinalEndMarker = template?.TableHasFinalEndMarker ?? (entries.FirstOrDefault()?.TableHasFinalEndMarker ?? true),
            PreserveOffsetWithoutMessageData = template?.PreserveOffsetWithoutMessageData ?? false,
            CodecMetadata = template?.CodecMetadata,
        };

        entries.Insert(insertIndex, entry);
        return MessageListOperationResult.Ok(insertIndex, $"Added message 0x{id:x4}.");
    }

    public static MessageListOperationResult ChangeId(List<MessageEntry> entries, int selectedIndex, int id)
    {
        if (!IsValidIndex(entries, selectedIndex))
        {
            return MessageListOperationResult.Fail("No message selected", "Select a message ID first.");
        }

        MessageEntry entry = entries[selectedIndex];
        if (entry.Id == id)
        {
            return MessageListOperationResult.Ok(selectedIndex, string.Empty);
        }

        if (entries.Any(existing => existing != entry && existing.Id == id))
        {
            return MessageListOperationResult.Fail("Message ID already exists", $"Message 0x{id:x4} already exists.");
        }

        entry.Id = id;
        entries.RemoveAt(selectedIndex);

        int insertIndex = entries.FindIndex(existing => existing.Id > id);
        if (insertIndex < 0)
        {
            insertIndex = entries.Count;
        }

        entries.Insert(insertIndex, entry);
        return MessageListOperationResult.Ok(insertIndex, $"Changed message ID to 0x{id:x4}.");
    }

    public static MessageListOperationResult Delete(List<MessageEntry> entries, int selectedIndex)
    {
        if (!IsValidIndex(entries, selectedIndex))
        {
            return MessageListOperationResult.Fail("No message selected", "Select a message ID first.");
        }

        int id = entries[selectedIndex].Id;
        entries.RemoveAt(selectedIndex);
        if (entries.Count == 0)
        {
            return MessageListOperationResult.Ok(-1, "Deleted the last message.");
        }

        int nextIndex = Math.Min(selectedIndex, entries.Count - 1);
        return MessageListOperationResult.Ok(nextIndex, $"Deleted message 0x{id:x4}.");
    }

    public static MessageListOperationResult Move(List<MessageEntry> entries, int fromIndex, int toIndex)
    {
        if (!IsValidIndex(entries, fromIndex)
            || !IsValidIndex(entries, toIndex)
            || fromIndex == toIndex)
        {
            return MessageListOperationResult.Ok(fromIndex, string.Empty);
        }

        MessageEntry entry = entries[fromIndex];
        entries.RemoveAt(fromIndex);
        entries.Insert(toIndex, entry);
        return MessageListOperationResult.Ok(toIndex, $"Moved message 0x{entry.Id:x4}.");
    }

    public static MessageListOperationResult MoveUnderId(List<MessageEntry> entries, int fromIndex, int targetId)
    {
        if (!IsValidIndex(entries, fromIndex))
        {
            return MessageListOperationResult.Ok(fromIndex, string.Empty);
        }

        MessageEntry entry = entries[fromIndex];
        int targetIndex = entries.FindIndex(existing => existing.Id == targetId);
        if (targetIndex < 0)
        {
            return MessageListOperationResult.Fail("Message ID not found", $"Message 0x{targetId:x4} was not found.");
        }

        if (targetIndex == fromIndex)
        {
            return MessageListOperationResult.Ok(fromIndex, string.Empty);
        }

        entries.RemoveAt(fromIndex);
        if (fromIndex < targetIndex)
        {
            targetIndex--;
        }

        int insertIndex = targetIndex + 1;
        entries.Insert(insertIndex, entry);
        return MessageListOperationResult.Ok(insertIndex, $"Moved message 0x{entry.Id:x4} under 0x{targetId:x4}.");
    }

    private static bool IsValidIndex(List<MessageEntry> entries, int index) =>
        index >= 0 && index < entries.Count;
}
