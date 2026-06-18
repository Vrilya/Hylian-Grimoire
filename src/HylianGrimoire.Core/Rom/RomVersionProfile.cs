using HylianGrimoire.Games;

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
    MessageBankProfile? JapaneseMessageBank = null,
    GameKind Game = GameKind.OcarinaOfTime,
    int? FontDmaEntryIndex = null,
    int? FontWidthTableOffset = null)
{
    public GameProfile GameProfile => GameProfiles.Get(Game);

    public RomProfileCapabilities Capabilities { get; } = new(
        SupportsMessageEditing: MessageBanks.Count > 0,
        SupportsMultipleMessageBanks: MessageBanks.Count > 1,
        SupportsCreditsEditing: CreditsTableOffset > 0
            && CreditsTableSize > 0
            && CreditsDataOffset > 0
            && CreditsDataSize > 0,
        SupportsJapaneseMessageExport: JapaneseMessageBank is not null,
        SupportsRomFontResources: GameProfiles.Get(Game).Capabilities.SupportsRomGlyphEditor && MessageBanks.Count > 0);

    public MessageBankProfile DefaultMessageBank => MessageBanks[0];

    public MessageBankProfile CreditsBank =>
        new("Credits", CreditsTableOffset, CreditsTableSize, CreditsDataOffset, CreditsDataSize);

    public int MessageTableOffset => DefaultMessageBank.MessageTableOffset;

    public int MessageTableSize => DefaultMessageBank.MessageTableSize;

    public int MessageDataOffset => DefaultMessageBank.MessageDataOffset;

    public int MessageDataSize => DefaultMessageBank.MessageDataSize;

    public bool IsRetail => Game switch
    {
        GameKind.OcarinaOfTime => Name.StartsWith("Retail ", StringComparison.Ordinal),
        GameKind.MajorasMask => Name.StartsWith("Majora's Mask ", StringComparison.Ordinal),
        _ => false,
    };

    public bool SupportsMessageEditing => Capabilities.SupportsMessageEditing;
}
