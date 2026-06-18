using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;
using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Services;

public static class MessageVisibilityService
{
    public static bool IsVisibleEditorEntry(
        MessageEntry entry,
        GameKind gameKind,
        IReadOnlyList<MessageEntry> entries,
        RomMessageData? romData) =>
        !ShouldHideFontOrderEntry(entry, entries, romData)
        && !ShouldHideMajorasMaskBuildHelperEntry(entry, gameKind);

    public static bool ShouldHideFontOrderEntry(IReadOnlyList<MessageEntry> entries, RomMessageData? romData)
        => romData is not null
            && romData.ActiveSection == RomMessageSection.Messages
            && entries.Any(entry => entry.Id == FontOrderCodec.MessageId);

    private static bool ShouldHideFontOrderEntry(
        MessageEntry entry,
        IReadOnlyList<MessageEntry> entries,
        RomMessageData? romData) =>
        entry.Id == FontOrderCodec.MessageId
        && ShouldHideFontOrderEntry(entries, romData);

    public static bool ShouldHideMajorasMaskBuildHelperEntry(MessageEntry entry, GameKind gameKind) =>
        gameKind == GameKind.MajorasMask
        && entry.Id is FontOrderCodec.MessageId or MmMessageTableCodec.DebuggerEndMessageId;
}
