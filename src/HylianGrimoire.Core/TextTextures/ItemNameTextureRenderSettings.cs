namespace HylianGrimoire.TextTextures;

public sealed record ItemNameTextureRenderSettings(
    double FontSize = 12,
    double StrokeWidth = 2.75,
    int XNudge = 0,
    int YOffset = 0,
    int BaselineY = 12,
    int HorizontalScale = 100,
    int VerticalScale = 100,
    int FillThreshold = 48,
    int StrokeThreshold = 16,
    int WhiteThreshold = 146,
    int FillFloor = 146,
    int FillBoost = 160,
    int RenderScale = 4,
    bool Center = true,
    bool FitToWidth = true,
    int MaxWidth = 128);
