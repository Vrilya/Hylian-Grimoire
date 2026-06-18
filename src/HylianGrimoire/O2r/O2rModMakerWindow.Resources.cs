using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow
{
    private void OnTextureTreeExpanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        if (GetFolderItem(args.Node) is not { } folder || folder.IsPopulated)
        {
            return;
        }

        PopulateExpandedTextureFolder(args.Node, folder);
    }

    private void OnTextureSelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs e)
        => RefreshSelectedDetails();

    private void OnResourceViewToggled(object sender, RoutedEventArgs e)
    {
        if (_updatingResourceView)
        {
            return;
        }

        _resourceViewMode = ReferenceEquals(sender, RomViewButton) ? ResourceViewMode.Rom : ResourceViewMode.Mod;
        UpdateResourceViewButtons();
        PopulateTextureTree();
    }

    private void OnIncludeTextChanged(object sender, RoutedEventArgs e)
    {
        if (_updatingIncludeChecks)
        {
            return;
        }

        RefreshTextCheckStates();
        SetEnabled(
            _textResources.Count > 0 || GetTextureResourceCount() > 0 || _existingEntries.Count > 0,
            hasTextureResources: GetTextureResourceCount() > 0);
    }

    private void OnIncludeTexturesChanged(object sender, RoutedEventArgs e)
    {
        if (_updatingIncludeChecks)
        {
            return;
        }

        TextureTree.IsEnabled = GetTextureResourceCount() > 0 && IncludeTexturesCheck.IsChecked == true;
        UpdateResourceViewButtons();
    }

    private void OnTreeItemCheckClicked(object sender, RoutedEventArgs e)
    {
        if (_updatingChecks || sender is not CheckBox checkBox)
        {
            return;
        }

        if (!TryApplyTreeItemSelection(checkBox, out bool shouldSelect))
        {
            return;
        }

        if (_resourceViewMode == ResourceViewMode.Mod && !shouldSelect)
        {
            PopulateTextureTree();
        }
        else
        {
            RefreshVisibleCheckStates();
        }

        MarkWorkspaceChanged();
        UpdateWorkspaceSummary();
        RefreshSelectedDetails();
    }
}
