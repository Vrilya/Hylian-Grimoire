namespace HylianGrimoire.Rom;

public sealed record RomProfileCapabilities(
    bool SupportsMessageEditing,
    bool SupportsMultipleMessageBanks,
    bool SupportsCreditsEditing,
    bool SupportsJapaneseMessageExport,
    bool SupportsRomFontResources);
