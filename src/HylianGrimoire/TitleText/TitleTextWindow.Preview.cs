using Microsoft.UI.Xaml.Media.Imaging;

namespace HylianGrimoire.TitleText;

public sealed partial class TitleTextWindow
{
    private void UpdatePreview()
    {
        if (_romData is null || _profile is null)
        {
            return;
        }

        try
        {
            Uri imageUri = TitleTextPreviewRenderer.Render(
                _romData.DecompressedRom,
                _profile,
                _romData.FontResources,
                GetNoControllerLine(),
                GetPressStartLine(),
                GuidesButton.IsChecked == true,
                _languageIndex);
            PreviewImage.Source = new BitmapImage(imageUri);
        }
        catch (Exception ex)
        {
            SetStatus(UiOperationExceptionHandler.GetDisplayMessage("Title text preview failed", ex));
        }
    }
}
