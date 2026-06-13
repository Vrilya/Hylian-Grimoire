using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private static readonly CompactTextUiSettings DefaultChoicePromptSettings = new(13.5, 100.0, 1);
    private static readonly CompactTextUiSettings DefaultSavePromptSettings = new(11.8, 102.5, 1);
    private static readonly CompactTextUiSettings DefaultSaveConfirmationSettings = new(11.8, 102.5, 1);

    private void SetPromptControlsFromSettings()
    {
        ConfigureStandardHorizontalPositionBox(_promptSettings.XNudge);
        ConfigureNumberBox(CompactTextYBox, -6, 6, 1, 1, 0);
        ConfigureNumberBox(CompactTextFontSizeBox, 8, 18, 0.1, 1, 1);
        ConfigureNumberBox(CompactTextWidthScaleBox, 65, 120, 0.5, 5, 1);
        CompactTextUiSettings choiceSettings = GetChoicePromptControlValues();
        CenterCheck.Content = IsChoicePromptTarget() ? "Center X" : "Center";
        CenterCheck.IsChecked = _promptSettings.Center;
        CompactTextYBox.Value = choiceSettings.YOffset;
        CompactTextFontSizeBox.Value = choiceSettings.FontSize;
        CompactTextWidthScaleBox.Value = choiceSettings.WidthScale;
    }

    private CompactTextTextureRenderSettings ReadPromptSettings()
    {
        if (GetSelectedTarget()?.Texture is { } texture && PausePromptTextureCatalog.IsChoicePromptTexture(texture))
        {
            ReadChoicePromptControls(texture);
        }

        return _promptSettings with
        {
            Center = CenterCheck.IsChecked == true,
            XNudge = CenterCheck.IsChecked == true ? DefaultHorizontalPosition : ReadInt(XNudgeBox, DefaultHorizontalPosition),
            FitToWidth = true,
            MaxWidth = GetSelectedTarget()?.Texture?.Width ?? PausePromptTextureCatalog.MaxWidth,
        };
    }

    private CompactTextTextureRenderSettings GetEffectivePromptSettings(TextureDefinition texture)
    {
        CompactTextTextureRenderSettings settings = _promptSettings with
        {
            FitToWidth = true,
            MaxWidth = texture.Width,
        };

        if (!PausePromptTextureCatalog.IsChoicePromptTexture(texture))
        {
            return settings;
        }

        return ApplyCompactTextSettings(settings, GetChoicePromptSettings(texture));
    }

    private CompactTextUiSettings GetChoicePromptControlValues()
        => GetSelectedTarget()?.Texture is { } texture
            ? GetChoicePromptSettings(texture)
            : DefaultChoicePromptSettings;

    private CompactTextUiSettings GetChoicePromptSettings(TextureDefinition texture)
        => _pausePromptChoiceSettings.TryGetValue(texture.Name, out CompactTextUiSettings? settings)
            ? settings
            : GetDefaultChoicePromptSettings(texture);

    private static CompactTextUiSettings GetDefaultChoicePromptSettings(TextureDefinition texture)
    {
        if (PausePromptTextureCatalog.IsSaveConfirmationTexture(texture))
        {
            return DefaultSaveConfirmationSettings;
        }

        return PausePromptTextureCatalog.IsSavePromptTexture(texture)
            ? DefaultSavePromptSettings
            : DefaultChoicePromptSettings;
    }

    private void ReadChoicePromptControls(TextureDefinition texture)
    {
        CompactTextUiSettings current = GetChoicePromptSettings(texture);
        _pausePromptChoiceSettings[texture.Name] = current with
        {
            FontSize = ReadDouble(CompactTextFontSizeBox, current.FontSize),
            WidthScale = ReadDouble(CompactTextWidthScaleBox, current.WidthScale),
            YOffset = ReadInt(CompactTextYBox, current.YOffset),
        };
    }

    private bool IsChoicePromptTarget()
        => GetSelectedTarget()?.Texture is { } texture && PausePromptTextureCatalog.IsChoicePromptTexture(texture);
}
