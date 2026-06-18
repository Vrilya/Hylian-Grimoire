using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private const double CompactTextStrokeWidth = 3.7;
    private const int CompactTextStrokeAlpha = 150;
    private const int CompactTextStrokeThreshold = 6;
    private const double CompactTextStrokeBlurRadius = 1.15;
    private const int CompactTextStrokeBlurStrength = 115;

    private void SetCompactTextControlsVisible(bool showPosition, bool showFontSize, bool showWidthScale)
    {
        SetVisible(CompactTextYBox, showPosition);
        SetVisible(CompactTextSettingsGrid, showFontSize || showWidthScale);
        SetVisible(CompactTextFontSizeBox, showFontSize);
        SetVisible(CompactTextWidthScaleBox, showWidthScale);
        Grid.SetColumn(CompactTextWidthScaleBox, showFontSize ? 1 : 0);
        Grid.SetColumnSpan(CompactTextWidthScaleBox, showFontSize ? 1 : 2);
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
        SetEnabledWithOpacity(CompactTextFontSizeBox, hasTarget && UsesCompactTextFontSizeControl());
        SetEnabledWithOpacity(CompactTextWidthScaleBox, hasTarget && UsesCompactTextWidthScaleControl());
    }

    private bool UsesCompactTextPositionControl()
        => UsesCompactTextFontSizeControl()
            || UsesCompactTextWidthScaleControl()
            || _selectedTextureKind is TextTextureKind.DungeonMapNames
                or TextTextureKind.FileSelect;

    private bool UsesCompactTextFontSizeControl()
        => (_selectedTextureKind == TextTextureKind.PausePrompts && IsChoicePromptTarget())
            || _selectedTextureKind == TextTextureKind.FileSelect;

    private bool UsesCompactTextWidthScaleControl()
        => (_selectedTextureKind == TextTextureKind.PausePrompts && IsChoicePromptTarget())
            || _selectedTextureKind == TextTextureKind.FileSelect;
}
