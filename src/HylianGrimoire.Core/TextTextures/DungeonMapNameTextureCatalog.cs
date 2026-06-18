using HylianGrimoire.Games;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static class DungeonMapNameTextureCatalog
{
    public const int Width = 96;
    public const int Height = 16;

    public static readonly IReadOnlyList<DungeonMapNameTextureSpec> Specs =
    [
        new("Bottom of the Well", "English", "textures/icon_item_nes_static", "gPauseBotWTitleENGTex", "Bottom of the Well"),
        new("Deku Tree", "English", "textures/icon_item_nes_static", "gPauseDekuTitleENGTex", "Inside the Deku Tree"),
        new("Dodongo's Cavern", "English", "textures/icon_item_nes_static", "gPauseDodongoTitleENGTex", "Dodongo's Cavern"),
        new("Fire Temple", "English", "textures/icon_item_nes_static", "gPauseFireTitleENGTex", "Fire Temple"),
        new("Ice Cavern", "English", "textures/icon_item_nes_static", "gPauseIceCavernTitleENGTex", "Ice Cavern"),
        new("Jabu-Jabu's Belly", "English", "textures/icon_item_nes_static", "gPauseJabuTitleENGTex", "Inside Jabu-Jabu's Belly"),
        new("Shadow Temple", "English", "textures/icon_item_nes_static", "gPauseShadowTitleENGTex", "Shadow Temple"),
        new("Spirit Temple", "English", "textures/icon_item_nes_static", "gPauseSpiritTitleENGTex", "Spirit Temple"),
        new("Water Temple", "English", "textures/icon_item_nes_static", "gPauseWaterTitleENGTex", "Water Temple"),
        new("Puits", "French", "textures/icon_item_fra_static", "gPauseBotWTitleFRATex", "Puits"),
        new("Arbre Mojo", "French", "textures/icon_item_fra_static", "gPauseDekuTitleFRATex", "Arbre Mojo"),
        new("Caverne Dodongo", "French", "textures/icon_item_fra_static", "gPauseDodongoTitleFRATex", "Caverne Dodongo"),
        new("Temple du Feu", "French", "textures/icon_item_fra_static", "gPauseFireTitleFRATex", "Temple du Feu"),
        new("Caverne Polaire", "French", "textures/icon_item_fra_static", "gPauseIceCavernTitleFRATex", "Caverne Polaire"),
        new("Ventre de Jabu-Jabu", "French", "textures/icon_item_fra_static", "gPauseJabuTitleFRATex", "Ventre de Jabu-Jabu"),
        new("Temple de l'Ombre", "French", "textures/icon_item_fra_static", "gPauseShadowTitleFRATex", "Temple de l'Ombre"),
        new("Temple de l'Esprit", "French", "textures/icon_item_fra_static", "gPauseSpiritTitleFRATex", "Temple de l'Esprit"),
        new("Temple de l'Eau", "French", "textures/icon_item_fra_static", "gPauseWaterTitleFRATex", "Temple de l'Eau"),
        new("Grund des Brunnens", "German", "textures/icon_item_ger_static", "gPauseBotWTitleGERTex", "Grund des Brunnens"),
        new("Im Deku-Baum", "German", "textures/icon_item_ger_static", "gPauseDekuTitleGERTex", "Im Deku-Baum"),
        new("Dodongos H\u00f6hle", "German", "textures/icon_item_ger_static", "gPauseDodongoTitleGERTex", "Dodongos H\u00f6hle"),
        new("Feuertempel", "German", "textures/icon_item_ger_static", "gPauseFireTitleGERTex", "Feuertempel"),
        new("Eish\u00f6hle", "German", "textures/icon_item_ger_static", "gPauseIceCavernTitleGERTex", "Eish\u00f6hle"),
        new("Jabu-Jabus Bauch", "German", "textures/icon_item_ger_static", "gPauseJabuTitleGERTex", "Jabu-Jabus Bauch"),
        new("Schattentempel", "German", "textures/icon_item_ger_static", "gPauseShadowTitleGERTex", "Schattentempel"),
        new("Geistertempel", "German", "textures/icon_item_ger_static", "gPauseSpiritTitleGERTex", "Geistertempel"),
        new("Wassertempel", "German", "textures/icon_item_ger_static", "gPauseWaterTitleGERTex", "Wassertempel"),
    ];

    private static readonly IReadOnlyDictionary<string, DungeonMapNameTextureSpec> SpecsByName = Specs
        .ToDictionary(spec => spec.TextureName, StringComparer.Ordinal);

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<TextureDefinition> textures)
    {
        if (profile.Game != GameKind.OcarinaOfTime || !TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
        {
            textures = [];
            return false;
        }

        Dictionary<string, TextureDefinition> byName = catalog
            .Where(IsDungeonMapNameTexture)
            .ToDictionary(texture => texture.Name, StringComparer.Ordinal);

        var result = new List<TextureDefinition>(Specs.Count);
        foreach (DungeonMapNameTextureSpec spec in Specs)
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
            : throw new NotSupportedException($"Dungeon map name texture catalog is not available for {profile.Name}.");

    public static bool IsDungeonMapNameTexture(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out DungeonMapNameTextureSpec? spec)
            && string.Equals(texture.Group, spec.Group, StringComparison.Ordinal)
            && texture.Width == Width
            && texture.Height == Height
            && texture.Format == TextureFormat.IA8;

    public static string GetDisplayText(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out DungeonMapNameTextureSpec? spec)
            ? spec.SampleText
            : texture.Name;

    public static string GetLabel(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out DungeonMapNameTextureSpec? spec)
            ? spec.Label
            : texture.Name;

    public static string GetLanguage(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out DungeonMapNameTextureSpec? spec)
            ? spec.Language
            : "Unknown";
}

public sealed record DungeonMapNameTextureSpec(
    string Label,
    string Language,
    string Group,
    string TextureName,
    string SampleText);
