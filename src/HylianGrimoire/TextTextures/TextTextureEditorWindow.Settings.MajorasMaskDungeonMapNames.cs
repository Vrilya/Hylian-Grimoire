using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private const double MajorasMaskDungeonMapNameDefaultOutlineWidth = 2.7;
    private const int MajorasMaskDungeonMapNameDefaultOutlineAlpha = 95;
    private const int MajorasMaskDungeonMapNameDefaultFillBoost = 120;
    private const int MajorasMaskDungeonMapNameWhiteThreshold = 255;

    private static readonly MajorasMaskDungeonMapNameUiSettings DefaultMajorasMaskDungeonMapNameSettings = new(
        Center: true,
        XNudge: DefaultHorizontalPosition,
        Text: new CompactTextUiSettings(12.8, 83.5, 0),
        FillBoost: MajorasMaskDungeonMapNameDefaultFillBoost,
        CapitalTRightTuck: 2,
        CapitalWRightTuck: 0.7);

    private static readonly IReadOnlyDictionary<string, MajorasMaskDungeonMapNameDefaultSpec> MajorasMaskDungeonMapNameTextureDefaults =
        new Dictionary<string, MajorasMaskDungeonMapNameDefaultSpec>(StringComparer.Ordinal)
        {
            ["gPauseWoodfallTitleENGTex"] = new(Center: false, XNudge: 12, WidthScale: 98.5),
            ["gPauseSnowheadTitleENGTex"] = new(Center: false, XNudge: 8, WidthScale: 99),
            ["gPauseGreatBayTitleENGTex"] = new(Center: false, XNudge: 8, WidthScale: 101),
            ["gPauseStoneTowerTitleENGTex"] = new(Center: false, XNudge: 1, WidthScale: 99.5),
        };

    private void SetMajorasMaskDungeonMapNameControlsFromSettings()
    {
        MajorasMaskDungeonMapNameUiSettings settings = GetSelectedMajorasMaskDungeonMapNameSettings();
        ConfigureStandardHorizontalPositionBox(settings.Center ? DefaultHorizontalPosition : settings.XNudge);
        ConfigureNumberBox(MajorasMaskDungeonMapNameWidthScaleBox, 65, 120, 0.5, 5, 1);
        CenterCheck.Content = "Center X";
        CenterCheck.IsChecked = settings.Center;
        MajorasMaskDungeonMapNameWidthScaleBox.Value = settings.Text.WidthScale;
    }

    private void ReadMajorasMaskDungeonMapNameSettings()
    {
        if (GetSelectedTarget()?.Texture is not { } texture || !MajorasMaskDungeonMapNameTextureCatalog.IsDungeonMapNameTexture(texture))
        {
            return;
        }

        MajorasMaskDungeonMapNameUiSettings current = GetMajorasMaskDungeonMapNameSettings(texture);
        bool center = CenterCheck.IsChecked == true;
        _majorasMaskDungeonMapNameSettings[texture.Name] = current with
        {
            Center = center,
            XNudge = center ? DefaultHorizontalPosition : ReadInt(XNudgeBox, current.XNudge),
            Text = current.Text with
            {
                WidthScale = ReadDouble(MajorasMaskDungeonMapNameWidthScaleBox, current.Text.WidthScale),
            },
        };
    }

    private CompactTextTextureRenderSettings GetEffectiveMajorasMaskDungeonMapNameSettings(TextureDefinition texture)
    {
        MajorasMaskDungeonMapNameUiSettings settings = GetMajorasMaskDungeonMapNameSettings(texture);
        var renderSettings = new CompactTextTextureRenderSettings(
            Center: settings.Center,
            XNudge: settings.Center ? DefaultHorizontalPosition : settings.XNudge,
            FitToWidth: true,
            MaxWidth: texture.Width);
        return ApplyCompactTextSettings(renderSettings, settings.Text) with
        {
            StrokeWidth = MajorasMaskDungeonMapNameDefaultOutlineWidth,
            StrokeAlpha = MajorasMaskDungeonMapNameDefaultOutlineAlpha,
            WhiteThreshold = MajorasMaskDungeonMapNameWhiteThreshold,
            FillBoost = settings.FillBoost,
            CapitalTRightTuck = settings.CapitalTRightTuck,
            CapitalWRightTuck = settings.CapitalWRightTuck,
        };
    }

    private MajorasMaskDungeonMapNameUiSettings GetSelectedMajorasMaskDungeonMapNameSettings()
        => GetSelectedTarget()?.Texture is { } texture && MajorasMaskDungeonMapNameTextureCatalog.IsDungeonMapNameTexture(texture)
            ? GetMajorasMaskDungeonMapNameSettings(texture)
            : DefaultMajorasMaskDungeonMapNameSettings;

    private MajorasMaskDungeonMapNameUiSettings GetMajorasMaskDungeonMapNameSettings(TextureDefinition texture)
        => _majorasMaskDungeonMapNameSettings.TryGetValue(texture.Name, out MajorasMaskDungeonMapNameUiSettings? settings)
            ? settings
            : GetDefaultMajorasMaskDungeonMapNameSettings(texture);

    private static MajorasMaskDungeonMapNameUiSettings GetDefaultMajorasMaskDungeonMapNameSettings(TextureDefinition texture)
        => MajorasMaskDungeonMapNameTextureDefaults.TryGetValue(texture.Name, out MajorasMaskDungeonMapNameDefaultSpec? defaults)
            ? DefaultMajorasMaskDungeonMapNameSettings with
            {
                Center = defaults.Center,
                XNudge = defaults.XNudge,
                Text = DefaultMajorasMaskDungeonMapNameSettings.Text with { WidthScale = defaults.WidthScale },
            }
            : DefaultMajorasMaskDungeonMapNameSettings;

    private sealed record MajorasMaskDungeonMapNameDefaultSpec(
        bool Center,
        int XNudge,
        double WidthScale);

    private sealed record MajorasMaskDungeonMapNameUiSettings(
        bool Center,
        int XNudge,
        CompactTextUiSettings Text,
        int FillBoost,
        double CapitalTRightTuck,
        double CapitalWRightTuck);
}
