using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public sealed partial class TextTextureEditorWindow
{
    private readonly Dictionary<string, ItemStyleControlSettings> _majorasMaskPausePromptSettings = new(StringComparer.Ordinal);

    private bool TryReadMajorasMaskPausePromptSettings()
    {
        if (GetSelectedTarget()?.Texture is not { } texture
            || _selectedTextureKind != TextTextureKind.MajorasMaskPausePrompts)
        {
            return false;
        }

        ItemStyleControlSettings current = GetMajorasMaskPausePromptControlSettings(texture);
        bool center = CenterCheck.IsChecked == true;
        _majorasMaskPausePromptSettings[texture.Name] = current with
        {
            Center = center,
            XNudge = center ? DefaultHorizontalPosition : ReadInt(XNudgeBox, current.XNudge),
            HorizontalScale = ReadInt(ItemWidthScaleBox, current.HorizontalScale),
        };

        return true;
    }

    private ItemNameTextureRenderSettings GetEffectiveMajorasMaskPausePromptItemSettings(
        TextureDefinition texture,
        ItemNameTextureRenderSettings baseSettings)
    {
        ItemStyleControlSettings promptSettings = GetMajorasMaskPausePromptControlSettings(texture);
        return baseSettings with
        {
            Center = promptSettings.Center,
            XNudge = promptSettings.XNudge,
            HorizontalScale = promptSettings.HorizontalScale,
            VerticalScale = DefaultItemScale,
            FitToWidth = true,
            MaxWidth = texture.Width,
        };
    }

    private ItemStyleControlSettings? GetSelectedMajorasMaskPausePromptControlSettingsOrNull()
        => GetSelectedTarget()?.Texture is { } texture
            && _selectedTextureKind == TextTextureKind.MajorasMaskPausePrompts
                ? GetMajorasMaskPausePromptControlSettings(texture)
                : null;

    private ItemStyleControlSettings GetMajorasMaskPausePromptControlSettings(TextureDefinition texture)
        => _majorasMaskPausePromptSettings.TryGetValue(texture.Name, out ItemStyleControlSettings? settings)
            ? settings
            : GetDefaultMajorasMaskPausePromptControlSettings(texture.Name);

    private static ItemStyleControlSettings GetDefaultMajorasMaskPausePromptControlSettings(string textureName)
        => new(
            Center: UsesCenteredMajorasMaskPausePromptDefault(textureName),
            XNudge: string.Equals(textureName, "gPauseToEquipENGTex", StringComparison.Ordinal) ? -1 : 0,
            HorizontalScale: string.Equals(textureName, "gPauseToPlayMelodyENGTex", StringComparison.Ordinal)
                ? 91
                : DefaultItemScale);

    private static bool UsesCenteredMajorasMaskPausePromptDefault(string textureName)
        => textureName is "gPauseToSelectItemENGTex"
            or "gPauseToMapENGTex"
            or "gPauseToQuestStatusENGTex"
            or "gPauseToMasksENGTex";
}
