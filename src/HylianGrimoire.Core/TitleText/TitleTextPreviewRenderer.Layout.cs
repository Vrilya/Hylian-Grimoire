using System.Drawing;

namespace HylianGrimoire.TitleText;

public static partial class TitleTextPreviewRenderer
{
    private static void DrawGuides(Graphics graphics, float scale)
    {
        DrawVerticalGuides(graphics, scale);
    }

    private static void DrawVerticalGuides(Graphics graphics, float scale)
    {
        int sideLines = GuideLineCount / 2;
        float width = Math.Max(2f, scale);

        for (int i = -sideLines; i <= sideLines; i++)
        {
            int x = GuideCenterX + i * GuideSpacing;
            Color color = i == 0
                ? Color.FromArgb(230, 255, 230, 40)
                : Color.FromArgb(220, 255, 60, 60);
            using var pen = new Pen(color, width);
            graphics.DrawLine(pen, ScaleX(x, scale), 0, ScaleX(x, scale), ScaleY(LogicalHeight, scale));
        }
    }

    private static (string CleanText, int GapAfterIndex) PrepareTextForDrawing(string text)
    {
        text = text.Trim().ToUpperInvariant();
        int gapIndex = text.IndexOf(' ', StringComparison.Ordinal);
        string cleanText = text.Replace(" ", string.Empty, StringComparison.Ordinal);
        int gapAfterIndex = gapIndex > 0 ? gapIndex - 1 : cleanText.Length - 1;
        return (cleanText, gapAfterIndex);
    }

    private static Rectangle ScaleRect(int x, int y, int width, int height, Graphics graphics)
    {
        float scale = graphics.VisibleClipBounds.Width / LogicalWidth;
        return ScaleRect(x, y, width, height, scale);
    }

    private static Rectangle ScaleRect(int x, int y, int width, int height, float scale)
    {
        return new Rectangle(
            ScaleX(x, scale),
            ScaleY(y, scale),
            Math.Max(1, (int)Math.Round(width * scale)),
            Math.Max(1, (int)Math.Round(height * scale)));
    }

    private static int ScaleX(int x, float scale) => (int)Math.Round(x * scale);

    private static int ScaleY(int y, float scale) => (int)Math.Round(y * scale);
}
