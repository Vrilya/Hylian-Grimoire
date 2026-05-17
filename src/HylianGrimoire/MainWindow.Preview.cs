using Microsoft.UI.Xaml;
using HylianGrimoire.Codecs;
using HylianGrimoire.Models;
using HylianGrimoire.Preview;
using HylianGrimoire.Services;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void OnTogglePreview(object sender, RoutedEventArgs e)
    {
        if (_previewWindow is not null)
        {
            _previewWindow.Close();
            return;
        }

        OpenPreviewWindow();
    }

    private void OpenPreviewWindow()
    {
        if (_previewWindow is null)
        {
            _previewWindow = new OotPreviewWindow();
            PreviewToggle.IsChecked = true;
            _previewWindow.Closed += (_, _) =>
            {
                _previewWindow = null;
                PreviewToggle.IsChecked = false;
            };
        }

        UpdatePreview();
        _previewWindow.Activate();
    }

    private void UpdatePreview()
    {
        if (_previewWindow is null)
        {
            return;
        }

        if (_currentIdx < 0 || _currentIdx >= _entries.Count)
        {
            _previewWindow.SetMessage(OotPreviewStyle.Black, Array.Empty<MessageToken>());
            return;
        }

        var entry = _entries[_currentIdx];
        try
        {
            _previewWindow.SetMessage(
                MessageTypeCatalog.ToPreviewStyle(entry.Type),
                MessageTextSyntax.FromEditorText(MessageTextSyntax.FromDisplay(GetEditorText())));
        }
        catch (InvalidDataException ex)
        {
            SetStatus($"Preview not updated: {ex.Message}");
        }
    }
}
