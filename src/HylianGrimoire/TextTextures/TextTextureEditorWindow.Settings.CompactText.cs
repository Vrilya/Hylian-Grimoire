namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private const double CompactTextStrokeWidth = 3.7;
    private const int CompactTextStrokeAlpha = 150;
    private const int CompactTextStrokeThreshold = 6;
    private const double CompactTextStrokeBlurRadius = 1.15;
    private const int CompactTextStrokeBlurStrength = 115;

    private void SetCompactTextControlsVisible(bool showPosition, bool showTypography)
    {
        SetVisible(CompactTextYBox, showPosition);
        SetVisible(CompactTextSettingsGrid, showTypography);
    }

    private void SetCompactTextControlsEnabled(bool enabled)
    {
        CompactTextYBox.IsEnabled = enabled;
        CompactTextFontSizeBox.IsEnabled = enabled;
        CompactTextWidthScaleBox.IsEnabled = enabled;
    }

    private static CompactTextTextureRenderSettings ApplyCompactTextSettings(
        CompactTextTextureRenderSettings settings,
        CompactTextUiSettings compactSettings)
        => settings with
        {
            FontSize = compactSettings.FontSize,
            HorizontalScale = compactSettings.WidthScale,
            StrokeWidth = CompactTextStrokeWidth,
            StrokeAlpha = CompactTextStrokeAlpha,
            StrokeThreshold = CompactTextStrokeThreshold,
            StrokeBlurRadius = CompactTextStrokeBlurRadius,
            StrokeBlurStrength = CompactTextStrokeBlurStrength,
            YOffset = compactSettings.YOffset,
        };

    private void UpdateCompactTextPositionControlState(bool hasTarget)
    {
        SetEnabledWithOpacity(CompactTextYBox, hasTarget && UsesCompactTextPositionControl());
        SetEnabledWithOpacity(CompactTextFontSizeBox, hasTarget && UsesCompactTextTypographyControls());
        SetEnabledWithOpacity(CompactTextWidthScaleBox, hasTarget && UsesCompactTextTypographyControls());
    }

    private bool UsesCompactTextPositionControl()
        => UsesCompactTextTypographyControls()
            || _selectedTextureKind is TextTextureKind.DungeonMapNames or TextTextureKind.FileSelect;

    private bool UsesCompactTextTypographyControls()
        => (_selectedTextureKind == TextTextureKind.PausePrompts && IsChoicePromptTarget())
            || _selectedTextureKind == TextTextureKind.FileSelect;
}
