namespace HylianGrimoire.Rom.MajorasMask;

using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;
using HylianGrimoire.Models;

public sealed class MmMessageBankCodec : IMessageBankCodec
{
    private const int StaffTableSegment = 0x07;
    private const string StaffPersistentTag = "[persistent]";
    private const string StaffPersistentEncodingTag = "[shop]";

    public static MmMessageBankCodec Instance { get; } = new();

    private MmMessageBankCodec()
    {
    }

    public bool CanParseTableFiles(byte[] tableBytes, byte[] messageBytes)
    {
        // Staff credits use the older 8-byte table format and raw staff text, not MM's 11-byte message header.
        return MmMessageTableCodec.LooksLikeMajorasMaskTable(tableBytes, messageBytes)
            || StaffCreditsMessageTableDetector.LooksLikeTableFiles(tableBytes, messageBytes);
    }

    public List<MessageEntry> Parse(
        byte[] tableBytes,
        byte[] messageBytes,
        MessageBankProfile bank,
        IReadOnlyList<int>? pointerBounds,
        MessageEncodingProfile encodingProfile,
        bool decodeMessages = true) =>
        MmMessageTableCodec.LooksLikeMajorasMaskTable(tableBytes, messageBytes)
            ? MmMessageTableCodec.ParseTable(
                tableBytes,
                messageBytes,
                encodingProfile,
                decodeMessages,
                GetExpectedTableSegment(bank))
            : ParseStaffCreditsTable(
                tableBytes,
                messageBytes,
                encodingProfile: encodingProfile,
                decodeMessages: decodeMessages);

    private static int? GetExpectedTableSegment(MessageBankProfile bank) =>
        bank.MessageTableOffset == 0 && bank.MessageDataOffset == 0
            ? null
            : bank.TableSegment;

    public (byte[] TableBytes, byte[] MessageBytes) Build(
        List<MessageEntry> entries,
        MessageEncodingProfile encodingProfile) =>
        entries.All(entry => entry.CodecMetadata is MajorasMaskMessageMetadata)
            ? MmMessageTableCodec.BuildFiles(entries, encodingProfile)
            : BuildStaffCreditsTable(entries, encodingProfile);

    private static List<MessageEntry> ParseStaffCreditsTable(
        byte[] tableBytes,
        byte[] messageBytes,
        MessageEncodingProfile encodingProfile,
        bool decodeMessages)
    {
        List<MessageEntry> entries = MessageTableCodec.ParseTable(
            tableBytes,
            messageBytes,
            tableSegment: StaffTableSegment,
            encodingProfile: encodingProfile,
            decodeMessages: decodeMessages);

        foreach (MessageEntry entry in entries)
        {
            entry.Text = DecodeStaffAliases(entry.Text);
            entry.OriginalText = entry.Text;
        }

        return entries;
    }

    private static (byte[] TableBytes, byte[] MessageBytes) BuildStaffCreditsTable(
        List<MessageEntry> entries,
        MessageEncodingProfile encodingProfile)
    {
        var encodingEntries = entries
            .Select(CloneForStaffCreditsEncoding)
            .ToList();

        var (tableBytes, messageBytes) = MessageTableCodec.BuildFiles(encodingEntries, encodingProfile);
        return (tableBytes, PadStaffCreditsMessageData(messageBytes));
    }

    private static byte[] PadStaffCreditsMessageData(byte[] messageBytes)
    {
        int alignedLength = (messageBytes.Length + 15) & ~15;
        if (alignedLength == messageBytes.Length)
        {
            return messageBytes;
        }

        var padded = new byte[alignedLength];
        Buffer.BlockCopy(messageBytes, 0, padded, 0, messageBytes.Length);
        return padded;
    }

    private static MessageEntry CloneForStaffCreditsEncoding(MessageEntry source)
    {
        var clone = new MessageEntry(source.Id, source.Type, source.Position, source.Bank, source.Offset)
        {
            TableEndMarkerId = source.TableEndMarkerId,
            TableHasFinalEndMarker = source.TableHasFinalEndMarker,
            PreserveOffsetWithoutMessageData = source.PreserveOffsetWithoutMessageData,
            Text = EncodeStaffAliases(source.Text),
            OriginalText = source.OriginalText is null ? null : EncodeStaffAliases(source.OriginalText),
            OriginalEncodedBytes = source.OriginalEncodedBytes,
            EncodedBytesOverride = source.EncodedBytesOverride,
            OriginalTrailingMessageData = source.OriginalTrailingMessageData,
            OriginalMessageDataSize = source.OriginalMessageDataSize,
            OriginalFinalTableEndMarkerBank = source.OriginalFinalTableEndMarkerBank,
            OriginalFinalTableEndMarkerOffset = source.OriginalFinalTableEndMarkerOffset,
            OriginalCodecMetadata = source.OriginalCodecMetadata,
            CodecMetadata = source.CodecMetadata,
        };

        return clone;
    }

    private static string DecodeStaffAliases(string text) =>
        text.Replace(StaffPersistentEncodingTag, StaffPersistentTag, StringComparison.OrdinalIgnoreCase);

    private static string EncodeStaffAliases(string text) =>
        text.Replace(StaffPersistentTag, StaffPersistentEncodingTag, StringComparison.OrdinalIgnoreCase);
}
