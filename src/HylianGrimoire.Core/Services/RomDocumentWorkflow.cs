using HylianGrimoire.Codecs;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Services;

public sealed class RomDocumentWorkflow
{
    public Task<RomMessageData> LoadAsync(
        string path,
        IProgress<RomFileOperationProgress>? progress = null)
    {
        return Task.Run(() => RomMessageService.LoadMessages(path, progress: progress));
    }

    public Task<RomMessageData> SaveAndReloadAsync(
        string path,
        RomMessageData romData,
        List<MessageEntry> entries,
        MessageEncodingProfile encodingProfile,
        bool? compressOverride = null,
        IProgress<RomFileOperationProgress>? progress = null)
    {
        RomMessageData romSnapshot = romData.CreateSnapshot();
        List<MessageEntry> entriesSnapshot = entries.Select(entry => entry.CreateSnapshot()).ToList();

        return Task.Run(
            () =>
            {
                RomMessageService.SaveMessages(
                    path,
                    romSnapshot,
                    entriesSnapshot,
                    progress: progress,
                    compressOverride: compressOverride,
                    encodingProfile: encodingProfile);

                return RomMessageService.LoadMessages(path, romSnapshot.ActiveMessageBankIndex, romSnapshot.ActiveSection);
            });
    }
}
