namespace HylianGrimoire.Rom.MajorasMask;

public sealed class MmMessageBankLayout : IMessageBankLayout
{
    public static MmMessageBankLayout Instance { get; } = new();

    private MmMessageBankLayout()
    {
    }

    public IReadOnlyList<MessageBankProfile> GetEditableBanks(RomVersionProfile profile) => profile.MessageBanks;

    public MessageBankProfile GetSection(
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section) =>
        section == RomMessageSection.Credits
            ? profile.CreditsBank
            : GetMessageBank(profile, messageBankIndex);

    public MessageBankProfile? GetJapaneseExportBank(RomVersionProfile profile) => null;

    public bool UsesFontOrderPointer(
        byte[] rom,
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section) => false;

    public int FindFontOrderRoutineOffset(byte[] rom) => -1;

    private static MessageBankProfile GetMessageBank(RomVersionProfile profile, int messageBankIndex)
    {
        if (messageBankIndex < 0 || messageBankIndex >= profile.MessageBanks.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(messageBankIndex),
                $"Message bank {messageBankIndex + 1} is outside the ROM profile's message bank list.");
        }

        return profile.MessageBanks[messageBankIndex];
    }
}
