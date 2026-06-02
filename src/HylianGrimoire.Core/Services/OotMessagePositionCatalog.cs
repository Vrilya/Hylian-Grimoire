using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public sealed class OotMessagePositionCatalog : IMessagePositionCatalog
{
    public static OotMessagePositionCatalog Instance { get; } = new();

    private OotMessagePositionCatalog()
    {
    }

    public IReadOnlyList<MessagePositionItem> Items { get; } =
    [
        new(0, "Auto"),
        new(1, "Top"),
        new(2, "Middle"),
        new(3, "Bottom"),
    ];
}
