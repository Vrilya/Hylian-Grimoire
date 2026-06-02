using HylianGrimoire.Codecs;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Services;

public sealed class RomDocumentWorkflow
{
    public Task<RomMessageData> LoadAsync(
        string path,
        IProgress<RomFileOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => RomMessageService.LoadMessages(path, progress: progress), cancellationToken);
    }

    public Task<RomMessageData> SaveAndReloadAsync(
        string path,
        RomMessageData romData,
        List<MessageEntry> entries,
        MessageEncodingProfile encodingProfile,
        bool? compressOverride = null,
        IProgress<RomFileOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                RomMessageService.SaveMessages(
                    path,
                    romData,
                    entries,
                    progress: progress,
                    compressOverride: compressOverride,
                    encodingProfile: encodingProfile);

                return RomMessageService.LoadMessages(path, romData.ActiveMessageBankIndex, romData.ActiveSection);
            },
            cancellationToken);
    }
}
