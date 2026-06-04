namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void MarkDirty()
    {
        _session.MarkDirty();
        UpdateDiagnosticsContext();
    }

    private void MarkClean()
    {
        _session.MarkClean();
        UpdateDiagnosticsContext();
    }

    private void MarkCurrentViewClean()
    {
        _session.MarkCurrentViewClean();
        UpdateDiagnosticsContext();
    }

    private void ApplyRomMutation()
    {
        _session.MarkRomBankDirty();
        UpdateDiagnosticsContext();
    }

    private void ApplyRomMutation(string status)
    {
        ApplyRomMutation();
        SetStatus(status);
    }

    private void MarkHeaderLanguageDirty()
    {
        _session.MarkHeaderLanguageDirty();
        UpdateDiagnosticsContext();
    }
}
