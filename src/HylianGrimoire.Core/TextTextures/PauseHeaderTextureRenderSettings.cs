namespace HylianGrimoire.TextTextures;

public sealed record PauseHeaderTextureRenderSettings
{
    public int FontSize { get; init; } = 13;

    public int StrokeWidth { get; init; } = 1;

    public double CenterX { get; init; } = 121;

    public double Y { get; init; } = 4;

    public double Tracking { get; init; }

    public double WidthScale { get; init; } = 0.96;

    public double FillStrength { get; init; } = 1;

    public double StrokeStrength { get; init; } = 0.35;

    public double HighlightStrength { get; init; } = 1;

    public int HighlightGray { get; init; } = 221;

    public int HighlightDx { get; init; } = -1;

    public int HighlightDy { get; init; } = -1;

    public double ShadowStrength { get; init; }

    public int ShadowDx { get; init; } = 1;

    public int ShadowDy { get; init; } = 1;

    public int RenderScale { get; init; } = 4;

    public bool Center { get; init; } = true;

    public int XNudge { get; init; }
}
