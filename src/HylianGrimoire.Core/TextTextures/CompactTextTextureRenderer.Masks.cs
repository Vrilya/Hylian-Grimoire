using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class CompactTextTextureRenderer
{
    private static (Bitmap Fill, Bitmap Stroke) CreateTextMasks(
        IReadOnlyList<DrawingTextRun> runs,
        CompactTextTextureRenderSettings settings,
        int width,
        int height)
    {
        int scale = Math.Max(1, settings.RenderScale);
        float emSize = Math.Max(1, (float)(settings.FontSize * scale));
        float strokeWidth = Math.Max(0, (float)(settings.StrokeWidth * scale));
        float fillStrokeWidth = Math.Max(0, (float)(settings.FillStrokeWidth * scale));
        float characterSpacing = (float)(NormalizeCharacterSpacing(settings.CharacterSpacing) * scale);
        int textElementCount = characterSpacing == 0 ? 0 : CountTextElements(runs);
        int textElementIndex = 0;

        DrawingTextRun baselineRun = runs[0];
        float baselineAscentPx = GetAscentPx(baselineRun.Family, baselineRun.Style, emSize);
        using GraphicsPath basePath = new();
        using Bitmap probeImage = new(1, 1, PixelFormat.Format32bppArgb);
        using Graphics probe = Graphics.FromImage(probeImage);
        using StringFormat pathFormat = CreateTypographicStringFormat(false);
        using StringFormat measureFormat = CreateTypographicStringFormat(true);

        float advanceX = 0;
        foreach (DrawingTextRun run in runs)
        {
            advanceX += (float)(run.LeadingSpacing * scale);
            float runEmSize = Math.Max(1, emSize * (float)run.FontScale);
            float runAscentPx = GetAscentPx(run.Family, run.Style, runEmSize);
            float runXOffset = (float)(run.XOffset * scale);
            float runAdvanceOffset = Math.Max(0, runXOffset);
            float runYOffset = (float)(run.YOffset * scale);
            float runY = baselineAscentPx - runAscentPx + runYOffset;
            float runX = advanceX + runXOffset;

            if (run.Kind == CompactTextTextureTextRunKind.Bullet)
            {
                advanceX += AddBulletPath(basePath, runX, baselineAscentPx, runEmSize, runYOffset);
                advanceX += GetCharacterSpacing(characterSpacing, textElementIndex++, textElementCount);
                advanceX += runAdvanceOffset;
                advanceX += (float)(run.TrailingSpacing * scale);
                continue;
            }

            if (characterSpacing == 0)
            {
                using GraphicsPath runPath = new();
                runPath.AddString(run.Text, run.Family, (int)run.Style, runEmSize, new PointF(runX, runY), pathFormat);
                if (runPath.PointCount > 0)
                {
                    basePath.AddPath(runPath, false);
                }

                advanceX += MeasureRunAdvance(probe, run.Text, run.Family, run.Style, runEmSize, measureFormat);
            }
            else
            {
                float runAdvance = 0;
                foreach (string textElement in EnumerateTextElements(run.Text))
                {
                    using GraphicsPath elementPath = new();
                    elementPath.AddString(
                        textElement,
                        run.Family,
                        (int)run.Style,
                        runEmSize,
                        new PointF(runX + runAdvance, runY),
                        pathFormat);
                    if (elementPath.PointCount > 0)
                    {
                        basePath.AddPath(elementPath, false);
                    }

                    runAdvance += MeasureRunAdvance(probe, textElement, run.Family, run.Style, runEmSize, measureFormat);
                    runAdvance += GetCharacterSpacing(characterSpacing, textElementIndex++, textElementCount);
                }

                advanceX += runAdvance;
            }

            advanceX += runAdvanceOffset;
            advanceX += (float)(run.TrailingSpacing * scale);
        }

        RectangleF bounds = basePath.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return (CreateBlankMask(width, height), CreateBlankMask(width, height));
        }

        int highWidth = width * scale;
        int highHeight = height * scale;
        double scaleX = Math.Max(1, settings.HorizontalScale) / 100d;
        double scaleY = Math.Max(1, settings.VerticalScale) / 100d;
        double contentRight = Math.Max(bounds.Right, advanceX);
        double strokeBoundsWidth = contentRight - bounds.Left + strokeWidth * 2;
        double targetWidth = strokeBoundsWidth * scaleX / scale;
        if (settings.FitToWidth && targetWidth > settings.MaxWidth)
        {
            double fitScale = settings.MaxWidth / targetWidth;
            scaleX *= fitScale;
        }

        float x = settings.Center
            ? (float)((highWidth - strokeBoundsWidth * scaleX) / 2d + settings.XNudge * scale - (bounds.Left - strokeWidth) * scaleX)
            : (float)(settings.XNudge * scale - (bounds.Left - strokeWidth) * scaleX);

        float ascentPx = baselineAscentPx;
        float baseline = (settings.BaselineY + settings.YOffset) * scale;
        float y = baseline - (float)(ascentPx * scaleY);

        using Bitmap highFillMask = new(highWidth, highHeight, PixelFormat.Format32bppArgb);
        using Bitmap highStrokeMask = new(highWidth, highHeight, PixelFormat.Format32bppArgb);
        using Matrix transform = new((float)scaleX, 0, 0, (float)scaleY, x, y);
        using GraphicsPath path = (GraphicsPath)basePath.Clone();
        path.Transform(transform);

        DrawFillMask(highFillMask, path, fillStrokeWidth);
        DrawStrokeMask(highStrokeMask, path, strokeWidth);

        using Bitmap lowFillMask = scale > 1 ? DownsampleAverage(highFillMask, scale) : (Bitmap)highFillMask.Clone();
        using Bitmap lowStrokeMask = scale > 1 ? DownsampleAverage(highStrokeMask, scale) : (Bitmap)highStrokeMask.Clone();
        return ((Bitmap)lowFillMask.Clone(), (Bitmap)lowStrokeMask.Clone());
    }

    private static float GetAscentPx(FontFamily family, FontStyle style, float emSize)
        => emSize * family.GetCellAscent(style) / family.GetEmHeight(style);

    private static float AddBulletPath(GraphicsPath path, float x, float baselineAscentPx, float emSize, float yOffset)
    {
        float diameter = Math.Max(1, emSize * 0.22f);
        float centerY = baselineAscentPx - emSize * 0.35f + yOffset;
        path.AddEllipse(x, centerY - diameter / 2f, diameter, diameter);
        return diameter;
    }

    private static float MeasureRunAdvance(
        Graphics graphics,
        string text,
        FontFamily family,
        FontStyle style,
        float emSize,
        StringFormat format)
    {
        using Font font = new(family, emSize, style, GraphicsUnit.Pixel);
        return graphics.MeasureString(text, font, PointF.Empty, format).Width;
    }

    private static StringFormat CreateTypographicStringFormat(bool measureTrailingSpaces)
    {
        var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        if (measureTrailingSpaces)
        {
            format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        }

        return format;
    }

    private static void DrawFillMask(Bitmap mask, GraphicsPath path, float fillStrokeWidth)
    {
        using Graphics graphics = Graphics.FromImage(mask);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        if (fillStrokeWidth > 0)
        {
            using Pen pen = new(Color.White, fillStrokeWidth)
            {
                LineJoin = LineJoin.Round,
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            graphics.DrawPath(pen, path);
        }

        using SolidBrush brush = new(Color.White);
        graphics.FillPath(brush, path);
    }

    private static void DrawStrokeMask(Bitmap mask, GraphicsPath path, float strokeWidth)
    {
        using Graphics graphics = Graphics.FromImage(mask);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        if (strokeWidth > 0)
        {
            using Pen pen = new(Color.White, strokeWidth)
            {
                LineJoin = LineJoin.Round,
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            graphics.DrawPath(pen, path);
        }

        using SolidBrush brush = new(Color.White);
        graphics.FillPath(brush, path);
    }
}
