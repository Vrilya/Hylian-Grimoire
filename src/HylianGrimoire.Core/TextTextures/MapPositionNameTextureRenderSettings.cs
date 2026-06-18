namespace HylianGrimoire.TextTextures;

public sealed record MapPositionNameTextureRenderSettings(
    double FontSize = 11,
    double FirstLineWidthScale = 100,
    double SecondLineWidthScale = 100,
    double StrokeWidth = 1.8,
    int StrokeAlpha = 220,
    double StrokeGamma = 0.50,
    double StrokeBlurRadius = 0.75,
    int StrokeBlurStrength = 140,
    int FillBoost = 135,
    int FillMin = 17,
    int WhiteThreshold = 204,
    int LineSpacing = 0,
    int RenderScale = 4);
