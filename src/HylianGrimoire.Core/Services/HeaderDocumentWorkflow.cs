using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Headers;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Services;

public sealed record HeaderDocumentLoadResult(HeaderFileDocument Document, int ActiveLanguageIndex);
public sealed record HeaderRomImportResult(
    RomMessageData RomData,
    IReadOnlyDictionary<int, List<MessageEntry>> ReplacementBanks);

public sealed class HeaderDocumentWorkflow
{
    public HeaderDocumentLoadResult Load(
        string path,
        GameEncodingProfileResolver? getEncodingProfile = null)
    {
        HeaderFileDocument document = HeaderDocumentService.LoadDocument(path, getEncodingProfile);
        int activeLanguageIndex = HeaderDocumentService.ChooseInitialLanguage(document.Languages);
        return new HeaderDocumentLoadResult(document, activeLanguageIndex);
    }

    public void Save(
        string path,
        List<MessageEntry> activeEntries,
        GameProfile activeGameProfile,
        IReadOnlyDictionary<int, List<MessageEntry>>? languageEntries = null,
        IReadOnlyList<MessageEntry>? japaneseEntries = null)
    {
        if (japaneseEntries is not null || languageEntries is not null && languageEntries.Count > 1)
        {
            MessageFileService.ExportHeaderLanguages(
                japaneseEntries,
                GetLanguageEntries(languageEntries, 0),
                GetLanguageEntries(languageEntries, 1),
                GetLanguageEntries(languageEntries, 2),
                path,
                activeGameProfile.EncodingProfile,
                GameProfiles.GetOriginalEncodingProfile(activeGameProfile.Kind));
            return;
        }

        MessageFileService.ExportHeader(activeEntries, path, activeGameProfile);
    }

    public bool CanExportAllRomLanguages(RomMessageData? romData)
        => romData is not null
            && romData.ActiveSection == RomMessageSection.Messages
            && (romData.Profile.Capabilities.SupportsMultipleMessageBanks
                || romData.Profile.Capabilities.SupportsJapaneseMessageExport);

    public void ExportCurrent(
        string path,
        IReadOnlyList<MessageEntry> entries,
        GameProfile gameProfile,
        CHeaderExportFormat format,
        RomMessageData? romData = null)
    {
        List<MessageEntry> exportEntries = gameProfile.Kind == GameKind.MajorasMask
            ? entries.ToList()
            : MessageExportService.GetHeaderExportEntries(entries, format, romData);
        MessageFileService.ExportHeader(exportEntries, path, gameProfile, format);
    }

    public void ExportAllRomLanguages(
        string path,
        RomMessageData romData,
        List<MessageEntry> currentEntries,
        GameProfile activeGameProfile)
    {
        if (!CanExportAllRomLanguages(romData))
        {
            throw new InvalidOperationException("The loaded ROM does not expose multiple header export languages.");
        }

        var banks = RomMessageService.LoadModernExportBanks(
            romData,
            currentEntries,
            activeGameProfile.EncodingProfile);
        MessageFileService.ExportHeaderLanguages(
            banks.Jpn,
            banks.Nes,
            banks.Ger,
            banks.Fra,
            path,
            activeGameProfile.EncodingProfile,
            GameProfiles.GetOriginalEncodingProfile(activeGameProfile.Kind));
    }

    public List<CHeaderMessageSlot> GetAvailableWesternImportSlots(
        string path,
        GameEncodingProfileResolver? getEncodingProfile = null)
        => HeaderDocumentService.GetAvailableWesternSlots(path, getEncodingProfile);

    public HeaderRomImportResult ImportIntoRom(
        string path,
        RomMessageData romData,
        List<MessageEntry> currentEntries,
        bool allWesternLanguages,
        CHeaderMessageSlot selectedSlot,
        GameEncodingProfileResolver? getEncodingProfile = null)
    {
        if (romData.ActiveSection != RomMessageSection.Messages)
        {
            throw new InvalidOperationException("Header imports can only target ROM message banks.");
        }

        MessageEncodingProfile encodingProfile = getEncodingProfile?.Invoke(romData.Profile.GameProfile.Kind)
            ?? romData.Profile.GameProfile.EncodingProfile;

        IReadOnlyDictionary<int, List<MessageEntry>> replacementBanks = allWesternLanguages
            ? HeaderDocumentService.BuildAllWesternRomImports(path, romData, currentEntries, getEncodingProfile)
            : HeaderDocumentService.BuildSelectedRomImport(
                path,
                selectedSlot,
                romData.ActiveMessageBankIndex,
                currentEntries,
                getEncodingProfile);

        if (replacementBanks.Count == 0)
        {
            return new HeaderRomImportResult(romData, replacementBanks);
        }

        RomMessageData updatedRomData = RomMessageService.ReplaceMessageBanks(
            romData,
            currentEntries,
            replacementBanks,
            encodingProfile);
        return new HeaderRomImportResult(updatedRomData, replacementBanks);
    }

    private static IReadOnlyList<MessageEntry>? GetLanguageEntries(
        IReadOnlyDictionary<int, List<MessageEntry>>? languageEntries,
        int languageIndex)
        => languageEntries is not null && languageEntries.TryGetValue(languageIndex, out List<MessageEntry>? entries)
            ? entries
            : null;
}
