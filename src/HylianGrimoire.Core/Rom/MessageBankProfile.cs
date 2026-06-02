namespace HylianGrimoire.Rom;

public sealed record MessageBankProfile(
    string Name,
    int MessageTableOffset,
    int MessageTableSize,
    int MessageDataOffset,
    int MessageDataSize,
    MessageBankOffsetMode OffsetMode = MessageBankOffsetMode.Table,
    bool ExcludesFontMessage = false,
    int PointerTableOffset = 0,
    int TableSegment = 0x07);
