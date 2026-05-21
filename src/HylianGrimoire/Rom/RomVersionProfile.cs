namespace HylianGrimoire.Rom;

public sealed record RomVersionProfile(
    string Name,
    string BuildDate,
    int BuildDateOffset,
    int DmaTableOffset,
    int DmaEntryCount,
    RomCodecKind Codec,
    bool RawDeflateHasNoHeader,
    int TargetCompressedSizeMiB,
    int CreditsTableOffset,
    int CreditsTableSize,
    int CreditsDataOffset,
    int CreditsDataSize,
    IReadOnlyList<MessageBankProfile> MessageBanks,
    IReadOnlySet<int> UncompressedEntryIndices,
    RomFontBaseline FontBaseline = RomFontBaseline.Standard,
    MessageBankProfile? JapaneseMessageBank = null)
{
    public MessageBankProfile DefaultMessageBank => MessageBanks[0];

    public MessageBankProfile CreditsBank =>
        new("Credits", CreditsTableOffset, CreditsTableSize, CreditsDataOffset, CreditsDataSize);

    public int MessageTableOffset => DefaultMessageBank.MessageTableOffset;

    public int MessageTableSize => DefaultMessageBank.MessageTableSize;

    public int MessageDataOffset => DefaultMessageBank.MessageDataOffset;

    public int MessageDataSize => DefaultMessageBank.MessageDataSize;

    public bool IsRetail => Name.StartsWith("Retail ", StringComparison.Ordinal);
}
