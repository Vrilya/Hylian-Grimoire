using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private const int FileSelectOutlineThreshold = 6;
    private const double FileSelectOutlineBlurRadius = 1.15;
    private const int FileSelectOutlineBlurStrength = 115;
    private const double FileSelectControlsOutlineBlurRadius = 1.0;
    private const int FileSelectControlsOutlineBlurStrength = 126;
    private const int FileSelectControlsFillThreshold = 24;
    private const int FileSelectControlsWhiteThreshold = 155;
    private const int FileSelectControlsFillFloor = 17;
    private const int FileSelectControlsFillBoost = 125;
    private const double FileSelectControlsFillStrokeWidth = 0.50;

    private static readonly FileSelectUiSettings DefaultFileSelectSettings = new(
        Center: false,
        XNudge: -1,
        Text: new CompactTextUiSettings(12.5, 100.0, 0),
        OutlineWidth: 2.9,
        OutlineAlpha: 119,
        OutlineBlurRadius: FileSelectOutlineBlurRadius,
        OutlineBlurStrength: FileSelectOutlineBlurStrength,
        CharacterSpacing: 0,
        FillStrokeWidth: 0,
        BulletScale: 100,
        BulletYOffset: 0);

    private static readonly FileSelectUiSettings DefaultFileSelectControlsSettings = DefaultFileSelectSettings with
    {
        Center = true,
        XNudge = 0,
        Text = new CompactTextUiSettings(10.4, 86.5, 0),
        OutlineWidth = 3.40,
        OutlineAlpha = 112,
        OutlineBlurRadius = FileSelectControlsOutlineBlurRadius,
        OutlineBlurStrength = FileSelectControlsOutlineBlurStrength,
        CharacterSpacing = 1.15,
        FillStrokeWidth = FileSelectControlsFillStrokeWidth,
        BulletScale = 235,
        BulletYOffset = 5.2,
    };

    private static readonly FileSelectUiSettings DefaultFileSelectZeroXSettings = DefaultFileSelectSettings with
    {
        XNudge = 0,
    };

    private static readonly FileSelectUiSettings DefaultFileSelectFileEmptySettings = DefaultFileSelectSettings with
    {
        XNudge = 1,
        Text = new CompactTextUiSettings(9.5, 92.5, 0),
        OutlineWidth = 3.60,
        OutlineAlpha = 119,
        OutlineBlurRadius = FileSelectOutlineBlurRadius,
        OutlineBlurStrength = FileSelectOutlineBlurStrength,
        CharacterSpacing = 0.35,
    };

    private static readonly FileSelectUiSettings DefaultFileSelectNameSettings = DefaultFileSelectSettings with
    {
        Text = new CompactTextUiSettings(13.3, 100.0, 1),
    };

    private static readonly FileSelectUiSettings DefaultFileSelectOptionsSettings = DefaultFileSelectZeroXSettings with
    {
        Text = new CompactTextUiSettings(12.2, 100.0, 0),
    };

    private void SetFileSelectControlsFromSettings()
    {
        FileSelectUiSettings settings = GetSelectedFileSelectSettings();
        ConfigureStandardHorizontalPositionBox(settings.Center ? DefaultHorizontalPosition : settings.XNudge);
        ConfigureNumberBox(CompactTextYBox, -6, 6, 1, 1, 0);
        ConfigureNumberBox(CompactTextFontSizeBox, 8, 18, 0.1, 1, 1);
        ConfigureNumberBox(CompactTextWidthScaleBox, 65, 120, 0.5, 5, 1);
        CenterCheck.Content = "Center X";
        CenterCheck.IsChecked = settings.Center;
        CompactTextYBox.Value = settings.Text.YOffset;
        CompactTextFontSizeBox.Value = settings.Text.FontSize;
        CompactTextWidthScaleBox.Value = settings.Text.WidthScale;
    }

    private void ReadFileSelectSettings()
    {
        if (GetSelectedTarget()?.Texture is not { } texture || !FileSelectTextureCatalog.IsFileSelectTexture(texture))
        {
            return;
        }

        FileSelectUiSettings current = GetFileSelectSettings(texture);
        bool center = CenterCheck.IsChecked == true;
        _fileSelectSettings[texture.Name] = current with
        {
            Center = center,
            XNudge = center ? DefaultHorizontalPosition : ReadInt(XNudgeBox, current.XNudge),
            Text = current.Text with
            {
                FontSize = ReadDouble(CompactTextFontSizeBox, current.Text.FontSize),
                WidthScale = ReadDouble(CompactTextWidthScaleBox, current.Text.WidthScale),
                YOffset = ReadInt(CompactTextYBox, current.Text.YOffset),
            },
        };
    }

    private CompactTextTextureRenderSettings GetEffectiveFileSelectSettings(TextureDefinition texture)
    {
        FileSelectUiSettings settings = GetFileSelectSettings(texture);
        bool isControls = FileSelectTextureCatalog.IsControlsTexture(texture);
        var renderSettings = new CompactTextTextureRenderSettings(
            Center: settings.Center,
            XNudge: settings.Center && !isControls ? DefaultHorizontalPosition : settings.XNudge,
            FitToWidth: true,
            MaxWidth: texture.Width);
        return ApplyCompactTextSettings(renderSettings, settings.Text) with
        {
            StrokeWidth = settings.OutlineWidth,
            StrokeAlpha = settings.OutlineAlpha,
            StrokeThreshold = FileSelectOutlineThreshold,
            StrokeBlurRadius = settings.OutlineBlurRadius,
            StrokeBlurStrength = settings.OutlineBlurStrength,
            FillThreshold = isControls ? FileSelectControlsFillThreshold : renderSettings.FillThreshold,
            WhiteThreshold = isControls ? FileSelectControlsWhiteThreshold : renderSettings.WhiteThreshold,
            FillFloor = isControls ? FileSelectControlsFillFloor : renderSettings.FillFloor,
            FillBoost = isControls ? FileSelectControlsFillBoost : renderSettings.FillBoost,
            BlendFillAndStrokeEdges = isControls,
            FillStrokeWidth = settings.FillStrokeWidth,
            CharacterSpacing = settings.CharacterSpacing,
        };
    }

    private bool IsFileSelectControlsTarget()
        => GetSelectedTarget()?.Texture is { } texture && IsFileSelectControlsTexture(texture);

    private static bool IsFileSelectControlsTexture(TextureDefinition texture)
        => FileSelectTextureCatalog.IsControlsTexture(texture);

    private FileSelectUiSettings GetSelectedFileSelectSettings()
        => GetSelectedTarget()?.Texture is { } texture && FileSelectTextureCatalog.IsFileSelectTexture(texture)
            ? GetFileSelectSettings(texture)
            : DefaultFileSelectSettings;

    private FileSelectUiSettings GetFileSelectSettings(TextureDefinition texture)
        => _fileSelectSettings.TryGetValue(texture.Name, out FileSelectUiSettings? settings)
            ? settings
            : GetDefaultFileSelectSettings(texture);

    private static FileSelectUiSettings GetDefaultFileSelectSettings(TextureDefinition texture)
        => FileSelectTextureCatalog.GetPreset(texture) switch
        {
            FileSelectTexturePreset.Controls => DefaultFileSelectControlsSettings,
            FileSelectTexturePreset.ZeroX => DefaultFileSelectZeroXSettings,
            FileSelectTexturePreset.FileEmpty => DefaultFileSelectFileEmptySettings,
            FileSelectTexturePreset.Name => DefaultFileSelectNameSettings,
            FileSelectTexturePreset.Options => DefaultFileSelectOptionsSettings,
            _ => DefaultFileSelectSettings,
        };
}
