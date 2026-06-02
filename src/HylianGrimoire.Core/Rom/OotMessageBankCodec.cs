namespace HylianGrimoire.Rom;

using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

public sealed class OotMessageBankCodec : IMessageBankCodec
{
    public static OotMessageBankCodec Instance { get; } = new();

    private OotMessageBankCodec()
    {
    }

    public bool CanParseTableFiles(byte[] tableBytes, byte[] messageBytes) =>
        MessageTableCodec.LooksLikeTableFiles(tableBytes, messageBytes)
            && !StaffCreditsMessageTableDetector.LooksLikeTableFiles(tableBytes, messageBytes);

    public List<MessageEntry> Parse(
        byte[] tableBytes,
        byte[] messageBytes,
        MessageBankProfile bank,
        IReadOnlyList<int>? pointerBounds,
        MessageEncodingProfile encodingProfile,
        bool decodeMessages = true) =>
        MessageTableCodec.ParseTable(
            tableBytes,
            messageBytes,
            bank.OffsetMode == MessageBankOffsetMode.Sequential,
            bank.ExcludesFontMessage,
            pointerBounds,
            encodingProfile,
            bank.TableSegment,
            decodeMessages);

    public (byte[] TableBytes, byte[] MessageBytes) Build(
        List<MessageEntry> entries,
        MessageEncodingProfile encodingProfile)
    {
        var (tableBytes, messageBytes) = MessageTableCodec.BuildFiles(entries, encodingProfile);
        return (tableBytes, messageBytes);
    }
}
