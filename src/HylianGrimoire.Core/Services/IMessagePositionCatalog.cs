using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public interface IMessagePositionCatalog
{
    IReadOnlyList<MessagePositionItem> Items { get; }
}
