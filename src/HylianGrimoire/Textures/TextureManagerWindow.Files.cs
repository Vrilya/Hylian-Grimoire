using HylianGrimoire.Services;
using Microsoft.UI.Xaml;

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
            PngFileWriter.Save(bitmap, path);
            SetLocalStatus("Exported texture successfully.");
            _onChanged(new TextureManagerChange($"Exported {item.Texture.Name}.", MutatedRom: false));
        }
        catch (Exception ex)
        {
            SetLocalStatus("Texture export failed.");
            await ShowOperationExceptionAsync("Failed to export texture", ex);
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
            await ShowOperationExceptionAsync("Failed to replace texture", ex);
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
            await ShowOperationExceptionAsync("Failed to export textures", ex);
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
            await ShowOperationExceptionAsync("Failed to replace textures", ex);
        }
    }
}
