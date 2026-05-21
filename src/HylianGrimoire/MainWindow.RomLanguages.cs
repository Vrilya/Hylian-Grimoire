using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private async void OnSelectLanguage1(object sender, RoutedEventArgs e) => await SelectLanguageAsync(0);

    private async void OnSelectLanguage2(object sender, RoutedEventArgs e) => await SelectLanguageAsync(1);

    private async void OnSelectLanguage3(object sender, RoutedEventArgs e) => await SelectLanguageAsync(2);

    private async Task SelectLanguageAsync(int languageIndex)
    {
        if (_documentKind == DocumentKind.Header)
        {
            SelectHeaderLanguage(languageIndex);
            return;
        }

        await SelectRomLanguageAsync(languageIndex);
    }

    private void SelectHeaderLanguage(int languageIndex)
    {
        if (_headerLanguageEntries is null
            || !_headerLanguageEntries.ContainsKey(languageIndex)
            || languageIndex == _activeHeaderLanguageIndex)
        {
            return;
        }

        CommitCurrent();
        CommitHeaderLanguageChanges();
        int selectedOrdinal = GetSelectedVisibleOrdinal();
        bool currentLanguageWasDirty = !string.Equals(
            GetDocumentFingerprint(),
            _cleanDocumentFingerprint,
            StringComparison.Ordinal);

        _activeHeaderLanguageIndex = languageIndex;
        _entries = _headerLanguageEntries[languageIndex];
        _currentIdx = -1;
        RefreshAuxiliaryWindowsForLoadedDocument();

        if (currentLanguageWasDirty)
        {
            MarkHeaderLanguageDirty();
        }

        UpdateWindowTitle();
        UpdateLanguageMenuState();
        ClearSearch();
        PopulateList();

        if (_items.Count > 0)
        {
            SelectVisibleOrdinal(selectedOrdinal);
        }
        else
        {
            ClearEditor();
        }

        MarkCurrentViewClean();
        SetStatus($"Switched to {GetHeaderLanguageName(languageIndex)}.");
    }

    private async Task SelectRomLanguageAsync(int bankIndex)
    {
        if (_romData is null ||
            (_romData.ActiveSection == RomMessageSection.Messages && bankIndex == _romData.ActiveMessageBankIndex))
        {
            return;
        }

        CommitCurrent();
        int selectedOrdinal = GetSelectedVisibleOrdinal();
        bool currentBankWasDirty = !string.Equals(
            GetDocumentFingerprint(),
            _cleanDocumentFingerprint,
            StringComparison.Ordinal);
        bool alreadyHadRomBankChanges = _hasUnsavedRomBankChanges;

        try
        {
            _romData = RomMessageService.SwitchMessageBank(_romData, _entries, bankIndex, currentBankWasDirty);
            _entries = _romData.Entries;
            _currentIdx = -1;
            RefreshAuxiliaryWindowsForLoadedDocument();

            if (currentBankWasDirty || alreadyHadRomBankChanges)
            {
                MarkRomBankDirty();
            }

            UpdateWindowTitle();
            UpdateLanguageMenuState();
            ClearSearch();
            PopulateList();

            if (_items.Count > 0)
            {
                SelectVisibleOrdinal(selectedOrdinal);
            }
            else
            {
                ClearEditor();
            }

            MarkCurrentViewClean();
            SetStatus($"Switched to {_romData.Profile.MessageBanks[bankIndex].Name}.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to switch language", ex.Message);
        }
    }

    private async void OnToggleCreditsMode(object sender, RoutedEventArgs e)
    {
        if (_romData is null)
        {
            CreditsModeItem.IsChecked = false;
            return;
        }

        var requestedSection = CreditsModeItem.IsChecked == true
            ? RomMessageSection.Credits
            : RomMessageSection.Messages;
        if (requestedSection == _romData.ActiveSection)
        {
            return;
        }

        await SelectRomSectionAsync(requestedSection);
    }

    private async Task SelectRomSectionAsync(RomMessageSection section)
    {
        if (_romData is null)
        {
            return;
        }

        CommitCurrent();
        int selectedOrdinal = GetSelectedVisibleOrdinal();
        bool currentSectionWasDirty = !string.Equals(
            GetDocumentFingerprint(),
            _cleanDocumentFingerprint,
            StringComparison.Ordinal);
        bool alreadyHadRomBankChanges = _hasUnsavedRomBankChanges;

        try
        {
            _romData = RomMessageService.SwitchMessageSection(_romData, _entries, section, currentSectionWasDirty);
            _entries = _romData.Entries;
            _currentIdx = -1;
            RefreshAuxiliaryWindowsForLoadedDocument();

            if (currentSectionWasDirty || alreadyHadRomBankChanges)
            {
                MarkRomBankDirty();
            }

            UpdateWindowTitle();
            UpdateLanguageMenuState();
            ClearSearch();
            PopulateList();

            if (_items.Count > 0)
            {
                SelectVisibleOrdinal(selectedOrdinal);
            }
            else
            {
                ClearEditor();
            }

            MarkCurrentViewClean();
            SetStatus(section == RomMessageSection.Credits
                ? "Switched to Credits."
                : $"Switched to {_romData.Profile.MessageBanks[_romData.ActiveMessageBankIndex].Name}.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to switch ROM section", ex.Message);
            UpdateLanguageMenuState();
        }
    }

    private void UpdateLanguageMenuState()
    {
        bool isRom = _documentKind == DocumentKind.Rom && _romData is not null;
        bool isCredits = _romData?.ActiveSection == RomMessageSection.Credits;
        bool isMultiHeader = _documentKind == DocumentKind.Header
            && _headerLanguageEntries is not null
            && _headerLanguageEntries.Count > 1;

        if (isMultiHeader)
        {
            LanguageMenu.IsEnabled = true;
            CreditsModeItem.IsEnabled = false;
            CreditsModeItem.IsChecked = false;
            UpdateHeaderLanguageItem(Language1Item, 0);
            UpdateHeaderLanguageItem(Language2Item, 1);
            UpdateHeaderLanguageItem(Language3Item, 2);
            return;
        }

        int bankCount = _romData?.Profile.MessageBanks.Count ?? 0;
        int activeBank = _romData?.ActiveMessageBankIndex ?? -1;

        LanguageMenu.IsEnabled = isRom && bankCount > 1 && !isCredits;

        CreditsModeItem.IsEnabled = isRom;
        CreditsModeItem.IsChecked = isCredits;

        UpdateLanguageItem(Language1Item, bankCount, activeBank, 0);
        UpdateLanguageItem(Language2Item, bankCount, activeBank, 1);
        UpdateLanguageItem(Language3Item, bankCount, activeBank, 2);
    }

    private static void UpdateLanguageItem(MenuFlyoutItem item, int bankCount, int activeBank, int itemIndex)
    {
        item.IsEnabled = itemIndex < bankCount && itemIndex != activeBank;
        item.Visibility = itemIndex < bankCount ? Visibility.Visible : Visibility.Collapsed;
        item.Icon = itemIndex == activeBank ? new SymbolIcon(Symbol.Accept) : null;
    }

    private void UpdateHeaderLanguageItem(MenuFlyoutItem item, int itemIndex)
    {
        bool exists = _headerLanguageEntries?.ContainsKey(itemIndex) == true;
        item.IsEnabled = exists && itemIndex != _activeHeaderLanguageIndex;
        item.Visibility = exists ? Visibility.Visible : Visibility.Collapsed;
        item.Icon = itemIndex == _activeHeaderLanguageIndex ? new SymbolIcon(Symbol.Accept) : null;
    }

    private static string GetHeaderLanguageName(int languageIndex)
        => languageIndex switch
        {
            1 => "Language 2",
            2 => "Language 3",
            _ => "Language 1",
        };
}
