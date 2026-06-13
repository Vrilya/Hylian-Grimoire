using HylianGrimoire.Games;
using HylianGrimoire.Models;
using HylianGrimoire.O2r;
using HylianGrimoire.PromptEditor;
using HylianGrimoire.Rom;
using HylianGrimoire.TextTextures;
using HylianGrimoire.Textures;
using HylianGrimoire.TitleText;

namespace HylianGrimoire.Services;

public static class ToolAvailabilityService
{
    private const string MajorasMaskTweaksProfileName = "Majora's Mask NTSC-U";

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
        bool canUseTextTextureEditor = romData is not null
            && canUseTextureManager
            && (ItemNameTextureCatalog.TryGetTargets(romData.Profile, out _)
                || PausePromptTextureCatalog.TryGetTargets(romData.Profile, out _)
                || EndTitleTextureCatalog.TryGetTargets(romData.Profile, out _)
                || PlaceTitleCardTextureCatalog.TryGetTargets(romData.Profile, out _)
                || BossTitleCardTextureCatalog.TryGetTargets(romData.Profile, out _));
        bool canUseO2rModMaker = capabilities.SupportsO2rModMaker
            && O2rModPortProfileCatalog.TryGetProfile(activeGameProfile, romData?.Profile, out O2rModPortProfile o2rProfile)
            && ((hasEntries && (romData is not null || o2rProfile.SupportsCurrentDocumentTextResources))
                || (romData is not null && canUseTextureManager));

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
            CanUseTextTextureEditor: canUseTextTextureEditor,
            CanUseO2rModMaker: canUseO2rModMaker,
            CanUseTweaks: CanUseTweaks(capabilities, romData));
    }

    private static bool CanUseTweaks(GameCapabilities capabilities, RomMessageData? romData)
    {
        if (romData is null || !capabilities.SupportsRomTweaks)
        {
            return false;
        }

        RomVersionProfile profile = romData.Profile;
        return profile.Game switch
        {
            GameKind.OcarinaOfTime => profile.IsRetail,
            GameKind.MajorasMask => string.Equals(profile.Name, MajorasMaskTweaksProfileName, StringComparison.Ordinal),
            _ => false,
        };
    }
}
