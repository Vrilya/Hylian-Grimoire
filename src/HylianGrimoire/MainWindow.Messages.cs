using HylianGrimoire.Services;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void ShowEntry(int idx)
    {
        using IDisposable update = BeginUpdate();
        var entry = _session.Entries[idx];
        _session.CurrentIndex = idx;
        UpdateDiagnosticsContext();

        TypeCombo.SelectedItem = CurrentGameProfile.MessageTypes.Items.FirstOrDefault(item => item.Value == entry.Type);
        PositionCombo.SelectedItem = CurrentGameProfile.MessagePositions.Items.FirstOrDefault(item => item.Value == entry.Position);
        UpdateMajorasMaskMetadataPanel(entry);
        TextEditor.Text = CurrentGameProfile.EditorTextSyntax.ToDisplay(entry.Text);
        UpdatePreview();
        RefreshMessageByteInspector();

        SetStatus($"Editing message 0x{entry.Id:x4}  ({GetVisibleMessageOrdinal(idx) + 1} / {CountVisibleMessageEntries()})");
    }

    private void CommitCurrent()
    {
        if (_session.CurrentIndex < 0 || _session.CurrentIndex >= _session.Entries.Count)
        {
            return;
        }

        string editorText = CurrentGameProfile.EditorTextSyntax.FromDisplay(GetEditorText());
        if (CurrentGameProfile.EditorTextSyntax.TryNormalizeEditorText(editorText, out string normalized))
        {
            editorText = normalized;
        }

        _session.Entries[_session.CurrentIndex].Text = editorText;
    }

    private bool ShouldHideFontOrderEntry()
        => MessageExportService.ShouldHideFontOrderEntry(_session.Entries, _session.RomData);
}
