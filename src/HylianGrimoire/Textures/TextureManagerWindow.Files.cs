using System.Drawing.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace HylianGrimoire.Textures;

public sealed partial class TextureManagerWindow
{
    private async void OnExportTexture(object sender, RoutedEventArgs e)
    {
        if (_romData is null || GetSelectedTextureItem() is not TextureListItem item)
        {
            return;
        }

        string? path = await PickSavePngAsync(item.Texture.ExportName);
        if (path is null)
        {
            return;
        }

        try
        {
            using var bitmap = TextureRomService.Decode(_romData.DecompressedRom, item.Texture);
            bitmap.Save(path, ImageFormat.Png);
            SetLocalStatus("Exported texture successfully.");
            _onChanged(new TextureManagerChange($"Exported {item.Texture.Name}.", MutatedRom: false));
        }
        catch (Exception ex)
        {
            SetLocalStatus("Texture export failed.");
            await ShowErrorAsync("Failed to export texture", ex.Message);
        }
    }

    private async void OnReplaceTexture(object sender, RoutedEventArgs e)
    {
        if (_romData is null || GetSelectedTextureItem() is not TextureListItem item)
        {
            return;
        }

        string? path = await PickOpenPngAsync();
        if (path is null)
        {
            return;
        }

        try
        {
            TextureRomService.EncodeAndWrite(_romData.DecompressedRom, item.Texture, path);
            RefreshSelectedTexture();
            SetLocalStatus("Replaced texture successfully.");
            _onChanged(new TextureManagerChange($"Replaced {item.Texture.Name}.", MutatedRom: true));
        }
        catch (Exception ex)
        {
            SetLocalStatus("Texture replacement failed.");
            await ShowErrorAsync("Failed to replace texture", ex.Message);
        }
    }

    private async void OnExportFolder(object sender, RoutedEventArgs e)
    {
        if (_romData is null || !TextureCatalog.TryGetTextures(_romData.Profile, out IReadOnlyList<TextureDefinition>? textures))
        {
            return;
        }

        string? folder = await PickFolderAsync();
        if (folder is null)
        {
            return;
        }

        try
        {
            var progress = new Progress<int>(UpdateProgress);
            using IDisposable busy = ShowProgress("Exporting textures");
            byte[] rom = _romData.DecompressedRom.ToArray();
            int exported = await Task.Run(() => ExportTexturesToFolder(rom, textures, folder, progress));

            SetLocalStatus($"Exported folder successfully. {exported} textures exported.");
            _onChanged(new TextureManagerChange($"Exported {exported} textures.", MutatedRom: false));
        }
        catch (Exception ex)
        {
            SetLocalStatus("Folder export failed.");
            await ShowErrorAsync("Failed to export textures", ex.Message);
        }
    }

    private async void OnReplaceFolder(object sender, RoutedEventArgs e)
    {
        if (_romData is null || !TextureCatalog.TryGetTextures(_romData.Profile, out IReadOnlyList<TextureDefinition>? textures))
        {
            return;
        }

        string? folder = await PickFolderAsync();
        if (folder is null)
        {
            return;
        }

        try
        {
            var progress = new Progress<int>(UpdateProgress);
            using IDisposable busy = ShowProgress("Replacing textures");
            byte[] originalRom = _romData.DecompressedRom.ToArray();
            byte[] updatedRom = originalRom.ToArray();
            int replaced = await Task.Run(() => ReplaceTexturesFromFolder(updatedRom, textures, folder, progress));

            if (replaced > 0)
            {
                if (!originalRom.AsSpan().SequenceEqual(_romData.DecompressedRom))
                {
                    throw new InvalidOperationException("The loaded ROM changed while replacing textures. Try the folder replacement again.");
                }

                updatedRom.CopyTo(_romData.DecompressedRom, 0);
            }

            RefreshSelectedTexture();
            if (replaced > 0)
            {
                SetLocalStatus($"Replaced folder successfully. {replaced} textures replaced.");
                _onChanged(new TextureManagerChange($"Replaced {replaced} textures.", MutatedRom: true));
            }
            else
            {
                SetLocalStatus("No matching texture PNGs were found.");
                _onChanged(new TextureManagerChange("No matching texture PNGs found.", MutatedRom: false));
            }
        }
        catch (Exception ex)
        {
            SetLocalStatus("Folder replacement failed.");
            await ShowErrorAsync("Failed to replace textures", ex.Message);
        }
    }

    private static int ExportTexturesToFolder(
        byte[] rom,
        IReadOnlyList<TextureDefinition> textures,
        string folder,
        IProgress<int> progress)
    {
        int exported = 0;
        for (int i = 0; i < textures.Count; i++)
        {
            TextureDefinition texture = textures[i];
            string path = GetTextureFilePath(folder, texture);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var bitmap = TextureRomService.Decode(rom, texture);
            bitmap.Save(path, ImageFormat.Png);
            exported++;
            progress.Report(GetPercent(i + 1, textures.Count));
        }

        return exported;
    }

    private static int ReplaceTexturesFromFolder(
        byte[] rom,
        IReadOnlyList<TextureDefinition> textures,
        string folder,
        IProgress<int> progress)
    {
        int replaced = 0;
        for (int i = 0; i < textures.Count; i++)
        {
            TextureDefinition texture = textures[i];
            string path = GetTextureFilePath(folder, texture);
            if (File.Exists(path))
            {
                TextureRomService.EncodeAndWrite(rom, texture, path);
                replaced++;
            }

            progress.Report(GetPercent(i + 1, textures.Count));
        }

        return replaced;
    }

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

    private static string GetTextureFilePath(string root, TextureDefinition texture)
    {
        string[] groupParts = texture.Group
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizePathPart)
            .ToArray();

        return Path.Combine([root, .. groupParts, $"{SanitizePathPart(texture.ExportName)}.png"]);
    }

    private static string SanitizePathPart(string value)
    {
        HashSet<char> invalid = Path.GetInvalidFileNameChars().ToHashSet();
        return string.Concat(value.Select(ch => invalid.Contains(ch) ? '_' : ch));
    }
}
