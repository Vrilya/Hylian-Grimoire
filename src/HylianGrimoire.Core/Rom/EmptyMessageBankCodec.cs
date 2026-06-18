namespace HylianGrimoire.Rom;

using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

public sealed class EmptyMessageBankCodec : IMessageBankCodec
{
    public static EmptyMessageBankCodec Instance { get; } = new();

    private EmptyMessageBankCodec()
    {
    }

    public bool CanParseTableFiles(byte[] tableBytes, byte[] messageBytes) => false;

    public List<MessageEntry> Parse(
        byte[] tableBytes,
        byte[] messageBytes,
        MessageBankProfile bank,
        IReadOnlyList<int>? pointerBounds,
        MessageEncodingProfile encodingProfile,
        bool decodeMessages = true) =>
        throw new NotSupportedException("This game profile does not have an editable message bank codec yet.");

    public (byte[] TableBytes, byte[] MessageBytes) Build(
        List<MessageEntry> entries,
        MessageEncodingProfile encodingProfile) =>
        throw new NotSupportedException("This game profile does not have an editable message bank codec yet.");
}
