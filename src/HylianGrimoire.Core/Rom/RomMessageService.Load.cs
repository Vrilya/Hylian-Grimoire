using HylianGrimoire.Codecs;

namespace HylianGrimoire.Rom;

public static partial class RomMessageService
{
    public static RomMessageData LoadMessages(
        string path,
        int messageBankIndex = 0,
        RomMessageSection section = RomMessageSection.Messages,
        IProgress<RomFileOperationProgress>? progress = null)
    {
        byte[] rom = File.ReadAllBytes(path);
        bool wasCompressed = HasCompressedEntries(rom);
        RomCompressionResult decompressed = RomCompressionService.DecompressRom(rom, progress);
        RomVersionProfile profile = decompressed.Profile;
        if (!profile.SupportsMessageEditing)
        {
            throw new InvalidDataException($"{profile.Name} is recognized for ROM compression, but message editing is not supported yet.");
        }

        RomFontResources fontResources = profile.Capabilities.SupportsRomFontResources
            ? RomFontService.Locate(decompressed.Data, profile)
            : RomFontResources.Empty;

        List<Models.MessageEntry> entries = LoadSectionEntries(decompressed.Data, profile, messageBankIndex, section);

        return new RomMessageData(entries, profile, wasCompressed, decompressed.Data, fontResources, messageBankIndex, section);
    }

    public static RomMessageData SwitchMessageBank(
        RomMessageData source,
        List<Models.MessageEntry> currentEntries,
        int messageBankIndex,
        bool patchCurrentBank,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= source.Profile.GameProfile.EncodingProfile;
        if (patchCurrentBank)
        {
            PatchActiveSection(source.DecompressedRom, source.Profile, source.ActiveMessageBankIndex, source.ActiveSection, currentEntries, encodingProfile);
        }

        List<Models.MessageEntry> entries = LoadSectionEntries(
            source.DecompressedRom,
            source.Profile,
            messageBankIndex,
            RomMessageSection.Messages,
            encodingProfile);
        return source with
        {
            Entries = entries,
            ActiveMessageBankIndex = messageBankIndex,
            ActiveSection = RomMessageSection.Messages,
        };
    }

    public static RomMessageData SwitchMessageSection(
        RomMessageData source,
        List<Models.MessageEntry> currentEntries,
        RomMessageSection section,
        bool patchCurrentSection,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= source.Profile.GameProfile.EncodingProfile;
        if (patchCurrentSection)
        {
            PatchActiveSection(source.DecompressedRom, source.Profile, source.ActiveMessageBankIndex, source.ActiveSection, currentEntries, encodingProfile);
        }

        List<Models.MessageEntry> entries = LoadSectionEntries(
            source.DecompressedRom,
            source.Profile,
            source.ActiveMessageBankIndex,
            section,
            encodingProfile);
        return source with
        {
            Entries = entries,
            ActiveSection = section,
        };
    }

    public static IReadOnlyList<List<Models.MessageEntry>> LoadAllMessageBanks(
        RomMessageData source,
        List<Models.MessageEntry> currentEntries,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= source.Profile.GameProfile.EncodingProfile;
        byte[] decompressed = source.DecompressedRom.ToArray();
        PatchActiveSection(
            decompressed,
            source.Profile,
            source.ActiveMessageBankIndex,
            source.ActiveSection,
            currentEntries,
            encodingProfile);

        IReadOnlyList<MessageBankProfile> editableBanks = source.Profile.GameProfile.MessageBankLayout.GetEditableBanks(source.Profile);
        var banks = new List<List<Models.MessageEntry>>(editableBanks.Count);
        for (int i = 0; i < editableBanks.Count; i++)
        {
            banks.Add(LoadSectionEntries(decompressed, source.Profile, i, RomMessageSection.Messages, encodingProfile));
        }

        return banks;
    }

    public static (List<Models.MessageEntry>? Jpn, List<Models.MessageEntry>? Nes, List<Models.MessageEntry>? Ger, List<Models.MessageEntry>? Fra)
        LoadModernExportBanks(
            RomMessageData source,
            List<Models.MessageEntry> currentEntries,
            MessageEncodingProfile? encodingProfile = null)
    {
        byte[] decompressed = source.DecompressedRom.ToArray();
        PatchActiveSection(
            decompressed,
            source.Profile,
            source.ActiveMessageBankIndex,
            source.ActiveSection,
            currentEntries,
            encodingProfile);

        List<Models.MessageEntry>? jpn = source.Profile.GameProfile.MessageBankLayout.GetJapaneseExportBank(source.Profile) is MessageBankProfile japaneseBank
            ? LoadBankEntries(decompressed, source.Profile, japaneseBank, decodeMessages: false)
            : null;
        IReadOnlyList<MessageBankProfile> editableBanks = source.Profile.GameProfile.MessageBankLayout.GetEditableBanks(source.Profile);
        List<Models.MessageEntry>? nes = editableBanks.Count > 0
            ? LoadBankEntries(decompressed, source.Profile, editableBanks[0])
            : null;
        List<Models.MessageEntry>? ger = editableBanks.Count > 1
            ? LoadBankEntries(decompressed, source.Profile, editableBanks[1])
            : null;
        List<Models.MessageEntry>? fra = editableBanks.Count > 2
            ? LoadBankEntries(decompressed, source.Profile, editableBanks[2])
            : null;

        return (jpn, nes, ger, fra);
    }

    public static List<Models.MessageEntry> LoadCreditsBank(
        RomMessageData source,
        List<Models.MessageEntry> currentEntries,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= source.Profile.GameProfile.EncodingProfile;
        byte[] decompressed = source.DecompressedRom.ToArray();
        PatchActiveSection(
            decompressed,
            source.Profile,
            source.ActiveMessageBankIndex,
            source.ActiveSection,
            currentEntries,
            encodingProfile);

        return LoadBankEntries(
            decompressed,
            source.Profile,
            source.Profile.GameProfile.MessageBankLayout.GetSection(source.Profile, source.ActiveMessageBankIndex, RomMessageSection.Credits),
            encodingProfile: encodingProfile);
    }

    public static List<Models.MessageEntry>? LoadJapaneseBank(
        RomMessageData source,
        List<Models.MessageEntry> currentEntries,
        MessageEncodingProfile? encodingProfile = null)
    {
        if (source.Profile.GameProfile.MessageBankLayout.GetJapaneseExportBank(source.Profile) is not MessageBankProfile japaneseBank)
        {
            return null;
        }

        encodingProfile ??= source.Profile.GameProfile.EncodingProfile;
        byte[] decompressed = source.DecompressedRom.ToArray();
        PatchActiveSection(
            decompressed,
            source.Profile,
            source.ActiveMessageBankIndex,
            source.ActiveSection,
            currentEntries,
            encodingProfile);

        return LoadBankEntries(decompressed, source.Profile, japaneseBank, decodeMessages: false, encodingProfile: encodingProfile);
    }

    private static List<Models.MessageEntry> LoadSectionEntries(
        byte[] decompressedRom,
        RomVersionProfile profile,
        int messageBankIndex,
        RomMessageSection section,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= profile.GameProfile.EncodingProfile;
        MessageBankProfile bank = profile.GameProfile.MessageBankLayout.GetSection(profile, messageBankIndex, section);
        byte[] tableBytes = Slice(decompressedRom, bank.MessageTableOffset, bank.MessageTableSize);
        int messageDataSize = GetMessageDataSize(decompressedRom, profile, bank);
        byte[] messageBytes = Slice(decompressedRom, bank.MessageDataOffset, messageDataSize);
        IReadOnlyList<int>? pointerBounds = bank.PointerTableOffset > 0
            ? ReadMessagePointerBounds(decompressedRom, bank.PointerTableOffset, CountMessageEntries(tableBytes, bank.ExcludesFontMessage) + 1)
            : null;

        return profile.GameProfile.MessageBankCodec.Parse(
            tableBytes,
            messageBytes,
            bank,
            pointerBounds,
            encodingProfile);
    }

    private static List<Models.MessageEntry> LoadBankEntries(
        byte[] decompressedRom,
        RomVersionProfile profile,
        MessageBankProfile bank,
        bool decodeMessages = true,
        MessageEncodingProfile? encodingProfile = null)
    {
        encodingProfile ??= profile.GameProfile.EncodingProfile;
        byte[] tableBytes = Slice(decompressedRom, bank.MessageTableOffset, bank.MessageTableSize);
        int messageDataSize = GetMessageDataSize(decompressedRom, profile, bank);
        byte[] messageBytes = Slice(decompressedRom, bank.MessageDataOffset, messageDataSize);
        IReadOnlyList<int>? pointerBounds = bank.PointerTableOffset > 0
            ? ReadMessagePointerBounds(decompressedRom, bank.PointerTableOffset, CountMessageEntries(tableBytes, bank.ExcludesFontMessage) + 1)
            : null;

        return profile.GameProfile.MessageBankCodec.Parse(
            tableBytes,
            messageBytes,
            bank,
            pointerBounds,
            encodingProfile,
            decodeMessages);
    }

    private static bool HasCompressedEntries(ReadOnlySpan<byte> rom)
    {
        RomVersionProfile profile = RomVersionDatabase.Detect(rom);
        List<DmaEntry> entries = DmaTable.Parse(rom, profile);
        return entries.Any(entry => !entry.IsDeleted && !entry.IsEmpty && entry.IsCompressed);
    }

    private static int GetMessageDataSize(byte[] rom, RomVersionProfile profile, MessageBankProfile bank)
    {
        DmaEntry? entry = FindDmaEntry(rom, profile, bank.MessageDataOffset);
        if (entry is not null && entry.VirtualEnd > bank.MessageDataOffset)
        {
            return checked((int)(entry.VirtualEnd - bank.MessageDataOffset));
        }

        return bank.MessageDataSize;
    }
}
