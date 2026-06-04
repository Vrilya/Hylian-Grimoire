namespace HylianGrimoire.Services;

public sealed record ToolAvailability(
    bool HasActiveProject,
    bool CanSaveDocument,
    bool CanSaveLoadedRom,
    bool CanUseCHeaders,
    bool CanExportHeader,
    bool CanImportHeaderIntoRom,
    bool CanUseGlyphTools,
    bool CanRemapGlyphBytes,
    bool CanUseMessagePreview,
    bool CanUseRomGlyphEditor,
    bool CanUseFontOrder,
    bool CanUseTitleText,
    bool CanUsePromptEditor,
    bool CanUseTextureManager,
    bool CanUseO2rModMaker,
    bool CanUseTweaks);
