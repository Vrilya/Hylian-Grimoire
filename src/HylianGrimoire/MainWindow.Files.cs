using Microsoft.UI.Xaml;
using HylianGrimoire.Games;
using HylianGrimoire.Headers;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private async void OnNewProject(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmDiscardCurrentProjectAsync())
        {
            return;
        }

        GameProfile? profile = await PromptForNewProjectAsync();
        if (profile is null)
        {
            return;
        }

        ResetProjectSession(closeAuxiliaryWindows: true);
        _session.UseProject(profile);
        SetActiveCharacterProfileGame(profile.Kind);
        RefreshDocumentShell();
        SetStatus($"Created new {profile.DisplayName} project.");
    }

    private async void OnLoadDocument(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmDiscardCurrentProjectAsync())
        {
            return;
        }

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

    private async void OnCloseProject(object sender, RoutedEventArgs e)
    {
        if (!HasActiveProject)
        {
            return;
        }

        if (!await ConfirmDiscardCurrentProjectAsync())
        {
            return;
        }

        ResetProjectSession(closeAuxiliaryWindows: true);
    }

    private async void OnSaveFiles(object sender, RoutedEventArgs e)
    {
        if (_session.Entries.Count == 0)
        {
            await ShowInfoAsync("Nothing to save", "No messages loaded.");
            return;
        }

        CommitCurrent();

        _ = _session.Kind switch
        {
            DocumentKind.DataFiles when _session.TablePath is not null && _session.BinaryPath is not null => await WriteFilesAsync(_session.TablePath, _session.BinaryPath) is not null,
            DocumentKind.Header when _session.HeaderPath is not null => await WriteHeaderAsync(_session.HeaderPath),
            DocumentKind.Rom when _session.RomPath is not null && _session.RomData is not null => await WriteRomAsync(_session.RomPath, _session.RomData),
            _ => await SaveCurrentFilesAsAsync(),
        };
    }

    private async void OnSaveAsFiles(object sender, RoutedEventArgs e)
    {
        if (_session.Entries.Count == 0)
        {
            await ShowInfoAsync("Nothing to save", "No messages loaded.");
            return;
        }

        CommitCurrent();
        _ = await SaveCurrentFilesAsAsync();
    }

    private async void OnSaveAsRom(object sender, RoutedEventArgs e)
    {
        if (_session.Entries.Count == 0)
        {
            await ShowInfoAsync("Nothing to save", "No messages loaded.");
            return;
        }

        if (_session.RomData is null)
        {
            await ShowInfoAsync("No ROM loaded", "Load a ROM before saving as ROM.");
            return;
        }

        CommitCurrent();

        string suggestedFileName = _session.RomPath is null
            ? "Hylian Grimoire.z64"
            : Path.GetFileName(_session.RomPath);
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

        if (await WriteRomAsync(path, _session.RomData, compress))
        {
            _session.MarkSavedAsRom(path);
            UpdateWindowTitle();
            UpdateLanguageMenuState();
        }
    }

    private async void OnExportAsHeader(object sender, RoutedEventArgs e)
    {
        if (!CanUseCHeaders())
        {
            await ShowInfoAsync("C headers unavailable", $"{GetActiveProjectDisplayName()} does not support C header export yet.");
            return;
        }

        if (_session.Entries.Count == 0)
        {
            await ShowInfoAsync("Nothing to export", "No messages loaded.");
            return;
        }

        CommitCurrent();

        HeaderExportChoice? choice = null;
        if (CurrentGameProfile.Kind != GameKind.MajorasMask)
        {
            choice = await PromptForHeaderExportFormatAsync(_headerDocumentWorkflow.CanExportAllRomLanguages(_session.RomData));
            if (choice is null)
            {
                return;
            }
        }

        string defaultFileName = CurrentGameProfile.Kind == GameKind.MajorasMask
            ? "message_data.h"
            : "message_data_static_NES.h";
        string? path = await PickSaveFileAsync(".h", defaultFileName);
        if (path is null)
        {
            return;
        }

        try
        {
            if (choice is { AllRomLanguages: true })
            {
                _headerDocumentWorkflow.ExportAllRomLanguages(
                    path,
                    _session.RomData ?? throw new InvalidOperationException("No ROM is loaded."),
                    _session.Entries,
                    CreateCurrentEncodingGameProfile());
            }
            else
            {
                CHeaderExportFormat format = choice?.Format ?? CHeaderExportFormat.Modern;
                _headerDocumentWorkflow.ExportCurrent(
                    path,
                    _session.Entries,
                    CreateCurrentEncodingGameProfile(),
                    format,
                    _session.RomData);
            }

            SetStatus(choice is { AllRomLanguages: true }
                ? "Exported all ROM languages to header."
                : GetExportedStatus("header"));
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to export", ex.Message);
        }
    }

    private async void OnImportHeaderIntoRom(object sender, RoutedEventArgs e)
    {
        if (!CanUseCHeaders())
        {
            await ShowInfoAsync("C headers unavailable", $"{GetActiveProjectDisplayName()} does not support C header import yet.");
            return;
        }

        if (_session.RomData is null)
        {
            await ShowInfoAsync("No ROM loaded", "Load a ROM before importing a header into it.");
            return;
        }

        if (_session.RomData.ActiveSection != RomMessageSection.Messages)
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

            List<CHeaderMessageSlot> availableWesternSlots = _headerDocumentWorkflow.GetAvailableWesternImportSlots(path, CreateEncodingProfile);
            if (availableWesternSlots.Count == 0)
            {
                await ShowInfoAsync("Nothing to import", "The selected header did not contain any western message slots.");
                return;
            }

            HeaderRomImportChoice? choice = await PromptForHeaderRomImportAsync(
                availableWesternSlots,
                _session.RomData.Profile.Capabilities.SupportsMultipleMessageBanks);
            if (choice is null)
            {
                return;
            }

            HeaderRomImportResult importResult = _headerDocumentWorkflow.ImportIntoRom(
                path,
                _session.RomData,
                _session.Entries,
                choice.Value.AllWesternLanguages,
                choice.Value.SelectedSlot,
                CreateEncodingProfile);
            if (importResult.ReplacementBanks.Count == 0)
            {
                await ShowInfoAsync("Nothing to import", "The selected header did not contain any matching western message slots.");
                return;
            }

            _session.UseRomData(importResult.RomData);
            MarkRomBankDirty();
            RefreshDocumentShell();
            RefreshMessageListAndSelectFirst();
            SetStatus(choice.Value.AllWesternLanguages
                ? "Imported western languages from header."
                : GetImportedStatus("header"));
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to import header into ROM", ex.Message);
        }
    }

}
