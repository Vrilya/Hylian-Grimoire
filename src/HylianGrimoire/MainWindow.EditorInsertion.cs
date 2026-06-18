using HylianGrimoire.Services;
using Microsoft.UI.Xaml;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
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
}
