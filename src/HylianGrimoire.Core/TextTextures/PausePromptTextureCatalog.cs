using HylianGrimoire.Games;
using HylianGrimoire.Rom;

using HylianGrimoire.Textures;

namespace HylianGrimoire.TextTextures;

public static class PausePromptTextureCatalog
{
    public const int Height = 16;
    public const int MaxWidth = 152;

    public static readonly IReadOnlyList<PausePromptTextureSpec> Specs =
    [
        new("Decide", "English", "textures/icon_item_nes_static", "gPauseToDecideENGTex", "to Decide", 64),
        new("Equip", "English", "textures/icon_item_nes_static", "gPauseToEquipENGTex", "to Equip", 56),
        new("Equipment", "English", "textures/icon_item_nes_static", "gPauseToEquipmentENGTex", "To Equipment", 128),
        new("PlayMelody", "English", "textures/icon_item_nes_static", "gPauseToPlayMelodyENGTex", "to Play Melody", 80),
        new("QuestStatus", "English", "textures/icon_item_nes_static", "gPauseToQuestStatusENGTex", "To Quest Status", 128),
        new("SavePrompt", "English", "textures/icon_item_nes_static", "gPauseSavePromptENGTex", "Would you like to save?", 152, PausePromptTextureStyle.Choice),
        new("SaveConfirmation", "English", "textures/icon_item_nes_static", "gPauseSaveConfirmationENGTex", "Game saved.", 152, PausePromptTextureStyle.Choice),
        new("SelectItem", "English", "textures/icon_item_nes_static", "gPauseToSelectItemENGTex", "To Select Item", 128),
        new("Yes", "English", "textures/icon_item_nes_static", "gPauseYesENGTex", "Yes", 48, PausePromptTextureStyle.Choice),
        new("No", "English", "textures/icon_item_nes_static", "gPauseNoENGTex", "No", 48, PausePromptTextureStyle.Choice),
        new("Decide", "French", "textures/icon_item_fra_static", "gPauseToDecideFRATex", "pour valider", 72),
        new("Equip", "French", "textures/icon_item_fra_static", "gPauseToEquipFRATex", "pour \u00e9quiper", 80),
        new("Equipment", "French", "textures/icon_item_fra_static", "gPauseToEquipmentFRATex", "Equipement", 128),
        new("PlayMelody", "French", "textures/icon_item_fra_static", "gPauseToPlayMelodyFRATex", "pour entendre le chant", 112),
        new("QuestStatus", "French", "textures/icon_item_fra_static", "gPauseToQuestStatusFRATex", "Statut", 128),
        new("SavePrompt", "French", "textures/icon_item_fra_static", "gPauseSavePromptFRATex", "Voulez-vous sauvegarder ?", 152, PausePromptTextureStyle.Choice),
        new("SaveConfirmation", "French", "textures/icon_item_fra_static", "gPauseSaveConfirmationFRATex", "Jeu sauvegard\u00e9", 152, PausePromptTextureStyle.Choice),
        new("SelectItem", "French", "textures/icon_item_fra_static", "gPauseToSelectItemFRATex", "Inventaire", 128),
        new("Yes", "French", "textures/icon_item_fra_static", "gPauseYesFRATex", "Oui", 48, PausePromptTextureStyle.Choice),
        new("No", "French", "textures/icon_item_fra_static", "gPauseNoFRATex", "Non", 48, PausePromptTextureStyle.Choice),
        new("Decide", "German", "textures/icon_item_ger_static", "gPauseToDecideGERTex", "Best\u00e4tigen mit", 88),
        new("Equip", "German", "textures/icon_item_ger_static", "gPauseToEquipGERTex", "Ausr\u00fcsten mit", 88),
        new("Equipment", "German", "textures/icon_item_ger_static", "gPauseToEquipmentGERTex", "Ausr\u00fcstung", 128),
        new("PlayMelody", "German", "textures/icon_item_ger_static", "gPauseToPlayMelodyGERTex", "Melodie anh\u00f6ren mit", 104),
        new("QuestStatus", "German", "textures/icon_item_ger_static", "gPauseToQuestStatusGERTex", "Status", 128),
        new("SavePrompt", "German", "textures/icon_item_ger_static", "gPauseSavePromptGERTex", "Spielstand sichern ?", 152, PausePromptTextureStyle.Choice),
        new("SaveConfirmation", "German", "textures/icon_item_ger_static", "gPauseSaveConfirmationGERTex", "Spielstand gesichert.", 152, PausePromptTextureStyle.Choice),
        new("SelectItem", "German", "textures/icon_item_ger_static", "gPauseToSelectItemGERTex", "Gegenst\u00e4nde", 128),
        new("Yes", "German", "textures/icon_item_ger_static", "gPauseYesGERTex", "Ja", 48, PausePromptTextureStyle.Choice),
        new("No", "German", "textures/icon_item_ger_static", "gPauseNoGERTex", "Nein", 48, PausePromptTextureStyle.Choice),
    ];

    private static readonly IReadOnlyDictionary<string, PausePromptTextureSpec> SpecsByName = Specs
        .ToDictionary(spec => spec.TextureName, StringComparer.Ordinal);

    public static bool TryGetTargets(RomVersionProfile profile, out IReadOnlyList<TextureDefinition> textures)
    {
        if (profile.Game != GameKind.OcarinaOfTime || !TextureCatalog.TryGetTextures(profile, out IReadOnlyList<TextureDefinition>? catalog))
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
            : throw new NotSupportedException($"Pause-prompt texture catalog is not available for {profile.Name}.");

    public static bool IsPausePromptTexture(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out PausePromptTextureSpec? spec)
            && string.Equals(texture.Group, spec.Group, StringComparison.Ordinal)
            && texture.Width == spec.Width
            && texture.Height == Height
            && texture.Format == TextureFormat.IA8;

    public static string GetDisplayText(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out PausePromptTextureSpec? spec)
            ? spec.SampleText
            : texture.Name;

    public static string GetLanguage(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out PausePromptTextureSpec? spec)
            ? spec.Language
            : "Unknown";

    public static PausePromptTextureStyle GetStyle(TextureDefinition texture)
        => SpecsByName.TryGetValue(texture.Name, out PausePromptTextureSpec? spec)
            ? spec.Style
            : PausePromptTextureStyle.Standard;

    public static bool IsChoicePromptTexture(TextureDefinition texture)
        => GetStyle(texture) == PausePromptTextureStyle.Choice;

    public static bool IsSavePromptTexture(TextureDefinition texture)
        => IsTextureWithKey(texture, "SavePrompt");

    public static bool IsSaveConfirmationTexture(TextureDefinition texture)
        => IsTextureWithKey(texture, "SaveConfirmation");

    private static bool IsTextureWithKey(TextureDefinition texture, string key)
        => SpecsByName.TryGetValue(texture.Name, out PausePromptTextureSpec? spec)
            && string.Equals(spec.Key, key, StringComparison.Ordinal)
            && IsPausePromptTexture(texture);
}

public sealed record PausePromptTextureSpec(
    string Key,
    string Language,
    string Group,
    string TextureName,
    string SampleText,
    int Width,
    PausePromptTextureStyle Style = PausePromptTextureStyle.Standard);

public enum PausePromptTextureStyle
{
    Standard,
    Choice,
}
