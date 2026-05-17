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
        Environment.SetEnvironmentVariable("OOT_EDITOR_DISABLE_GLYPH_OVERRIDES", null);
        Environment.SetEnvironmentVariable(
            "OOT_EDITOR_GLYPH_OVERRIDE_CONFIG_DIR",
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
    public void EncodingProfileAndGlyphOverridesPreserveUnderlyingBytes()
    {
        var encodingProfile = MessageEncodingProfile.Default;
        Assert.True(encodingProfile.TryGetByte('â', out byte lowerRing));
        Assert.Equal(0x92, lowerRing);
        Assert.True(encodingProfile.TryGetByte('Â', out byte upperRing));
        Assert.Equal(0x82, upperRing);
        Assert.Equal("âÂ", encodingProfile.ToHeaderText("âÂ"));
        Assert.Equal("âÂ", encodingProfile.HeaderTextToEditorText("âÂ"));

        GlyphOverrideStore.Current.SetDisplayChar(0x92, 'å');
        Assert.Equal("å", DecodeEditorText([0x92, 0x02, 0x00, 0x00]));
        Assert.Equal([0x92, 0x02, 0x00, 0x00], EncodeEditorText("å"));
        GlyphOverrideStore.Current.ResetDisplayChar(0x92);
        Assert.Equal("â", DecodeEditorText([0x92, 0x02, 0x00, 0x00]));
    }

    [Fact]
    public void GlyphOverrideStoreExposesLoadWarnings()
    {
        Assert.Null(GlyphOverrideStore.Current.LoadWarning);
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
                Text = "[unskippable]Jag observerade hur du smet fÃ¶rbi\nvÃ¢ra vakter. Du Ã¤r bÃ¢de stark och\nsnabb.[break]",
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

    private static string PreviewGlyphText(string text)
    {
        return new string(
            OotPreviewTextPage.FromMessageTokens(MessageTextSyntax.FromEditorText(text))
                .Where(token => token.Kind == OotPreviewTokenKind.Glyph && token.Value is >= 0x20 and <= 0x7e)
                .Select(token => (char)token.Value)
                .ToArray());
    }

    private static string DecodeEditorText(byte[] raw)
    {
        return MessageTextSyntax.ToEditorText(MessageCodec.DecodeMessageTokens(raw, 0, raw.Length));
    }

    private static byte[] EncodeEditorText(string text)
    {
        return MessageCodec.EncodeMessageTokens(MessageTextSyntax.FromEditorText(text));
    }

    private static string FixturePath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
    }
}
