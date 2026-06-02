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
            await ShowErrorAsync("Failed to load data files", ex.Message);
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
            await ShowErrorAsync("Failed to load header", ex.Message);
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
            await ShowErrorAsync("Failed to load ROM", ex.Message);
        }
    }

    private void ApplyGlyphProfileForLoadedRom(RomMessageData romData)
    {
        _characterProfileRuntime.ApplyAutomaticProfileForLoadedRom(romData);
        SyncActiveCharacterProfileName();
    }
}
