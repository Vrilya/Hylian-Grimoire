using HylianGrimoire.Headers;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Services;

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

    public static Dictionary<int, List<MessageEntry>> LoadLanguageEntries(string path)
    {
        var languages = new Dictionary<int, List<MessageEntry>>();

        List<MessageEntry> nesEntries = TryImportHeaderSlot(path, CHeaderMessageSlot.Nes, allowWesternFallback: true);
        if (nesEntries.Count > 0)
        {
            languages[0] = nesEntries;
        }

        List<MessageEntry> gerEntries = TryImportHeaderSlot(path, CHeaderMessageSlot.Ger, allowWesternFallback: false);
        if (gerEntries.Count > 0)
        {
            languages[1] = gerEntries;
        }

        List<MessageEntry> fraEntries = TryImportHeaderSlot(path, CHeaderMessageSlot.Fra, allowWesternFallback: false);
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

    public static int ChooseInitialLanguage(IReadOnlyDictionary<int, List<MessageEntry>> languages)
        => languages.ContainsKey(0)
            ? 0
            : languages.Keys.Order().First();

    public static List<CHeaderMessageSlot> GetAvailableWesternSlots(string path)
    {
        var slots = new List<CHeaderMessageSlot>();

        if (TryImportHeaderSlot(path, CHeaderMessageSlot.Nes, allowWesternFallback: false).Count > 0)
        {
            slots.Add(CHeaderMessageSlot.Nes);
        }

        if (TryImportHeaderSlot(path, CHeaderMessageSlot.Ger, allowWesternFallback: false).Count > 0)
        {
            slots.Add(CHeaderMessageSlot.Ger);
        }

        if (TryImportHeaderSlot(path, CHeaderMessageSlot.Fra, allowWesternFallback: false).Count > 0)
        {
            slots.Add(CHeaderMessageSlot.Fra);
        }

        return slots;
    }

    public static IReadOnlyDictionary<int, List<MessageEntry>> BuildSelectedRomImport(
        string path,
        CHeaderMessageSlot slot,
        int activeBankIndex,
        IReadOnlyList<MessageEntry> currentEntries)
    {
        List<MessageEntry> parsedEntries = MessageFileService.ImportHeader(path, slot);
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
        IReadOnlyList<MessageEntry> currentEntries)
    {
        IReadOnlyList<List<MessageEntry>> existingBanks = RomMessageService.LoadAllMessageBanks(romData, currentEntries.ToList());
        var result = new Dictionary<int, List<MessageEntry>>();
        for (int bankIndex = 0; bankIndex < Math.Min(3, romData.Profile.MessageBanks.Count); bankIndex++)
        {
            CHeaderMessageSlot slot = GetMessageSlotForBankIndex(bankIndex);
            List<MessageEntry> importedEntries = TryImportHeaderSlot(path, slot, allowWesternFallback: false);
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
        string path,
        CHeaderMessageSlot slot,
        bool allowWesternFallback)
    {
        try
        {
            return MessageFileService.ImportHeader(path, slot, allowWesternFallback);
        }
        catch (InvalidDataException ex) when (ex.Message == "No DEFINE_MESSAGE entries were found.")
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
