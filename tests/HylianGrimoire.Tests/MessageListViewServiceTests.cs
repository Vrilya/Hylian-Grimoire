using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;
using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class MessageListViewServiceTests
{
    [Fact]
    public void BuildFiltersBySearchAndPreservesVisibleSelection()
    {
        List<MessageEntry> entries =
        [
            CreateEntry(0x0001, "Hello"),
            CreateEntry(0x0002, "World"),
        ];

        MessageListViewResult result = MessageListViewService.Build(
            entries,
            "World",
            GameKind.OcarinaOfTime,
            romData: null,
            OotEditorTextSyntax.Instance,
            selectedEntryIndex: 1);

        Assert.Equal([1], result.VisibleEntryIndices);
        Assert.Equal("1 matches", result.StatusText);
        Assert.Equal(1, result.SelectedEntryIndex);
    }

    [Fact]
    public void BuildUsesVisibleMessageCountWhenSearchIsEmpty()
    {
        List<MessageEntry> entries =
        [
            CreateEntry(0x0001, "Hello"),
            CreateEntry(0x0002, "World"),
        ];

        MessageListViewResult result = MessageListViewService.Build(
            entries,
            string.Empty,
            GameKind.OcarinaOfTime,
            romData: null,
            OotEditorTextSyntax.Instance,
            selectedEntryIndex: -1);

        Assert.Equal([0, 1], result.VisibleEntryIndices);
        Assert.Equal("2 messages", result.StatusText);
        Assert.Null(result.SelectedEntryIndex);
    }

    [Fact]
    public void BuildHidesLoadedRomFontOrderEntry()
    {
        List<MessageEntry> entries =
        [
            CreateEntry(0x0001, "Visible"),
            CreateEntry(FontOrderCodec.MessageId, "Font order"),
        ];
        RomMessageData romData = CreateRomData(GameKind.OcarinaOfTime, entries);

        MessageListViewResult result = MessageListViewService.Build(
            entries,
            string.Empty,
            GameKind.OcarinaOfTime,
            romData,
            OotEditorTextSyntax.Instance,
            selectedEntryIndex: 1);

        Assert.Equal([0], result.VisibleEntryIndices);
        Assert.Equal("1 messages", result.StatusText);
        Assert.Null(result.SelectedEntryIndex);
        Assert.Equal(1, MessageListViewService.CountVisible(entries, GameKind.OcarinaOfTime, romData));
    }

    [Fact]
    public void BuildHidesMajorasMaskHelperEntries()
    {
        List<MessageEntry> entries =
        [
            CreateEntry(0x0200, "Visible"),
            CreateEntry(FontOrderCodec.MessageId, "Font order"),
            CreateEntry(MmMessageTableCodec.DebuggerEndMessageId, "Debugger end"),
        ];

        MessageListViewResult result = MessageListViewService.Build(
            entries,
            string.Empty,
            GameKind.MajorasMask,
            romData: null,
            MmEditorTextSyntax.Instance,
            selectedEntryIndex: -1);

        Assert.Equal([0], result.VisibleEntryIndices);
        Assert.Equal("1 messages", result.StatusText);
    }

    [Fact]
    public void VisibleOrdinalFallsBackToLastVisibleEntryWhenSelectionIsHidden()
    {
        List<MessageEntry> entries =
        [
            CreateEntry(0x0001, "First"),
            CreateEntry(FontOrderCodec.MessageId, "Font order"),
            CreateEntry(0x0002, "Second"),
        ];
        RomMessageData romData = CreateRomData(GameKind.OcarinaOfTime, entries);

        Assert.Equal(1, MessageListViewService.GetVisibleOrdinal(
            entries,
            GameKind.OcarinaOfTime,
            romData,
            entryIndex: 1));
        Assert.Equal(1, MessageListViewService.GetVisibleOrdinal(
            entries,
            GameKind.OcarinaOfTime,
            romData,
            entryIndex: 99));
        Assert.Equal(0, MessageListViewService.GetSelectedVisibleOrdinal(
            entries,
            GameKind.OcarinaOfTime,
            romData,
            selectedEntryIndex: -1));
    }

    private static MessageEntry CreateEntry(int id, string text)
        => new(id, type: 0, position: 0, bank: 7, offset: 0)
        {
            Text = text,
        };

    private static RomMessageData CreateRomData(GameKind gameKind, List<MessageEntry> entries)
    {
        var profile = new RomVersionProfile(
            "Test ROM",
            string.Empty,
            0,
            0,
            0,
            RomCodecKind.Yaz0,
            false,
            0,
            0,
            0,
            0,
            0,
            [new MessageBankProfile("Messages", 0, 0, 0, 0)],
            new HashSet<int>(),
            Game: gameKind);
        return new RomMessageData(entries, profile, false, [], RomFontResources.Empty, 0, RomMessageSection.Messages);
    }
}
