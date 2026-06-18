using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private async Task<TableFileSaveResult?> WriteFilesAsync(string tblPath, string binPath)
    {
        try
        {
            GameProfile saveProfile = CreateCurrentEncodingGameProfile();
            TableFileSaveResult result = _tableFileWorkflow.Save(
                tblPath,
                binPath,
                _session.Entries,
                _session.Kind == DocumentKind.Rom,
                saveProfile);
            MarkClean();
            UpdateWindowTitle();
            SetStatus(GetSavedStatus("data files"));
            return result;
        }
        catch (Exception ex)
        {
            await ShowOperationExceptionAsync(
                "Failed to save",
                ex,
                "Existing data files were left unchanged.",
                "Save failed. Existing data files were left unchanged.");
            return null;
        }
    }

    private async Task<bool> SaveCurrentFilesAsAsync()
    {
        GameProfile savedGameProfile = CurrentGameProfile;
        string? binPath = await PickSaveFileAsync(".bin", "nes_message_data_static.bin");
        if (binPath is null)
        {
            return false;
        }

        string tblPath = Path.ChangeExtension(binPath, ".tbl");

        TableFileSaveResult? saveResult = await WriteFilesAsync(tblPath, binPath);
        if (saveResult is null)
        {
            return false;
        }

        _session.ConvertToTableFiles(saveResult.SavedEntries, savedGameProfile, tblPath, binPath);
        SetActiveCharacterProfileGame(savedGameProfile.Kind);
        ClearCustomGlyphProfileSelection();
        RefreshDocumentShell();
        RefreshMessageListAndSelectFirst();
        return true;
    }

    private async Task<bool> WriteHeaderAsync(string path)
    {
        try
        {
            CommitHeaderLanguageChanges();
            _headerDocumentWorkflow.Save(
                path,
                _session.Entries,
                CreateCurrentEncodingGameProfile(),
                _session.HeaderLanguageEntries,
                _session.HeaderJapaneseEntries);

            MarkClean();
            UpdateWindowTitle();
            SetStatus(GetSavedStatus("header"));
            return true;
        }
        catch (Exception ex)
        {
            await ShowOperationExceptionAsync(
                "Failed to save header",
                ex,
                "The existing header file was left unchanged.",
                "Save failed. Existing header was left unchanged.");
            return false;
        }
    }

    private async Task<bool> SaveCurrentFilesForCloseAsync()
    {
        if (_session.Entries.Count == 0)
        {
            MarkClean();
            return true;
        }

        CommitCurrent();
        return _session.Kind switch
        {
            DocumentKind.DataFiles when _session.TablePath is not null && _session.BinaryPath is not null => await WriteFilesAsync(_session.TablePath, _session.BinaryPath) is not null,
            DocumentKind.Header when _session.HeaderPath is not null => await WriteHeaderAsync(_session.HeaderPath),
            DocumentKind.Rom when _session.RomPath is not null && _session.RomData is not null => await WriteRomAsync(_session.RomPath, _session.RomData),
            _ => await SaveCurrentFilesAsAsync(),
        };
    }

    private async Task<bool> WriteRomAsync(string path, RomMessageData romData, bool? compressOverride = null)
    {
        if (!romData.Profile.Capabilities.SupportsMessageEditing)
        {
            await ShowInfoAsync(
                "ROM save unavailable",
                $"{romData.Profile.Name} is recognized, but message editing is not enabled for this ROM profile.");
            return false;
        }

        bool compress = compressOverride ?? romData.WasCompressed;
        var progress = new Progress<RomFileOperationProgress>(UpdateBusyProgress);
        IDisposable? busy = compress ? ShowProgress("Compressing ROM", "Compressing ROM") : null;
        try
        {
            var encodingProfile = CreateEncodingProfile(romData.Profile.GameProfile);
            int saveRevision = _session.ChangeRevision;
            RomMessageData savedRomData = await _romDocumentWorkflow.SaveAndReloadAsync(
                path,
                romData,
                _session.Entries,
                encodingProfile,
                compressOverride,
                compress ? progress : null);

            if (_session.ChangeRevision != saveRevision)
            {
                busy?.Dispose();
                UpdateWindowTitle();
                SetStatus("Saved ROM. Newer unsaved changes remain.");
                return true;
            }

            _session.RefreshRomDataAfterSave(savedRomData);
            MarkClean();
            RefreshDocumentShell();
            busy?.Dispose();
            SetStatus(GetSavedStatus("ROM"));
            return true;
        }
        catch (Exception ex)
        {
            busy?.Dispose();
            await ShowOperationExceptionAsync(
                "Failed to save ROM",
                ex,
                "The existing ROM file was left unchanged. Your unsaved editor changes are still loaded.",
                "Save failed. Existing ROM was left unchanged.");
            return false;
        }
    }

    private void CommitHeaderLanguageChanges()
    {
        if (_session.Kind == DocumentKind.Header && _session.HeaderLanguageEntries is not null)
        {
            _session.HeaderLanguageEntries[_session.ActiveHeaderLanguageIndex] = _session.Entries;
        }
    }
}
