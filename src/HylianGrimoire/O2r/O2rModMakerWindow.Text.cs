using HylianGrimoire.Models;
using HylianGrimoire.Rom;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow
{
    private void PopulateTextResources()
    {
        TextResourcePanel.Children.Clear();
        foreach (O2rTextResourceDefinition resource in _textResources)
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
        if (_updatingTextChecks || sender is not CheckBox { DataContext: O2rTextResourceDefinition resource })
        {
            return;
        }

        _selectedTextResources.Add(resource.ResourcePath);
        MarkWorkspaceChanged();
        UpdateWorkspaceSummary();
    }

    private void OnTextResourceUnchecked(object sender, RoutedEventArgs e)
    {
        if (_updatingTextChecks || sender is not CheckBox { DataContext: O2rTextResourceDefinition resource })
        {
            return;
        }

        _selectedTextResources.Remove(resource.ResourcePath);
        MarkWorkspaceChanged();
        UpdateWorkspaceSummary();
    }

    private void RefreshTextCheckStates()
    {
        using IDisposable update = BeginTextCheckUpdate();

        foreach (CheckBox checkBox in TextResourcePanel.Children.OfType<CheckBox>())
        {
            if (checkBox.DataContext is O2rTextResourceDefinition resource)
            {
                checkBox.IsChecked = _selectedTextResources.Contains(resource.ResourcePath);
            }
        }
    }

    private List<O2rTextPayload> BuildTextPayloads(
        IReadOnlyList<O2rTextResourceDefinition> selectedResources,
        List<MessageEntry> currentEntries)
    {
        if (selectedResources.Count == 0)
        {
            return [];
        }

        IReadOnlyList<List<MessageEntry>>? messageBanks = null;
        List<MessageEntry>? creditsBank = null;
        var payloads = new List<O2rTextPayload>(selectedResources.Count);

        foreach (O2rTextResourceDefinition resource in selectedResources)
        {
            List<MessageEntry> entries = resource.Kind switch
            {
                O2rTextResourceKind.CurrentDocument => GetCurrentDocumentTextEntries(resource, currentEntries),
                O2rTextResourceKind.MessageBank => (messageBanks ??= RomMessageService.LoadAllMessageBanks(
                    _romData ?? throw new InvalidDataException("A ROM is required to read ROM language banks."),
                    currentEntries,
                    _encodingProfile))[resource.BankIndex],
                O2rTextResourceKind.Credits => creditsBank ??= RomMessageService.LoadCreditsBank(
                    _romData ?? throw new InvalidDataException("A ROM is required to read credits text."),
                    currentEntries,
                    _encodingProfile),
                _ => throw new InvalidDataException($"Unsupported O2R text resource: {resource.DisplayName}."),
            };

            payloads.Add(new O2rTextPayload(resource.ResourcePath, PackTextEntries(resource, entries)));
        }

        return payloads;
    }

    private List<MessageEntry> GetCurrentDocumentTextEntries(
        O2rTextResourceDefinition resource,
        List<MessageEntry> currentEntries)
    {
        IReadOnlyDictionary<int, List<MessageEntry>> languages = _getCurrentTextLanguages();
        return languages.TryGetValue(resource.BankIndex, out List<MessageEntry>? entries)
            ? entries
            : currentEntries;
    }

    private byte[] PackTextEntries(O2rTextResourceDefinition resource, List<MessageEntry> entries)
    {
        var (tableBytes, messageBytes) = _portProfile.BuildTextFiles(resource, entries, _encodingProfile);
        return _portProfile.PackTextResource(resource, messageBytes, tableBytes);
    }
}
