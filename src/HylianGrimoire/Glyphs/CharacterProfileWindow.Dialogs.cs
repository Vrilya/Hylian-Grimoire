using System.Drawing;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileWindow
{
    private async Task<string?> PickImageAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail,
        };
        picker.FileTypeFilter.Add(".png");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    private async Task ShowDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };
        _ = await dialog.ShowAsync();
    }

    private static bool HasSameSize(string replacementPath, string originalPath)
    {
        using var replacement = new Bitmap(replacementPath);
        using var original = new Bitmap(originalPath);
        return replacement.Width == original.Width && replacement.Height == original.Height;
    }
}
