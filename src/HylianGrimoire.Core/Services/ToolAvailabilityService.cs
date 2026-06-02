using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.PromptEditor;
using HylianGrimoire.Rom;
using HylianGrimoire.Textures;
using HylianGrimoire.TitleText;

namespace HylianGrimoire.Services;

public static class ToolAvailabilityService
{
    public static ToolAvailability Build(
        GameProfile? activeGameProfile,
        DocumentKind documentKind,
        IReadOnlyList<MessageEntry> entries,
        RomMessageData? romData)
    {
        GameCapabilities capabilities = activeGameProfile?.Capabilities ?? GameCapabilities.None;
        bool hasActiveProject = activeGameProfile is not null;
        bool hasEntries = entries.Count > 0;
        bool canSaveLoadedRom = romData?.Profile.Capabilities.SupportsMessageEditing == true;
        bool canUseCHeaders = capabilities.SupportsCHeaders;
        bool canUseGlyphTools = capabilities.SupportsGlyphTools;
        bool canUseTextureManager = romData is not null
            && capabilities.SupportsTextureManager
            && TextureCatalog.TryGetTextures(romData.Profile, out _);

        return new ToolAvailability(
            HasActiveProject: hasActiveProject,
            CanSaveDocument: hasActiveProject && (documentKind != DocumentKind.None || hasEntries),
            CanSaveLoadedRom: canSaveLoadedRom,
            CanUseCHeaders: canUseCHeaders,
            CanExportHeader: canUseCHeaders && hasEntries,
            CanImportHeaderIntoRom: canUseCHeaders && canSaveLoadedRom,
            CanUseGlyphTools: canUseGlyphTools,
            CanRemapGlyphBytes: canUseGlyphTools && hasEntries,
            CanUseMessagePreview: capabilities.SupportsMessagePreview,
            CanUseRomGlyphEditor: romData is not null && capabilities.SupportsRomGlyphEditor,
            CanUseFontOrder: capabilities.SupportsFontOrderEditor && FontOrderService.CanEdit(entries, romData),
            CanUseTitleText: romData is not null
                && capabilities.SupportsTitleTextEditor
                && TitleTextService.TryGetProfile(romData.Profile, out _),
            CanUsePromptEditor: romData is not null
                && capabilities.SupportsPromptEditor
                && PromptEditorProfileCatalog.TryGetProfile(romData.Profile, out _),
            CanUseTextureManager: canUseTextureManager,
            CanUseSohModMaker: capabilities.SupportsSohModMaker
                && (hasEntries || (romData is not null && canUseTextureManager)),
            CanUseTweaks: romData?.Profile.IsRetail == true && capabilities.SupportsRomTweaks);
    }
}
