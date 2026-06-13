using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private const double DungeonMapNameDefaultOutlineWidth = 2.7;
    private const int DungeonMapNameDefaultOutlineAlpha = 95;
    private const int DungeonMapNameDefaultFillBoost = 120;
    private const int DungeonMapNameWhiteThreshold = 255;

    private static readonly DungeonMapNameUiSettings DefaultDungeonMapNameSettings = new(
        Center: true,
        XNudge: DefaultHorizontalPosition,
        Text: new CompactTextUiSettings(12.8, 83.5, 0),
        FillBoost: DungeonMapNameDefaultFillBoost);

    private void SetDungeonMapNameControlsFromSettings()
    {
        DungeonMapNameUiSettings settings = GetSelectedDungeonMapNameSettings();
        ConfigureStandardHorizontalPositionBox(settings.Center ? DefaultHorizontalPosition : settings.XNudge);
        ConfigureNumberBox(CompactTextYBox, -6, 6, 1, 1, 0);
        CenterCheck.Content = "Center X";
        CenterCheck.IsChecked = settings.Center;
        CompactTextYBox.Value = settings.Text.YOffset;
    }

    private void ReadDungeonMapNameSettings()
    {
        if (GetSelectedTarget()?.Texture is not { } texture || !DungeonMapNameTextureCatalog.IsDungeonMapNameTexture(texture))
        {
            return;
        }

        DungeonMapNameUiSettings current = GetDungeonMapNameSettings(texture);
        bool center = CenterCheck.IsChecked == true;
        _dungeonMapNameSettings[texture.Name] = current with
        {
            Center = center,
            XNudge = center ? DefaultHorizontalPosition : ReadInt(XNudgeBox, current.XNudge),
            Text = current.Text with
            {
                YOffset = ReadInt(CompactTextYBox, current.Text.YOffset),
            },
        };
    }

    private CompactTextTextureRenderSettings GetEffectiveDungeonMapNameSettings(TextureDefinition texture)
    {
        DungeonMapNameUiSettings settings = GetDungeonMapNameSettings(texture);
        var renderSettings = new CompactTextTextureRenderSettings(
            Center: settings.Center,
            XNudge: settings.Center ? DefaultHorizontalPosition : settings.XNudge,
            FitToWidth: true,
            MaxWidth: texture.Width);
        return ApplyCompactTextSettings(renderSettings, settings.Text) with
        {
            StrokeWidth = DungeonMapNameDefaultOutlineWidth,
            StrokeAlpha = DungeonMapNameDefaultOutlineAlpha,
            WhiteThreshold = DungeonMapNameWhiteThreshold,
            FillBoost = settings.FillBoost,
        };
    }

    private DungeonMapNameUiSettings GetSelectedDungeonMapNameSettings()
        => GetSelectedTarget()?.Texture is { } texture && DungeonMapNameTextureCatalog.IsDungeonMapNameTexture(texture)
            ? GetDungeonMapNameSettings(texture)
            : DefaultDungeonMapNameSettings;

    private DungeonMapNameUiSettings GetDungeonMapNameSettings(TextureDefinition texture)
        => _dungeonMapNameSettings.TryGetValue(texture.Name, out DungeonMapNameUiSettings? settings)
            ? settings
            : DefaultDungeonMapNameSettings;
}
