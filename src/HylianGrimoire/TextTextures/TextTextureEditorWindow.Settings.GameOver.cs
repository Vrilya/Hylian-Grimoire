namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private void SetGameOverControlsVisible(bool visible)
    {
        bool showContinue = visible && IsContinuePlayingTarget();
        bool showGameOver = visible && !showContinue;
        SetVisible(GameOverYBox, visible);
        SetVisible(GameOverTypographyGrid, showGameOver);
        SetVisible(ContinueScaleSettingsGrid, showContinue);
    }

    private void SetGameOverControlsEnabled(bool enabled)
    {
        GameOverYBox.IsEnabled = enabled;
        GameOverFontSizeBox.IsEnabled = enabled;
        GameOverBlurStrengthBox.IsEnabled = enabled;
        ContinueWidthScaleBox.IsEnabled = enabled;
        ContinueHeightScaleBox.IsEnabled = enabled;
    }

    private void SetGameOverControlsFromSettings()
    {
        if (IsContinuePlayingTarget())
        {
            ConfigureContinuePlayingSharedControls();
            ConfigureContinuePlayingScaleControls();
            ConfigureHorizontalPositionBox(
                HorizontalPositionMinimum,
                HorizontalPositionMaximum,
                PositionHeader,
                _continuePlayingSettings.Center
                    ? ContinuePlayingTextureRenderSettings.CenteredXNudge
                    : _continuePlayingSettings.XNudge,
                smallChange: 0.1,
                largeChange: 1);
            CenterCheck.Content = "Center X";
            CenterCheck.IsChecked = _continuePlayingSettings.Center;
            GameOverYBox.Value = _continuePlayingSettings.YNudge;
            ContinueWidthScaleBox.Value = _continuePlayingSettings.WidthScale;
            ContinueHeightScaleBox.Value = _continuePlayingSettings.HeightScale;
            return;
        }

        ConfigureGameOverSharedControls();
        ConfigureStandardHorizontalPositionBox(_gameOverSettings.Center
            ? GameOverTextureRenderSettings.CenteredXNudge
            : _gameOverSettings.XNudge);
        CenterCheck.IsChecked = _gameOverSettings.Center;
        GameOverYBox.Value = _gameOverSettings.Center
            ? GameOverTextureRenderSettings.DefaultY
            : _gameOverSettings.Y;
        GameOverFontSizeBox.Value = _gameOverSettings.FontSize;
        GameOverBlurStrengthBox.Value = _gameOverSettings.BlurStrength;
    }

    private void ReadGameOverSettings()
    {
        bool center = CenterCheck.IsChecked == true;
        if (IsContinuePlayingTarget())
        {
            _continuePlayingSettings = _continuePlayingSettings with
            {
                Center = center,
                XNudge = center ? ContinuePlayingTextureRenderSettings.CenteredXNudge : ReadDouble(XNudgeBox, _continuePlayingSettings.XNudge),
                YNudge = ReadDouble(GameOverYBox, _continuePlayingSettings.YNudge),
                WidthScale = ReadDouble(ContinueWidthScaleBox, _continuePlayingSettings.WidthScale),
                HeightScale = ReadDouble(ContinueHeightScaleBox, _continuePlayingSettings.HeightScale),
            };
            return;
        }

        _gameOverSettings = _gameOverSettings with
        {
            Center = center,
            XNudge = center ? GameOverTextureRenderSettings.CenteredXNudge : ReadDouble(XNudgeBox, _gameOverSettings.XNudge),
            Y = center ? GameOverTextureRenderSettings.DefaultY : ReadInt(GameOverYBox, _gameOverSettings.Y),
            FontSize = ReadDouble(GameOverFontSizeBox, _gameOverSettings.FontSize),
            BlurStrength = ReadInt(GameOverBlurStrengthBox, _gameOverSettings.BlurStrength),
        };
    }

    private void UpdateGameOverPositionControlState(bool hasTarget)
    {
        bool isContinuePlaying = IsContinuePlayingTarget();
        bool canPosition = hasTarget && CenterCheck.IsChecked != true;
        SetEnabledWithOpacity(GameOverYBox, isContinuePlaying ? hasTarget : canPosition);
        SetEnabledWithOpacity(GameOverFontSizeBox, hasTarget);
        SetEnabledWithOpacity(GameOverBlurStrengthBox, hasTarget);
        SetEnabledWithOpacity(ContinueWidthScaleBox, hasTarget);
        SetEnabledWithOpacity(ContinueHeightScaleBox, hasTarget);
    }

    private bool IsContinuePlayingTarget()
        => GetSelectedTarget()?.GameOverTarget?.Spec.Kind == GameOverTextureTargetKind.ContinuePlaying;

    private void ConfigureContinuePlayingSharedControls()
    {
        ConfigureNumberBox(GameOverYBox, -6, 6, 0.1, 1, 1);
        ConfigureNumberBox(GameOverFontSizeBox, 8, 22, 0.1, 1, 1);
        ConfigureNumberBox(GameOverBlurStrengthBox, 0, 400, 1, 10, 0);
    }

    private void ConfigureGameOverSharedControls()
    {
        ConfigureNumberBox(GameOverYBox, -4, 12, 1, 5, 0);
        ConfigureNumberBox(GameOverFontSizeBox, 18, 42, 0.1, 1, 1);
        ConfigureNumberBox(GameOverBlurStrengthBox, 0, 180, 1, 5, 0);
    }

    private void ConfigureContinuePlayingScaleControls()
    {
        ConfigureNumberBox(ContinueWidthScaleBox, 65, 120, 0.5, 5, 1);
        ConfigureNumberBox(ContinueHeightScaleBox, 65, 120, 0.5, 5, 1);
    }

    private static void ConfigureNumberBox(
        Microsoft.UI.Xaml.Controls.NumberBox box,
        double minimum,
        double maximum,
        double smallChange,
        double largeChange,
        int fractionDigits)
    {
        box.Minimum = minimum;
        box.Maximum = maximum;
        box.SmallChange = smallChange;
        box.LargeChange = largeChange;
        ConfigureNumberFormatter(box, fractionDigits);
    }
}
