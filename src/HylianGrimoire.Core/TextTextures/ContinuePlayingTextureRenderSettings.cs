namespace HylianGrimoire.TextTextures;

public sealed record ContinuePlayingTextureRenderSettings(
    double FontSize = 13.7,
    double StrokeWidth = 5.3,
    int StrokeAlpha = 134,
    double StrokeGamma = 1.5,
    double BlurRadius = 0.85,
    int BlurStrength = 90,
    double Tracking = -1.1,
    double GlyphGap = 0.7,
    double XNudge = 0.0,
    double YNudge = 0.6,
    double WidthScale = 92.0,
    double HeightScale = 84.0,
    int FillBoost = 130,
    int FillThreshold = 18,
    int WhiteThreshold = 255,
    int RenderScale = 4,
    bool Center = true)
{
    public const double CenteredXNudge = 0.0;
}
