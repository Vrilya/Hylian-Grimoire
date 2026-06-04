using HylianGrimoire.Rom;
using HylianGrimoire.Services;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private async Task LoadTableFilesDocumentAsync(string binPath, string tblPath)
    {
        try
        {
            MessageFileDocument document = _tableFileWorkflow.Load(tblPath, binPath);
            _session.LoadTableFiles(document, tblPath, binPath);
            SetActiveCharacterProfileGame(document.GameProfile.Kind);
            ClearCustomGlyphProfileSelection();
            RefreshLoadedDocumentView(GetLoadedStatus("data files"));
        }
        catch (Exception ex)
        {
            await ShowOperationExceptionAsync(
                "Failed to load data files",
                ex,
                "No data files were loaded.",
                "Load failed. No data files were loaded.");
        }
    }

    private async Task LoadHeaderDocumentAsync(string path)
    {
        try
        {
            CommitCurrent();

            HeaderDocumentLoadResult result = _headerDocumentWorkflow.Load(path, CreateEncodingProfile);
            _session.LoadHeader(result.Document, path, result.ActiveLanguageIndex);
            SetActiveCharacterProfileGame(result.Document.GameProfile.Kind);
            ClearCustomGlyphProfileSelection();
            RefreshLoadedDocumentView(GetLoadedStatus("header"));
        }
        catch (Exception ex)
        {
            await ShowOperationExceptionAsync(
                "Failed to load header",
                ex,
                "No header was loaded.",
                "Load failed. No header was loaded.");
        }
    }

    private async Task LoadRomDocumentAsync(string path)
    {
        IDisposable? busy = null;
        try
        {
            CommitCurrent();

            var progress = new Progress<RomFileOperationProgress>(UpdateBusyProgress);
            busy = ShowProgress("Loading ROM", "Loading ROM");
            RomMessageData romData = await _romDocumentWorkflow.LoadAsync(path, progress);
            _session.LoadRom(romData, path);
            SetActiveCharacterProfileGame(romData.Profile.GameProfile.Kind);
            ApplyGlyphProfileForLoadedRom(romData);
            busy.Dispose();
            busy = null;
            RefreshLoadedDocumentView(GetLoadedStatus("ROM"));
        }
        catch (Exception ex)
        {
            busy?.Dispose();
            await ShowOperationExceptionAsync(
                "Failed to load ROM",
                ex,
                "No ROM was loaded.",
                "Load failed. No ROM was loaded.");
        }
    }

    private void ApplyGlyphProfileForLoadedRom(RomMessageData romData)
    {
        _characterProfileRuntime.ApplyAutomaticProfileForLoadedRom(romData);
        SyncActiveCharacterProfileName();
    }
}
