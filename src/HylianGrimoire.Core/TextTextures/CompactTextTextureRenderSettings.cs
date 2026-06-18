namespace HylianGrimoire.TextTextures;

public static class CompactTextTextureRenderDefaults
{
    public const int MaxWidth = 152;
}

public sealed record CompactTextTextureRenderSettings(
    double FontSize = 12,
    double StrokeWidth = 2.75,
    int StrokeAlpha = 100,
    double StrokeBlurRadius = 0,
    int StrokeBlurStrength = 0,
    int XNudge = 0,
    int YOffset = 0,
    int BaselineY = 12,
    double HorizontalScale = 100,
    int VerticalScale = 100,
    int FillThreshold = 48,
    int StrokeThreshold = 16,
    int WhiteThreshold = 146,
    int FillFloor = 146,
    int FillBoost = 160,
    int RenderScale = 4,
    bool Center = true,
    bool FitToWidth = true,
    int MaxWidth = CompactTextTextureRenderDefaults.MaxWidth,
    bool BlendFillAndStrokeEdges = false,
    double FillStrokeWidth = 0,
    double CharacterSpacing = 0,
    double CapitalTRightTuck = 0,
    double CapitalWRightTuck = 0);
