namespace HylianGrimoire.Rom;

public interface IMessageBankLayout
{
    IReadOnlyList<MessageBankProfile> GetEditableBanks(RomVersionProfile profile);

    MessageBankProfile GetSection(
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section);

    MessageBankProfile? GetJapaneseExportBank(RomVersionProfile profile);

    bool UsesFontOrderPointer(
        byte[] rom,
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section);

    int FindFontOrderRoutineOffset(byte[] rom);
}
