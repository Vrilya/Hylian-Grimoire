using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public enum CompactTextTextureTextRunKind
{
    Text,
    Bullet,
}

public sealed record CompactTextTextureTextRun(
    string Text,
    TextTextureFont Font,
    double FontScale = 1,
    CompactTextTextureTextRunKind Kind = CompactTextTextureTextRunKind.Text,
    double YOffset = 0,
    double XOffset = 0,
    double LeadingSpacing = 0,
    double TrailingSpacing = 0);

public static partial class CompactTextTextureRenderer
{
    public static Bitmap Render(string text, string fontPath, CompactTextTextureRenderSettings settings, int width, int height)
        => Render(text, TextTextureFont.FromPath(fontPath), settings, width, height);

    public static Bitmap Render(string text, TextTextureFont font, CompactTextTextureRenderSettings settings, int width, int height)
        => Render([new CompactTextTextureTextRun(text, font)], settings, width, height);

    public static Bitmap Render(IReadOnlyList<CompactTextTextureTextRun> runs, CompactTextTextureRenderSettings settings, int width, int height)
    {
        CompactTextTextureTextRun[] activeRuns = runs
            .Where(run => run.Text.Length > 0)
            .ToArray();
        if (activeRuns.Length == 0 || activeRuns.All(run => string.IsNullOrWhiteSpace(run.Text)))
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        foreach (CompactTextTextureTextRun run in activeRuns)
        {
            if (!File.Exists(run.Font.Path))
            {
                throw new FileNotFoundException("Compact text texture font is missing.", run.Font.Path);
            }
        }

        var drawingFonts = new List<TextTextureDrawingFont>(activeRuns.Length);
        var drawingRuns = new List<DrawingTextRun>(activeRuns.Length);
        try
        {
            foreach (CompactTextTextureTextRun run in activeRuns)
            {
                TextTextureDrawingFont drawingFont = new(run.Font);
                drawingFonts.Add(drawingFont);
                drawingRuns.Add(new DrawingTextRun(
                    run.Text,
                    drawingFont.Family,
                    drawingFont.Style,
                    NormalizeFontScale(run.FontScale),
                    run.Kind,
                    NormalizeOffset(run.YOffset),
                    NormalizeOffset(run.XOffset),
                    NormalizeSpacing(run.LeadingSpacing),
                    NormalizeSpacing(run.TrailingSpacing)));
            }

            (Bitmap fillMask, Bitmap strokeMask) = CreateTextMasks(drawingRuns, settings, width, height);
            using (fillMask)
            using (strokeMask)
            {
                using Bitmap adjustedStrokeMask = AdjustStrokeMask(strokeMask, settings);
                using Bitmap blurredStrokeMask = BlurAlphaMask(adjustedStrokeMask, settings.StrokeBlurRadius);
                return Compose(fillMask, adjustedStrokeMask, blurredStrokeMask, settings, width, height);
            }
        }
        finally
        {
            foreach (TextTextureDrawingFont drawingFont in drawingFonts)
            {
                drawingFont.Dispose();
            }
        }
    }
}
