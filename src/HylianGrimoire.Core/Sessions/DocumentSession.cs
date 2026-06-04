using System.Text;
using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;

namespace HylianGrimoire.Sessions;

public sealed class DocumentSession
{
    public List<MessageEntry> Entries { get; private set; } = [];
    public int CurrentIndex { get; set; } = -1;
    public DocumentKind Kind { get; private set; } = DocumentKind.None;
    public string? TablePath { get; private set; }
    public string? BinaryPath { get; private set; }
    public string? HeaderPath { get; private set; }
    public Dictionary<int, List<MessageEntry>>? HeaderLanguageEntries { get; private set; }
    public int ActiveHeaderLanguageIndex { get; private set; }
    public string? RomPath { get; private set; }
    public RomMessageData? RomData { get; private set; }
    public GameProfile? DocumentGameProfile { get; private set; }
    public string SearchText { get; set; } = string.Empty;
    private string CleanDocumentFingerprint { get; set; } = string.Empty;
    public int ChangeRevision { get; private set; }
    public bool HasUnsavedChanges { get; private set; }
    public bool HasUnsavedRomBankChanges { get; private set; }
    public bool HasUnsavedHeaderLanguageChanges { get; private set; }

    public GameProfile? ActiveGameProfile => RomData?.Profile.GameProfile ?? DocumentGameProfile;

    public bool HasActiveProject => ActiveGameProfile is not null;

    public GameProfile CurrentGameProfile =>
        ActiveGameProfile ?? throw new InvalidOperationException("No active project.");

    public void Reset()
    {
        Entries = [];
        CurrentIndex = -1;
        Kind = DocumentKind.None;
        ClearDocumentReferences();
        SearchText = string.Empty;
        MarkClean();
    }

    public void LoadTableFiles(MessageFileDocument document, string tablePath, string binaryPath)
    {
        ClearDocumentReferences();
        UseEntries(document.Entries, DocumentKind.DataFiles, document.GameProfile);
        TablePath = tablePath;
        BinaryPath = binaryPath;
        MarkClean();
    }

    public void LoadHeader(HeaderFileDocument document, string path, int activeLanguageIndex)
    {
        ClearDocumentReferences();
        HeaderLanguageEntries = document.Languages;
        ActiveHeaderLanguageIndex = activeLanguageIndex;
        UseEntries(HeaderLanguageEntries[ActiveHeaderLanguageIndex], DocumentKind.Header, document.GameProfile);
        HeaderPath = path;
        MarkClean();
    }

    public void LoadRom(RomMessageData romData, string path)
    {
        ClearDocumentReferences();
        UseEntries(romData.Entries, DocumentKind.Rom, romData.Profile.GameProfile);
        RomPath = path;
        RomData = romData;
        MarkClean();
    }

    public void ConvertToTableFiles(List<MessageEntry> entries, GameProfile gameProfile, string tablePath, string binaryPath)
    {
        ClearDocumentReferences();
        UseEntries(entries, DocumentKind.DataFiles, gameProfile);
        TablePath = tablePath;
        BinaryPath = binaryPath;
        MarkClean();
    }

    public void UseProject(GameProfile gameProfile)
    {
        Reset();
        Kind = DocumentKind.Project;
        DocumentGameProfile = gameProfile;
        MarkClean();
    }

    public void SwitchHeaderLanguage(int languageIndex)
    {
        if (HeaderLanguageEntries is null)
        {
            throw new InvalidOperationException("No header document is loaded.");
        }

        ActiveHeaderLanguageIndex = languageIndex;
        Entries = HeaderLanguageEntries[languageIndex];
        CurrentIndex = -1;
    }

    public void UseRomData(RomMessageData romData)
    {
        RomData = romData;
        Entries = romData.Entries;
        DocumentGameProfile = romData.Profile.GameProfile;
        CurrentIndex = -1;
    }

    public void RefreshRomDataAfterSave(RomMessageData romData)
    {
        RomData = romData;
        DocumentGameProfile = romData.Profile.GameProfile;
    }

    public void MarkSavedAsRom(string path)
    {
        Kind = DocumentKind.Rom;
        RomPath = path;
    }

    public bool IsCurrentViewDirty()
        => !string.Equals(GetDocumentFingerprint(), CleanDocumentFingerprint, StringComparison.Ordinal);

    public void MarkDirty()
    {
        MarkChanged();
        HasUnsavedChanges = IsCurrentViewDirty()
            || HasUnsavedRomBankChanges
            || HasUnsavedHeaderLanguageChanges;
    }

    public void MarkClean()
    {
        HasUnsavedRomBankChanges = false;
        HasUnsavedHeaderLanguageChanges = false;
        CleanDocumentFingerprint = GetDocumentFingerprint();
        HasUnsavedChanges = false;
    }

    public void MarkCurrentViewClean()
    {
        CleanDocumentFingerprint = GetDocumentFingerprint();
        HasUnsavedChanges = HasUnsavedRomBankChanges || HasUnsavedHeaderLanguageChanges;
    }

    public void MarkRomBankDirty()
    {
        MarkChanged();
        HasUnsavedRomBankChanges = true;
        HasUnsavedChanges = true;
    }

    public void MarkHeaderLanguageDirty()
    {
        MarkChanged();
        HasUnsavedHeaderLanguageChanges = true;
        HasUnsavedChanges = true;
    }

    public void ForceDirty()
    {
        MarkChanged();
        HasUnsavedChanges = true;
    }

    private void MarkChanged()
    {
        unchecked
        {
            ChangeRevision++;
        }
    }

    private void UseEntries(List<MessageEntry> entries, DocumentKind kind, GameProfile gameProfile)
    {
        Entries = entries;
        CurrentIndex = -1;
        Kind = kind;
        DocumentGameProfile = gameProfile;
        SearchText = string.Empty;
    }

    private void ClearDocumentReferences()
    {
        TablePath = null;
        BinaryPath = null;
        HeaderPath = null;
        HeaderLanguageEntries = null;
        ActiveHeaderLanguageIndex = 0;
        RomPath = null;
        RomData = null;
        DocumentGameProfile = null;
    }

    private string GetDocumentFingerprint()
    {
        var fingerprint = new StringBuilder();
        fingerprint.Append(Entries.Count).Append('\n');

        foreach (var entry in Entries)
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
            AppendFingerprintValue(fingerprint, entry.CodecMetadata);
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

    private static void AppendFingerprintValue(StringBuilder fingerprint, object? value)
    {
        switch (value)
        {
            case null:
                fingerprint.Append("metadata:null|");
                break;
            case IMessageEntryMetadataFingerprint metadata:
                metadata.AppendFingerprint(fingerprint);
                break;
            default:
                fingerprint.Append("metadata:").Append(value.GetType().FullName).Append('|');
                break;
        }
    }
}
