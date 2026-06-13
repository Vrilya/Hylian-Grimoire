namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private void SetItemControlsVisible(bool visible)
        => SetVisible(ItemWidthScaleBox, visible);

    private void SetItemControlsEnabled(bool enabled)
        => ItemWidthScaleBox.IsEnabled = enabled;

    private void SetItemControlsFromSettings()
    {
        ConfigureStandardHorizontalPositionBox(_itemSettings.XNudge);
        CenterCheck.IsChecked = _itemSettings.Center;
        ItemWidthScaleBox.Value = _itemSettings.HorizontalScale;
    }

    private ItemNameTextureRenderSettings ReadItemSettings()
        => _itemSettings with
        {
            Center = CenterCheck.IsChecked == true,
            XNudge = CenterCheck.IsChecked == true ? DefaultHorizontalPosition : ReadInt(XNudgeBox, DefaultHorizontalPosition),
            HorizontalScale = ReadInt(ItemWidthScaleBox, _itemSettings.HorizontalScale),
            FitToWidth = false,
            MaxWidth = ItemNameTextureCatalog.Width,
        };

    private void UpdateItemPositionControlState(bool hasTarget)
        => SetEnabledWithOpacity(ItemWidthScaleBox, hasTarget);
}
