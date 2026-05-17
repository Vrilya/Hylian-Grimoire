using Microsoft.UI.Xaml;
using HylianGrimoire.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private async void OnLoadFiles(object sender, RoutedEventArgs e)
    {
        var binPath = await PickOpenFileAsync([".bin"]);
        if (binPath is null)
        {
            return;
        }

        var tblPath = await PickOpenFileAsync([".tbl"]);
        if (tblPath is null)
        {
            return;
        }

        try
        {
            _entries = MessageFileService.LoadTableFiles(tblPath, binPath);
            _tblPath = tblPath;
            _binPath = binPath;
            MarkClean();
            ClearSearch();
            PopulateList();
            SetStatus($"Loaded {_entries.Count} messages.");

            if (_items.Count > 0)
            {
                MessageList.SelectedIndex = 0;
                ShowEntry(_items[0].Index);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to load files", ex.Message);
        }
    }

    private async void OnSaveFiles(object sender, RoutedEventArgs e)
    {
        if (_entries.Count == 0 || _tblPath is null || _binPath is null)
        {
            await ShowInfoAsync("Nothing to save", "No messages loaded.");
            return;
        }

        CommitCurrent();
        _ = await WriteFilesAsync(_tblPath, _binPath);
    }

    private async void OnSaveAsFiles(object sender, RoutedEventArgs e)
    {
        if (_entries.Count == 0)
        {
            await ShowInfoAsync("Nothing to save", "No messages loaded.");
            return;
        }

        CommitCurrent();
        _ = await SaveCurrentFilesAsAsync();
    }

    private async void OnExportAsHeader(object sender, RoutedEventArgs e)
    {
        if (_entries.Count == 0)
        {
            await ShowInfoAsync("Nothing to export", "No messages loaded.");
            return;
        }

        CommitCurrent();

        string? path = await PickSaveFileAsync(".h", "message_data_static_NES.h");
        if (path is null)
        {
            return;
        }

        try
        {
            MessageFileService.ExportHeader(_entries, path);
            SetStatus($"Exported C header to {Path.GetFileName(path)}.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to export", ex.Message);
        }
    }

    private async void OnImportHeader(object sender, RoutedEventArgs e)
    {
        string? path = await PickOpenFileAsync([".h"]);
        if (path is null)
        {
            return;
        }

        try
        {
            CommitCurrent();

            _entries = MessageFileService.ImportHeader(path);
            _tblPath = null;
            _binPath = null;
            _currentIdx = -1;
            MarkClean();
            ClearSearch();

            PopulateList();
            SetStatus($"Imported {_entries.Count} messages from {Path.GetFileName(path)}.");

            if (_items.Count > 0)
            {
                MessageList.SelectedIndex = 0;
                ShowEntry(_items[0].Index);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to import header", ex.Message);
        }
    }

    private async Task<bool> WriteFilesAsync(string tblPath, string binPath)
    {
        try
        {
            MessageFileService.SaveTableFiles(_entries, tblPath, binPath);
            MarkClean();
            SetStatus($"Saved to {Path.GetFileName(tblPath)} and {Path.GetFileName(binPath)}.");
            return true;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to save", ex.Message);
            return false;
        }
    }

    private async Task<bool> SaveCurrentFilesAsAsync()
    {
        string? binPath = await PickSaveFileAsync(".bin", "nes_message_data_static.bin");
        if (binPath is null)
        {
            return false;
        }

        string tblPath = Path.ChangeExtension(binPath, ".tbl");

        if (!await WriteFilesAsync(tblPath, binPath))
        {
            return false;
        }

        _tblPath = tblPath;
        _binPath = binPath;
        return true;
    }

    private async Task<bool> SaveCurrentFilesForCloseAsync()
    {
        if (_entries.Count == 0)
        {
            MarkClean();
            return true;
        }

        CommitCurrent();
        return _tblPath is not null && _binPath is not null
            ? await WriteFilesAsync(_tblPath, _binPath)
            : await SaveCurrentFilesAsAsync();
    }

    private async Task<string?> PickOpenFileAsync(IReadOnlyList<string> extensions)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.List,
        };
        picker.FileTypeFilter.Clear();
        foreach (string extension in extensions)
        {
            picker.FileTypeFilter.Add(extension);
        }

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    private async Task<string?> PickSaveFileAsync(string extension, string suggestedFileName)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName,
        };
        picker.FileTypeChoices.Add($"{extension.TrimStart('.').ToUpperInvariant()} files", [extension]);

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }
}
