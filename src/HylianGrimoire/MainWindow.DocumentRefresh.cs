namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void RefreshDocumentShell()
    {
        RefreshAuxiliaryWindowsForLoadedDocument();
        UpdateWindowTitle();
        UpdateLanguageMenuState();
    }

    private void RefreshMessageListAndSelectFirst()
    {
        ClearSearch();
        PopulateList();
        SelectFirstVisibleMessage();
    }

    private void RefreshMessageListAndRestoreVisibleOrdinal(int selectedOrdinal)
    {
        ClearSearch();
        PopulateList();
        SelectVisibleOrdinal(selectedOrdinal);
    }

    private void RefreshLoadedDocumentView(string status)
    {
        RefreshDocumentShell();
        RefreshMessageListAndSelectFirst();
        SetStatus(status);
    }

    private void SelectFirstVisibleMessage()
    {
        if (_items.Count == 0)
        {
            return;
        }

        MessageList.SelectedIndex = 0;
        ShowEntry(_items[0].Index);
    }
}
