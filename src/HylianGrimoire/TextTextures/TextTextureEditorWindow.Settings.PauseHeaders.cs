namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private const int PauseHeaderCenteredX = 121;
    private const string PauseHeaderCenterPositionHeader = "Center X";

    private void SetPauseHeaderControlsVisible(bool visible)
    {
        SetVisible(PauseOriginalColorsCheck, visible);
        SetVisible(PauseHighlightGrayBox, visible);
        SetVisible(PauseHighlightOffsetGrid, visible);
    }

    private void SetPauseHeaderControlsEnabled(bool enabled)
    {
        PauseOriginalColorsCheck.IsEnabled = enabled;
        PauseHighlightGrayBox.IsEnabled = enabled;
        PauseHighlightXBox.IsEnabled = enabled;
        PauseHighlightYBox.IsEnabled = enabled;
    }

    private void SetPauseHeaderControlsFromSettings()
    {
        ConfigureHorizontalPositionBox(
            minimum: 0,
            maximum: PauseHeaderTextureCatalog.Width,
            header: PauseHeaderCenterPositionHeader,
            value: _pauseSettings.Center ? _pauseSettings.CenterX : _pauseSettings.XNudge);
        CenterCheck.IsChecked = _pauseSettings.Center;
        PauseOriginalColorsCheck.IsChecked = _showPauseOriginalColors;
        PauseHighlightGrayBox.Value = _pauseSettings.HighlightGray;
        PauseHighlightXBox.Value = _pauseSettings.HighlightDx;
        PauseHighlightYBox.Value = _pauseSettings.HighlightDy;
    }

    private PauseHeaderTextureRenderSettings ReadPauseSettings()
    {
        bool center = CenterCheck.IsChecked == true;
        int x = ReadInt(XNudgeBox, (int)Math.Round(_pauseSettings.CenterX));
        return _pauseSettings with
        {
            Center = center,
            CenterX = center ? PauseHeaderCenteredX : x,
            XNudge = center ? DefaultHorizontalPosition : x,
            HighlightGray = ReadInt(PauseHighlightGrayBox, _pauseSettings.HighlightGray),
            HighlightDx = ReadInt(PauseHighlightXBox, _pauseSettings.HighlightDx),
            HighlightDy = ReadInt(PauseHighlightYBox, _pauseSettings.HighlightDy),
        };
    }

    private void UpdatePauseHeaderPositionControlState(bool hasTarget)
    {
        SetEnabledWithOpacity(PauseHighlightXBox, hasTarget);
        SetEnabledWithOpacity(PauseHighlightYBox, hasTarget);
    }
}
