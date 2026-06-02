namespace HylianGrimoire.Preview;

public static class MmPreviewLayout
{
    public static float GetStartY(MmPreviewStyle style, IReadOnlyList<OotPreviewToken> tokens)
        => style == MmPreviewStyle.Ocarina ? 2 : GetStartY(tokens);

    public static float GetStartY(IReadOnlyList<OotPreviewToken> tokens)
    {
        int lineBreaks = tokens.Count(token => token.Kind == OotPreviewTokenKind.LineBreak);
        // MM BOX_BREAK2 uses a centered continuation layout where the filler line before the break is not visible.
        if (tokens.Any(token => token.Kind == OotPreviewTokenKind.BoxBreak2) && lineBreaks > 0)
        {
            lineBreaks--;
        }

        return Math.Max(8, (52 - (12 * lineBreaks)) / 2.0f);
    }
}
