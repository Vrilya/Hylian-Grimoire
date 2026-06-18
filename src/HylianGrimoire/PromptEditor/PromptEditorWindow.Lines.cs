using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.PromptEditor;

public sealed partial class PromptEditorWindow
{
    private void OnPromptSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating)
        {
            return;
        }

        RefreshSelectedLineView();
    }

    private void ShowSelectedLine()
    {
        using IDisposable update = BeginUpdate();
        if (PromptList.SelectedItem is not PromptEditorLine selected)
        {
            SelectedPromptText.Text = "Select a prompt";
            IconXBox.Value = double.NaN;
            TextXBox.Value = double.NaN;
            return;
        }

        SelectedPromptText.Text = selected.Label;
        IconXBox.Value = selected.IconX;
        TextXBox.Value = selected.TextX;
    }

    private void RefreshSelectedLineView()
    {
        ShowSelectedLine();
        UpdatePreview();
    }

    private void ReplaceLines(IEnumerable<PromptEditorLine> lines)
    {
        using IDisposable update = BeginUpdate();
        _lines.Clear();
        foreach (PromptEditorLine line in lines)
        {
            _lines.Add(line);
        }
    }

    private void ReplaceLine(int index, PromptEditorLine line)
    {
        using IDisposable update = BeginUpdate();
        _lines[index] = line;
        SelectLineIndex(index);
    }

    private void SelectLineIndex(int index)
    {
        using IDisposable update = BeginUpdate();
        PromptList.SelectedIndex = _lines.Count == 0
            ? -1
            : Math.Clamp(index, 0, _lines.Count - 1);
    }

    private int FindLineIndex(PromptEditorKind kind)
    {
        for (int index = 0; index < _lines.Count; index++)
        {
            if (_lines[index].Kind == kind)
            {
                return index;
            }
        }

        return -1;
    }

    private void SetControlsEnabled(bool enabled)
    {
        PromptList.IsEnabled = enabled;
        IconXBox.IsEnabled = enabled;
        TextXBox.IsEnabled = enabled;
        FramesButton.IsEnabled = enabled;
        GuidesButton.IsEnabled = enabled;
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
        StatusText.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
    }
}
