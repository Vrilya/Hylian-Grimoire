using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private static readonly CompactTextUiSettings DefaultChoicePromptSettings = new(13.5, 100.0, 1);
    private static readonly CompactTextUiSettings DefaultSavePromptSettings = new(11.8, 102.5, 1);
    private static readonly CompactTextUiSettings DefaultSaveConfirmationSettings = new(11.8, 102.5, 1);

    private readonly Dictionary<string, PausePromptControlSettings> _pausePromptControlSettings = new(StringComparer.Ordinal);

    private void SetPromptControlsFromSettings()
    {
        PausePromptControlSettings controlSettings = GetSelectedPromptControlSettings();
        ConfigureStandardHorizontalPositionBox(controlSettings.Center ? DefaultHorizontalPosition : controlSettings.XNudge);
        ConfigureNumberBox(CompactTextYBox, -6, 6, 1, 1, 0);
        ConfigureNumberBox(CompactTextFontSizeBox, 8, 18, 0.1, 1, 1);
        ConfigureNumberBox(CompactTextWidthScaleBox, 65, 120, 0.5, 5, 1);
        CompactTextUiSettings choiceSettings = GetChoicePromptControlValues();
        CenterCheck.Content = IsChoicePromptTarget() ? "Center X" : "Center";
        CenterCheck.IsChecked = controlSettings.Center;
        CompactTextYBox.Value = choiceSettings.YOffset;
        CompactTextFontSizeBox.Value = choiceSettings.FontSize;
        CompactTextWidthScaleBox.Value = choiceSettings.WidthScale;
    }

    private CompactTextTextureRenderSettings ReadPromptSettings()
    {
        TextureDefinition? texture = GetSelectedTarget()?.Texture;
        if (texture is null)
        {
            return _promptSettings with
            {
                FitToWidth = true,
                MaxWidth = PausePromptTextureCatalog.MaxWidth,
            };
        }

        if (!IsChoicePromptTexture(texture))
        {
            ReadStandardPromptControls(texture);
            return _promptSettings;
        }

        ReadChoicePromptControls(texture);

        return _promptSettings with
        {
            Center = CenterCheck.IsChecked == true,
            XNudge = CenterCheck.IsChecked == true ? DefaultHorizontalPosition : ReadInt(XNudgeBox, DefaultHorizontalPosition),
            FitToWidth = true,
            MaxWidth = texture.Width,
        };
    }

    private CompactTextTextureRenderSettings GetEffectivePromptSettings(TextureDefinition texture)
    {
        CompactTextTextureRenderSettings settings = _promptSettings with
        {
            FitToWidth = true,
            MaxWidth = texture.Width,
        };

        if (!IsChoicePromptTexture(texture))
        {
            return ApplyPromptControlSettings(settings, GetPromptControlSettings(texture));
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

    private void ReadStandardPromptControls(TextureDefinition texture)
    {
        PausePromptControlSettings current = GetPromptControlSettings(texture);
        bool center = CenterCheck.IsChecked == true;
        _pausePromptControlSettings[texture.Name] = current with
        {
            Center = center,
            XNudge = center ? DefaultHorizontalPosition : ReadInt(XNudgeBox, current.XNudge),
        };
    }

    private PausePromptControlSettings GetSelectedPromptControlSettings()
        => GetSelectedTarget()?.Texture is { } texture
            ? GetPromptControlSettings(texture)
            : new PausePromptControlSettings(_promptSettings.Center, _promptSettings.XNudge);

    private PausePromptControlSettings GetPromptControlSettings(TextureDefinition texture)
    {
        if (IsChoicePromptTexture(texture))
        {
            return new PausePromptControlSettings(_promptSettings.Center, _promptSettings.XNudge);
        }

        return _pausePromptControlSettings.TryGetValue(texture.Name, out PausePromptControlSettings? settings)
            ? settings
            : GetDefaultStandardPromptControlSettings(texture.Name);
    }

    private PausePromptControlSettings GetDefaultStandardPromptControlSettings(string textureName)
        => textureName switch
        {
            "gPauseToDecideENGTex" => new PausePromptControlSettings(Center: false, XNudge: 0),
            "gPauseToEquipENGTex" => new PausePromptControlSettings(Center: false, XNudge: -1),
            "gPauseToEquipmentENGTex" => new PausePromptControlSettings(Center: true, XNudge: DefaultHorizontalPosition),
            _ => new PausePromptControlSettings(_promptSettings.Center, _promptSettings.XNudge),
        };

    private static CompactTextTextureRenderSettings ApplyPromptControlSettings(
        CompactTextTextureRenderSettings settings,
        PausePromptControlSettings controlSettings)
        => settings with
        {
            Center = controlSettings.Center,
            XNudge = controlSettings.XNudge,
        };

    private bool IsChoicePromptTarget()
        => GetSelectedTarget()?.Texture is { } texture && IsChoicePromptTexture(texture);

    private static bool IsChoicePromptTexture(TextureDefinition texture)
        => PausePromptTextureCatalog.IsChoicePromptTexture(texture);

    private sealed record PausePromptControlSettings(bool Center, int XNudge);
}
