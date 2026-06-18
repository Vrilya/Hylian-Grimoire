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
            ContinuePlayingTextureRenderSettings settings = GetCurrentContinuePlayingSettings();
            ConfigureContinuePlayingSharedControls();
            ConfigureContinuePlayingScaleControls();
            ConfigureHorizontalPositionBox(
                HorizontalPositionMinimum,
                HorizontalPositionMaximum,
                PositionHeader,
                settings.Center
                    ? ContinuePlayingTextureRenderSettings.CenteredXNudge
                    : settings.XNudge,
                smallChange: 0.1,
                largeChange: 1);
            CenterCheck.Content = "Center X";
            CenterCheck.IsChecked = settings.Center;
            GameOverYBox.Value = settings.YNudge;
            ContinueWidthScaleBox.Value = settings.WidthScale;
            ContinueHeightScaleBox.Value = settings.HeightScale;
            return;
        }

        GameOverTextureRenderSettings gameOverSettings = GetCurrentGameOverSettings();
        ConfigureGameOverSharedControls();
        ConfigureStandardHorizontalPositionBox(gameOverSettings.Center
            ? GameOverTextureRenderSettings.CenteredXNudge
            : gameOverSettings.XNudge);
        CenterCheck.IsChecked = gameOverSettings.Center;
        GameOverYBox.Value = gameOverSettings.Center
            ? GameOverTextureRenderSettings.DefaultY
            : gameOverSettings.Y;
        GameOverFontSizeBox.Value = gameOverSettings.FontSize;
        GameOverBlurStrengthBox.Value = gameOverSettings.BlurStrength;
    }

    private void ReadGameOverSettings()
    {
        bool center = CenterCheck.IsChecked == true;
        if (IsContinuePlayingTarget())
        {
            ContinuePlayingTextureRenderSettings settings = GetCurrentContinuePlayingSettings();
            SetCurrentContinuePlayingSettings(settings with
            {
                Center = center,
                XNudge = center ? ContinuePlayingTextureRenderSettings.CenteredXNudge : ReadDouble(XNudgeBox, settings.XNudge),
                YNudge = ReadDouble(GameOverYBox, settings.YNudge),
                WidthScale = ReadDouble(ContinueWidthScaleBox, settings.WidthScale),
                HeightScale = ReadDouble(ContinueHeightScaleBox, settings.HeightScale),
            });
            return;
        }

        GameOverTextureRenderSettings gameOverSettings = GetCurrentGameOverSettings();
        SetCurrentGameOverSettings(gameOverSettings with
        {
            Center = center,
            XNudge = center ? GameOverTextureRenderSettings.CenteredXNudge : ReadDouble(XNudgeBox, gameOverSettings.XNudge),
            Y = center ? GameOverTextureRenderSettings.DefaultY : ReadInt(GameOverYBox, gameOverSettings.Y),
            FontSize = ReadDouble(GameOverFontSizeBox, gameOverSettings.FontSize),
            BlurStrength = ReadInt(GameOverBlurStrengthBox, gameOverSettings.BlurStrength),
        });
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

    private GameOverTextureRenderSettings GetCurrentGameOverSettings()
        => _selectedTextureKind == TextTextureKind.MajorasMaskGameOver
            ? _majorasMaskGameOverSettings
            : _gameOverSettings;

    private void SetCurrentGameOverSettings(GameOverTextureRenderSettings settings)
    {
        if (_selectedTextureKind == TextTextureKind.MajorasMaskGameOver)
        {
            _majorasMaskGameOverSettings = settings;
            return;
        }

        _gameOverSettings = settings;
    }

    private ContinuePlayingTextureRenderSettings GetCurrentContinuePlayingSettings()
        => _selectedTextureKind == TextTextureKind.MajorasMaskGameOver
            ? _majorasMaskContinuePlayingSettings
            : _continuePlayingSettings;

    private void SetCurrentContinuePlayingSettings(ContinuePlayingTextureRenderSettings settings)
    {
        if (_selectedTextureKind == TextTextureKind.MajorasMaskGameOver)
        {
            _majorasMaskContinuePlayingSettings = settings;
            return;
        }

        _continuePlayingSettings = settings;
    }

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
