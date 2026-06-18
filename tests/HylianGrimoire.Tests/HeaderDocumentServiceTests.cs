using HylianGrimoire.Games;
using HylianGrimoire.Headers;
using HylianGrimoire.Models;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class HeaderDocumentServiceTests
{
    [Fact]
    public void CHeaderImporterUsesTypedExceptionWhenNoEntriesAreFound()
    {
        Assert.Throws<HeaderMessageEntriesNotFoundException>(
            () => CHeaderImporter.Import(string.Empty));
    }

    [Fact]
    public void GetAvailableWesternSlotsIgnoresSlotsWithoutMessages()
    {
        string path = Path.Combine(Path.GetTempPath(), $"hylian-grimoire-header-slots-{Guid.NewGuid():N}.h");
        File.WriteAllText(path, """
            DEFINE_MESSAGE(0x0001, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_VARIABLE,
            MSG(/* MISSING */)
            ,
            MSG(/* MISSING */)
            ,
            MSG("GER")
            ,
            MSG(/* MISSING */)
            )
            """);

        try
        {
            List<CHeaderMessageSlot> slots = HeaderDocumentService.GetAvailableWesternSlots(path);

            Assert.Equal([CHeaderMessageSlot.Ger], slots);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void LoadDocumentDoesNotDetectMajorasMaskFromHeaderTextInsideOcarinaString()
    {
        string path = Path.Combine(Path.GetTempPath(), $"hylian-grimoire-header-detection-{Guid.NewGuid():N}.h");
        File.WriteAllText(path, """
            DEFINE_MESSAGE(0x0001, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_VARIABLE,
            MSG("This text mentions HEADER( but is still Ocarina.")
            )
            """);

        try
        {
            HeaderFileDocument document = HeaderDocumentService.LoadDocument(path);

            Assert.Equal(GameKind.OcarinaOfTime, document.GameProfile.Kind);
            MessageEntry entry = Assert.Single(document.Languages[0]);
            Assert.Equal("This text mentions HEADER( but is still Ocarina.", entry.Text);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void BuildSelectedRomImportDoesNotFallBackToAnotherWesternSlot()
    {
        string path = Path.Combine(Path.GetTempPath(), $"hylian-grimoire-selected-slot-{Guid.NewGuid():N}.h");
        File.WriteAllText(path, """
            DEFINE_MESSAGE(0x0001, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_VARIABLE,
            MSG(/* MISSING */)
            ,
            MSG(/* MISSING */)
            ,
            MSG("GER")
            ,
            MSG("FRA")
            )
            """);
        List<MessageEntry> currentEntries =
        [
            new(0x0001, 0, 0, 0, 0) { Text = "Current NES" },
        ];

        try
        {
            IReadOnlyDictionary<int, List<MessageEntry>> imports =
                HeaderDocumentService.BuildSelectedRomImport(
                    path,
                    CHeaderMessageSlot.Nes,
                    activeBankIndex: 0,
                    currentEntries);

            Assert.Empty(imports);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void BuildSelectedRomImportUsesRequestedWesternSlot()
    {
        string path = Path.Combine(Path.GetTempPath(), $"hylian-grimoire-selected-slot-{Guid.NewGuid():N}.h");
        File.WriteAllText(path, """
            DEFINE_MESSAGE(0x0001, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_VARIABLE,
            MSG(/* MISSING */)
            ,
            MSG("NES")
            ,
            MSG("GER")
            ,
            MSG("FRA")
            )
            """);
        List<MessageEntry> currentEntries =
        [
            new(0x0001, 0, 0, 0, 0) { Text = "Current GER" },
        ];

        try
        {
            IReadOnlyDictionary<int, List<MessageEntry>> imports =
                HeaderDocumentService.BuildSelectedRomImport(
                    path,
                    CHeaderMessageSlot.Ger,
                    activeBankIndex: 1,
                    currentEntries);

            KeyValuePair<int, List<MessageEntry>> import = Assert.Single(imports);
            Assert.Equal(1, import.Key);
            MessageEntry entry = Assert.Single(import.Value);
            Assert.Equal("GER", entry.Text);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
