using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HylianGrimoire.Games;
using HylianGrimoire.Games.MajorasMask;
using HylianGrimoire.Models;
using HylianGrimoire.Services;

namespace HylianGrimoire;

public sealed partial class MainWindow
{
    private void InitializeMajorasMaskMetadataControls()
    {
        MmIconCombo.DisplayMemberPath = nameof(MmMessageIconEntry.Label);
        MmIconCombo.ItemsSource = MmMessageIconCatalog.Items;
    }

    private void SetMajorasMaskMetadataVisibility(Visibility visibility)
    {
        MmIconLabel.Visibility = visibility;
        MmIconCombo.Visibility = visibility;
        MmNextMessageBox.Visibility = visibility;
        MmNextMessageLabel.Visibility = visibility;
        MmFirstPriceBox.Visibility = visibility;
        MmFirstPriceLabel.Visibility = visibility;
        MmSecondPriceBox.Visibility = visibility;
        MmSecondPriceLabel.Visibility = visibility;
        MmUnskippableCheck.Visibility = visibility;
        MmInstantTextCheck.Visibility = visibility;
        MmCenteredCheck.Visibility = visibility;
    }

    private void UpdateMajorasMaskMetadataPanel(MessageEntry? entry)
    {
        if (ActiveGameProfile?.Kind != GameKind.MajorasMask
            || entry?.CodecMetadata is not MajorasMaskMessageMetadata metadata)
        {
            SetMajorasMaskMetadataVisibility(Visibility.Collapsed);
            MmIconCombo.SelectedIndex = -1;
            return;
        }

        SetMajorasMaskMetadataVisibility(Visibility.Visible);
        MajorasMaskMetadataFields fields = MajorasMaskMetadataService.CreateFields(metadata);
        MmIconCombo.SelectedItem = MmMessageIconCatalog.Get(fields.IconId);
        MmNextMessageBox.Text = fields.NextTextId;
        MmFirstPriceBox.Text = fields.FirstChoicePrice;
        MmSecondPriceBox.Text = fields.SecondChoicePrice;
        MmUnskippableCheck.IsChecked = fields.IsUnskippable;
        MmInstantTextCheck.IsChecked = fields.DrawInstantly;
        MmCenteredCheck.IsChecked = fields.IsCentered;
    }

    private void OnMmIconChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating || MmIconCombo.SelectedItem is not MmMessageIconEntry icon)
        {
            return;
        }

        UpdateCurrentMajorasMaskMetadata(metadata => MajorasMaskMetadataService.SetIcon(metadata, icon.Value));
    }

    private void OnMmNextMessageChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating)
        {
            return;
        }

        TryUpdateCurrentMajorasMaskMetadata(metadata =>
            MajorasMaskMetadataService.TrySetNextTextId(metadata, MmNextMessageBox.Text, out MajorasMaskMessageMetadata updated)
                ? updated
                : null);
    }

    private void OnMmFirstPriceChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating)
        {
            return;
        }

        TryUpdateCurrentMajorasMaskMetadata(metadata =>
            MajorasMaskMetadataService.TrySetFirstChoicePrice(metadata, MmFirstPriceBox.Text, out MajorasMaskMessageMetadata updated)
                ? updated
                : null);
    }

    private void OnMmSecondPriceChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating)
        {
            return;
        }

        TryUpdateCurrentMajorasMaskMetadata(metadata =>
            MajorasMaskMetadataService.TrySetSecondChoicePrice(metadata, MmSecondPriceBox.Text, out MajorasMaskMessageMetadata updated)
                ? updated
                : null);
    }

    private void OnMmUnskippableChanged(object sender, RoutedEventArgs e)
    {
        if (_updating)
        {
            return;
        }

        bool isChecked = MmUnskippableCheck.IsChecked == true;
        UpdateCurrentMajorasMaskMetadata(metadata => MajorasMaskMetadataService.SetUnskippable(metadata, isChecked));
        if (MmInstantTextCheck.IsChecked == true && MmUnskippableCheck.IsChecked != true)
        {
            MmUnskippableCheck.IsChecked = true;
        }
    }

    private void OnMmInstantTextChanged(object sender, RoutedEventArgs e)
    {
        if (_updating)
        {
            return;
        }

        bool isChecked = MmInstantTextCheck.IsChecked == true;
        if (isChecked)
        {
            MmUnskippableCheck.IsChecked = true;
        }

        UpdateCurrentMajorasMaskMetadata(metadata => MajorasMaskMetadataService.SetInstantText(metadata, isChecked));
    }

    private void OnMmCenteredChanged(object sender, RoutedEventArgs e)
    {
        if (_updating)
        {
            return;
        }

        UpdateCurrentMajorasMaskMetadata(metadata => MajorasMaskMetadataService.SetCentered(
            metadata,
            MmCenteredCheck.IsChecked == true));
    }

    private void TryUpdateCurrentMajorasMaskMetadata(Func<MajorasMaskMessageMetadata, MajorasMaskMessageMetadata?> update)
    {
        UpdateCurrentMajorasMaskMetadata(metadata => update(metadata) ?? metadata);
    }

    private void UpdateCurrentMajorasMaskMetadata(Func<MajorasMaskMessageMetadata, MajorasMaskMessageMetadata> update)
    {
        if (_session.CurrentIndex < 0
            || _session.CurrentIndex >= _session.Entries.Count
            || _session.Entries[_session.CurrentIndex].CodecMetadata is not MajorasMaskMessageMetadata metadata)
        {
            return;
        }

        MajorasMaskMessageMetadata updated = update(metadata);
        if (updated == metadata)
        {
            return;
        }

        _session.Entries[_session.CurrentIndex].CodecMetadata = updated;
        MarkDirty();
        UpdatePreview();
    }

}
