using HylianGrimoire.Headers;
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
}
