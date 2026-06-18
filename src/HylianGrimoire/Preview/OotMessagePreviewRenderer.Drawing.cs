using System.Drawing;
using HylianGrimoire.Glyphs;

namespace HylianGrimoire.Preview;

public static partial class OotMessagePreviewRenderer
{
    private static void DrawChoiceArrows(Graphics graphics, byte choiceCount, float scale)
    {
        float x = 16;
        float y = choiceCount == 3 ? 20 : 32;
        int size = (int)(16 * scale);
        string arrow = Assets.Resolve(@"message_static\gMessageArrowTex.png");

        for (int i = 0; i < choiceCount; i++)
        {
            DrawMaskImage(graphics, arrow, Color.FromArgb(255, 0, 110, 255), (int)x, (int)y, size, size, brighten: false);
            y += 12;
        }
    }

    private static void DrawAlignmentGuides(Graphics graphics, int width, int height)
    {
        float centerX = (width / 2.0f) + AlignmentGuideCenterOffset;
        float leftX = centerX - AlignmentGuideHalfSpan;
        float step = (AlignmentGuideHalfSpan * 2) / (AlignmentGuideCount - 1);

        using var redPen = new Pen(AlignmentGuideRed, 1);
        using var greenPen = new Pen(AlignmentGuideGreen, 1);

        for (int i = 0; i < AlignmentGuideCount; i++)
        {
            float x = leftX + (step * i);
            Pen pen = i % 2 == 0 ? redPen : greenPen;
            graphics.DrawLine(pen, x, 0, x, height);
        }
    }

    private static void DrawIcon(Graphics graphics, byte value, float x, float y)
    {
        string path = ResolveIconAsset(value);
        if (!File.Exists(path))
        {
            return;
        }

        using var icon = new Bitmap(path);
        int size = value < 102 ? 32 : 24;
        graphics.DrawImage(icon, (int)x, (int)y, size, size);
    }

    private static void DrawGlyph(Graphics graphics, byte value, Color color, float x, float y, bool shadow, float scale, IGlyphSource glyphSource)
    {
        string path = glyphSource.GetGlyphPath(value);
        if (!File.Exists(path) || value == 0x20)
        {
            return;
        }

        int size = (int)(16 * scale);
        if (shadow)
        {
            DrawMaskImage(graphics, path, Color.Black, (int)x + 1, (int)y + 1, size, size, brighten: false);
        }

        DrawMaskImage(graphics, path, color, (int)x, (int)y, size, size, brighten: false);
    }

    private static void DrawMaskImage(Graphics graphics, string source, Color color, int x, int y, int width, int height, bool brighten)
    {
        using var mask = new Bitmap(source);
        using var tinted = PreviewBitmapTransforms.CreateTintedMask(mask, color, brighten);
        graphics.DrawImage(tinted, x, y, width, height);
    }

    private static string ResolveIconAsset(byte value)
    {
        return OotPreviewIconCatalog.TryGetRelativePath(value, out string? relativePath)
            ? Assets.Resolve(relativePath)
            : Assets.ResolveMissing($"icon_{value}.png");
    }
}
