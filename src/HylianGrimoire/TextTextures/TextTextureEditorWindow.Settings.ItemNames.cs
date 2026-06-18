using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private const int DefaultItemScale = 100;
    private const int DefaultMapNameScale = 94;

    private void SetItemControlsVisible(bool visible)
        => SetVisible(ItemWidthScaleBox, visible);

    private void SetItemControlsEnabled(bool enabled)
        => ItemWidthScaleBox.IsEnabled = enabled;

    private void SetItemControlsFromSettings()
    {
        ItemStyleControlSettings settings = GetSelectedItemStyleControlSettings();

        ConfigureStandardHorizontalPositionBox(settings.Center ? DefaultHorizontalPosition : settings.XNudge);
        CenterCheck.IsChecked = settings.Center;
        ItemWidthScaleBox.Value = settings.HorizontalScale;
    }

    private void ReadItemSettings()
    {
        if (TryReadMajorasMaskPausePromptSettings())
        {
            return;
        }

        ItemNameTextureRenderSettings current = GetCurrentItemStyleSettings();
        SetCurrentItemStyleSettings(current with
        {
            Center = CenterCheck.IsChecked == true,
            XNudge = CenterCheck.IsChecked == true ? DefaultHorizontalPosition : ReadInt(XNudgeBox, DefaultHorizontalPosition),
            HorizontalScale = ReadInt(ItemWidthScaleBox, current.HorizontalScale),
            VerticalScale = DefaultItemScale,
            FitToWidth = true,
            MaxWidth = GetSelectedTarget()?.Texture?.Width ?? ItemNameTextureCatalog.Width,
        });
    }

    private void UpdateItemPositionControlState(bool hasTarget)
        => SetEnabledWithOpacity(ItemWidthScaleBox, hasTarget);

    private ItemNameTextureRenderSettings GetEffectiveItemStyleSettingsForTexture(TextureDefinition texture)
    {
        if (_selectedTextureKind == TextTextureKind.MajorasMaskPausePrompts)
        {
            return GetEffectiveMajorasMaskPausePromptItemSettings(texture, _majorasMaskPausePromptBaseSettings);
        }

        return GetCurrentItemStyleSettings() with
        {
            FitToWidth = true,
            MaxWidth = texture.Width,
        };
    }

    private ItemStyleControlSettings GetSelectedItemStyleControlSettings()
    {
        if (GetSelectedMajorasMaskPausePromptControlSettingsOrNull() is { } promptSettings)
        {
            return promptSettings;
        }

        ItemNameTextureRenderSettings settings = GetCurrentItemStyleSettings();
        return new ItemStyleControlSettings(settings.Center, settings.XNudge, settings.HorizontalScale);
    }

    private ItemNameTextureRenderSettings GetCurrentItemStyleSettings()
        => _selectedTextureKind switch
        {
            TextTextureKind.MapNames => _mapNameSettings,
            TextTextureKind.MajorasMaskItemNames => _majorasMaskItemSettings,
            TextTextureKind.MajorasMaskMapNames => _majorasMaskMapNameSettings,
            TextTextureKind.MajorasMaskPausePrompts => _majorasMaskPausePromptBaseSettings,
            _ => _itemSettings,
        };

    private void SetCurrentItemStyleSettings(ItemNameTextureRenderSettings settings)
    {
        switch (_selectedTextureKind)
        {
            case TextTextureKind.MapNames:
                _mapNameSettings = settings;
                break;
            case TextTextureKind.MajorasMaskItemNames:
                _majorasMaskItemSettings = settings;
                break;
            case TextTextureKind.MajorasMaskMapNames:
                _majorasMaskMapNameSettings = settings;
                break;
            case TextTextureKind.MajorasMaskPausePrompts:
                _majorasMaskPausePromptBaseSettings = settings;
                break;
            default:
                _itemSettings = settings;
                break;
        }
    }

    private sealed record ItemStyleControlSettings(bool Center, int XNudge, int HorizontalScale);
}
