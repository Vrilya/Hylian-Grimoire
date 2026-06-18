using HylianGrimoire.Codecs.MajorasMask;
using HylianGrimoire.Games;
using HylianGrimoire.Headers.MajorasMask;
using HylianGrimoire.Models;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class MajorasMaskRoundtripTests
{
    private const int TableEntrySize = 8;
    private const int MessageHeaderSize = 11;

    [Fact]
    public void RealFixtureMetadataMatchesRawMessageHeaders()
    {
        byte[] tableBytes = File.ReadAllBytes(FixturePath("MajorasMask", "real_mm_sample.tbl"));
        byte[] messageBytes = File.ReadAllBytes(FixturePath("MajorasMask", "real_mm_sample.bin"));

        List<MessageEntry> entries = MmMessageTableCodec.ParseTable(tableBytes, messageBytes);

        Assert.Equal(64, entries.Count);
        Assert.Equal(0xffff, ReadUInt16BigEndian(tableBytes, entries.Count * TableEntrySize));

        for (int i = 0; i < entries.Count; i++)
        {
            MessageEntry entry = entries[i];
            var metadata = Assert.IsType<MajorasMaskMessageMetadata>(entry.CodecMetadata);
            Assert.Equal(metadata, entry.OriginalCodecMetadata);

            int tableOffset = i * TableEntrySize;
            Assert.Equal(entry.Id, ReadUInt16BigEndian(tableBytes, tableOffset));
            Assert.Equal(metadata.TableTypePosition, tableBytes[tableOffset + 2]);
            uint pointer = ReadUInt32BigEndian(tableBytes, tableOffset + 4);
            Assert.Equal(0x08u, pointer >> 24);
            Assert.Equal(entry.Offset, (int)(pointer & 0x00ff_ffff));

            Assert.NotNull(entry.OriginalEncodedBytes);
            Assert.True(entry.OriginalEncodedBytes.Length >= MessageHeaderSize);
            Assert.Equal(
                entry.OriginalEncodedBytes.Take(MessageHeaderSize).ToArray(),
                metadata.BuildHeader(entry.Type, entry.Position));
            Assert.Equal(metadata.Type, entry.Type);
            Assert.Equal(metadata.Position, entry.Position);
        }
    }

    [Fact]
    public void EditedRealFixtureReloadsSemanticallyAndResavesByteForByte()
    {
        string sourceTblPath = FixturePath("MajorasMask", "real_mm_sample.tbl");
        string sourceBinPath = FixturePath("MajorasMask", "real_mm_sample.bin");
        string testDir = Path.Combine(Path.GetTempPath(), "HylianGrimoireTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        string outputTblPath = Path.Combine(testDir, "real_mm_sample_edited.tbl");
        string outputBinPath = Path.Combine(testDir, "real_mm_sample_edited.bin");
        string resavedTblPath = Path.Combine(testDir, "real_mm_sample_edited_resaved.tbl");
        string resavedBinPath = Path.Combine(testDir, "real_mm_sample_edited_resaved.bin");

        MessageFileDocument document = MessageFileService.LoadTableFiles(sourceTblPath, sourceBinPath);
        Dictionary<int, EntrySnapshot> originalEntries = document.Entries.ToDictionary(
            entry => entry.Id,
            entry => new EntrySnapshot(entry.Text, entry.Type, entry.Position, entry.CodecMetadata));
        MessageEntry editedEntry = document.Entries.Single(entry => entry.Id == 0x0004);
        string editedText = "[quicktexton]Du fick en [color:red]Rod Rupi[color:default]![quicktextoff]\n"
            + "[delay:000a]Den ar vard [color:red]20 Rupier[color:default]!\n"
            + "Det ar ganska nice!";

        editedEntry.Text = editedText;
        MessageFileService.SaveTableFiles(document.Entries, outputTblPath, outputBinPath, document.GameProfile);

        MessageFileDocument reloaded = MessageFileService.LoadTableFiles(outputTblPath, outputBinPath);

        Assert.Equal(GameKind.MajorasMask, reloaded.GameProfile.Kind);
        Assert.Equal(document.Entries.Count, reloaded.Entries.Count);
        foreach (MessageEntry entry in reloaded.Entries)
        {
            EntrySnapshot original = originalEntries[entry.Id];
            if (entry.Id == 0x0004)
            {
                Assert.Equal(editedText, entry.Text);
                Assert.Equal(original.Type, entry.Type);
                Assert.Equal(original.Position, entry.Position);
                Assert.Equal(original.Metadata, entry.CodecMetadata);
                continue;
            }

            Assert.Equal(original.Text, entry.Text);
            Assert.Equal(original.Type, entry.Type);
            Assert.Equal(original.Position, entry.Position);
            Assert.Equal(original.Metadata, entry.CodecMetadata);
        }

        MessageFileService.SaveTableFiles(reloaded.Entries, resavedTblPath, resavedBinPath, reloaded.GameProfile);

        Assert.Equal(File.ReadAllBytes(outputTblPath), File.ReadAllBytes(resavedTblPath));
        Assert.Equal(File.ReadAllBytes(outputBinPath), File.ReadAllBytes(resavedBinPath));
    }

    [Fact]
    public void OriginalLayoutReusesSharedMessageDataPointersWhenBytesMatch()
    {
        MajorasMaskMessageMetadata metadata = CreateMajorasMaskMetadata();
        byte[] encodedBytes = CreateEncodedMajorasMaskMessage(metadata, "Shared");
        var entries = new List<MessageEntry>
        {
            CreateOriginalMajorasMaskEntry(0x1000, 0, encodedBytes, metadata, "Shared"),
            CreateOriginalMajorasMaskEntry(0x1001, 0, encodedBytes, metadata, "Shared"),
        };

        (byte[] tableBytes, byte[] messageBytes) = MmMessageTableCodec.BuildFiles(entries);

        Assert.Equal(encodedBytes, messageBytes);
        Assert.Equal(0x1000, ReadUInt16BigEndian(tableBytes, 0));
        Assert.Equal(0, ReadPointerOffset(tableBytes, 0));
        Assert.Equal(0x1001, ReadUInt16BigEndian(tableBytes, TableEntrySize));
        Assert.Equal(0, ReadPointerOffset(tableBytes, TableEntrySize));
        Assert.Equal(0xffff, ReadUInt16BigEndian(tableBytes, TableEntrySize * 2));
    }

    [Fact]
    public void OriginalLayoutRejectsConflictingOverlappingOriginalBytes()
    {
        MajorasMaskMessageMetadata metadata = CreateMajorasMaskMetadata();
        var entries = new List<MessageEntry>
        {
            CreateOriginalMajorasMaskEntry(0x1000, 0, [0x01, 0x02, 0x03, 0x04], metadata),
            CreateOriginalMajorasMaskEntry(0x1001, 2, [0x03, 0x09], metadata),
        };

        InvalidDataException exception = Assert.Throws<InvalidDataException>(
            () => MmMessageTableCodec.BuildFiles(entries));

        Assert.Contains("overlaps", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0x1001", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OriginalLayoutKeepsFinalEndMarkerPointingAtDebuggerMessage()
    {
        MajorasMaskMessageMetadata metadata = CreateMajorasMaskMetadata();
        byte[] normalBytes = CreateEncodedMajorasMaskMessage(metadata, "Main");
        byte[] debuggerBytes = CreateEncodedMajorasMaskMessage(metadata, "end!");
        var entries = new List<MessageEntry>
        {
            CreateOriginalMajorasMaskEntry(
                0x1000,
                0,
                normalBytes,
                metadata,
                "Main",
                finalEndMarkerBank: 0x08,
                finalEndMarkerOffset: 0x44),
            CreateOriginalMajorasMaskEntry(
                MmMessageTableCodec.DebuggerEndMessageId,
                0x20,
                debuggerBytes,
                metadata,
                "end!"),
        };

        (byte[] tableBytes, byte[] messageBytes) = MmMessageTableCodec.BuildFiles(entries);

        int finalMarkerOffset = TableEntrySize * 2;
        Assert.Equal(0xffff, ReadUInt16BigEndian(tableBytes, finalMarkerOffset));
        Assert.Equal(0x08, ReadPointerBank(tableBytes, finalMarkerOffset));
        Assert.Equal(0x20, ReadPointerOffset(tableBytes, finalMarkerOffset));
        Assert.Equal(normalBytes, messageBytes.AsSpan(0, normalBytes.Length).ToArray());
        Assert.Equal(debuggerBytes, messageBytes.AsSpan(0x20, debuggerBytes.Length).ToArray());
    }

    [Fact]
    public void HeaderImporterParsesDecompStyleMetadata()
    {
        const string header = """
DEFINE_MESSAGE(0x0011, 0x02, 0x03,
MSG(
HEADER(0x0201, 0x11, 0x1234, 0x0005, 0xFFFF, 0xABCD)
QUICKTEXT_ENABLE "You found a " COLOR_RED "Stray Fairy" COLOR_DEFAULT "!" QUICKTEXT_DISABLE "\n"
DELAY(10) "This is your " STRAY_FAIRIES COLOR_RED " one!" FADE(40)
)
)
""";

        List<MessageEntry> entries = MmCHeaderImporter.Import(header);

        MessageEntry entry = Assert.Single(entries);
        var metadata = Assert.IsType<MajorasMaskMessageMetadata>(entry.CodecMetadata);
        Assert.Equal(0x0011, entry.Id);
        Assert.Equal(0x23, metadata.TableTypePosition);
        Assert.Equal(0x0201, metadata.TextBoxProperties);
        Assert.Equal(0x11, metadata.IconId);
        Assert.Equal(0x1234, metadata.NextTextId);
        Assert.Equal(0x0005, metadata.FirstChoicePrice);
        Assert.Equal(0xffff, metadata.SecondChoicePrice);
        Assert.Equal(0xabcd, metadata.Unknown);
        Assert.Equal(2, entry.Type);
        Assert.Equal(0, entry.Position);
        Assert.Equal(
            "[quicktexton]You found a [color:red]Stray Fairy[color:default]![quicktextoff]\n"
            + "[delay:000a]This is your [strayfairies][color:red] one![fade:0028]",
            entry.Text);
    }

    [Fact]
    public void HeaderImporterIgnoresDefineMessageInsideCommentsAndStrings()
    {
        const string header = """
// DEFINE_MESSAGE(0x0001, 0x00, 0x00,
// MSG(HEADER(0x0000, 0xFE, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF) "line comment"))
/*
DEFINE_MESSAGE(0x0002, 0x00, 0x00,
MSG(HEADER(0x0000, 0xFE, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF) "block comment"))
*/
static const char* ignored = "DEFINE_MESSAGE(0x0003, 0x00, 0x00, MSG(HEADER(0x0000, 0xFE, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF) \"string literal\"))";
DEFINE_MESSAGE(0x0011, 0x02, 0x03,
MSG(
HEADER(0x0201, 0x11, 0x1234, 0x0005, 0xFFFF, 0xABCD)
"real message"
)
)
""";

        List<MessageEntry> entries = MmCHeaderImporter.Import(header);

        MessageEntry entry = Assert.Single(entries);
        Assert.Equal(0x0011, entry.Id);
        Assert.Equal("real message", entry.Text);
    }

    [Fact]
    public void HeaderExporterRoundtripsTextAndMetadata()
    {
        const string header = """
DEFINE_MESSAGE(0x04B7, 0x00, 0x00,
MSG(
HEADER(0x0301, 0xFE, 0xFFFF, 0x0001, 0xFFFF, 0xFFFF)
THREE_CHOICE COLOR_GREEN "One\n"
"Two\n"
"She doesn't use balloons"
)
)
""";

        List<MessageEntry> imported = MmCHeaderImporter.Import(header);
        string exported = MmCHeaderExporter.Export(imported);
        List<MessageEntry> reparsed = MmCHeaderImporter.Import(exported);

        MessageEntry original = Assert.Single(imported);
        MessageEntry roundtripped = Assert.Single(reparsed);
        Assert.Equal(original.Id, roundtripped.Id);
        Assert.Equal(original.Text, roundtripped.Text);
        Assert.Equal(original.Type, roundtripped.Type);
        Assert.Equal(original.Position, roundtripped.Position);
        Assert.Equal(original.CodecMetadata, roundtripped.CodecMetadata);
    }

    [Fact]
    public void HeaderExporterKeepsReadableEuropeanTextCharacters()
    {
        var metadata = new MajorasMaskMessageMetadata(
            TableTypePosition: 0,
            TextBoxProperties: 0,
            IconId: 0xfe,
            NextTextId: 0xffff,
            FirstChoicePrice: 0xffff,
            SecondChoicePrice: 0xffff,
            Unknown: 0xffff);
        var entries = new List<MessageEntry>
        {
            new(0x3543, 0, 0, 0, 0)
            {
                Text = "[sfx:2913]Auuuuuu.\n"
                    + "[quicktexton]Estoy cansado. Mañana me tomaré\n"
                    + "el día libre. ¿No pueden hacer eso\n"
                    + "los perros?[quicktextoff]",
                CodecMetadata = metadata,
            },
        };

        string exported = MmCHeaderExporter.Export(entries);

        Assert.Contains("Mañana me tomaré", exported, StringComparison.Ordinal);
        Assert.Contains("el día libre. ¿No pueden", exported, StringComparison.Ordinal);
        Assert.DoesNotContain("\\xF1", exported, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\xE9", exported, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\xED", exported, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\xBF", exported, StringComparison.OrdinalIgnoreCase);

        MessageEntry reparsed = Assert.Single(MmCHeaderImporter.Import(exported));
        Assert.Equal(entries[0].Text, reparsed.Text);
    }

    [Fact]
    public void HeaderExporterOmitsBuildGeneratedHelperMessages()
    {
        var metadata = new MajorasMaskMessageMetadata(
            TableTypePosition: 0,
            TextBoxProperties: 0,
            IconId: 0xfe,
            NextTextId: 0xffff,
            FirstChoicePrice: 0xffff,
            SecondChoicePrice: 0xffff,
            Unknown: 0xffff);
        var entries = new List<MessageEntry>
        {
            new(0x0001, 0, 0, 8, 0)
            {
                Text = "Visible",
                CodecMetadata = metadata,
            },
            new(0xfffc, 0, 0, 8, 0)
            {
                CodecMetadata = metadata,
            },
            new(MmMessageTableCodec.DebuggerEndMessageId, 0, 0, 8, 0)
            {
                Text = "end!",
                CodecMetadata = metadata,
            },
        };

        string exported = MmCHeaderExporter.Export(entries);

        Assert.Contains("DEFINE_MESSAGE(0x0001", exported, StringComparison.Ordinal);
        Assert.DoesNotContain("0xFFFC", exported, StringComparison.Ordinal);
        Assert.DoesNotContain("0xFFFD", exported, StringComparison.Ordinal);
        Assert.DoesNotContain("end!", exported, StringComparison.Ordinal);
    }

    [Fact]
    public void StaffHeaderImporterParsesDecompStyleCredits()
    {
        const string header = """
DEFINE_MESSAGE(0x4E20, 0x0B, 0x00,
MSG(
QUICKTEXT_ENABLE "\n"
SHIFT(60) "Producer / Supervisor\n"
SHIFT(96) "SHIGERU MIYAMOTO\n"
QUICKTEXT_DISABLE FADE2(80)
)
)

DEFINE_MESSAGE(0x4E4C, 0x0B, 0x00,
MSG(
SHIFT(60) "The End\n"
QUICKTEXT_DISABLE PERSISTENT
)
)
""";

        List<MessageEntry> entries = MmCHeaderImporter.Import(header);

        Assert.Equal(2, entries.Count);
        MessageEntry first = entries[0];
        Assert.Null(first.CodecMetadata);
        Assert.Equal(0x4e20, first.Id);
        Assert.Equal(0x0b, first.Type);
        Assert.Equal(0, first.Position);
        Assert.Equal(0x07, first.Bank);
        Assert.Equal("[quicktexton]\n[shift:3c]Producer / Supervisor\n[shift:60]SHIGERU MIYAMOTO\n[quicktextoff][endfade:0050]", first.Text);

        MessageEntry last = entries[1];
        Assert.Null(last.CodecMetadata);
        Assert.Equal("[shift:3c]The End\n[quicktextoff][persistent]", last.Text);
    }

    [Fact]
    public void StaffHeaderExporterUsesDecompStyleCredits()
    {
        var entries = new List<MessageEntry>
        {
            new(0x4e20, 0x0b, 0, 0x07, 0)
            {
                Text = "[quicktexton]\n[shift:3c]Producer / Supervisor\n[shift:60]SHIGERU MIYAMOTO\n[quicktextoff][endfade:0050]",
                OriginalText = "[quicktexton]\n[shift:3c]Producer / Supervisor\n[shift:60]SHIGERU MIYAMOTO\n[quicktextoff][endfade:0050]",
                TableEndMarkerId = 0xffff,
                TableHasFinalEndMarker = true,
            },
        };

        string exported = MmCHeaderExporter.Export(entries);

        string expected = """
DEFINE_MESSAGE(0x4E20, 0x0B, 0x00,
MSG(
QUICKTEXT_ENABLE "\n"
SHIFT(60) "Producer / Supervisor\n"
SHIFT(96) "SHIGERU MIYAMOTO\n"
QUICKTEXT_DISABLE FADE2(80)
)
)
""".ReplaceLineEndings("\n");
        Assert.Equal(expected.TrimEnd('\n'), exported.TrimEnd('\n'));
    }

    [Fact]
    public void StaffDataFilesExportToDecompHeader()
    {
        MessageFileDocument document = MessageFileService.LoadTableFiles(
            FixturePath("MajorasMask", "mm_staff_credits.tbl"),
            FixturePath("MajorasMask", "mm_staff_credits.bin"));

        string actual = MmCHeaderExporter.Export(document.Entries).ReplaceLineEndings("\n");
        string expected = File.ReadAllText(FixturePath("MajorasMask", "message_data_staff_decomp.h")).ReplaceLineEndings("\n");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StaffDecompHeaderSavesBackToOriginalDataFiles()
    {
        string testDir = Path.Combine(Path.GetTempPath(), "HylianGrimoireTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
        string outputTblPath = Path.Combine(testDir, "mm_staff_credits.tbl");
        string outputBinPath = Path.Combine(testDir, "mm_staff_credits.bin");

        List<MessageEntry> entries = MessageFileService.ImportHeader(FixturePath("MajorasMask", "message_data_staff_decomp.h"));
        MessageFileService.SaveTableFiles(entries, outputTblPath, outputBinPath, GameProfiles.Get(GameKind.MajorasMask));

        Assert.Equal(File.ReadAllBytes(FixturePath("MajorasMask", "mm_staff_credits.tbl")), File.ReadAllBytes(outputTblPath));
        Assert.Equal(File.ReadAllBytes(FixturePath("MajorasMask", "mm_staff_credits.bin")), File.ReadAllBytes(outputBinPath));
    }

    private static MajorasMaskMessageMetadata CreateMajorasMaskMetadata()
    {
        return new MajorasMaskMessageMetadata(
            TableTypePosition: 0,
            TextBoxProperties: 0,
            IconId: 0xfe,
            NextTextId: 0xffff,
            FirstChoicePrice: 0xffff,
            SecondChoicePrice: 0xffff,
            Unknown: 0xffff);
    }

    private static MessageEntry CreateOriginalMajorasMaskEntry(
        int id,
        int offset,
        byte[] encodedBytes,
        MajorasMaskMessageMetadata metadata,
        string text = "",
        int? finalEndMarkerBank = null,
        int? finalEndMarkerOffset = null)
    {
        return new MessageEntry(id, metadata.Type, metadata.Position, 0x08, offset)
        {
            Text = text,
            OriginalText = text,
            OriginalEncodedBytes = encodedBytes,
            CodecMetadata = metadata,
            OriginalCodecMetadata = metadata,
            TableEndMarkerId = 0xffff,
            TableHasFinalEndMarker = true,
            OriginalFinalTableEndMarkerBank = finalEndMarkerBank,
            OriginalFinalTableEndMarkerOffset = finalEndMarkerOffset,
        };
    }

    private static byte[] CreateEncodedMajorasMaskMessage(MajorasMaskMessageMetadata metadata, string text)
    {
        var bytes = new List<byte>(metadata.BuildHeader(type: 0, position: 0));
        bytes.AddRange(text.Select(character => (byte)character));
        bytes.Add(0xbf);
        while ((bytes.Count & 3) != 0)
        {
            bytes.Add(0x00);
        }

        return bytes.ToArray();
    }

    private static int ReadPointerBank(byte[] tableBytes, int tableOffset)
        => tableBytes[tableOffset + 4];

    private static int ReadPointerOffset(byte[] tableBytes, int tableOffset)
        => (int)(ReadUInt32BigEndian(tableBytes, tableOffset + 4) & 0x00ff_ffff);

    private static string FixturePath(params string[] pathParts)
    {
        string[] parts = new string[pathParts.Length + 1];
        parts[0] = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        Array.Copy(pathParts, 0, parts, 1, pathParts.Length);
        return Path.Combine(parts);
    }

    private static ushort ReadUInt16BigEndian(byte[] data, int offset)
    {
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static uint ReadUInt32BigEndian(byte[] data, int offset)
    {
        return ((uint)data[offset] << 24)
            | ((uint)data[offset + 1] << 16)
            | ((uint)data[offset + 2] << 8)
            | data[offset + 3];
    }

    private sealed record EntrySnapshot(string Text, int Type, int Position, object? Metadata);
}
