using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace HylianGrimoire.Textures;

public sealed partial class TextureManagerWindow
{
    private async Task<string?> PickOpenPngAsync()
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.FileTypeFilter.Add(".png");
        StorageFile? file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    private async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.FileTypeFilter.Add("*");
        StorageFolder? folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private async Task<string?> PickSavePngAsync(string textureName)
    {
        var picker = new FileSavePicker
        {
            SuggestedFileName = textureName,
        };
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.FileTypeChoices.Add("PNG image", [".png"]);
        StorageFile? file = await picker.PickSaveFileAsync();
        return file?.Path;
    }
}
