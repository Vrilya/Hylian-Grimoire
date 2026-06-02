using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using HylianGrimoire.Sessions;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class DocumentSessionTests
{
    [Fact]
    public void UseProjectAndResetManageActiveProjectState()
    {
        var session = new DocumentSession();
        GameProfile profile = GameProfiles.Get(GameKind.OcarinaOfTime);

        Assert.False(session.HasActiveProject);
        Assert.Throws<InvalidOperationException>(() => session.CurrentGameProfile);

        session.UseProject(profile);
        session.SearchText = "zelda";
        session.ForceDirty();

        Assert.True(session.HasActiveProject);
        Assert.Equal(DocumentKind.Project, session.Kind);
        Assert.Same(profile, session.CurrentGameProfile);
        Assert.True(session.HasUnsavedChanges);

        session.Reset();

        Assert.False(session.HasActiveProject);
        Assert.Equal(DocumentKind.None, session.Kind);
        Assert.Empty(session.Entries);
        Assert.Equal(-1, session.CurrentIndex);
        Assert.Equal(string.Empty, session.SearchText);
        Assert.False(session.HasUnsavedChanges);
    }

    [Fact]
    public void LoadTableFilesTracksPathsEntriesProfileAndCleanState()
    {
        List<MessageEntry> entries = [CreateEntry(0x0001, "Hello")];
        GameProfile profile = GameProfiles.Get(GameKind.OcarinaOfTime);
        var document = new MessageFileDocument(entries, profile);
        var session = new DocumentSession();

        session.LoadTableFiles(document, "messages.tbl", "messages.bin");

        Assert.Equal(DocumentKind.DataFiles, session.Kind);
        Assert.Same(entries, session.Entries);
        Assert.Same(profile, session.CurrentGameProfile);
        Assert.Equal("messages.tbl", session.TablePath);
        Assert.Equal("messages.bin", session.BinaryPath);
        Assert.False(session.HasUnsavedChanges);
        Assert.False(session.IsCurrentViewDirty());

        entries[0].Text = "Changed";
        session.MarkDirty();

        Assert.True(session.IsCurrentViewDirty());
        Assert.True(session.HasUnsavedChanges);
    }

    [Fact]
    public void LoadHeaderAndSwitchHeaderLanguageKeepLanguageEntriesCentralized()
    {
        List<MessageEntry> nes = [CreateEntry(0x0001, "NES")];
        List<MessageEntry> ger = [CreateEntry(0x0001, "GER")];
        var languages = new Dictionary<int, List<MessageEntry>>
        {
            [0] = nes,
            [1] = ger,
        };
        var document = new HeaderFileDocument(languages, GameProfiles.Get(GameKind.OcarinaOfTime));
        var session = new DocumentSession();

        session.LoadHeader(document, "message_data.h", activeLanguageIndex: 0);
        session.CurrentIndex = 7;

        Assert.Equal(DocumentKind.Header, session.Kind);
        Assert.Same(languages, session.HeaderLanguageEntries);
        Assert.Same(nes, session.Entries);
        Assert.Equal("message_data.h", session.HeaderPath);

        session.SwitchHeaderLanguage(1);

        Assert.Equal(1, session.ActiveHeaderLanguageIndex);
        Assert.Same(ger, session.Entries);
        Assert.Equal(-1, session.CurrentIndex);

        session.MarkHeaderLanguageDirty();
        session.MarkCurrentViewClean();

        Assert.True(session.HasUnsavedHeaderLanguageChanges);
        Assert.True(session.HasUnsavedChanges);
    }

    [Fact]
    public void RomSessionTracksRomDataAndSavedRomPath()
    {
        RomMessageData loadedRom = CreateRomData([CreateEntry(0x0001, "Original")]);
        RomMessageData switchedRom = loadedRom with
        {
            Entries = [CreateEntry(0x0002, "Switched")],
        };
        var session = new DocumentSession();

        session.LoadRom(loadedRom, "input.z64");

        Assert.Equal(DocumentKind.Rom, session.Kind);
        Assert.Same(loadedRom, session.RomData);
        Assert.Same(loadedRom.Entries, session.Entries);
        Assert.Equal("input.z64", session.RomPath);
        Assert.Same(loadedRom.Profile.GameProfile, session.CurrentGameProfile);

        session.UseRomData(switchedRom);

        Assert.Same(switchedRom, session.RomData);
        Assert.Same(switchedRom.Entries, session.Entries);

        session.MarkSavedAsRom("output.z64");

        Assert.Equal(DocumentKind.Rom, session.Kind);
        Assert.Equal("output.z64", session.RomPath);
    }

    [Fact]
    public void DirtyFingerprintIncludesMajorasMaskMetadata()
    {
        var metadata = new MajorasMaskMessageMetadata(
            TableTypePosition: 0x10,
            TextBoxProperties: 0x0200,
            IconId: 0x01,
            NextTextId: 0xffff,
            FirstChoicePrice: 0xffff,
            SecondChoicePrice: 0xffff,
            Unknown: 0xffff);
        MessageEntry entry = CreateEntry(0x0200, "MM");
        entry.CodecMetadata = metadata;
        var session = new DocumentSession();
        session.LoadTableFiles(
            new MessageFileDocument([entry], GameProfiles.Get(GameKind.MajorasMask)),
            "mm.tbl",
            "mm.bin");

        Assert.False(session.IsCurrentViewDirty());

        entry.CodecMetadata = metadata with { IconId = 0x02 };
        session.MarkDirty();

        Assert.True(session.IsCurrentViewDirty());
        Assert.True(session.HasUnsavedChanges);
    }

    private static MessageEntry CreateEntry(int id, string text)
        => new(id, type: 0, position: 0, bank: 7, offset: 0)
        {
            Text = text,
        };

    private static RomMessageData CreateRomData(List<MessageEntry> entries)
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
            [new MessageBankProfile("English", 0, 0, 0, 0)],
            new HashSet<int>());
        return new RomMessageData(entries, profile, false, [], RomFontResources.Empty, 0, RomMessageSection.Messages);
    }
}
