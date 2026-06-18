using Microsoft.UI.Xaml.Media.Imaging;

namespace HylianGrimoire.PromptEditor;

public sealed partial class PromptEditorWindow
{
    private void UpdatePreview()
    {
        if (_romData is null || _profile is null || _lines.Count == 0)
        {
            return;
        }

        try
        {
            PromptEditorKind selected = PromptList.SelectedItem is PromptEditorLine line
                ? line.Kind
                : _lines[0].Kind;
            Uri imageUri = PromptEditorPreviewRenderer.Render(
                _romData.DecompressedRom,
                _profile,
                _languageKey,
                _lines,
                selected,
                GuidesButton.IsChecked == true,
                FramesButton.IsChecked == true);
            PreviewImage.Source = new BitmapImage(imageUri);
        }
        catch (Exception ex)
        {
            SetStatus(UiOperationExceptionHandler.GetDisplayMessage("Prompt editor preview failed", ex));
        }
    }
}
