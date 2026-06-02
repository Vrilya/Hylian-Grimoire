using HylianGrimoire.Models;

namespace HylianGrimoire.Services;

public interface IMessageTypeCatalog
{
    IReadOnlyList<MessageTypeItem> Items { get; }
}
