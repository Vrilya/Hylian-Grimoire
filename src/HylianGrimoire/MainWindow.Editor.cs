using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating || _currentIdx < 0)
        {
            return;
        }

        string editorText = GetEditorText();
        string raw = MessageTextSyntax.FromDisplay(editorText);
        if (!string.Equals(_entries[_currentIdx].Text, raw, StringComparison.Ordinal))
        {
            _entries[_currentIdx].Text = raw;
            MarkDirty();
        }

        string normalized = MessageTextSyntax.ToDisplay(raw);
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

        RefreshListItem(_currentIdx);
        UpdatePreview();
    }

    private void OnTypeChange(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _currentIdx < 0)
        {
            return;
        }

        if (TypeCombo.SelectedItem is MessageTypeItem item)
        {
            if (_entries[_currentIdx].Type == item.Value)
            {
                return;
            }

            _entries[_currentIdx].Type = item.Value;
            MarkDirty();
            UpdatePreview();
        }
    }

    private void OnPosChange(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || _currentIdx < 0)
        {
            return;
        }

        if (_entries[_currentIdx].Position != PositionCombo.SelectedIndex)
        {
            _entries[_currentIdx].Position = PositionCombo.SelectedIndex;
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

    private void OnOpenGlyphOverrides(object sender, RoutedEventArgs e)
    {
        if (_glyphOverrideWindow is null)
        {
            _glyphOverrideWindow = new Glyphs.GlyphOverrideWindow();
            _glyphOverrideWindow.Closed += (_, _) => _glyphOverrideWindow = null;
        }

        _glyphOverrideWindow.Activate();
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private string GetEditorText() => TextEditor.Text.Replace("\r\n", "\n").Replace('\r', '\n');
}
