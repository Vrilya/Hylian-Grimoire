using Microsoft.UI.Xaml;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void OnOpenLogsFolder(object sender, RoutedEventArgs e)
    {
        try
        {
            AppDiagnostics.OpenLogDirectory();
            SetStatus($"Opened logs folder: {AppDiagnostics.LogDirectory}");
        }
        catch (Exception ex)
        {
            _ = ShowExceptionAsync("Failed to open logs folder", ex);
        }
    }

    private void UpdateDiagnosticsContext()
    {
        string? romFormat = _session.RomData is null
            ? null
            : _session.RomData.WasCompressed ? "Compressed ROM" : "Decompressed ROM";

        int? currentEntryIndex = null;
        string? currentMessageId = null;
        if (_session.CurrentIndex >= 0 && _session.CurrentIndex < _session.Entries.Count)
        {
            currentEntryIndex = _session.CurrentIndex;
            currentMessageId = $"0x{_session.Entries[_session.CurrentIndex].Id:x4}";
        }

        AppDiagnostics.UpdateDocumentContext(
            _session.Kind.ToString(),
            ActiveGameProfile?.DisplayName,
            GetActiveDiagnosticDocumentPath(),
            _session.RomData?.Profile.Name,
            romFormat,
            _session.RomData?.ActiveSection.ToString(),
            GetActiveDiagnosticLanguage(),
            _activeCharacterProfileName,
            _session.Entries.Count,
            currentEntryIndex,
            currentMessageId,
            _session.HasUnsavedChanges);
    }

    private string? GetActiveDiagnosticDocumentPath()
    {
        return _session.Kind switch
        {
            DocumentKind.DataFiles => _session.BinaryPath,
            DocumentKind.Header => _session.HeaderPath,
            DocumentKind.Rom => _session.RomPath,
            _ => null,
        };
    }

    private string? GetActiveDiagnosticLanguage()
    {
        if (_session.Kind == DocumentKind.Header && _session.HeaderLanguageEntries is not null)
        {
            return GetHeaderLanguageName(_session.ActiveHeaderLanguageIndex);
        }

        if (_session.RomData is null)
        {
            return null;
        }

        return _session.RomData.ActiveSection == RomMessageSection.Credits
            ? "Credits"
            : GetRomMessageBankName(_session.RomData, _session.RomData.ActiveMessageBankIndex);
    }
}
