using HylianGrimoire.Games;
using HylianGrimoire.Headers;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using Microsoft.UI.Xaml;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
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
            await ShowOperationExceptionAsync(
                "Failed to export",
                ex,
                "The existing export file was left unchanged.",
                "Export failed. Existing file was left unchanged.");
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
            ApplyRomMutation();
            RefreshDocumentShell();
            RefreshMessageListAndSelectFirst();
            SetStatus(choice.Value.AllWesternLanguages
                ? "Imported western languages from header."
                : GetImportedStatus("header"));
        }
        catch (Exception ex)
        {
            await ShowOperationExceptionAsync(
                "Failed to import header into ROM",
                ex,
                "The header import was not applied. Your loaded ROM data is still intact.",
                "Import failed. Loaded ROM data was not changed.");
        }
    }
}
