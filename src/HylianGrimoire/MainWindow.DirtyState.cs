using System.Text;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void MarkDirty()
    {
        _hasUnsavedChanges = !string.Equals(
            GetDocumentFingerprint(),
            _cleanDocumentFingerprint,
            StringComparison.Ordinal);
    }

    private void MarkClean()
    {
        _cleanDocumentFingerprint = GetDocumentFingerprint();
        _hasUnsavedChanges = false;
    }

    private string GetDocumentFingerprint()
    {
        var fingerprint = new StringBuilder();
        fingerprint.Append(_entries.Count).Append('\n');

        foreach (var entry in _entries)
        {
            AppendFingerprintValue(fingerprint, entry.Id);
            AppendFingerprintValue(fingerprint, entry.Type);
            AppendFingerprintValue(fingerprint, entry.Position);
            AppendFingerprintValue(fingerprint, entry.Bank);
            AppendFingerprintValue(fingerprint, entry.TableEndMarkerId);
            AppendFingerprintValue(fingerprint, entry.PreserveOffsetWithoutMessageData);
            AppendFingerprintValue(fingerprint, entry.Text);
        }

        return fingerprint.ToString();
    }

    private static void AppendFingerprintValue(StringBuilder fingerprint, string value)
    {
        fingerprint.Append(value.Length).Append(':').Append(value).Append('|');
    }

    private static void AppendFingerprintValue(StringBuilder fingerprint, int value)
    {
        fingerprint.Append(value).Append('|');
    }

    private static void AppendFingerprintValue(StringBuilder fingerprint, bool value)
    {
        fingerprint.Append(value ? '1' : '0').Append('|');
    }
}
