using HylianGrimoire.Codecs;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Services;

public sealed record FontOrderUpdateResult(bool Changed, string? ErrorMessage);

public static class FontOrderService
{
    public static bool CanEdit(IReadOnlyList<MessageEntry> entries, RomMessageData? romData)
        => romData is not null
            && RomMessageService.UsesActiveFontOrderPointer(romData)
            && FindEntry(entries, romData) is not null;

    public static MessageEntry? FindEntry(IReadOnlyList<MessageEntry> entries, RomMessageData? romData)
        => romData is not null && romData.ActiveSection == RomMessageSection.Messages
            ? entries.FirstOrDefault(entry => entry.Id == FontOrderCodec.MessageId)
            : null;

    public static string GetEditorText(MessageEntry entry, RomMessageData? romData)
        => entry.EncodedBytesOverride is not null
            ? FontOrderCodec.ToEditorText(entry.EncodedBytesOverride)
            : GetLoadedEditorText(entry, romData);

    public static string GetLoadedEditorText(MessageEntry entry, RomMessageData? romData)
        => entry.OriginalEncodedBytes is not null
            ? FontOrderCodec.ToEditorText(entry.OriginalEncodedBytes)
            : romData is not null && RomMessageService.TryReadActiveFontOrderBytes(romData, out byte[] romBytes)
            ? FontOrderCodec.ToEditorText(romBytes)
            : entry.Text;

    public static FontOrderUpdateResult ApplyEditorText(MessageEntry entry, string editorText)
    {
        byte[] encoded;
        try
        {
            encoded = FontOrderCodec.FromEditorText(editorText);
            editorText = FontOrderCodec.ToEditorText(encoded);
        }
        catch (Exception ex)
        {
            return new FontOrderUpdateResult(Changed: false, ErrorMessage: ex.Message);
        }

        bool matchesOriginal = entry.OriginalEncodedBytes is not null
            && entry.OriginalEncodedBytes.AsSpan().SequenceEqual(encoded);
        bool matchesOverride = entry.EncodedBytesOverride is not null
            && entry.EncodedBytesOverride.AsSpan().SequenceEqual(encoded);

        if (matchesOverride && string.Equals(entry.Text, editorText, StringComparison.Ordinal))
        {
            return new FontOrderUpdateResult(Changed: false, ErrorMessage: null);
        }

        entry.EncodedBytesOverride = matchesOriginal ? null : encoded;
        entry.Text = matchesOriginal && entry.OriginalText is not null ? entry.OriginalText : editorText;
        return new FontOrderUpdateResult(Changed: true, ErrorMessage: null);
    }
}
