using System.Drawing;
using HylianGrimoire.Codecs;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Headers;
using HylianGrimoire.Models;
using HylianGrimoire.Preview;
using HylianGrimoire.Services;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace HylianGrimoire.Tests;

public sealed class ExportParityTests
{
    public ExportParityTests()
    {
        Environment.SetEnvironmentVariable(
            "OOT_EDITOR_CHARACTER_PROFILE_CONFIG_DIR",
            Path.Combine(Path.GetTempPath(), "HylianGrimoireTests", Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public void WinUiMultilineTextRoundtripsWithoutChangingExportedBytes()
    {
        var baselineEntries = CreateEntries();
        var winUiEntries = CreateEntries();

        // Simulate what a WinUI TextBox can hand back after editing multiline text.
        winUiEntries[0].Text = MessageTextSyntax.FromDisplay(
            NormalizeEditorText(MessageTextSyntax.ToDisplay(winUiEntries[0].Text).Replace("\n", "\r\n")));

        Assert.Equal(CHeaderExporter.Export(baselineEntries), CHeaderExporter.Export(winUiEntries));

        var baselineFiles = MessageTableCodec.BuildFiles(baselineEntries);
        var winUiFiles = MessageTableCodec.BuildFiles(winUiEntries);
        Assert.Equal(baselineFiles.tableBytes, winUiFiles.tableBytes);
        Assert.Equal(baselineFiles.msgBytes, winUiFiles.msgBytes);
    }

    [Fact]
    public void MessageListServiceHandlesAddChangeMoveAndDeleteRules()
    {
        var entries = new List<MessageEntry>
        {
            new(0x1000, 1, 2, 7, 0) { TableEndMarkerId = 0xfffd, Text = "First" },
            new(0x1002, 1, 2, 7, 0) { TableEndMarkerId = 0xfffd, Text = "Second" },
        };

        var addResult = MessageListService.AddAfterSelected(entries, 0, 0x1001);
        Assert.True(addResult.Succeeded);
        Assert.Equal(1, addResult.SelectedIndex);
        Assert.Equal(0x1001, entries[1].Id);

        Assert.False(MessageListService.AddAfterSelected(entries, 0, 0x1001).Succeeded);

        var changeResult = MessageListService.ChangeId(entries, 1, 0x1003);
        Assert.True(changeResult.Succeeded);
        Assert.Equal(0x1003, entries[^1].Id);

        var moveResult = MessageListService.MoveUnderId(entries, 2, 0x1000);
        Assert.True(moveResult.Succeeded);
        Assert.Equal(0x1003, entries[1].Id);

        var deleteResult = MessageListService.Delete(entries, 1);
        Assert.True(deleteResult.Succeeded);
        Assert.Equal(2, entries.Count);
        Assert.DoesNotContain(entries, entry => entry.Id == 0x1003);
    }

    [Fact]
    public void MessageTypeCatalogMapsCreditsAndUnknownTypes()
    {
        Assert.Equal("Credits", MessageTypeCatalog.Items.FirstOrDefault(item => item.Value == 11)?.Name);
        Assert.Equal(OotPreviewStyle.Credits, MessageTypeCatalog.ToPreviewStyle(11));
        Assert.Equal(OotPreviewStyle.Black, MessageTypeCatalog.ToPreviewStyle(99));
    }

    [Fact]
    public void MessageSearchCachesDisplayTextAndInvalidatesWhenTextChanges()
    {
        var entry = new MessageEntry(0x0001, 0, 0, 7, 0)
        {
            Text = "First[break]Second",
        };

        Assert.True(MessageSearch.Matches(entry, "First[break]Second"));
        Assert.True(MessageSearch.Matches(entry, "\n[break]\n"));

        entry.Text = "Plain";
        Assert.False(MessageSearch.Matches(entry, "\n[break]\n"));
        Assert.True(MessageSearch.Matches(entry, "Plain"));
    }

    [Fact]
    public void MessageFileServiceRoundtripsTableFilesAndHeaders()
    {
        var baselineEntries = CreateEntries();
        var baselineFiles = MessageTableCodec.BuildFiles(baselineEntries);

        string serviceTestDir = Path.Combine(Path.GetTempPath(), "HylianGrimoireTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(serviceTestDir);
        string serviceTblPath = Path.Combine(serviceTestDir, "messages.tbl");
        string serviceBinPath = Path.Combine(serviceTestDir, "messages.bin");
        string serviceHeaderPath = Path.Combine(serviceTestDir, "messages.h");

        MessageFileService.SaveTableFiles(baselineEntries, serviceTblPath, serviceBinPath);
        var serviceEntries = MessageFileService.LoadTableFiles(serviceTblPath, serviceBinPath);
        Assert.Equal(baselineEntries.Count, serviceEntries.Count);
        Assert.Equal(baselineFiles.tableBytes, File.ReadAllBytes(serviceTblPath));
        Assert.Equal(baselineFiles.msgBytes, File.ReadAllBytes(serviceBinPath));

        MessageFileService.ExportHeader(baselineEntries, serviceHeaderPath);
        Assert.Equal(CHeaderExporter.Export(baselineEntries), File.ReadAllText(serviceHeaderPath));
        Assert.Equal(baselineEntries.Count, MessageFileService.ImportHeader(serviceHeaderPath).Count);
    }

    [Fact]
    public void InvalidSyntaxErrorsIncludeMessageContext()
    {
        var invalidEntries = new List<MessageEntry>
        {
            new(0x1234, 0, 0, 7, 0)
            {
                Text = "[sfx:nothex]",
            },
        };

        AssertInvalidEntryContext(() => MessageTableCodec.BuildFiles(invalidEntries), "Message 0x1234");
        AssertInvalidEntryContext(() => CHeaderExporter.Export(invalidEntries), "Message 0x1234");
        AssertInvalidSyntax("[sfx:nothex]", "[sfx:nothex]");
        AssertInvalidSyntax("[item:300]", "[item:300]");
    }

    [Fact]
    public void HeaderImportAndExportRoundtripSpecialSyntax()
    {
        var baselineEntries = CreateEntries();
        string exportedHeader = CHeaderExporter.Export(baselineEntries);

        var importedHeaderEntries = CHeaderImporter.Import(exportedHeader);
        Assert.Equal(baselineEntries.Count, importedHeaderEntries.Count);
        for (int i = 0; i < baselineEntries.Count; i++)
        {
            Assert.Equal(baselineEntries[i].Id, importedHeaderEntries[i].Id);
            Assert.Equal(baselineEntries[i].Type, importedHeaderEntries[i].Type);
            Assert.Equal(baselineEntries[i].Position, importedHeaderEntries[i].Position);
            Assert.Equal(baselineEntries[i].Text, importedHeaderEntries[i].Text);
        }

        string choiceHeader = """
        DEFINE_MESSAGE(0x002B, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_BOTTOM,
        "En omgâng kostar " COLOR(RED) "20 rupier" COLOR(DEFAULT) ".\n"
        "Vill du göra ett försök?"
        TWO_CHOICE
        COLOR(GREEN) "Ja\n"
        "Nej" COLOR(DEFAULT)
        )
        """;
        var choiceEntries = CHeaderImporter.Import(choiceHeader);
        Assert.Equal("En omgâng kostar [color:red]20 rupier[color:default].\nVill du göra ett försök?[twochoice][color:green]Ja\nNej[color:default]", choiceEntries[0].Text);

        var triangleEntry = new List<MessageEntry>
        {
            new(0x0100, 0, 3, 7, 0)
            {
                Text = "Tryck [Triangle] här",
            },
        };
        string triangleHeader = CHeaderExporter.Export(triangleEntry);
        Assert.Contains("[Triangle]", triangleHeader);
        Assert.DoesNotContain('▼', triangleHeader);
        Assert.Equal(triangleEntry[0].Text, CHeaderImporter.Import(triangleHeader)[0].Text);
        Assert.Equal("Tryck [Triangle]", CHeaderImporter.Import("""
        DEFINE_MESSAGE(0x0101, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_BOTTOM,
        "Tryck <TRIANGLE>"
        )
        """)[0].Text);

        string otrModHeader = CHeaderExporter.Export(triangleEntry, CHeaderExportFormat.OTRMod);
        Assert.Contains('▼', otrModHeader);
        Assert.DoesNotContain("[Triangle]", otrModHeader);
        Assert.Equal(triangleEntry[0].Text, CHeaderImporter.Import(otrModHeader)[0].Text);

        var adjacentMacroEntry = new List<MessageEntry>
        {
            new(0x0102, 2, 3, 7, 0)
            {
                Text = "[unskippable][item:2d][quicktexton]Text",
            },
        };
        string adjacentMacroHeader = CHeaderExporter.Export(adjacentMacroEntry, CHeaderExportFormat.OTRMod);
        Assert.Contains("UNSKIPPABLE  ITEM_ICON(\"\\x2D\")  QUICKTEXT_ENABLE \"Text\"", adjacentMacroHeader);
        Assert.Equal(adjacentMacroEntry[0].Text, CHeaderImporter.Import(adjacentMacroHeader)[0].Text);

        var fontOrderEntry = new List<MessageEntry>
        {
            new(0xfffc, 0, 0, 7, 0)
            {
                Text = FontOrderCodec.GetStandardEditorText() + "\n",
            },
        };
        string otrModFontOrder = CHeaderExporter.Export(fontOrderEntry, CHeaderExportFormat.OTRMod);
        Assert.Contains("DEFINE_MESSAGE(0xFFFC, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_VARIABLE,", otrModFontOrder);
        Assert.Contains("\"0123456789\\n\"", otrModFontOrder);
        Assert.Contains("\" -.\\n\"", otrModFontOrder);
        Assert.Equal(fontOrderEntry[0].Text, CHeaderImporter.Import(otrModFontOrder)[0].Text);
    }

    [Fact]
    public void ModernHeaderImportSelectsRequestedLanguageSlot()
    {
        const string modernHeader = """
        DEFINE_MESSAGE_NES(0x0002, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        MSG(/* MISSING */)
        ,
        MSG(
        UNSKIPPABLE ITEM_ICON(ITEM_POCKET_EGG) QUICKTEXT_ENABLE "You borrowed a " COLOR(RED) "Pocket Egg" COLOR(DEFAULT) "!" QUICKTEXT_DISABLE 0x01,
        "It will hatch overnight."
        )
        ,
        MSG(
        UNSKIPPABLE "Deutsch"
        )
        ,
        MSG(
        UNSKIPPABLE "Francais"
        )
        )
        """;

        var nes = CHeaderImporter.Import(modernHeader, CHeaderMessageSlot.Nes);
        Assert.Equal("[unskippable][item:2d][quicktexton]You borrowed a [color:red]Pocket Egg[color:default]![quicktextoff]\nIt will hatch overnight.", nes[0].Text);

        var ger = CHeaderImporter.Import(modernHeader, CHeaderMessageSlot.Ger);
        Assert.Equal("[unskippable]Deutsch", ger[0].Text);

        var fra = CHeaderImporter.Import(modernHeader, CHeaderMessageSlot.Fra);
        Assert.Equal("[unskippable]Francais", fra[0].Text);

        var commented = CHeaderImporter.Import("""
        DEFINE_MESSAGE_NES(0x0003, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        MSG(/* MISSING */), MSG("Text" /* comment */), MSG(/* UNUSED */), MSG(/* UNUSED */))
        """);
        Assert.Equal("Text", commented[0].Text);
    }

    [Fact]
    public void ModernHeaderImportSkipsLanguageSpecificMacrosOutsideRequestedSlot()
    {
        const string modernHeader = """
        DEFINE_MESSAGE_JPN(0x0100, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        MSG("JPN only"), MSG(/* MISSING */), MSG(/* MISSING */), MSG(/* MISSING */))

        DEFINE_MESSAGE_NES(0x0101, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        MSG(/* MISSING */), MSG("NES only"), MSG(/* MISSING */), MSG(/* MISSING */))
        """;

        var nes = CHeaderImporter.Import(modernHeader, CHeaderMessageSlot.Nes);
        Assert.Single(nes);
        Assert.Equal(0x0101, nes[0].Id);
        Assert.Equal("NES only", nes[0].Text);

        var jpn = CHeaderImporter.Import(modernHeader, CHeaderMessageSlot.Jpn);
        Assert.Single(jpn);
        Assert.Equal(0x0100, jpn[0].Id);
        Assert.Equal("JPN only", jpn[0].Text);
    }

    [Fact]
    public void ModernHeaderImportNeverFallsBackToJapaneseForWesternSlots()
    {
        const string modernHeader = """
        DEFINE_MESSAGE(0x5072, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_BOTTOM,
        MSG("ゴ")
        ,
        MSG(/* MISSING */)
        ,
        MSG(/* MISSING */)
        ,
        MSG(/* MISSING */)
        )

        DEFINE_MESSAGE(0x5073, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_BOTTOM,
        MSG("日本語")
        ,
        MSG("English")
        ,
        MSG(/* MISSING */)
        ,
        MSG(/* MISSING */)
        )
        """;

        var nes = CHeaderImporter.Import(modernHeader, CHeaderMessageSlot.Nes);
        Assert.Single(nes);
        Assert.Equal(0x5073, nes[0].Id);
        Assert.Equal("English", nes[0].Text);

        var ger = CHeaderImporter.Import(modernHeader, CHeaderMessageSlot.Ger);
        Assert.Single(ger);
        Assert.Equal(0x5073, ger[0].Id);
        Assert.Equal("English", ger[0].Text);

        var fra = CHeaderImporter.Import(modernHeader, CHeaderMessageSlot.Fra);
        Assert.Single(fra);
        Assert.Equal(0x5073, fra[0].Id);
        Assert.Equal("English", fra[0].Text);

        var jpn = CHeaderImporter.Import(modernHeader, CHeaderMessageSlot.Jpn);
        Assert.Equal([0x5072, 0x5073], jpn.Select(entry => entry.Id).ToArray());
        Assert.Equal("ゴ", jpn[0].Text);
        Assert.Equal("日本語", jpn[1].Text);
    }

    [Fact]
    public void ModernHeaderExportUsesMsgSlots()
    {
        var entries = new List<MessageEntry>
        {
            new(0x0001, 0, 3, 7, 0)
            {
                Text = "[unskippable][item:2d]Hello[Triangle][shift:13][sfx:Laugh2][fish]",
            },
        };

        string exported = CHeaderExporter.Export(entries, CHeaderExportFormat.Modern);
        Assert.Contains("DEFINE_MESSAGE(0x0001, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_BOTTOM,", exported);
        Assert.DoesNotContain("MSG(", exported);
        Assert.Contains("UNSKIPPABLE", exported);
        Assert.Contains("ITEM_ICON(ITEM_POCKET_EGG)", exported);
        Assert.Contains("SHIFT(19)", exported);
        Assert.Contains("SFX(NA_SE_VO_Z0_SMILE_0)", exported);
        Assert.Contains("HIGHSCORE(HS_FISHING)", exported);
        Assert.Contains("\"Hello▼\"", exported);
        Assert.DoesNotContain("[Triangle]", exported);
        Assert.Equal(1, CountOccurrences(exported, "UNSKIPPABLE"));
        Assert.DoesNotContain("MSG(/* MISSING */)", exported);
    }

    [Fact]
    public void ModernHeaderExportCanWriteAllRomLanguageSlots()
    {
        var nes = new List<MessageEntry>
        {
            new(0x0001, 0, 3, 7, 0) { Text = "English" },
        };
        var ger = new List<MessageEntry>
        {
            new(0x0001, 0, 3, 7, 0) { Text = "Deutsch" },
        };
        var fra = new List<MessageEntry>
        {
            new(0x0001, 0, 3, 7, 0) { Text = "Francais" },
        };

        string exported = CHeaderExporter.ExportModernLanguages(null, nes, ger, fra);
        Assert.StartsWith("DEFINE_MESSAGE(0x0001", exported);
        Assert.Contains("\"English\"", exported);
        Assert.Contains("\"Deutsch\"", exported);
        Assert.Contains("\"Francais\"", exported);
        Assert.Contains("MSG(/* MISSING */)\n,\nMSG(", exported);
        Assert.Equal(1, CountOccurrences(exported, "MSG(/* MISSING */)"));
    }

    [Fact]
    public void ModernHeaderExportCanWriteJapaneseSlot()
    {
        var jpn = new List<MessageEntry>
        {
            new(0x0001, 2, 3, 0x08, 0)
            {
                OriginalEncodedBytes = [0x81, 0x99, 0x82, 0x50, 0x81, 0x70, 0x00, 0x00],
            },
        };
        var nes = new List<MessageEntry>
        {
            new(0x0001, 2, 3, 0x07, 0)
            {
                Text = "Hello!",
            },
        };

        string exported = CHeaderExporter.ExportModernLanguages(jpn, nes, null, null);

        Assert.Contains("DEFINE_MESSAGE(0x0001", exported);
        Assert.Contains("MSG(\nUNSKIPPABLE \"１\"\n)", exported);
        Assert.Contains("MSG(\n\"Hello!\"", exported);
        Assert.DoesNotContain("0x70", exported);
    }

    [Fact]
    public void ModernHeaderExportUsesLanguageSpecificMacrosForSingleLanguageEntries()
    {
        var jpn = new List<MessageEntry>
        {
            new(0x0347, 4, 2, 0x08, 0)
            {
                OriginalEncodedBytes = [0x82, 0x51, 0x81, 0x70],
            },
        };
        var nes = new List<MessageEntry>
        {
            new(0x0346, 4, 2, 0x07, 0)
            {
                Text = "English only",
            },
        };

        string exported = CHeaderExporter.ExportModernLanguages(jpn, nes, null, null);

        Assert.Contains("DEFINE_MESSAGE_NES(0x0346", exported);
        Assert.Contains("DEFINE_MESSAGE_JPN(0x0347", exported);
        Assert.True(exported.IndexOf("0x0346", StringComparison.Ordinal) < exported.IndexOf("0x0347", StringComparison.Ordinal));
    }

    [Fact]
    public void ModernHeaderExportUsesDecompJapaneseWaveDash()
    {
        var jpn = new List<MessageEntry>
        {
            new(0x0002, 2, 3, 0x08, 0)
            {
                OriginalEncodedBytes = [0x81, 0x60, 0x81, 0x70],
            },
        };

        string exported = CHeaderExporter.ExportModernLanguages(jpn, null, null, null);

        Assert.Contains("\"〜\"", exported);
        Assert.DoesNotContain("～", exported);
    }

    [Fact]
    public void ModernHeaderExportKeepsJapaneseArgumentsEndingInZero()
    {
        var jpn = new List<MessageEntry>
        {
            new(0x0006, 0, 3, 0x08, 0)
            {
                OriginalEncodedBytes =
                [
                    0x81, 0x89, 0x83, 0x6F, 0x83, 0x4E, 0x83, 0x5F, 0x83, 0x93, 0x81, 0x40,
                    0x82, 0x51, 0x82, 0x4F, 0x83, 0x52, 0x81, 0x40, 0x82, 0x57, 0x82, 0x4F,
                    0x83, 0x8B, 0x83, 0x73, 0x81, 0x5B, 0x81, 0x8A, 0x00, 0x0A, 0x81, 0xBC,
                    0x00, 0x0B, 0x0C, 0x02, 0x82, 0xA9, 0x82, 0xA4, 0x00, 0x0A, 0x82, 0xE2,
                    0x82, 0xDF, 0x82, 0xC6, 0x82, 0xAD, 0x00, 0x0B, 0x0C, 0x00, 0x81, 0x70,
                ],
            },
        };

        string exported = CHeaderExporter.ExportModernLanguages(jpn, null, null, null);

        Assert.Contains("QUICKTEXT_ENABLE \"バクダン", exported);
        Assert.Contains("TWO_CHOICE", exported);
        Assert.Contains("COLOR(ADJUSTABLE) \"かう\\n\"", exported);
        Assert.Contains("\"やめとく\" COLOR(DEFAULT)", exported);
        Assert.DoesNotContain("0x81, 0x89", exported);
    }

    [Fact]
    public void ModernHeaderExportDecodesJapaneseBackground()
    {
        var jpn = new List<MessageEntry>
        {
            new(0x0300, 4, 2, 0x08, 0)
            {
                OriginalEncodedBytes = [0x86, 0xB3, 0x00, 0x00, 0x01, 0x10, 0x81, 0x70],
            },
        };

        string exported = CHeaderExporter.ExportModernLanguages(jpn, null, null, null);

        Assert.Contains("BACKGROUND(X_LEFT, WHITE, GOLD, 2, 0)", exported);
        Assert.DoesNotContain("0x86, 0xB3", exported);
    }

    [Fact]
    public void ModernHeaderExportDecodesJapaneseBackgroundBeforePersistent()
    {
        var jpn = new List<MessageEntry>
        {
            new(0x088b, 5, 2, 0x08, 0)
            {
                OriginalEncodedBytes = [0x86, 0xB3, 0x00, 0x00, 0x20, 0x00, 0x86, 0xC8, 0x81, 0x70],
            },
        };

        string exported = CHeaderExporter.ExportModernLanguages(jpn, null, null, null);

        Assert.Contains("BACKGROUND(X_LEFT, ORANGE, BLACK, 1, 0) PERSISTENT", exported);
        Assert.DoesNotContain("0x86, 0xB3", exported);
    }

    [Fact]
    public void MalformedHeadersFailWithUsefulErrors()
    {
        var missingClose = Assert.Throws<InvalidDataException>(() => CHeaderImporter.Import("""
        DEFINE_MESSAGE(0x0003, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        "Missing close"

        DEFINE_MESSAGE(0x0004, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        "Next message"
        )
        """));
        Assert.Contains("missing a closing parenthesis", missingClose.Message, StringComparison.OrdinalIgnoreCase);

        var emptyColor = Assert.Throws<InvalidDataException>(() => CHeaderImporter.Import("""
        DEFINE_MESSAGE(0x0005, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        COLOR()
        )
        """));
        Assert.Contains("0x0005", emptyColor.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("line 1", emptyColor.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Invalid empty integer value", emptyColor.Message, StringComparison.OrdinalIgnoreCase);

        var oversizedByteArgument = Assert.Throws<InvalidDataException>(() => CHeaderImporter.Import("""
        DEFINE_MESSAGE(0x0006, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        SHIFT(0x123)
        )
        """));
        Assert.Contains("0x0006", oversizedByteArgument.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Expected byte-sized argument", oversizedByteArgument.Message, StringComparison.OrdinalIgnoreCase);

        var oversizedWordArgument = Assert.Throws<InvalidDataException>(() => CHeaderImporter.Import("""
        DEFINE_MESSAGE(0x0007, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        TEXTID(0x12345)
        )
        """));
        Assert.Contains("0x0007", oversizedWordArgument.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Expected 16-bit argument", oversizedWordArgument.Message, StringComparison.OrdinalIgnoreCase);

        var unknownMacro = Assert.Throws<InvalidDataException>(() => CHeaderImporter.Import("""
        DEFINE_MESSAGE(0x0008, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        QICKTEXT_ENABLE "Typo"
        )
        """));
        Assert.Contains("0x0008", unknownMacro.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Unknown C header message macro", unknownMacro.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StaffCreditsTablesAndCreditsTokensRoundtrip()
    {
        byte[] staffTable =
        [
            0x05, 0x00, 0xb0, 0x00, 0x07, 0x00, 0x00, 0x00,
            0x05, 0x01, 0xb0, 0x00, 0x07, 0x00, 0x00, 0x04,
            0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        ];
        byte[] staffMessages =
        [
            (byte)'A', (byte)'B', 0x02, 0x00,
            (byte)'C', 0x01, (byte)'D', 0x02,
        ];

        var staffEntries = MessageTableCodec.ParseTable(staffTable, staffMessages);
        Assert.Equal(2, staffEntries.Count);
        Assert.Equal("AB", staffEntries[0].Text);
        Assert.Equal("C\nD", staffEntries[1].Text);
        var staffFiles = MessageTableCodec.BuildFiles(staffEntries);
        Assert.Equal(staffTable, staffFiles.tableBytes);

        var centered = DecodeEditorText([(byte)0xfd, (byte)'A', (byte)'B', 0x02]);
        Assert.Equal("[center]AB", centered);
        Assert.Equal([(byte)0xfd, (byte)'A', (byte)'B', 0x02], EncodeEditorText(centered));
    }

    [Fact]
    public void EndFadeExportsTwoBytes()
    {
        var endFade = DecodeEditorText([0x11, 0x00, 0x3c, 0x02]);
        Assert.Equal("[endfade:003c]", endFade);
        Assert.Equal([0x11, 0x00, 0x3c, 0x02], EncodeEditorText(endFade));

        string endFadeHeader = CHeaderExporter.Export(
        [
            new MessageEntry(0x0400, 0, 3, 7, 0)
            {
                Text = "[endfade:013c]",
            },
        ]);
        Assert.Contains("""FADE2("\x01\x3C")""", endFadeHeader);
    }

    [Fact]
    public void PreviewTokensHandleBreakdelayChoiceAndHighscores()
    {
        var delayedPages = OotPreviewTextPage.FromMessageTokensPages(MessageTextSyntax.FromEditorText("First[breakdelay:20]Second"));
        Assert.Equal(2, delayedPages.Count);

        var choiceTokens = OotPreviewTextPage.FromMessageTokens(MessageTextSyntax.FromEditorText("[twochoice]\nJa\nNej"));
        Assert.Contains(choiceTokens, token => token.Kind == OotPreviewTokenKind.Choice && token.Value == 2);

        Assert.Equal("1500", PreviewGlyphText("[minigame:00]"));
        Assert.Equal("1000", PreviewGlyphText("[minigame:01]"));
        Assert.Equal("35", PreviewGlyphText("[minigame:02]"));
        Assert.Equal("02:35", PreviewGlyphText("[minigame:03]"));
        Assert.Equal("02:35", PreviewGlyphText("[minigame:04]"));
        Assert.Equal("1500", PreviewGlyphText("[archery]"));
        Assert.Equal("1000", PreviewGlyphText("[poe]"));
        Assert.Equal("35", PreviewGlyphText("[fish]"));
        Assert.Equal("02:35", PreviewGlyphText("[horserace]"));
        Assert.Equal("02:35", PreviewGlyphText("[marathon]"));
    }

    [Fact]
    public void SemanticHighscoreTagsRoundtrip()
    {
        Assert.Equal("[archery]", DecodeEditorText([0x1e, 0x00, 0x02, 0x00]));
        Assert.Equal([0x1e, 0x00, 0x02, 0x00], EncodeEditorText("[archery]"));
        Assert.Equal("[points]", DecodeEditorText([0x18, 0x02, 0x00, 0x00]));
        Assert.Equal([0x18, 0x02, 0x00, 0x00], EncodeEditorText("[points]"));

        string highscoreHeader = CHeaderExporter.Export(
        [
            new MessageEntry(0x0200, 0, 3, 7, 0)
            {
                Text = "[poe]",
            },
        ]);
        Assert.Contains("HIGHSCORE(HS_POE_POINTS)", highscoreHeader, StringComparison.Ordinal);
        Assert.Equal("[poe]", CHeaderImporter.Import(highscoreHeader)[0].Text);
    }

    [Fact]
    public void SfxTagsSupportKnownNamesAndUnknownHexValues()
    {
        Assert.Equal("[sfx:Laugh2]", DecodeEditorText([0x12, 0x68, 0x6d, 0x02]));
        Assert.Equal([0x12, 0x68, 0x6d, 0x02], EncodeEditorText("[sfx:Laugh2]"));
        Assert.Equal("[sfx:1234]", DecodeEditorText([0x12, 0x12, 0x34, 0x02]));
        Assert.Equal([0x12, 0x12, 0x34, 0x02], EncodeEditorText("[sfx:1234]"));
        Assert.Equal("[sfx:NaviHello]", CHeaderImporter.Import("""
        DEFINE_MESSAGE(0x0300, TEXTBOX_TYPE_BLUE, TEXTBOX_POS_BOTTOM,
        SFX("\x68\x44") "Hej"
        )
        """)[0].Text[..15]);
    }

    [Fact]
    public void TokenSyntaxRoundtripsBytes()
    {
        byte[] tokenSampleBytes =
        [
            0x1a, 0x13, 0x30, 0x08, (byte)'D', (byte)'u', (byte)' ',
            0x05, 0x41, (byte)'K', (byte)'o', (byte)'j', (byte)'i', (byte)'r', (byte)'o',
            0x05, 0x40, 0x01, 0x12, 0x68, 0x6d, 0x04, 0x02, 0x00,
        ];
        var tokenSample = MessageCodec.DecodeMessageTokens(tokenSampleBytes, 0, tokenSampleBytes.Length);
        Assert.Equal("[unskippable][item:30][quicktexton]Du [color:red]Kojiro[color:default]\n[sfx:Laugh2][break]", MessageTextSyntax.ToEditorText(tokenSample));
        Assert.Equal(tokenSampleBytes, MessageCodec.EncodeMessageTokens(tokenSample));
        Assert.Equal(
            tokenSampleBytes,
            MessageCodec.EncodeMessageTokens(MessageTextSyntax.FromEditorText(MessageTextSyntax.ToEditorText(tokenSample))));
    }

    [Fact]
    public void EncodingProfileAndCharacterProfilesPreserveUnderlyingBytes()
    {
        var encodingProfile = MessageEncodingProfile.Default;
        Assert.True(encodingProfile.TryGetByte('â', out byte lowerRing));
        Assert.Equal(0x92, lowerRing);
        Assert.True(encodingProfile.TryGetByte('Â', out byte upperRing));
        Assert.Equal(0x82, upperRing);
        Assert.Equal("âÂ", encodingProfile.ToHeaderText("âÂ"));
        Assert.Equal("âÂ", encodingProfile.HeaderTextToEditorText("âÂ"));

        string profileName = $"Swedish {Guid.NewGuid():N}";
        CharacterProfileStore.Current.CreateProfile(profileName);
        try
        {
            CharacterProfileStore.Current.SetDisplayChar(0x92, 'å');
            Assert.Equal("å", DecodeEditorText([0x92, 0x02, 0x00, 0x00]));
            Assert.Equal([0x92, 0x02, 0x00, 0x00], EncodeEditorText("å"));
            Assert.Equal("å", encodingProfile.HeaderTextToEditorText("â"));
            Assert.Equal("â", encodingProfile.ToHeaderText("å"));
            Assert.Equal("Profil å", CHeaderImporter.Import("""
                DEFINE_MESSAGE(0x0001, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_BOTTOM,
                "Profil â"
                )
                """)[0].Text);
            Assert.True(MessageEncodingProfile.Original.TryGetByte('â', out byte originalLowerCircumflex));
            Assert.Equal(0x92, originalLowerCircumflex);
            Assert.False(MessageEncodingProfile.Original.TryGetByte('å', out _));
        }
        finally
        {
            CharacterProfileStore.Current.ResetDisplayChar(0x92);
            CharacterProfileStore.Current.DeleteSelectedProfile();
        }
        Assert.Equal("â", DecodeEditorText([0x92, 0x02, 0x00, 0x00]));
    }

    [Fact]
    public void OriginalEncodingProfileIgnoresCharacterProfilesForRomData()
    {
        CharacterProfileStore.Current.CreateProfile("Swedish ROM");
        CharacterProfileStore.Current.SetDisplayChar(0x92, 'å');
        try
        {
            Assert.Equal("â", DecodeEditorText([0x92, 0x02, 0x00, 0x00], MessageEncodingProfile.Original));
            Assert.Equal([0x92, 0x02, 0x00, 0x00], EncodeEditorText("â", MessageEncodingProfile.Original));
            Assert.Throws<InvalidDataException>(() => EncodeEditorText("å", MessageEncodingProfile.Original));
        }
        finally
        {
            CharacterProfileStore.Current.ResetDisplayChar(0x92);
            CharacterProfileStore.Current.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void CharacterProfileRemapKeepsBytesButUpdatesEditorCharacters()
    {
        CharacterProfileStore.Current.CreateProfile("Swedish Remap");
        CharacterProfileStore.Current.SetDisplayChar(0x82, 'Å');
        CharacterProfileStore.Current.SetDisplayChar(0x92, 'å');
        try
        {
            string swedish = CharacterProfileStore.Current.RemapEditorText(
                "Ââ [color:red]Ââ",
                CharacterProfileStore.DefaultProfileName,
                "Swedish Remap");
            Assert.Equal("Åå [color:red]Åå", swedish);

            string defaultText = CharacterProfileStore.Current.RemapEditorText(
                swedish,
                "Swedish Remap",
                CharacterProfileStore.DefaultProfileName);
            Assert.Equal("Ââ [color:red]Ââ", defaultText);
        }
        finally
        {
            CharacterProfileStore.Current.ResetDisplayChar(0x82);
            CharacterProfileStore.Current.ResetDisplayChar(0x92);
            CharacterProfileStore.Current.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void DeletedCharacterProfileCanStillRemapBackToDefault()
    {
        CharacterProfileStore.Current.CreateProfile("Deleted Swedish");
        CharacterProfileStore.Current.SetDisplayChar(0x92, 'å');
        CharacterProfile? deletedProfile = null;
        CharacterProfileStore.Current.SelectionChanged += (_, args) => deletedProfile = args.PreviousProfile;
        CharacterProfileStore.Current.DeleteSelectedProfile();

        Assert.NotNull(deletedProfile);
        string defaultText = CharacterProfileStore.Current.RemapEditorText(
            "å",
            deletedProfile,
            CharacterProfileStore.DefaultProfileName);
        Assert.Equal("â", defaultText);
    }

    [Fact]
    public void CharacterProfileWidthUsesCallerBaselineDefault()
    {
        CharacterProfileStore.Current.CreateProfile("PAL baseline");
        try
        {
            CharacterProfileStore.Current.SetWidth(0x81, 6.0, 6.0);
            Assert.False(CharacterProfileStore.Current.TryGetWidth(0x81, out _));

            CharacterProfileStore.Current.SetWidth(0x81, 12.0, 6.0);
            Assert.True(CharacterProfileStore.Current.TryGetWidth(0x81, out double width));
            Assert.Equal(12.0, width);
        }
        finally
        {
            CharacterProfileStore.Current.ResetWidth(0x81);
            CharacterProfileStore.Current.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void CharacterProfileImagesAreStoredInsideProfileConfig()
    {
        string profileName = $"Image profile {Guid.NewGuid():N}";
        string sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        using (var bitmap = new Bitmap(16, 16))
        {
            bitmap.SetPixel(0, 0, Color.White);
            bitmap.Save(sourcePath);
        }

        CharacterProfileStore.Current.CreateProfile(profileName);
        try
        {
            CharacterProfileStore.Current.SetImage(0x82, sourcePath);
            Assert.True(CharacterProfileStore.Current.TryGetImagePath(0x82, out string? storedPath));
            Assert.NotNull(storedPath);
            File.Delete(storedPath);

            Assert.True(CharacterProfileStore.Current.TryGetImagePath(0x82, out string? restoredPath));
            Assert.True(File.Exists(restoredPath));
        }
        finally
        {
            CharacterProfileStore.Current.ResetImage(0x82);
            CharacterProfileStore.Current.DeleteSelectedProfile();
            if (File.Exists(sourcePath))
            {
                File.Delete(sourcePath);
            }
        }
    }

    [Fact]
    public void GlyphRemapperReplacesUnderlyingBytesInTextTokensOnly()
    {
        CharacterProfileStore.Current.CreateProfile("Swedish Remapper");
        CharacterProfileStore.Current.SetDisplayChar(0x82, 'Å');
        try
        {
            var entries = new List<MessageEntry>
            {
                new(0x0001, 0, 0, 0, 0)
                {
                    Text = "Å [item:82] }",
                },
            };

            Assert.Equal(1, MessageGlyphRemapper.CountOccurrences(entries, 0x82));
            Assert.Equal(1, MessageGlyphRemapper.CountOccurrences(entries, 0x7d));

            int replacements = MessageGlyphRemapper.Replace(entries, 0x82, 0x7d);
            Assert.Equal(1, replacements);
            Assert.Equal("} [item:82] }", entries[0].Text);
            Assert.Equal(0, MessageGlyphRemapper.CountOccurrences(entries, 0x82));
            Assert.Equal(2, MessageGlyphRemapper.CountOccurrences(entries, 0x7d));
        }
        finally
        {
            CharacterProfileStore.Current.ResetDisplayChar(0x82);
            CharacterProfileStore.Current.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void EditorSyntaxParsesSemanticTokens()
    {
        var syntaxTokens = MessageTextSyntax.FromEditorText("[item:30][sfx:Laugh2][sfx:1234][Triangle][color:red]âÂ[break]");
        Assert.Contains(new IconToken(0x30), syntaxTokens);
        Assert.Contains(new SfxToken(0x686d), syntaxTokens);
        Assert.Contains(new SfxToken(0x1234), syntaxTokens);
        Assert.Contains(new ButtonToken(MessageButton.Triangle), syntaxTokens);
        Assert.Contains(new ColorToken(MessageColor.Red), syntaxTokens);
        Assert.Equal(0xa9, syntaxTokens.OfType<ButtonToken>().First(token => token.Button == MessageButton.Triangle).Code);
        Assert.Equal(0x41, syntaxTokens.OfType<ColorToken>().First(token => token.Color == MessageColor.Red).Index);
        Assert.Contains(new CommandToken(MessageCommand.Break), syntaxTokens);
        Assert.Equal("[item:30][sfx:Laugh2][sfx:1234][Triangle][color:red]âÂ[break]", MessageTextSyntax.ToEditorText(syntaxTokens));
        Assert.Equal(
            [0x13, 0x30, 0x12, 0x68, 0x6d, 0x12, 0x12, 0x34, 0xa9, 0x05, 0x41, 0x92, 0x82, 0x04, 0x02, 0x00],
            MessageCodec.EncodeMessageTokens(syntaxTokens));
    }

    [Fact]
    public void EditorSyntaxAcceptsTagNamesCaseInsensitivelyAndNormalizesOutput()
    {
        string text = "[ITEM:30][SFX:laugh2][triangle][COLOR:RED]Text[BREAK][TWOCHOICE][ARCHERY]";

        Assert.True(MessageTextSyntax.TryNormalizeEditorText(text, out string normalized));
        Assert.Equal("[item:30][sfx:Laugh2][Triangle][color:red]Text[break][twochoice][archery]", normalized);
    }

    [Fact]
    public void UnknownEditorTagsStayAsLiteralText()
    {
        Assert.Equal("Plain [unknown] text", MessageTextSyntax.ToEditorText(MessageTextSyntax.FromEditorText("Plain [unknown] text")));
        Assert.Equal("Plain [unfinished", MessageTextSyntax.ToEditorText(MessageTextSyntax.FromEditorText("Plain [unfinished")));
    }

    [Fact]
    public void UnsupportedUnicodeCharactersFailInsteadOfTruncating()
    {
        var ex = Assert.Throws<InvalidDataException>(() => EncodeEditorText("Unsupported 😀"));
        Assert.Contains("Unsupported character", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("U+D83D", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnsupportedLatin1CharactersFailWithMessageIdContext()
    {
        var entries = new List<MessageEntry>
        {
            new(0x0001, 0, 0, 0, 0)
            {
                Text = "Otillåtet å",
            },
        };

        var ex = Assert.Throws<InvalidDataException>(() => MessageTableCodec.BuildFiles(entries, MessageEncodingProfile.Original));
        Assert.Contains("Message 0x0001", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Unsupported character 'å'", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FixturesRoundtripAndAssetsStaySelfContained()
    {
        var staffEntries = CreateStaffEntries();
        var staffSourceFiles = MessageTableCodec.BuildFiles(staffEntries);
        var fixtureStaffEntries = MessageTableCodec.ParseTable(staffSourceFiles.tableBytes, staffSourceFiles.msgBytes);
        Assert.Equal(48, fixtureStaffEntries.Count);
        Assert.Equal(0x0500, fixtureStaffEntries[0].Id);
        Assert.Equal(0x052f, fixtureStaffEntries[^1].Id);
        Assert.True(string.IsNullOrEmpty(fixtureStaffEntries.First(e => e.Id == 0x0526).Text));
        var fixtureStaffFiles = MessageTableCodec.BuildFiles(fixtureStaffEntries);
        Assert.Equal(staffSourceFiles.tableBytes, fixtureStaffFiles.tableBytes);
        Assert.Equal(staffSourceFiles.msgBytes, fixtureStaffFiles.msgBytes);

        var fixtureHeaderEntries = CHeaderImporter.Import(File.ReadAllText(FixturePath("valid_message_header.h")));
        Assert.Equal(2, fixtureHeaderEntries.Count);
        Assert.Equal("Press [Triangle]", fixtureHeaderEntries[1].Text);

        string assetRoot = Path.Combine(RepositoryRoot(), "src", "HylianGrimoire", "Assets", "Preview", "Oot");
        Assert.False(File.Exists(Path.Combine(assetRoot, "resource_map.json")));
        if (Directory.Exists(assetRoot))
        {
            Assert.Empty(Directory.EnumerateFiles(assetRoot, "*.png", SearchOption.TopDirectoryOnly));
        }

        Assert.Empty(Directory.EnumerateFiles(Path.Combine(assetRoot, "nes_font_static"), "gMsgCharAB*Tex.png"));
    }

    private static string NormalizeEditorText(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HylianGrimoire.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private static List<MessageEntry> CreateStaffEntries()
    {
        var entries = new List<MessageEntry>();
        for (int i = 0; i < 48; i++)
        {
            int id = 0x0500 + i;
            entries.Add(new MessageEntry(id, 0x0b, 0, 7, 0)
            {
                Text = id == 0x0526
                    ? string.Empty
                    : $"[quicktexton][shift:24]Staff role {i:x2}\n[shift:24]STAFF NAME {i:x2}[quicktextoff][endfade:0000]",
            });
        }

        return entries;
    }

    private static List<MessageEntry> CreateEntries()
    {
        return
        [
            new MessageEntry(0x6004, 0, 0, 7, 0)
            {
                Text = "[unskippable]Jag observerade hur du smet förbi\nvâra vakter. Du är bâde stark och\nsnabb.[break]",
            },
            new MessageEntry(0x6005, 2, 3, 7, 0)
            {
                Text = "[quicktexton][shift:22]Prata med [A-button][color:red]Zelda[color:default]!",
            },
        ];
    }

    private static void AssertInvalidSyntax(string text, string expectedMessagePart)
    {
        var ex = Assert.Throws<InvalidDataException>(() => MessageTextSyntax.FromEditorText(text));
        Assert.Contains(expectedMessagePart, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertInvalidEntryContext(Action action, string expectedMessagePart)
    {
        var ex = Assert.Throws<InvalidDataException>(action);
        Assert.Contains(expectedMessagePart, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string PreviewGlyphText(string text)
    {
        return new string(
            OotPreviewTextPage.FromMessageTokens(MessageTextSyntax.FromEditorText(text))
                .Where(token => token.Kind == OotPreviewTokenKind.Glyph && token.Value is >= 0x20 and <= 0x7e)
                .Select(token => (char)token.Value)
                .ToArray());
    }

    private static string DecodeEditorText(byte[] raw, MessageEncodingProfile? encodingProfile = null)
    {
        return MessageTextSyntax.ToEditorText(MessageCodec.DecodeMessageTokens(raw, 0, raw.Length, encodingProfile));
    }

    private static byte[] EncodeEditorText(string text, MessageEncodingProfile? encodingProfile = null)
    {
        return MessageCodec.EncodeMessageTokens(MessageTextSyntax.FromEditorText(text), encodingProfile);
    }

    private static string FixturePath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
    }
}
