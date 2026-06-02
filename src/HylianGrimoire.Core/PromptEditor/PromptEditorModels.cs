namespace HylianGrimoire.PromptEditor;

public enum PromptEditorKind
{
    File,
    Melody,
    Gear,
    Item,
    Equip,
    Notebook,
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

public sealed record PromptEditorLineDefinition(
    PromptEditorKind Kind,
    string Label,
    string IconKey,
    string TextKey,
    string IconSiteName,
    string TextSiteName);

public sealed record PromptEditorPatchSite(
    int Offset,
    uint Expected,
    int ExpectedRt,
    int ExpectedRs,
    int WriteRt,
    int WriteRs);

public sealed record PromptEditorFixedPatch(
    int Offset,
    uint Expected,
    uint Value);

public enum PromptEditorAssetFormat
{
    Ia8,
    Ia4,
}

public enum PromptEditorSegmentFormat
{
    Raw,
    CmpDmaArchive,
}

public sealed record PromptEditorSegment(
    int RomBase,
    PromptEditorSegmentFormat Format);

public sealed record PromptEditorAsset(
    int LocalOffset,
    int Width,
    int Height,
    int? ColorDisplayListOffset,
    System.Drawing.Color FallbackColor,
    PromptEditorAssetFormat Format,
    int DrawWidth);

public sealed record PromptEditorProfile(
    string DisplayName,
    IReadOnlyList<string> LanguageKeys,
    IReadOnlyDictionary<string, PromptEditorLanguage> Languages,
    IReadOnlyList<PromptEditorLineDefinition> Lines,
    PromptEditorSegment IconSegment,
    IReadOnlyDictionary<string, PromptEditorSegment> TextSegments,
    IReadOnlyDictionary<string, PromptEditorAsset> IconAssets,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, PromptEditorAsset>> TextAssets,
    IReadOnlyDictionary<string, PromptEditorPatchSite> PatchSites,
    IReadOnlyDictionary<string, PromptEditorFixedPatch> FixedPatches);
