namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void MarkDirty() => _session.MarkDirty();

    private void MarkClean() => _session.MarkClean();

    private void MarkCurrentViewClean() => _session.MarkCurrentViewClean();

    private void MarkRomBankDirty() => _session.MarkRomBankDirty();

    private void MarkHeaderLanguageDirty() => _session.MarkHeaderLanguageDirty();
}
