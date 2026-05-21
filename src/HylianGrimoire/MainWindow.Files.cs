using Microsoft.UI.Xaml;
using HylianGrimoire.Codecs;
using HylianGrimoire.Headers;
using HylianGrimoire.Models;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private async void OnLoadDocument(object sender, RoutedEventArgs e)
    {
        string? path = await PickOpenFileAsync([".z64", ".bin", ".h"]);
        if (path is null)
        {
            return;
        }

        string extension = Path.GetExtension(path).ToLowerInvariant();
        switch (extension)
        {
            case ".z64":
                await LoadRomDocumentAsync(path);
                break;
            case ".bin":
                string? tblPath = await PickOpenFileAsync([".tbl"]);
                if (tblPath is not null)
                {
                    await LoadTableFilesDocumentAsync(path, tblPath);
                }
                break;
            case ".h":
                await LoadHeaderDocumentAsync(path);
                break;
            default:
                await ShowInfoAsync("Unsupported file", "Choose a .z64, .bin, or .h file.");
                break;
        }
    }

    private async void OnSaveFiles(object sender, RoutedEventArgs e)
    {
        if (_entries.Count == 0)
        {
            await ShowInfoAsync("Nothing to save", "No messages loaded.");
            return;
        }

        CommitCurrent();

        _ = _documentKind switch
        {
            DocumentKind.DataFiles when _tblPath is not null && _binPath is not null => await WriteFilesAsync(_tblPath, _binPath),
            DocumentKind.Header when _headerPath is not null => await WriteHeaderAsync(_headerPath),
            DocumentKind.Rom when _romPath is not null && _romData is not null => await WriteRomAsync(_romPath, _romData),
            _ => await SaveCurrentFilesAsAsync(),
        };
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

    private async void OnSaveAsRom(object sender, RoutedEventArgs e)
    {
        if (_entries.Count == 0)
        {
            await ShowInfoAsync("Nothing to save", "No messages loaded.");
            return;
        }

        if (_romData is null)
        {
            await ShowInfoAsync("No ROM loaded", "Load a ROM before saving as ROM.");
            return;
        }

        CommitCurrent();

        string suggestedFileName = _romPath is null
            ? "Hylian Grimoire.z64"
            : Path.GetFileName(_romPath);
        string? path = await PickSaveFileAsync(".z64", suggestedFileName);
        if (path is null)
        {
            return;
        }

        bool? compress = await PromptForRomCompressionAsync();
        if (compress is null)
        {
            return;
        }

        if (await WriteRomAsync(path, _romData, compress))
        {
            _documentKind = DocumentKind.Rom;
            _romPath = path;
            UpdateWindowTitle();
            UpdateLanguageMenuState();
        }
    }

    private async void OnExportAsHeader(object sender, RoutedEventArgs e)
    {
        if (_entries.Count == 0)
        {
            await ShowInfoAsync("Nothing to export", "No messages loaded.");
            return;
        }

        CommitCurrent();

        HeaderExportChoice? choice = await PromptForHeaderExportFormatAsync(CanExportAllRomLanguages());
        if (choice is null)
        {
            return;
        }

        string? path = await PickSaveFileAsync(".h", "message_data_static_NES.h");
        if (path is null)
        {
            return;
        }

        try
        {
            if (choice.Value.AllRomLanguages)
            {
                ExportAllRomLanguagesToHeader(path);
            }
            else
            {
                MessageFileService.ExportHeader(
                    MessageExportService.GetHeaderExportEntries(_entries, choice.Value.Format, _romData),
                    path,
                    choice.Value.Format);
            }

            SetStatus($"Exported C header to {Path.GetFileName(path)}.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to export", ex.Message);
        }
    }

    private async void OnImportHeaderIntoRom(object sender, RoutedEventArgs e)
    {
        if (_romData is null)
        {
            await ShowInfoAsync("No ROM loaded", "Load a ROM before importing a header into it.");
            return;
        }

        if (_romData.ActiveSection != RomMessageSection.Messages)
        {
            await ShowInfoAsync("Credits mode is active", "Switch back to message IDs before importing a header into the ROM.");
            return;
        }

        string? path = await PickOpenFileAsync([".h"]);
        if (path is null)
        {
            return;
        }

        try
        {
            CommitCurrent();

            List<CHeaderMessageSlot> availableWesternSlots = HeaderDocumentService.GetAvailableWesternSlots(path);
            if (availableWesternSlots.Count == 0)
            {
                await ShowInfoAsync("Nothing to import", "The selected header did not contain any western message slots.");
                return;
            }

            HeaderRomImportChoice? choice = await PromptForHeaderRomImportAsync(
                availableWesternSlots,
                _romData.Profile.MessageBanks.Count >= 3);
            if (choice is null)
            {
                return;
            }

            IReadOnlyDictionary<int, List<MessageEntry>> replacementBanks = choice.Value.AllWesternLanguages
                ? HeaderDocumentService.BuildAllWesternRomImports(path, _romData, _entries)
                : HeaderDocumentService.BuildSelectedRomImport(
                    path,
                    choice.Value.SelectedSlot,
                    _romData.ActiveMessageBankIndex,
                    _entries);
            if (replacementBanks.Count == 0)
            {
                await ShowInfoAsync("Nothing to import", "The selected header did not contain any matching western message slots.");
                return;
            }

            _romData = RomMessageService.ReplaceMessageBanks(_romData, _entries, replacementBanks);
            _entries = _romData.Entries;
            _currentIdx = -1;
            RefreshAuxiliaryWindowsForLoadedDocument();
            MarkRomBankDirty();
            UpdateWindowTitle();
            UpdateLanguageMenuState();
            ClearSearch();
            PopulateList();
            SetStatus(choice.Value.AllWesternLanguages
                ? $"Imported western languages from {Path.GetFileName(path)} into the loaded ROM."
                : $"Imported {_entries.Count} messages from {Path.GetFileName(path)} into the current ROM language.");

            if (_items.Count > 0)
            {
                MessageList.SelectedIndex = 0;
                ShowEntry(_items[0].Index);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to import header into ROM", ex.Message);
        }
    }

    private async Task LoadTableFilesDocumentAsync(string binPath, string tblPath)
    {
        try
        {
            _entries = MessageFileService.LoadTableFiles(tblPath, binPath);
            _documentKind = DocumentKind.DataFiles;
            _tblPath = tblPath;
            _binPath = binPath;
            _headerPath = null;
            _headerLanguageEntries = null;
            _activeHeaderLanguageIndex = 0;
            _romPath = null;
            _romData = null;
            CharacterProfileStore.Current.SetCustomGlyphsAvailable(false);
            _activeCharacterProfileName = CharacterProfileStore.Current.SelectedProfileName;
            _currentIdx = -1;
            RefreshAuxiliaryWindowsForLoadedDocument();
            MarkClean();
            UpdateWindowTitle();
            UpdateLanguageMenuState();
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
            await ShowErrorAsync("Failed to load data files", ex.Message);
        }
    }

    private async Task LoadHeaderDocumentAsync(string path)
    {
        try
        {
            CommitCurrent();

            _headerLanguageEntries = HeaderDocumentService.LoadLanguageEntries(path);
            _activeHeaderLanguageIndex = HeaderDocumentService.ChooseInitialLanguage(_headerLanguageEntries);
            _entries = _headerLanguageEntries[_activeHeaderLanguageIndex];
            _documentKind = DocumentKind.Header;
            _tblPath = null;
            _binPath = null;
            _headerPath = path;
            _romPath = null;
            _romData = null;
            CharacterProfileStore.Current.SetCustomGlyphsAvailable(false);
            _activeCharacterProfileName = CharacterProfileStore.Current.SelectedProfileName;
            _currentIdx = -1;
            RefreshAuxiliaryWindowsForLoadedDocument();
            MarkClean();
            UpdateWindowTitle();
            UpdateLanguageMenuState();
            ClearSearch();

            PopulateList();
            SetStatus($"Loaded {_entries.Count} messages from {Path.GetFileName(path)}.");

            if (_items.Count > 0)
            {
                MessageList.SelectedIndex = 0;
                ShowEntry(_items[0].Index);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to load header", ex.Message);
        }
    }

    private async Task LoadRomDocumentAsync(string path)
    {
        try
        {
            CommitCurrent();

            RomMessageData romData = RomMessageService.LoadMessages(path);
            _entries = romData.Entries;
            _documentKind = DocumentKind.Rom;
            _tblPath = null;
            _binPath = null;
            _headerPath = null;
            _headerLanguageEntries = null;
            _activeHeaderLanguageIndex = 0;
            _romPath = path;
            _romData = romData;
            ApplyGlyphProfileForLoadedRom(romData);
            _activeCharacterProfileName = CharacterProfileStore.Current.SelectedProfileName;
            _currentIdx = -1;
            RefreshAuxiliaryWindowsForLoadedDocument();
            MarkClean();
            UpdateWindowTitle();
            UpdateLanguageMenuState();
            ClearSearch();

            PopulateList();
            string compressionStatus = romData.WasCompressed ? "compressed" : "decompressed";
            SetStatus($"Loaded {_entries.Count} messages from {Path.GetFileName(path)} ({romData.Profile.Name}, {compressionStatus}).");

            if (_items.Count > 0)
            {
                MessageList.SelectedIndex = 0;
                ShowEntry(_items[0].Index);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to load ROM", ex.Message);
        }
    }

    private static void ApplyGlyphProfileForLoadedRom(RomMessageData romData)
    {
        var glyphSession = new RomGlyphEditorSession(
            romData.DecompressedRom,
            romData.FontResources,
            romData.Profile.FontBaseline);
        CharacterProfileStore.Current.ApplyAutomaticProfile(glyphSession.HasLoadedCustomGlyphOrWidth());
    }

    private async Task<bool> WriteFilesAsync(string tblPath, string binPath)
    {
        try
        {
            MessageFileService.SaveTableFiles(
                MessageExportService.GetTableFileSaveEntries(_entries, _documentKind == DocumentKind.Rom),
                tblPath,
                binPath);
            MarkClean();
            UpdateWindowTitle();
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
        List<MessageEntry> savedEntries = MessageExportService.GetTableFileSaveEntries(_entries, _documentKind == DocumentKind.Rom);
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

        _documentKind = DocumentKind.DataFiles;
        _entries = savedEntries;
        _currentIdx = -1;
        _tblPath = tblPath;
        _binPath = binPath;
        _headerPath = null;
        _headerLanguageEntries = null;
        _activeHeaderLanguageIndex = 0;
        _romPath = null;
        _romData = null;
        CharacterProfileStore.Current.SetCustomGlyphsAvailable(false);
        _activeCharacterProfileName = CharacterProfileStore.Current.SelectedProfileName;
        RefreshAuxiliaryWindowsForLoadedDocument();
        ClearSearch();
        PopulateList();
        if (_items.Count > 0)
        {
            MessageList.SelectedIndex = 0;
            ShowEntry(_items[0].Index);
        }
        UpdateWindowTitle();
        UpdateLanguageMenuState();
        MarkClean();
        return true;
    }

    private async Task<bool> WriteHeaderAsync(string path)
    {
        try
        {
            CommitHeaderLanguageChanges();
            if (_headerLanguageEntries is not null && _headerLanguageEntries.Count > 1)
            {
                MessageFileService.ExportHeaderLanguages(
                    null,
                    GetHeaderLanguageEntries(0),
                    GetHeaderLanguageEntries(1),
                    GetHeaderLanguageEntries(2),
                    path);
            }
            else
            {
                MessageFileService.ExportHeader(_entries, path);
            }

            MarkClean();
            UpdateWindowTitle();
            SetStatus($"Saved header to {Path.GetFileName(path)}.");
            return true;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to save header", ex.Message);
            return false;
        }
    }

    private async Task<bool> SaveCurrentFilesForCloseAsync()
    {
        if (_entries.Count == 0)
        {
            MarkClean();
            return true;
        }

        CommitCurrent();
        return _documentKind switch
        {
            DocumentKind.DataFiles when _tblPath is not null && _binPath is not null => await WriteFilesAsync(_tblPath, _binPath),
            DocumentKind.Header when _headerPath is not null => await WriteHeaderAsync(_headerPath),
            DocumentKind.Rom when _romPath is not null && _romData is not null => await WriteRomAsync(_romPath, _romData),
            _ => await SaveCurrentFilesAsAsync(),
        };
    }

    private async Task<bool> WriteRomAsync(string path, RomMessageData romData, bool? compressOverride = null)
    {
        bool compress = compressOverride ?? romData.WasCompressed;
        var progress = new Progress<RomCompressionProgress>(UpdateBusyProgress);
        IDisposable? busy = compress ? ShowProgress("Compressing ROM", "Compressing ROM") : null;
        try
        {
            _romData = await Task.Run(() =>
            {
                RomMessageService.SaveMessages(path, romData, _entries, compress ? progress : null, compressOverride);
                return RomMessageService.LoadMessages(path, romData.ActiveMessageBankIndex, romData.ActiveSection);
            });
            RefreshAuxiliaryWindowsForLoadedDocument();
            MarkClean();
            UpdateWindowTitle();
            UpdateLanguageMenuState();
            busy?.Dispose();
            SetStatus($"Saved ROM to {Path.GetFileName(path)}.");
            return true;
        }
        catch (Exception ex)
        {
            busy?.Dispose();
            await ShowErrorAsync("Failed to save ROM", ex.Message);
            return false;
        }
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

    private CHeaderMessageSlot GetActiveHeaderMessageSlot()
    {
        if (_documentKind == DocumentKind.Header)
        {
            return HeaderDocumentService.GetMessageSlotForBankIndex(_activeHeaderLanguageIndex);
        }

        if (_romData is null || _romData.ActiveSection == RomMessageSection.Credits)
        {
            return CHeaderMessageSlot.Nes;
        }

        return HeaderDocumentService.GetMessageSlotForBankIndex(_romData.ActiveMessageBankIndex);
    }

    private bool CanExportAllRomLanguages()
        => _romData is not null
            && _romData.ActiveSection == RomMessageSection.Messages
            && (_romData.Profile.MessageBanks.Count > 1 || _romData.Profile.JapaneseMessageBank is not null);

    private void CommitHeaderLanguageChanges()
    {
        if (_documentKind == DocumentKind.Header && _headerLanguageEntries is not null)
        {
            _headerLanguageEntries[_activeHeaderLanguageIndex] = _entries;
        }
    }

    private List<MessageEntry>? GetHeaderLanguageEntries(int languageIndex)
        => _headerLanguageEntries is not null && _headerLanguageEntries.TryGetValue(languageIndex, out List<MessageEntry>? entries)
            ? entries
            : null;

    private void ExportAllRomLanguagesToHeader(string path)
    {
        if (_romData is null)
        {
            throw new InvalidOperationException("No ROM is loaded.");
        }

        var banks = RomMessageService.LoadModernExportBanks(_romData, _entries);
        MessageFileService.ExportHeaderLanguages(
            banks.Jpn,
            banks.Nes,
            banks.Ger,
            banks.Fra,
            path);
    }

}
