namespace HylianGrimoire.Games;

public sealed record GameCapabilities(
    bool SupportsMessagePreview,
    bool SupportsRomTweaks,
    bool SupportsTitleTextEditor,
    bool SupportsPromptEditor,
    bool SupportsTextureManager,
    bool SupportsO2rModMaker,
    bool SupportsFontOrderEditor,
    bool SupportsGlyphTools,
    bool SupportsRomGlyphEditor,
    bool SupportsCHeaders)
{
    public static GameCapabilities OcarinaOfTime { get; } = new(
        SupportsMessagePreview: true,
        SupportsRomTweaks: true,
        SupportsTitleTextEditor: true,
        SupportsPromptEditor: true,
        SupportsTextureManager: true,
        SupportsO2rModMaker: true,
        SupportsFontOrderEditor: true,
        SupportsGlyphTools: true,
        SupportsRomGlyphEditor: true,
        SupportsCHeaders: true);

    public static GameCapabilities None { get; } = new(
        SupportsMessagePreview: false,
        SupportsRomTweaks: false,
        SupportsTitleTextEditor: false,
        SupportsPromptEditor: false,
        SupportsTextureManager: false,
        SupportsO2rModMaker: false,
        SupportsFontOrderEditor: false,
        SupportsGlyphTools: false,
        SupportsRomGlyphEditor: false,
        SupportsCHeaders: false);

    public static GameCapabilities MajorasMask { get; } = None with
    {
        SupportsMessagePreview = true,
        SupportsRomTweaks = true,
        SupportsTitleTextEditor = true,
        SupportsPromptEditor = true,
        SupportsTextureManager = true,
        SupportsO2rModMaker = true,
        SupportsGlyphTools = true,
        SupportsRomGlyphEditor = true,
        SupportsCHeaders = true
    };
}
