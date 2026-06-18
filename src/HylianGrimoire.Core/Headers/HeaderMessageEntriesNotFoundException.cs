namespace HylianGrimoire.Headers;

public sealed class HeaderMessageEntriesNotFoundException : Exception
{
    public HeaderMessageEntriesNotFoundException(string message)
        : base(message)
    {
    }
}
