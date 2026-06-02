namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private async Task<bool> ConfirmDiscardCurrentProjectAsync()
    {
        if (!_session.HasUnsavedChanges)
        {
            return true;
        }

        return await ConfirmCloseWithUnsavedChangesAsync();
    }

    private void ResetProjectSession(bool closeAuxiliaryWindows)
    {
        if (closeAuxiliaryWindows)
        {
            CloseAuxiliaryWindows();
        }

        _session.Reset();
        _items.Clear();
        SearchBox.Text = string.Empty;
        ClearCustomGlyphProfileSelection();
        ClearEditor();
        UpdateWindowTitle();
        UpdateLanguageMenuState();
        RefreshAuxiliaryWindowsForLoadedDocument();
        SetStatus(string.Empty);
    }

    private string GetActiveProjectDisplayName()
        => ActiveGameProfile?.DisplayName ?? "The active project";
}
