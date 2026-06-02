using HylianGrimoire.Models;
using HylianGrimoire.Preview;

namespace HylianGrimoire.Services;

public sealed class MmMessageTypeCatalog : IMessageTypeCatalog
{
    public static MmMessageTypeCatalog Instance { get; } = new();

    private MmMessageTypeCatalog()
    {
    }

    public IReadOnlyList<MessageTypeItem> Items { get; } =
    [
        new(0x0, "Black"),
        new(0x1, "Wooden"),
        new(0x2, "Blue faded"),
        new(0x3, "Ocarina"),
        new(0x4, "Type 4"),
        new(0x5, "Clear"),
        new(0x6, "Display all"),
        new(0x7, "Clear display all"),
        new(0x8, "Blue"),
        new(0x9, "Pause info"),
        new(0xA, "Type A"),
        new(0xB, "Type B"),
        new(0xC, "Title card"),
        new(0xD, "Notebook notification"),
        new(0xE, "Ocarina free play"),
        new(0xF, "Type F"),
    ];

    public static MmPreviewStyle ToPreviewStyle(int type) => type switch
    {
        0x1 => MmPreviewStyle.Wooden,
        0x2 => MmPreviewStyle.Blue,
        0x3 => MmPreviewStyle.Ocarina,
        0x4 or 0x7 => MmPreviewStyle.Clear,
        0x5 => MmPreviewStyle.ClearBlackText,
        0xB => MmPreviewStyle.TypeB,
        0x8 => MmPreviewStyle.BlueDefault,
        0xC => MmPreviewStyle.TitleCard,
        0xD => MmPreviewStyle.Notebook,
        0xE => MmPreviewStyle.OcarinaFreePlay,
        _ => MmPreviewStyle.Black,
    };
}
