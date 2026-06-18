namespace HylianGrimoire.Rom;

public sealed class OotMessageBankLayout : IMessageBankLayout
{
    private static readonly byte[] FontLoadOrderedFontProlog = [0x27, 0xbd, 0xff, 0xc0, 0xaf, 0xb3, 0x00, 0x24];

    public static OotMessageBankLayout Instance { get; } = new();

    private OotMessageBankLayout()
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

    public MessageBankProfile? GetJapaneseExportBank(RomVersionProfile profile) => profile.JapaneseMessageBank;

    public bool UsesFontOrderPointer(
        byte[] rom,
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section) =>
        section == RomMessageSection.Messages
        && messageBankIndex == 0
        && profile.MessageBanks.Count > 1
        && FindBytes(rom, FontLoadOrderedFontProlog) >= 0;

    public int FindFontOrderRoutineOffset(byte[] rom) => FindBytes(rom, FontLoadOrderedFontProlog);

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

    private static int FindBytes(byte[] data, ReadOnlySpan<byte> pattern)
    {
        for (int i = 0; i <= data.Length - pattern.Length; i++)
        {
            if (data.AsSpan(i, pattern.Length).SequenceEqual(pattern))
            {
                return i;
            }
        }

        return -1;
    }
}
