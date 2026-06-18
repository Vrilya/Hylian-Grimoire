using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public sealed class EmptyMessageTypeCatalog : IMessageTypeCatalog
{
    public static EmptyMessageTypeCatalog Instance { get; } = new();

    private EmptyMessageTypeCatalog()
    {
    }

    public IReadOnlyList<MessageTypeItem> Items { get; } = [];
}
