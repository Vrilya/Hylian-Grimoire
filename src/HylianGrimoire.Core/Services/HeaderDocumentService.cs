using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Headers;
using HylianGrimoire.Headers.MajorasMask;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Services;

public sealed record HeaderFileDocument(Dictionary<int, List<MessageEntry>> Languages, GameProfile GameProfile);

public static class HeaderDocumentService
{
    public static CHeaderMessageSlot GetMessageSlotForBankIndex(int bankIndex)
    {
        return bankIndex switch
        {
            1 => CHeaderMessageSlot.Ger,
            2 => CHeaderMessageSlot.Fra,
            _ => CHeaderMessageSlot.Nes,
        };
    }

    public static Dictionary<int, List<MessageEntry>> LoadLanguageEntries(
        string path,
        GameEncodingProfileResolver? getEncodingProfile = null)
    {
        string content = File.ReadAllText(path);
        return LoadLanguageEntriesFromContent(content, getEncodingProfile);
    }

    private static Dictionary<int, List<MessageEntry>> LoadLanguageEntriesFromContent(
        string content,
        GameEncodingProfileResolver? getEncodingProfile = null)
    {
        if (MmCHeaderImporter.LooksLikeMajorasMask(content))
        {
            return new Dictionary<int, List<MessageEntry>>
            {
                [0] = MessageFileService.ImportHeaderContent(content, getEncodingProfile: getEncodingProfile),
            };
        }

        var languages = new Dictionary<int, List<MessageEntry>>();

        List<MessageEntry> nesEntries = TryImportHeaderSlot(content, CHeaderMessageSlot.Nes, allowWesternFallback: true, getEncodingProfile);
        if (nesEntries.Count > 0)
        {
            languages[0] = nesEntries;
        }

        List<MessageEntry> gerEntries = TryImportHeaderSlot(content, CHeaderMessageSlot.Ger, allowWesternFallback: false, getEncodingProfile);
        if (gerEntries.Count > 0)
        {
            languages[1] = gerEntries;
        }

        List<MessageEntry> fraEntries = TryImportHeaderSlot(content, CHeaderMessageSlot.Fra, allowWesternFallback: false, getEncodingProfile);
        if (fraEntries.Count > 0)
        {
            languages[2] = fraEntries;
        }

        if (languages.Count == 0)
        {
            languages[0] = nesEntries;
        }

        return languages;
    }

    public static HeaderFileDocument LoadDocument(
        string path,
        GameEncodingProfileResolver? getEncodingProfile = null)
    {
        string content = File.ReadAllText(path);
        if (MmCHeaderImporter.LooksLikeMajorasMask(content))
        {
            return new HeaderFileDocument(
                new Dictionary<int, List<MessageEntry>>
                {
                    [0] = MessageFileService.ImportHeaderContent(content, getEncodingProfile: getEncodingProfile),
                },
                GameProfiles.Get(GameKind.MajorasMask));
        }

        return new HeaderFileDocument(
            LoadLanguageEntriesFromContent(content, getEncodingProfile),
            GameProfiles.Get(GameKind.OcarinaOfTime));
    }

    public static int ChooseInitialLanguage(IReadOnlyDictionary<int, List<MessageEntry>> languages)
        => languages.ContainsKey(0)
            ? 0
            : languages.Keys.Order().First();

    public static List<CHeaderMessageSlot> GetAvailableWesternSlots(
        string path,
        GameEncodingProfileResolver? getEncodingProfile = null)
    {
        string content = File.ReadAllText(path);
        if (MmCHeaderImporter.LooksLikeMajorasMask(content))
        {
            return MessageFileService.ImportHeaderContent(content, getEncodingProfile: getEncodingProfile).Count > 0
                ? [CHeaderMessageSlot.Nes]
                : [];
        }

        var slots = new List<CHeaderMessageSlot>();

        if (TryImportHeaderSlot(content, CHeaderMessageSlot.Nes, allowWesternFallback: false, getEncodingProfile).Count > 0)
        {
            slots.Add(CHeaderMessageSlot.Nes);
        }

        if (TryImportHeaderSlot(content, CHeaderMessageSlot.Ger, allowWesternFallback: false, getEncodingProfile).Count > 0)
        {
            slots.Add(CHeaderMessageSlot.Ger);
        }

        if (TryImportHeaderSlot(content, CHeaderMessageSlot.Fra, allowWesternFallback: false, getEncodingProfile).Count > 0)
        {
            slots.Add(CHeaderMessageSlot.Fra);
        }

        return slots;
    }

    public static IReadOnlyDictionary<int, List<MessageEntry>> BuildSelectedRomImport(
        string path,
        CHeaderMessageSlot slot,
        int activeBankIndex,
        IReadOnlyList<MessageEntry> currentEntries,
        GameEncodingProfileResolver? getEncodingProfile = null)
    {
        string content = File.ReadAllText(path);
        List<MessageEntry> parsedEntries = MessageFileService.ImportHeaderContent(
            content,
            slot,
            getEncodingProfile: getEncodingProfile);
        if (parsedEntries.Count == 0)
        {
            return new Dictionary<int, List<MessageEntry>>();
        }

        List<MessageEntry> importedEntries = MergeImportedRomBank(parsedEntries, currentEntries);
        PreserveRomTableMetadata(importedEntries, currentEntries);
        return new Dictionary<int, List<MessageEntry>>
        {
            [activeBankIndex] = importedEntries,
        };
    }

    public static IReadOnlyDictionary<int, List<MessageEntry>> BuildAllWesternRomImports(
        string path,
        RomMessageData romData,
        IReadOnlyList<MessageEntry> currentEntries,
        GameEncodingProfileResolver? getEncodingProfile = null)
    {
        string content = File.ReadAllText(path);
        MessageEncodingProfile encodingProfile = getEncodingProfile?.Invoke(romData.Profile.GameProfile.Kind)
            ?? romData.Profile.GameProfile.EncodingProfile;
        IReadOnlyList<List<MessageEntry>> existingBanks = RomMessageService.LoadAllMessageBanks(
            romData,
            currentEntries.ToList(),
            encodingProfile);
        var result = new Dictionary<int, List<MessageEntry>>();
        IReadOnlyList<MessageBankProfile> editableBanks = romData.Profile.GameProfile.MessageBankLayout.GetEditableBanks(romData.Profile);
        for (int bankIndex = 0; bankIndex < Math.Min(3, editableBanks.Count); bankIndex++)
        {
            CHeaderMessageSlot slot = GetMessageSlotForBankIndex(bankIndex);
            List<MessageEntry> importedEntries = TryImportHeaderSlot(content, slot, allowWesternFallback: false, getEncodingProfile);
            if (importedEntries.Count == 0)
            {
                continue;
            }

            List<MessageEntry> mergedEntries = MergeImportedRomBank(importedEntries, existingBanks[bankIndex]);
            PreserveRomTableMetadata(mergedEntries, existingBanks[bankIndex]);
            result[bankIndex] = mergedEntries;
        }

        return result;
    }

    private static List<MessageEntry> TryImportHeaderSlot(
        string content,
        CHeaderMessageSlot slot,
        bool allowWesternFallback,
        GameEncodingProfileResolver? getEncodingProfile = null)
    {
        try
        {
            return MessageFileService.ImportHeaderContent(content, slot, allowWesternFallback, getEncodingProfile);
        }
        catch (HeaderMessageEntriesNotFoundException)
        {
            return [];
        }
    }

    private static List<MessageEntry> MergeImportedRomBank(
        IReadOnlyList<MessageEntry> importedEntries,
        IReadOnlyList<MessageEntry> existingEntries)
    {
        Dictionary<int, MessageEntry> importedById = importedEntries.ToDictionary(entry => entry.Id);
        var merged = new List<MessageEntry>(Math.Max(importedEntries.Count, existingEntries.Count));
        var usedIds = new HashSet<int>();

        foreach (MessageEntry existingEntry in existingEntries)
        {
            if (importedById.TryGetValue(existingEntry.Id, out MessageEntry? importedEntry))
            {
                merged.Add(importedEntry);
                usedIds.Add(importedEntry.Id);
            }
            else
            {
                merged.Add(existingEntry);
            }
        }

        foreach (MessageEntry importedEntry in importedEntries)
        {
            if (usedIds.Add(importedEntry.Id))
            {
                merged.Add(importedEntry);
            }
        }

        return merged;
    }

    private static void PreserveRomTableMetadata(List<MessageEntry> importedEntries, IReadOnlyList<MessageEntry> existingEntries)
    {
        MessageEntry? template = existingEntries.FirstOrDefault();
        if (template is null)
        {
            return;
        }

        foreach (MessageEntry entry in importedEntries)
        {
            entry.TableEndMarkerId = template.TableEndMarkerId;
            entry.TableHasFinalEndMarker = template.TableHasFinalEndMarker;
            entry.Bank = template.Bank;
            entry.OriginalTrailingMessageData = template.OriginalTrailingMessageData;
            entry.OriginalMessageDataSize = template.OriginalMessageDataSize;
        }
    }
}
