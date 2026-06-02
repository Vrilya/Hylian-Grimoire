namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private string GetLoadedStatus(string source) =>
        $"Loaded {GetMessageCountText()} from {source}.";

    private string GetImportedStatus(string source) =>
        $"Imported {GetMessageCountText()} from {source}.";

    private string GetExportedStatus(string destination) =>
        $"Exported {GetMessageCountText()} to {destination}.";

    private string GetSavedStatus(string destination) =>
        $"Saved {GetMessageCountText()} to {destination}.";

    private string GetMessageCountText()
    {
        string noun = _session.Entries.Count == 1 ? "message" : "messages";
        return $"{_session.Entries.Count} {noun}";
    }
}
