namespace HylianGrimoire.Rom;

using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

public interface IMessageBankCodec
{
    bool CanParseTableFiles(byte[] tableBytes, byte[] messageBytes);

    List<MessageEntry> Parse(
        byte[] tableBytes,
        byte[] messageBytes,
        MessageBankProfile bank,
        IReadOnlyList<int>? pointerBounds,
        MessageEncodingProfile encodingProfile,
        bool decodeMessages = true);

    (byte[] TableBytes, byte[] MessageBytes) Build(
        List<MessageEntry> entries,
        MessageEncodingProfile encodingProfile);
}
