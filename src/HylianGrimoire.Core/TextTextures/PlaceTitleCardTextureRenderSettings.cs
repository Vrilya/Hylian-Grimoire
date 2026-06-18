namespace HylianGrimoire.TextTextures;

public sealed record PlaceTitleCardTextureRenderSettings(
    int FontSize = 20,
    double StrokeWidth = 5,
    int StrokeAlpha = 108,
    double StrokeSoftness = 0.75,
    int XNudge = 0,
    int YOffset = 0,
    double HorizontalScale = 85.0,
    double HeightScale = 93.5,
    int MaxHeight = 24,
    int MaxWidth = 140,
    int FillBoost = 125,
    int FillThreshold = 24,
    int WhiteThreshold = 204,
    int FillFloor = 0,
    int RenderScale = 4,
    bool Center = true);
