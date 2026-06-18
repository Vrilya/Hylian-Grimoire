using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public sealed class MmMessagePositionCatalog : IMessagePositionCatalog
{
    public static MmMessagePositionCatalog Instance { get; } = new();

    private MmMessagePositionCatalog()
    {
    }

    public IReadOnlyList<MessagePositionItem> Items { get; } =
    [
        new(0, "Auto"),
        new(1, "Top"),
        new(2, "Middle"),
        new(3, "Bottom"),
        new(7, "Fixed"),
    ];
}
