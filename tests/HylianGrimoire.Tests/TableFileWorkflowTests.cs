using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class TableFileWorkflowTests
{
    [Fact]
    public void SaveAndLoadRoundtripsOotTableFiles()
    {
        string testDir = CreateTempDirectory();
        string tablePath = Path.Combine(testDir, "messages.tbl");
        string binaryPath = Path.Combine(testDir, "messages.bin");
        List<MessageEntry> entries = CreateOotEntries();
        var workflow = new TableFileWorkflow();

        try
        {
            TableFileSaveResult saveResult = workflow.Save(
                tablePath,
                binaryPath,
                entries,
                excludeFontOrderEntry: false,
                GameProfiles.Get(GameKind.OcarinaOfTime));
            MessageFileDocument document = workflow.Load(tablePath, binaryPath);
            var expectedFiles = MessageTableCodec.BuildFiles(entries);

            Assert.Equal(entries.Count, saveResult.SavedEntries.Count);
            Assert.Equal(GameKind.OcarinaOfTime, document.GameProfile.Kind);
            Assert.Equal(entries.Select(entry => entry.Text), document.Entries.Select(entry => entry.Text));
            Assert.Equal(expectedFiles.tableBytes, File.ReadAllBytes(tablePath));
            Assert.Equal(expectedFiles.msgBytes, File.ReadAllBytes(binaryPath));
        }
        finally
        {
            DeleteDirectoryIfExists(testDir);
        }
    }

    [Fact]
    public void SaveReturnsEntriesActuallyWrittenWhenFontOrderEntryIsExcluded()
    {
        string testDir = CreateTempDirectory();
        string tablePath = Path.Combine(testDir, "messages.tbl");
        string binaryPath = Path.Combine(testDir, "messages.bin");
        List<MessageEntry> entries = CreateOotEntries();
        entries.Add(new MessageEntry(FontOrderCodec.MessageId, 0, 0, 7, 0)
        {
            Text = "ABC",
        });
        var workflow = new TableFileWorkflow();

        try
        {
            TableFileSaveResult saveResult = workflow.Save(
                tablePath,
                binaryPath,
                entries,
                excludeFontOrderEntry: true,
                GameProfiles.Get(GameKind.OcarinaOfTime));
            MessageFileDocument document = workflow.Load(tablePath, binaryPath);

            Assert.DoesNotContain(saveResult.SavedEntries, entry => entry.Id == FontOrderCodec.MessageId);
            Assert.DoesNotContain(document.Entries, entry => entry.Id == FontOrderCodec.MessageId);
        }
        finally
        {
            DeleteDirectoryIfExists(testDir);
        }
    }

    private static List<MessageEntry> CreateOotEntries()
    {
        return
        [
            new MessageEntry(0x6004, 0, 0, 7, 0)
            {
                Text = "[unskippable]Hello[break]",
            },
            new MessageEntry(0x6005, 2, 3, 7, 0)
            {
                Text = "[quicktexton][shift:22]Talk to [A-button][color:red]Zelda[color:default]!",
            },
        ];
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "HylianGrimoireTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
