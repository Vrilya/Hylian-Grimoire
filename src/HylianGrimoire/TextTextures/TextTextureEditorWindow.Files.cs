using System.Drawing.Imaging;
using HylianGrimoire.Textures;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private async void OnSavePng(object sender, RoutedEventArgs e)
    {
        if (_lastGenerated is null)
        {
            return;
        }

        string? path = await PickSavePngAsync(CreateSuggestedFileName());
        if (path is null)
        {
            return;
        }

        try
        {
            _lastGenerated.Save(path, ImageFormat.Png);
            SetStatus("Saved generated text texture.");
            _onChanged(new TextureManagerChange("Saved generated text texture.", MutatedRom: false));
        }
        catch (Exception ex)
        {
            SetStatus("PNG save failed.");
            await ShowOperationExceptionAsync("Failed to save PNG", ex);
        }
    }

    private async void OnApplyToRom(object sender, RoutedEventArgs e)
    {
        if (_romData is null || GetSelectedTarget() is not TextTextureTargetItem item || _lastGenerated is null)
        {
            return;
        }

        try
        {
            if (item.PauseHeaderTarget is { } pauseTarget)
            {
                ApplyPauseHeaderToRom(_romData.DecompressedRom, pauseTarget, _lastGenerated);
            }
            else if (item.GameOverTarget is { } gameOverTarget)
            {
                ApplyGameOverToRom(_romData.DecompressedRom, gameOverTarget, _lastGenerated);
            }
            else if (item.Texture is { } texture)
            {
                TextureRomService.EncodeAndWrite(_romData.DecompressedRom, texture, _lastGenerated);
            }
            else
            {
                throw new InvalidOperationException("No texture target is selected.");
            }

            RefreshPreview();
            SetStatus($"Applied {item.StatusLabel}.");
            _onChanged(new TextureManagerChange($"Replaced {item.StatusLabel}.", MutatedRom: true));
        }
        catch (Exception ex)
        {
            SetStatus("ROM replacement failed.");
            await ShowOperationExceptionAsync("Failed to apply texture", ex);
        }
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

    private string CreateSuggestedFileName()
    {
        string text = _selectedTextureKind switch
        {
            TextTextureKind.BossTitleCards => BossBottomTextBox.Text.Trim().Length > 0 ? BossBottomTextBox.Text.Trim() : BossTopTextBox.Text.Trim(),
            TextTextureKind.EndTitles => PrimaryTextBox.Text.Trim(),
            _ => PrimaryTextBox.Text.Trim(),
        };
        if (text.Length == 0)
        {
            return SelectedTextureKind.EmptyFileName;
        }

        HashSet<char> invalid = Path.GetInvalidFileNameChars().ToHashSet();
        return string.Concat(text.Select(ch => invalid.Contains(ch) ? '_' : ch)).Replace(' ', '_');
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };

        await dialog.ShowAsync();
    }

    private async Task ShowOperationExceptionAsync(string title, Exception exception, string? recoveryMessage = null)
    {
        await UiOperationExceptionHandler.ShowAsync(title, exception, ShowErrorAsync, recoveryMessage);
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
        StatusText.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _lastGenerated?.Dispose();
        _lastGenerated = null;
        try
        {
            if (Directory.Exists(_previewRoot))
            {
                Directory.Delete(_previewRoot, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
