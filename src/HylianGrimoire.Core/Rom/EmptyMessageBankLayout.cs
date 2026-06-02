namespace HylianGrimoire.Rom;

public sealed class EmptyMessageBankLayout : IMessageBankLayout
{
    public static EmptyMessageBankLayout Instance { get; } = new();

    private EmptyMessageBankLayout()
    {
    }

    public IReadOnlyList<MessageBankProfile> GetEditableBanks(RomVersionProfile profile) => [];

    public MessageBankProfile GetSection(
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section) =>
        throw new NotSupportedException("This game profile does not have an editable message bank layout yet.");

    public MessageBankProfile? GetJapaneseExportBank(RomVersionProfile profile) => null;

    public bool UsesFontOrderPointer(
        byte[] rom,
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section) => false;

    public int FindFontOrderRoutineOffset(byte[] rom) => -1;
}
