namespace HylianGrimoire.Preview;

public sealed record MmPreviewRenderOptions(byte IconId, bool Centered)
{
    public static MmPreviewRenderOptions Default { get; } = new(0xfe, false);
}
