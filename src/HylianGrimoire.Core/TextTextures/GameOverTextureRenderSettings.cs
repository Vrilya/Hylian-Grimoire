namespace HylianGrimoire.TextTextures;

public sealed record GameOverTextureRenderSettings(
    double FontSize = 31.0,
    double StrokeWidth = 5.0,
    int StrokeAlpha = 91,
    double StrokeGamma = 0.9,
    double BlurRadius = 2.1,
    int BlurStrength = 100,
    int Tracking = -2,
    double XNudge = 0.0,
    int Y = 4,
    double WidthScale = 73.0,
    double HeightScale = 72.0,
    int FillBoost = 100,
    int WhiteThreshold = 250,
    int RenderScale = 4,
    bool Center = true)
{
    public const double CenteredXNudge = 0.0;
    public const int DefaultY = 4;
}
