using HylianGrimoire.Games;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static class MajorasMaskPausePromptTextureCatalog
{
    public const int Height = PausePromptTextureCatalog.Height;
    public const int MaxWidth = 128;

    public static readonly IReadOnlyList<PausePromptTextureSpec> Specs =
    [
        new("Equip", "English", "interface/icon_item_jpn_static", "gPauseToEquipENGTex", "to Equip", 64),
        new("PlayMelody", "English", "interface/icon_item_jpn_static", "gPauseToPlayMelodyENGTex", "to Play Melody", 96),
        new("ViewNotebook", "English", "interface/icon_item_jpn_static", "gPauseToViewNotebookENGTex", "to View Notebook", 96),
        new("SelectItem", "English", "interface/icon_item_jpn_static", "gPauseToSelectItemENGTex", "To Select Item", 128),
        new("Map", "English", "interface/icon_item_jpn_static", "gPauseToMapENGTex", "To Map", 128),
        new("QuestStatus", "English", "interface/icon_item_jpn_static", "gPauseToQuestStatusENGTex", "To Quest Status", 128),
        new("Masks", "English", "interface/icon_item_jpn_static", "gPauseToMasksENGTex", "To Masks", 128),
    ];

    private static readonly IReadOnlyDictionary<string, PausePromptTextureSpec> SpecsByName = Specs
        .ToDictionary(spec => spec.TextureName, StringComparer.Ordinal);

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<TextureDefinition> textures)
    {
        if (profile.Game != GameKind.MajorasMask
            || !TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
        {
            textures = [];
            return false;
        }

        Dictionary<string, TextureDefinition> byName = catalog
            .Where(IsPausePromptTexture)
            .ToDictionary(texture => texture.Name, StringComparer.Ordinal);

        var result = new List<TextureDefinition>(Specs.Count);
        foreach (PausePromptTextureSpec spec in Specs)
        {
            if (byName.TryGetValue(spec.TextureName, out TextureDefinition? texture))
            {
                result.Add(texture);
            }
        }

        textures = result;
        return textures.Count > 0;
    }

    public static IReadOnlyList<TextureDefinition> GetTargets(RomVersionProfile profile)
        => TryGetTargets(profile, out IReadOnlyList<TextureDefinition>? textures)
            ? textures
            : throw new NotSupportedException($"Majora's Mask pause-prompt texture catalog is not available for {profile.Name}.");

    public static bool IsPausePromptTexture(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out PausePromptTextureSpec? spec)
            && string.Equals(texture.Group, spec.Group, StringComparison.Ordinal)
            && texture.Width == spec.Width
            && texture.Height == Height
            && texture.Format == TextureFormat.IA4
            && texture.StorageKind == TextureStorageKind.Rom;

    public static string GetDisplayText(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out PausePromptTextureSpec? spec)
            ? spec.SampleText
            : texture.Name;

    public static string GetLanguage(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out PausePromptTextureSpec? spec)
            ? spec.Language
            : "Unknown";
}
