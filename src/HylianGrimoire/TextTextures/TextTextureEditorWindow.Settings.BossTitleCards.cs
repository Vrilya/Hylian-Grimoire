namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private void SetBossTitleCardControlsVisible(bool visible)
    {
        SetVisible(BossTextPanel, visible);
        SetVisible(BossPositionGrid, visible);
    }

    private void SetBossTitleCardControlsEnabled(bool enabled)
    {
        BossTopTextBox.IsEnabled = enabled;
        BossBottomTextBox.IsEnabled = enabled;
        BossTopCenterCheck.IsEnabled = enabled;
        BossBottomCenterCheck.IsEnabled = enabled;
        BossSizeBox.IsEnabled = enabled;
        BossWidthScaleBox.IsEnabled = enabled;
    }

    private void SetBossTitleCardControlsFromSettings()
    {
        ConfigureStandardHorizontalPositionBox(_bossSettings.XNudge);
        CenterCheck.IsChecked = _bossSettings.Center;
        BossTopCenterCheck.IsChecked = _bossSettings.TopCenter;
        BossBottomCenterCheck.IsChecked = _bossSettings.BottomCenter;
        BossTopXBox.Value = _bossSettings.TopXNudge;
        BossBottomXBox.Value = _bossSettings.BottomXNudge;
        BossSizeBox.Value = _bossSettings.BottomFontSize;
        BossWidthScaleBox.Value = _bossSettings.BottomWidthScale;
    }

    private BossTitleCardTextureRenderSettings ReadBossSettings()
    {
        bool topCenter = BossTopCenterCheck.IsChecked == true;
        bool bottomCenter = BossBottomCenterCheck.IsChecked == true;
        return _bossSettings with
        {
            Center = topCenter && bottomCenter,
            XNudge = DefaultHorizontalPosition,
            TopCenter = topCenter,
            TopXNudge = ReadInt(BossTopXBox, _bossSettings.TopXNudge),
            BottomCenter = bottomCenter,
            BottomXNudge = ReadInt(BossBottomXBox, _bossSettings.BottomXNudge),
            BottomFontSize = ReadDouble(BossSizeBox, _bossSettings.BottomFontSize),
            BottomWidthScale = ReadDouble(BossWidthScaleBox, _bossSettings.BottomWidthScale),
        };
    }

    private void UpdateBossTitleCardPositionControlState(bool hasTarget)
    {
        SetEnabledWithOpacity(BossTopXBox, hasTarget && BossTopCenterCheck.IsChecked != true);
        SetEnabledWithOpacity(BossBottomXBox, hasTarget && BossBottomCenterCheck.IsChecked != true);
        SetEnabledWithOpacity(BossSizeBox, hasTarget);
        SetEnabledWithOpacity(BossWidthScaleBox, hasTarget);
    }
}
