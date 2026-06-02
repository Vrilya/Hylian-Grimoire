using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Services;

public sealed record MessageListViewResult(
    IReadOnlyList<int> VisibleEntryIndices,
    string StatusText,
    int? SelectedEntryIndex);

public static class MessageListViewService
{
    public static MessageListViewResult Build(
        IReadOnlyList<MessageEntry> entries,
        string searchText,
        GameKind gameKind,
        RomMessageData? romData,
        IEditorTextSyntax syntax,
        int selectedEntryIndex)
    {
        var visibleIndices = new List<int>();
        for (int index = 0; index < entries.Count; index++)
        {
            MessageEntry entry = entries[index];
            if (IsVisible(entry, gameKind, entries, romData)
                && MessageSearch.Matches(entry, searchText, syntax))
            {
                visibleIndices.Add(index);
            }
        }

        string statusText = string.IsNullOrWhiteSpace(searchText)
            ? $"{CountVisible(entries, gameKind, romData)} messages"
            : $"{visibleIndices.Count} matches";
        int? selectedVisibleIndex = selectedEntryIndex >= 0 && visibleIndices.Contains(selectedEntryIndex)
            ? selectedEntryIndex
            : null;

        return new MessageListViewResult(visibleIndices, statusText, selectedVisibleIndex);
    }

    public static int CountVisible(
        IReadOnlyList<MessageEntry> entries,
        GameKind gameKind,
        RomMessageData? romData)
        => entries.Count(entry => IsVisible(entry, gameKind, entries, romData));

    public static int GetVisibleOrdinal(
        IReadOnlyList<MessageEntry> entries,
        GameKind gameKind,
        RomMessageData? romData,
        int entryIndex)
    {
        int ordinal = 0;
        for (int index = 0; index < entries.Count; index++)
        {
            if (!IsVisible(entries[index], gameKind, entries, romData))
            {
                continue;
            }

            if (index == entryIndex)
            {
                return ordinal;
            }

            ordinal++;
        }

        return Math.Max(0, ordinal - 1);
    }

    public static int GetSelectedVisibleOrdinal(
        IReadOnlyList<MessageEntry> entries,
        GameKind gameKind,
        RomMessageData? romData,
        int selectedEntryIndex)
        => selectedEntryIndex >= 0
            ? GetVisibleOrdinal(entries, gameKind, romData, selectedEntryIndex)
            : 0;

    private static bool IsVisible(
        MessageEntry entry,
        GameKind gameKind,
        IReadOnlyList<MessageEntry> entries,
        RomMessageData? romData)
        => MessageVisibilityService.IsVisibleEditorEntry(entry, gameKind, entries, romData);
}
