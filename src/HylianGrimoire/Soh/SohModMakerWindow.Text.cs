using HylianGrimoire.Codecs;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.Soh;

public sealed partial class SohModMakerWindow
{
    private static IReadOnlyList<SohTextResourceItem> BuildTextResources(RomMessageData romData)
    {
        var resources = new List<SohTextResourceItem>();
        IReadOnlyList<MessageBankProfile> editableBanks = romData.Profile.GameProfile.MessageBankLayout.GetEditableBanks(romData.Profile);
        for (int bankIndex = 0; bankIndex < editableBanks.Count && bankIndex < 3; bankIndex++)
        {
            resources.Add(new SohTextResourceItem(
                editableBanks[bankIndex].Name,
                SohResourcePacker.GetMessageResourcePath(bankIndex),
                SohTextResourceKind.MessageBank,
                bankIndex));
        }

        resources.Add(new SohTextResourceItem(
            "Credits",
            SohResourcePacker.CreditsResourcePath,
            SohTextResourceKind.Credits,
            BankIndex: -1));

        return resources;
    }

    private static IReadOnlyList<SohTextResourceItem> BuildCurrentDocumentTextResources(
        IReadOnlyDictionary<int, List<MessageEntry>> languageEntries)
    {
        return
        [
            .. languageEntries
                .Where(pair => pair.Key is >= 0 and <= 2 && pair.Value.Count > 0)
                .OrderBy(pair => pair.Key)
                .Select(pair => new SohTextResourceItem(
                    $"Language {pair.Key + 1}",
                    SohResourcePacker.GetMessageResourcePath(pair.Key),
                    SohTextResourceKind.CurrentDocument,
                    pair.Key)),
        ];
    }

    private void PopulateTextResources()
    {
        TextResourcePanel.Children.Clear();
        foreach (SohTextResourceItem resource in _textResources)
        {
            var checkBox = new CheckBox
            {
                Content = resource.DisplayName,
                DataContext = resource,
                IsChecked = _selectedTextResources.Contains(resource.ResourcePath),
            };
            checkBox.Checked += OnTextResourceChecked;
            checkBox.Unchecked += OnTextResourceUnchecked;
            TextResourcePanel.Children.Add(checkBox);
        }
    }

    private void OnTextResourceChecked(object sender, RoutedEventArgs e)
    {
        if (_updatingTextChecks || sender is not CheckBox { DataContext: SohTextResourceItem resource })
        {
            return;
        }

        _selectedTextResources.Add(resource.ResourcePath);
        MarkWorkspaceChanged();
        UpdateWorkspaceSummary();
    }

    private void OnTextResourceUnchecked(object sender, RoutedEventArgs e)
    {
        if (_updatingTextChecks || sender is not CheckBox { DataContext: SohTextResourceItem resource })
        {
            return;
        }

        _selectedTextResources.Remove(resource.ResourcePath);
        MarkWorkspaceChanged();
        UpdateWorkspaceSummary();
    }

    private void RefreshTextCheckStates()
    {
        _updatingTextChecks = true;
        try
        {
            foreach (CheckBox checkBox in TextResourcePanel.Children.OfType<CheckBox>())
            {
                if (checkBox.DataContext is SohTextResourceItem resource)
                {
                    checkBox.IsChecked = _selectedTextResources.Contains(resource.ResourcePath);
                }
            }
        }
        finally
        {
            _updatingTextChecks = false;
        }
    }

    private List<SohTextPayload> BuildTextPayloads(
        IReadOnlyList<SohTextResourceItem> selectedResources,
        List<MessageEntry> currentEntries)
    {
        if (selectedResources.Count == 0)
        {
            return [];
        }

        IReadOnlyList<List<MessageEntry>>? messageBanks = null;
        List<MessageEntry>? creditsBank = null;
        var payloads = new List<SohTextPayload>(selectedResources.Count);

        foreach (SohTextResourceItem resource in selectedResources)
        {
            List<MessageEntry> entries = resource.Kind switch
            {
                SohTextResourceKind.CurrentDocument => GetCurrentDocumentTextEntries(resource, currentEntries),
                SohTextResourceKind.MessageBank => (messageBanks ??= RomMessageService.LoadAllMessageBanks(
                    _romData ?? throw new InvalidDataException("A ROM is required to read ROM language banks."),
                    currentEntries,
                    _encodingProfile))[resource.BankIndex],
                SohTextResourceKind.Credits => creditsBank ??= RomMessageService.LoadCreditsBank(
                    _romData ?? throw new InvalidDataException("A ROM is required to read credits text."),
                    currentEntries,
                    _encodingProfile),
                _ => throw new InvalidDataException($"Unsupported SoH text resource: {resource.DisplayName}."),
            };

            payloads.Add(new SohTextPayload(resource.ResourcePath, PackTextEntries(entries)));
        }

        return payloads;
    }

    private List<MessageEntry> GetCurrentDocumentTextEntries(SohTextResourceItem resource, List<MessageEntry> currentEntries)
    {
        IReadOnlyDictionary<int, List<MessageEntry>> languages = _getCurrentTextLanguages();
        return languages.TryGetValue(resource.BankIndex, out List<MessageEntry>? entries)
            ? entries
            : currentEntries;
    }

    private byte[] PackTextEntries(List<MessageEntry> entries)
    {
        var (tableBytes, messageBytes) = MessageTableCodec.BuildFiles(entries, _encodingProfile);
        return SohResourcePacker.PackText(messageBytes, tableBytes, addFontOrder: true);
    }
}
