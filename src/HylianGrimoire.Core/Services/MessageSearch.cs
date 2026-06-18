using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public static class MessageSearch
{
    public static bool Matches(MessageEntry entry, string searchText, IEditorTextSyntax syntax)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return entry.Id.ToString("x4").Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || $"0x{entry.Id:x4}".Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || entry.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || entry.GetDisplayText(syntax).Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
}
