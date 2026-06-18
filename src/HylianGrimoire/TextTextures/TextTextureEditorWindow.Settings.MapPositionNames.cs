using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private static readonly MapPositionNameTextureRenderSettings DefaultMapPositionNameSettings = new();

    private static readonly MapPositionNameDefaultSpec[] MapPositionNameTextureDefaults =
    [
        new("DeathMountainCraterPositionName", FirstLineWidthScale: 76.5),
        new("DeathMountainTrailPositionName", FirstLineWidthScale: 76.5),
        new("SacredForestMeadowPositionName", FirstLineWidthScale: 87),
    ];

    private void SetMapPositionNameControlsVisible(bool visible)
        => SetVisible(MapPositionLineWidthGrid, visible);

    private void SetMapPositionNameControlsEnabled(bool enabled)
    {
        SetEnabledWithOpacity(MapPositionLine1WidthScaleBox, enabled);
        SetEnabledWithOpacity(MapPositionLine2WidthScaleBox, enabled);
    }

    private void SetMapPositionNameControlsFromSettings()
    {
        _mapPositionNameSettings = GetSelectedMapPositionNameSettings();
        ConfigureNumberBox(MapPositionLine1WidthScaleBox, 65, 120, 0.5, 5, 1);
        ConfigureNumberBox(MapPositionLine2WidthScaleBox, 65, 120, 0.5, 5, 1);
        MapPositionLine1WidthScaleBox.Value = _mapPositionNameSettings.FirstLineWidthScale;
        MapPositionLine2WidthScaleBox.Value = _mapPositionNameSettings.SecondLineWidthScale;
    }

    private void ReadMapPositionNameSettings()
    {
        if (GetSelectedTarget()?.Texture is not { } texture || !MapPositionNameTextureCatalog.IsMapPositionNameTexture(texture))
        {
            return;
        }

        MapPositionNameTextureRenderSettings current = GetMapPositionNameSettings(texture);
        _mapPositionNameSettings = current with
        {
            FirstLineWidthScale = ReadDouble(MapPositionLine1WidthScaleBox, current.FirstLineWidthScale),
            SecondLineWidthScale = ReadDouble(MapPositionLine2WidthScaleBox, current.SecondLineWidthScale),
        };
        _mapPositionNameSettingsByTexture[texture.Name] = _mapPositionNameSettings;
    }

    private MapPositionNameTextureRenderSettings GetSelectedMapPositionNameSettings()
        => GetSelectedTarget()?.Texture is { } texture && MapPositionNameTextureCatalog.IsMapPositionNameTexture(texture)
            ? GetMapPositionNameSettings(texture)
            : DefaultMapPositionNameSettings;

    private MapPositionNameTextureRenderSettings GetMapPositionNameSettings(TextureDefinition texture)
        => _mapPositionNameSettingsByTexture.TryGetValue(texture.Name, out MapPositionNameTextureRenderSettings? settings)
            ? settings
            : GetDefaultMapPositionNameSettings(texture);

    private static MapPositionNameTextureRenderSettings GetDefaultMapPositionNameSettings(TextureDefinition texture)
        => MapPositionNameTextureDefaults.FirstOrDefault(defaults => texture.Name.Contains(defaults.TextureNameFragment, StringComparison.Ordinal)) is { } defaults
            ? DefaultMapPositionNameSettings with
            {
                FirstLineWidthScale = defaults.FirstLineWidthScale,
                SecondLineWidthScale = defaults.SecondLineWidthScale,
            }
            : DefaultMapPositionNameSettings;

    private sealed record MapPositionNameDefaultSpec(
        string TextureNameFragment,
        double FirstLineWidthScale = 100,
        double SecondLineWidthScale = 100);
}
