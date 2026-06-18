namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private void SetPlaceTitleCardControlsVisible(bool visible)
    {
        SetVisible(PlaceOutlineAlphaBox, visible);
        SetVisible(PlaceScaleSettingsGrid, visible);
    }

    private void SetPlaceTitleCardControlsEnabled(bool enabled)
    {
        PlaceOutlineAlphaBox.IsEnabled = enabled;
        PlaceWidthScaleBox.IsEnabled = enabled;
        PlaceHeightScaleBox.IsEnabled = enabled;
    }

    private void SetPlaceTitleCardControlsFromSettings()
    {
        ConfigureStandardHorizontalPositionBox(_placeSettings.XNudge);
        CenterCheck.IsChecked = _placeSettings.Center;
        PlaceOutlineAlphaBox.Value = _placeSettings.StrokeAlpha;
        PlaceWidthScaleBox.Value = _placeSettings.HorizontalScale;
        PlaceHeightScaleBox.Value = _placeSettings.HeightScale;
    }

    private PlaceTitleCardTextureRenderSettings ReadPlaceSettings()
        => _placeSettings with
        {
            Center = CenterCheck.IsChecked == true,
            XNudge = CenterCheck.IsChecked == true ? DefaultHorizontalPosition : ReadInt(XNudgeBox, DefaultHorizontalPosition),
            StrokeAlpha = ReadInt(PlaceOutlineAlphaBox, _placeSettings.StrokeAlpha),
            HorizontalScale = ReadDouble(PlaceWidthScaleBox, _placeSettings.HorizontalScale),
            HeightScale = ReadDouble(PlaceHeightScaleBox, _placeSettings.HeightScale),
        };

    private void UpdatePlaceTitleCardPositionControlState(bool hasTarget)
    {
        SetEnabledWithOpacity(PlaceOutlineAlphaBox, hasTarget);
        SetEnabledWithOpacity(PlaceWidthScaleBox, hasTarget);
        SetEnabledWithOpacity(PlaceHeightScaleBox, hasTarget);
    }
}
