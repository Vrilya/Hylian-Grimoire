using System.Text;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void MarkDirty()
    {
        _hasUnsavedChanges = !string.Equals(
            GetDocumentFingerprint(),
            _cleanDocumentFingerprint,
            StringComparison.Ordinal) || _hasUnsavedRomBankChanges || _hasUnsavedHeaderLanguageChanges;
    }

    private void MarkClean()
    {
        _hasUnsavedRomBankChanges = false;
        _hasUnsavedHeaderLanguageChanges = false;
        _cleanDocumentFingerprint = GetDocumentFingerprint();
        _hasUnsavedChanges = false;
    }

    private void MarkCurrentViewClean()
    {
        _cleanDocumentFingerprint = GetDocumentFingerprint();
        _hasUnsavedChanges = _hasUnsavedRomBankChanges || _hasUnsavedHeaderLanguageChanges;
    }

    private void MarkRomBankDirty()
    {
        _hasUnsavedRomBankChanges = true;
        _hasUnsavedChanges = true;
    }

    private void MarkHeaderLanguageDirty()
    {
        _hasUnsavedHeaderLanguageChanges = true;
        _hasUnsavedChanges = true;
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
            AppendFingerprintValue(fingerprint, entry.TableHasFinalEndMarker);
            AppendFingerprintValue(fingerprint, entry.PreserveOffsetWithoutMessageData);
            AppendFingerprintValue(fingerprint, entry.Text);
            AppendFingerprintValue(fingerprint, entry.EncodedBytesOverride);
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

    private static void AppendFingerprintValue(StringBuilder fingerprint, byte[]? value)
    {
        if (value is null)
        {
            fingerprint.Append("null|");
            return;
        }

        fingerprint.Append(value.Length).Append(':').Append(Convert.ToHexString(value)).Append('|');
    }
}
