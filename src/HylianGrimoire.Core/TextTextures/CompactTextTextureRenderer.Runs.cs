using System.Drawing;
using System.Globalization;

namespace HylianGrimoire.TextTextures;

public static partial class CompactTextTextureRenderer
{
    private sealed record DrawingTextRun(
        string Text,
        FontFamily Family,
        FontStyle Style,
        double FontScale,
        CompactTextTextureTextRunKind Kind,
        double YOffset,
        double XOffset,
        double LeadingSpacing,
        double TrailingSpacing);

    private static double NormalizeFontScale(double value)
        => double.IsFinite(value) ? Math.Clamp(value, 0.01, 10) : 1;

    private static double NormalizeOffset(double value)
        => double.IsFinite(value) ? Math.Clamp(value, -64, 64) : 0;

    private static double NormalizeSpacing(double value)
        => double.IsFinite(value) ? Math.Clamp(value, 0, 64) : 0;

    private static double NormalizeCharacterSpacing(double value)
        => double.IsFinite(value) ? Math.Clamp(value, -64, 64) : 0;

    private static float GetCharacterSpacing(float characterSpacing, int textElementIndex, int textElementCount)
        => textElementIndex < textElementCount - 1 ? characterSpacing : 0;

    private static float GetCapitalRightTuck(
        float capitalTRightTuck,
        float capitalWRightTuck,
        string textElement,
        int textElementIndex,
        int textElementCount)
        => textElementIndex < textElementCount - 1
            ? textElement switch
            {
                "T" => capitalTRightTuck,
                "W" => capitalWRightTuck,
                _ => 0,
            }
            : 0;

    private static int CountTextElements(IReadOnlyList<DrawingTextRun> runs)
        => runs.Sum(run => run.Kind == CompactTextTextureTextRunKind.Bullet
            ? 1
            : EnumerateTextElements(run.Text).Count());

    private static IEnumerable<string> EnumerateTextElements(string text)
    {
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            yield return enumerator.GetTextElement();
        }
    }
}
