using HylianGrimoire.Models;
using HylianGrimoire.Preview;

namespace HylianGrimoire.Services;

public sealed class OotMessageTypeCatalog : IMessageTypeCatalog
{
    public static OotMessageTypeCatalog Instance { get; } = new();

    private OotMessageTypeCatalog()
    {
    }

    public IReadOnlyList<MessageTypeItem> Items { get; } =
    [
        new(0, "Black"),
        new(1, "Wood"),
        new(2, "Blue"),
        new(3, "Ocarina"),
        new(4, "None"),
        new(5, "None (black text)"),
        new(11, "Credits"),
    ];

    public static OotPreviewStyle ToPreviewStyle(int type) => type switch
    {
        1 => OotPreviewStyle.Wooden,
        2 => OotPreviewStyle.Blue,
        3 => OotPreviewStyle.Ocarina,
        4 => OotPreviewStyle.None,
        5 => OotPreviewStyle.NoneDarkText,
        11 => OotPreviewStyle.Credits,
        _ => OotPreviewStyle.Black,
    };
}
