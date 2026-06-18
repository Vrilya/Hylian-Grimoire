using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Headers;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class HeaderDocumentWorkflowTests
{
    [Fact]
    public void LoadSelectsInitialLanguageWithExistingHeaderRules()
    {
        string path = CreateTempHeaderPath();
        File.WriteAllText(path, """
        DEFINE_MESSAGE(0x0001, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_BOTTOM,
        MSG(/* MISSING */)
        ,
        MSG("NES")
        ,
        MSG("GER")
        ,
        MSG(/* MISSING */)
        )
        """);

        try
        {
            var workflow = new HeaderDocumentWorkflow();

            HeaderDocumentLoadResult result = workflow.Load(path);

            Assert.Equal(GameKind.OcarinaOfTime, result.Document.GameProfile.Kind);
            Assert.Equal(0, result.ActiveLanguageIndex);
            Assert.Equal([0, 1], result.Document.Languages.Keys.Order().ToArray());
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SaveSingleLanguageHeaderUsesActiveEncodingProfile()
    {
        string path = CreateTempHeaderPath();
        List<MessageEntry> entries =
        [
            new(0x0001, 0, 0, 0, 0)
            {
                Text = "Single language",
            },
        ];
        var workflow = new HeaderDocumentWorkflow();

        try
        {
            workflow.Save(path, entries, GameProfiles.Get(GameKind.OcarinaOfTime));

            Assert.Equal(CHeaderExporter.Export(entries), File.ReadAllText(path));
            HeaderDocumentLoadResult loaded = workflow.Load(path);
            Assert.Equal("Single language", Assert.Single(loaded.Document.Languages[0]).Text);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void SaveMultiLanguageHeaderPreservesWesternSlots()
    {
        string path = CreateTempHeaderPath();
        List<MessageEntry> nes =
        [
            new(0x0001, 0, 0, 0, 0)
            {
                Text = "NES text",
            },
        ];
        List<MessageEntry> ger =
        [
            new(0x0001, 0, 0, 0, 0)
            {
                Text = "GER text",
            },
        ];
        List<MessageEntry> fra =
        [
            new(0x0001, 0, 0, 0, 0)
            {
                Text = "FRA text",
            },
        ];
        var languages = new Dictionary<int, List<MessageEntry>>
        {
            [0] = nes,
            [1] = ger,
            [2] = fra,
        };
        var workflow = new HeaderDocumentWorkflow();

        try
        {
            workflow.Save(path, nes, GameProfiles.Get(GameKind.OcarinaOfTime), languages);
            string content = File.ReadAllText(path);

            Assert.Equal("NES text", Assert.Single(CHeaderImporter.Import(content, CHeaderMessageSlot.Nes)).Text);
            Assert.Equal("GER text", Assert.Single(CHeaderImporter.Import(content, CHeaderMessageSlot.Ger)).Text);
            Assert.Equal("FRA text", Assert.Single(CHeaderImporter.Import(content, CHeaderMessageSlot.Fra)).Text);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void LoadAndSaveModernHeaderPreservesJapaneseOnlySlot()
    {
        string path = CreateTempHeaderPath();
        File.WriteAllText(path, """
        DEFINE_MESSAGE_JPN(0x0100, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        MSG("JPN only")
        ,
        MSG(/* MISSING */)
        ,
        MSG(/* MISSING */)
        ,
        MSG(/* MISSING */)
        )
        """);
        var workflow = new HeaderDocumentWorkflow();

        try
        {
            HeaderDocumentLoadResult loaded = workflow.Load(path);

            Assert.NotNull(loaded.Document.JapaneseEntries);
            Assert.Equal("JPN only", Assert.Single(loaded.Document.JapaneseEntries!).Text);

            workflow.Save(
                path,
                loaded.Document.Languages[loaded.ActiveLanguageIndex],
                loaded.Document.GameProfile,
                loaded.Document.Languages,
                loaded.Document.JapaneseEntries);
            string content = File.ReadAllText(path);

            Assert.Equal("JPN only", Assert.Single(CHeaderImporter.Import(content, CHeaderMessageSlot.Jpn)).Text);
            Assert.Throws<HeaderMessageEntriesNotFoundException>(
                () => CHeaderImporter.Import(content, CHeaderMessageSlot.Nes, allowWesternFallback: false));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void LoadAndSaveModernHeaderPreservesJapaneseAndWesternSlots()
    {
        string path = CreateTempHeaderPath();
        File.WriteAllText(path, """
        DEFINE_MESSAGE(0x0101, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        MSG("JPN text")
        ,
        MSG("NES text")
        ,
        MSG(/* MISSING */)
        ,
        MSG(/* MISSING */)
        )
        """);
        var workflow = new HeaderDocumentWorkflow();

        try
        {
            HeaderDocumentLoadResult loaded = workflow.Load(path);

            workflow.Save(
                path,
                loaded.Document.Languages[loaded.ActiveLanguageIndex],
                loaded.Document.GameProfile,
                loaded.Document.Languages,
                loaded.Document.JapaneseEntries);
            string content = File.ReadAllText(path);

            Assert.Equal("JPN text", Assert.Single(CHeaderImporter.Import(content, CHeaderMessageSlot.Jpn)).Text);
            Assert.Equal("NES text", Assert.Single(CHeaderImporter.Import(content, CHeaderMessageSlot.Nes, allowWesternFallback: false)).Text);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void CanExportAllRomLanguagesRequiresMessageSectionAndExportableProfile()
    {
        var workflow = new HeaderDocumentWorkflow();

        Assert.False(workflow.CanExportAllRomLanguages(null));
        Assert.False(workflow.CanExportAllRomLanguages(CreateRomData(bankCount: 1)));
        Assert.True(workflow.CanExportAllRomLanguages(CreateRomData(bankCount: 2)));
        Assert.False(workflow.CanExportAllRomLanguages(CreateRomData(bankCount: 2, activeSection: RomMessageSection.Credits)));
        Assert.True(workflow.CanExportAllRomLanguages(CreateRomData(
            bankCount: 1,
            japaneseBank: new MessageBankProfile("Japanese", 0, 0, 0, 0))));
    }

    [Fact]
    public void ExportCurrentHeaderUsesExistingOotFilteringRules()
    {
        string path = CreateTempHeaderPath();
        List<MessageEntry> entries =
        [
            new(0x0001, 0, 0, 0, 0)
            {
                Text = "Visible",
            },
            new(FontOrderCodec.MessageId, 0, 0, 7, 0)
            {
                Text = "0123456789",
            },
        ];
        RomMessageData romData = CreateRomData(bankCount: 1);
        GameProfile gameProfile = GameProfiles.Get(GameKind.OcarinaOfTime);
        var workflow = new HeaderDocumentWorkflow();

        try
        {
            workflow.ExportCurrent(path, entries, gameProfile, CHeaderExportFormat.Legacy, romData);
            List<MessageEntry> expectedEntries = MessageExportService.GetHeaderExportEntries(
                entries,
                CHeaderExportFormat.Legacy,
                romData);

            Assert.Equal(CHeaderExporter.Export(expectedEntries), File.ReadAllText(path));
            Assert.DoesNotContain("0xFFFC", File.ReadAllText(path));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void GetAvailableWesternImportSlotsUsesExistingHeaderRules()
    {
        string path = CreateTempHeaderPath();
        File.WriteAllText(path, """
        DEFINE_MESSAGE(0x0001, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_BOTTOM,
        MSG(/* MISSING */)
        ,
        MSG("NES")
        ,
        MSG("GER")
        ,
        MSG(/* MISSING */)
        )
        """);
        var workflow = new HeaderDocumentWorkflow();

        try
        {
            Assert.Equal(
                [CHeaderMessageSlot.Nes, CHeaderMessageSlot.Ger],
                workflow.GetAvailableWesternImportSlots(path));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static string CreateTempHeaderPath()
        => Path.Combine(Path.GetTempPath(), $"hylian-grimoire-header-workflow-{Guid.NewGuid():N}.h");

    private static RomMessageData CreateRomData(
        int bankCount,
        RomMessageSection activeSection = RomMessageSection.Messages,
        MessageBankProfile? japaneseBank = null)
    {
        List<MessageBankProfile> banks = Enumerable
            .Range(0, bankCount)
            .Select(index => new MessageBankProfile($"Bank {index + 1}", 0, 0, 0, 0))
            .ToList();
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
            banks,
            new HashSet<int>(),
            JapaneseMessageBank: japaneseBank);
        return new RomMessageData([], profile, false, [], RomFontResources.Empty, 0, activeSection);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
