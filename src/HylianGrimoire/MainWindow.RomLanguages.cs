using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HylianGrimoire.Models;
using HylianGrimoire.Rom;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private async Task SelectLanguageAsync(int languageIndex)
    {
        if (_session.Kind == DocumentKind.Header)
        {
            SelectHeaderLanguage(languageIndex);
            return;
        }

        await SelectRomLanguageAsync(languageIndex);
    }

    private void SelectHeaderLanguage(int languageIndex)
    {
        if (_session.HeaderLanguageEntries is null
            || !_session.HeaderLanguageEntries.ContainsKey(languageIndex)
            || languageIndex == _session.ActiveHeaderLanguageIndex)
        {
            return;
        }

        CommitCurrent();
        CommitHeaderLanguageChanges();
        int selectedOrdinal = GetSelectedVisibleOrdinal();
        bool currentLanguageWasDirty = _session.IsCurrentViewDirty();

        _session.SwitchHeaderLanguage(languageIndex);

        if (currentLanguageWasDirty)
        {
            MarkHeaderLanguageDirty();
        }

        RefreshDocumentShell();
        RefreshMessageListAndRestoreVisibleOrdinal(selectedOrdinal);

        MarkCurrentViewClean();
        SetStatus($"Switched to {GetHeaderLanguageName(languageIndex)}.");
    }

    private async Task SelectRomLanguageAsync(int bankIndex)
    {
        if (_session.RomData is null ||
            (_session.RomData.ActiveSection == RomMessageSection.Messages && bankIndex == _session.RomData.ActiveMessageBankIndex))
        {
            return;
        }

        CommitCurrent();
        int selectedOrdinal = GetSelectedVisibleOrdinal();
        bool currentBankWasDirty = _session.IsCurrentViewDirty();
        bool alreadyHadRomBankChanges = _session.HasUnsavedRomBankChanges;

        try
        {
            _session.UseRomData(RomMessageService.SwitchMessageBank(
                _session.RomData,
                _session.Entries,
                bankIndex,
                currentBankWasDirty,
                CreateCurrentEncodingProfile()));

            if (currentBankWasDirty || alreadyHadRomBankChanges)
            {
                MarkRomBankDirty();
            }

            RefreshDocumentShell();
            RefreshMessageListAndRestoreVisibleOrdinal(selectedOrdinal);

            MarkCurrentViewClean();
            SetStatus($"Switched to {GetRomMessageBankName(_session.RomData, bankIndex)}.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to switch language", ex.Message);
        }
    }

    private async void OnToggleCreditsMode(object sender, RoutedEventArgs e)
    {
        if (_session.RomData is null)
        {
            CreditsModeItem.IsChecked = false;
            return;
        }

        var requestedSection = CreditsModeItem.IsChecked == true
            ? RomMessageSection.Credits
            : RomMessageSection.Messages;
        if (requestedSection == _session.RomData.ActiveSection)
        {
            return;
        }

        await SelectRomSectionAsync(requestedSection);
    }

    private async Task SelectRomSectionAsync(RomMessageSection section)
    {
        if (_session.RomData is null)
        {
            return;
        }

        CommitCurrent();
        int selectedOrdinal = GetSelectedVisibleOrdinal();
        bool currentSectionWasDirty = _session.IsCurrentViewDirty();
        bool alreadyHadRomBankChanges = _session.HasUnsavedRomBankChanges;

        try
        {
            _session.UseRomData(RomMessageService.SwitchMessageSection(
                _session.RomData,
                _session.Entries,
                section,
                currentSectionWasDirty,
                CreateCurrentEncodingProfile()));

            if (currentSectionWasDirty || alreadyHadRomBankChanges)
            {
                MarkRomBankDirty();
            }

            RefreshDocumentShell();
            RefreshMessageListAndRestoreVisibleOrdinal(selectedOrdinal);

            MarkCurrentViewClean();
            SetStatus(section == RomMessageSection.Credits
                ? "Switched to Credits."
                : $"Switched to {GetRomMessageBankName(_session.RomData, _session.RomData.ActiveMessageBankIndex)}.");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Failed to switch ROM section", ex.Message);
            UpdateLanguageMenuState();
        }
    }

    private void UpdateLanguageMenuState()
    {
        LanguageMenuFlyout.Items.Clear();

        bool isRom = _session.Kind == DocumentKind.Rom && _session.RomData is not null;
        bool isCredits = _session.RomData?.ActiveSection == RomMessageSection.Credits;
        bool isMultiHeader = _session.Kind == DocumentKind.Header
            && _session.HeaderLanguageEntries is not null
            && _session.HeaderLanguageEntries.Count > 1;

        if (isMultiHeader)
        {
            LanguageMenu.IsEnabled = true;
            CreditsModeItem.IsEnabled = false;
            CreditsModeItem.IsChecked = false;
            PopulateHeaderLanguageMenu();
            return;
        }

        int bankCount = _session.RomData is null
            ? 0
            : _session.RomData.Profile.GameProfile.MessageBankLayout.GetEditableBanks(_session.RomData.Profile).Count;
        int activeBank = _session.RomData?.ActiveMessageBankIndex ?? -1;
        bool supportsMultipleMessageBanks = _session.RomData?.Profile.Capabilities.SupportsMultipleMessageBanks == true;
        bool supportsCredits = _session.RomData?.Profile.Capabilities.SupportsCreditsEditing == true;

        LanguageMenu.IsEnabled = isRom && supportsMultipleMessageBanks && !isCredits;

        CreditsModeItem.IsEnabled = isRom && supportsCredits;
        CreditsModeItem.IsChecked = isCredits;

        PopulateRomLanguageMenu(bankCount, activeBank);
    }

    private void PopulateHeaderLanguageMenu()
    {
        if (_session.HeaderLanguageEntries is null)
        {
            return;
        }

        foreach (int languageIndex in _session.HeaderLanguageEntries.Keys.Order())
        {
            LanguageMenuFlyout.Items.Add(CreateLanguageMenuItem(
                GetHeaderLanguageName(languageIndex),
                languageIndex,
                languageIndex == _session.ActiveHeaderLanguageIndex));
        }
    }

    private void PopulateRomLanguageMenu(int bankCount, int activeBank)
    {
        if (_session.RomData is null)
        {
            return;
        }

        for (int bankIndex = 0; bankIndex < bankCount; bankIndex++)
        {
            LanguageMenuFlyout.Items.Add(CreateLanguageMenuItem(
                GetRomMessageBankName(_session.RomData, bankIndex),
                bankIndex,
                bankIndex == activeBank));
        }
    }

    private MenuFlyoutItem CreateLanguageMenuItem(string text, int languageIndex, bool isActive)
    {
        var item = new MenuFlyoutItem
        {
            Text = text,
            IsEnabled = !isActive,
            Icon = isActive ? new SymbolIcon(Symbol.Accept) : null,
        };
        item.Click += async (_, _) => await SelectLanguageAsync(languageIndex);
        return item;
    }

    private static string GetHeaderLanguageName(int languageIndex)
        => $"Language {languageIndex + 1}";

    private static string GetRomMessageBankName(RomMessageData romData, int bankIndex)
    {
        IReadOnlyList<MessageBankProfile> banks = romData.Profile.GameProfile.MessageBankLayout.GetEditableBanks(romData.Profile);
        return bankIndex >= 0 && bankIndex < banks.Count
            ? banks[bankIndex].Name
            : $"Language {bankIndex + 1}";
    }
}
