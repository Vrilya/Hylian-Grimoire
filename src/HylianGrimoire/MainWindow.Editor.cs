using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HylianGrimoire.Models;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _session.CurrentIndex < 0)
        {
            return;
        }

        string editorText = GetEditorText();
        string raw = CurrentGameProfile.EditorTextSyntax.FromDisplay(editorText);
        if (!string.Equals(_session.Entries[_session.CurrentIndex].Text, raw, StringComparison.Ordinal))
        {
            _session.Entries[_session.CurrentIndex].Text = raw;
            MarkDirty();
        }

        string normalized = CurrentGameProfile.EditorTextSyntax.ToDisplay(raw);
        if (normalized != editorText)
        {
            _updating = true;
            try
            {
                int caret = TextEditor.SelectionStart;
                TextEditor.Text = normalized;
                TextEditor.SelectionStart = Math.Min(caret, TextEditor.Text.Length);
            }
            finally
            {
                _updating = false;
            }
        }

        RefreshListItem(_session.CurrentIndex);
        UpdatePreview();
        RefreshMessageByteInspector();
    }

    private void OnTypeChange(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _session.CurrentIndex < 0)
        {
            return;
        }

        if (TypeCombo.SelectedItem is MessageTypeItem item)
        {
            if (_session.Entries[_session.CurrentIndex].Type == item.Value)
            {
                return;
            }

            _session.Entries[_session.CurrentIndex].Type = item.Value;
            MarkDirty();
            UpdatePreview();
            ApplySearchFilter();
        }
    }

    private void OnPosChange(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _session.CurrentIndex < 0)
        {
            return;
        }

        if (PositionCombo.SelectedItem is not MessagePositionItem item)
        {
            return;
        }

        if (_session.Entries[_session.CurrentIndex].Position != item.Value)
        {
            _session.Entries[_session.CurrentIndex].Position = item.Value;
            MarkDirty();
        }
    }

    private void OnWordWrapToggled(object sender, RoutedEventArgs e)
    {
        bool wrap = WordWrapSwitch.IsOn;
        TextEditor.TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
        ScrollViewer.SetHorizontalScrollBarVisibility(
            TextEditor,
            wrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto);
    }

    private void OnEditorFontSizeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EditorFontSizeBox.SelectedItem is ComboBoxItem item
            && item.Content is string text
            && double.TryParse(text, out double fontSize))
        {
            TextEditor.FontSize = fontSize;
        }
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
        AppDiagnostics.UpdateStatus(message);
    }

    private string GetEditorText() => TextEditor.Text.Replace("\r\n", "\n").Replace('\r', '\n');
}
