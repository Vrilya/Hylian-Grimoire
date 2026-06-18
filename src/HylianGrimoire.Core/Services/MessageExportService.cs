using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Headers;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Services;

public static class MessageExportService
{
    public static bool ShouldHideFontOrderEntry(IReadOnlyList<MessageEntry> entries, RomMessageData? romData)
        => MessageVisibilityService.ShouldHideFontOrderEntry(entries, romData);

    public static List<MessageEntry> GetHeaderExportEntries(
        IReadOnlyList<MessageEntry> entries,
        CHeaderExportFormat format,
        RomMessageData? romData)
    {
        List<MessageEntry> exportEntries = ShouldHideFontOrderEntry(entries, romData)
            ? entries.Where(entry => entry.Id != FontOrderCodec.MessageId).ToList()
            : entries.ToList();

        if (format == CHeaderExportFormat.OTRMod && ShouldAppendOtrModFontOrderEntry(romData))
        {
            exportEntries.RemoveAll(entry => entry.Id == FontOrderCodec.MessageId);
            exportEntries.Add(CreateOtrModFontOrderEntry(romData!));
        }

        return exportEntries;
    }

    public static List<MessageEntry> GetTableFileSaveEntries(
        IReadOnlyList<MessageEntry> entries,
        bool excludeFontOrderEntry,
        GameProfile gameProfile)
        => excludeFontOrderEntry && gameProfile.Kind == GameKind.OcarinaOfTime
            ? entries.Where(entry => entry.Id != FontOrderCodec.MessageId).ToList()
            : entries.ToList();

    private static bool ShouldAppendOtrModFontOrderEntry(RomMessageData? romData)
        => romData is { ActiveSection: RomMessageSection.Messages }
            && romData.Profile.GameProfile.MessageBankLayout.GetEditableBanks(romData.Profile).Count > 1;

    private static MessageEntry CreateOtrModFontOrderEntry(RomMessageData romData)
    {
        string text = string.Empty;
        byte[]? encodedBytes = null;

        if (romData.ActiveMessageBankIndex == 0
            && RomMessageService.TryReadActiveFontOrderBytes(romData, out byte[] rawFontOrder))
        {
            text = FontOrderCodec.ToEditorText(rawFontOrder) + "\n";
            encodedBytes = rawFontOrder;
        }

        return new MessageEntry(FontOrderCodec.MessageId, type: 0, position: 0, bank: 7, offset: 0)
        {
            Text = text,
            OriginalText = text,
            OriginalEncodedBytes = encodedBytes,
        };
    }
}
