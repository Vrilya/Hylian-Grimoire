namespace HylianGrimoire.PromptEditor;

public enum PromptEditorKind
{
    File,
    Melody,
    Gear,
    Item,
}

public sealed record PromptEditorLine(
    PromptEditorKind Kind,
    string Label,
    string IconKey,
    string TextKey,
    int IconX,
    int TextX);

public sealed record PromptEditorLanguage(
    string Key,
    string Label,
    IReadOnlyDictionary<PromptEditorKind, PromptEditorDefaults> Defaults);

public sealed record PromptEditorDefaults(int IconX, int TextX);

public sealed record PromptEditorPatchSite(
    int Offset,
    uint Expected,
    int Rt,
    int Rs);

public sealed record PromptEditorFixedPatch(
    int Offset,
    uint Expected,
    uint Value);

public sealed record PromptEditorAsset(
    int LocalOffset,
    int Width,
    int Height,
    int? ColorDisplayListOffset,
    System.Drawing.Color FallbackColor);

public sealed record PromptEditorProfile(
    string DisplayName,
    IReadOnlyList<string> LanguageKeys,
    int IconSegmentRomBase,
    int TextEnglishRomBase,
    int? TextGermanRomBase,
    int? TextFrenchRomBase,
    IReadOnlyDictionary<string, PromptEditorAsset> IconAssets,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, PromptEditorAsset>> TextAssets,
    IReadOnlyDictionary<string, PromptEditorPatchSite> PatchSites,
    IReadOnlyDictionary<string, PromptEditorFixedPatch> FixedPatches);
