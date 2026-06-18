using HylianGrimoire.Games;
using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public sealed record TableFileSaveResult(List<MessageEntry> SavedEntries);

public sealed class TableFileWorkflow
{
    public MessageFileDocument Load(string tablePath, string binaryPath)
        => MessageFileService.LoadTableFiles(tablePath, binaryPath);

    public TableFileSaveResult Save(
        string tablePath,
        string binaryPath,
        IReadOnlyList<MessageEntry> entries,
        bool excludeFontOrderEntry,
        GameProfile gameProfile)
    {
        List<MessageEntry> savedEntries = MessageExportService.GetTableFileSaveEntries(
            entries,
            excludeFontOrderEntry,
            gameProfile);
        MessageFileService.SaveTableFiles(savedEntries, tablePath, binaryPath, gameProfile);
        return new TableFileSaveResult(savedEntries);
    }
}
