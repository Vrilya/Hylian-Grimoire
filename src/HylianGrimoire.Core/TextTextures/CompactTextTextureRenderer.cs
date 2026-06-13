using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;

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

public static class CompactTextTextureRenderer
{
    private static readonly int[] Ia8Steps = [0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255];

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

    private static Bitmap AdjustStrokeMask(Bitmap mask, CompactTextTextureRenderSettings settings)
    {
        Bitmap output = new(mask.Width, mask.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < mask.Height; y++)
        {
            for (int x = 0; x < mask.Width; x++)
            {
                int value = mask.GetPixel(x, y).A;
                if (value == 0)
                {
                    continue;
                }

                int scaled = Math.Min(255, (int)Math.Round(value * settings.StrokeAlpha / 100d));
                output.SetPixel(x, y, Color.FromArgb(scaled, 255, 255, 255));
            }
        }

        return output;
    }

    private static Bitmap Compose(
        Bitmap fillMask,
        Bitmap strokeMask,
        Bitmap blurredStrokeMask,
        CompactTextTextureRenderSettings settings,
        int width,
        int height)
    {
        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int fillValue = fillMask.GetPixel(x, y).A;
                int strokeValue = strokeMask.GetPixel(x, y).A;
                int blurredValue = Math.Min(255, (int)Math.Round(blurredStrokeMask.GetPixel(x, y).A * settings.StrokeBlurStrength / 100d));
                bool hasFill = fillValue >= settings.FillThreshold;
                bool hasStroke = strokeValue >= settings.StrokeThreshold || blurredValue >= settings.StrokeThreshold;
                if (!hasFill && !hasStroke)
                {
                    continue;
                }

                if (hasFill)
                {
                    int boosted = Math.Min(255, (int)Math.Round(fillValue * settings.FillBoost / 100d));
                    int gray = GetFillGray(fillValue, boosted, settings);
                    if (settings.BlendFillAndStrokeEdges && hasStroke)
                    {
                        output.SetPixel(x, y, BlendFillOverStroke(gray, Math.Max(strokeValue, blurredValue)));
                        continue;
                    }

                    output.SetPixel(x, y, Color.FromArgb(255, gray, gray, gray));
                    continue;
                }

                int alpha = NearestIa8Step(Math.Max(strokeValue, blurredValue));
                if (alpha > 0)
                {
                    output.SetPixel(x, y, Color.FromArgb(alpha, 0, 0, 0));
                }
            }
        }

        return output;
    }

    private static int GetFillGray(int fillValue, int boosted, CompactTextTextureRenderSettings settings)
    {
        int gray = fillValue >= settings.WhiteThreshold ? 255 : NearestIa8Step(boosted);
        return Math.Max(gray, settings.FillFloor);
    }

    private static Color BlendFillOverStroke(int fillAlpha, int strokeAlpha)
    {
        double whiteAlpha = Math.Clamp(fillAlpha, 0, 255);
        double blackAlpha = Math.Clamp(strokeAlpha, 0, 255);
        double alpha = whiteAlpha + blackAlpha * (1 - whiteAlpha / 255d);
        if (alpha <= 0)
        {
            return Color.Transparent;
        }

        int outputAlpha = NearestIa8Step((int)Math.Round(alpha));
        int gray = NearestIa8Step((int)Math.Round(255 * whiteAlpha / alpha));
        return Color.FromArgb(outputAlpha, gray, gray, gray);
    }

    private static Bitmap BlurAlphaMask(Bitmap source, double radius)
    {
        if (radius <= 0)
        {
            return (Bitmap)source.Clone();
        }

        int kernelRadius = Math.Max(1, (int)Math.Ceiling(radius * 2));
        double[] kernel = CreateGaussianKernel(radius, kernelRadius);
        using Bitmap horizontal = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
        Bitmap output = new(source.Width, source.Height, PixelFormat.Format32bppArgb);

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                double alpha = 0;
                for (int offset = -kernelRadius; offset <= kernelRadius; offset++)
                {
                    alpha += GetAlphaOrZero(source, x + offset, y) * kernel[offset + kernelRadius];
                }

                int value = Math.Clamp((int)Math.Round(alpha), 0, 255);
                horizontal.SetPixel(x, y, Color.FromArgb(value, 255, 255, 255));
            }
        }

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                double alpha = 0;
                for (int offset = -kernelRadius; offset <= kernelRadius; offset++)
                {
                    alpha += GetAlphaOrZero(horizontal, x, y + offset) * kernel[offset + kernelRadius];
                }

                int value = Math.Clamp((int)Math.Round(alpha), 0, 255);
                output.SetPixel(x, y, Color.FromArgb(value, 255, 255, 255));
            }
        }

        return output;
    }

    private static double[] CreateGaussianKernel(double radius, int kernelRadius)
    {
        double sigma = Math.Max(0.1, radius);
        double[] kernel = new double[kernelRadius * 2 + 1];
        double total = 0;
        for (int offset = -kernelRadius; offset <= kernelRadius; offset++)
        {
            double value = Math.Exp(-(offset * offset) / (2 * sigma * sigma));
            kernel[offset + kernelRadius] = value;
            total += value;
        }

        for (int i = 0; i < kernel.Length; i++)
        {
            kernel[i] /= total;
        }

        return kernel;
    }

    private static int GetAlphaOrZero(Bitmap source, int x, int y)
    {
        if (x < 0 || x >= source.Width || y < 0 || y >= source.Height)
        {
            return 0;
        }

        return source.GetPixel(x, y).A;
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

    private static Bitmap DownsampleAverage(Bitmap source, int scale)
    {
        int width = Math.Max(1, source.Width / scale);
        int height = Math.Max(1, source.Height / scale);
        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int total = 0;
                for (int sy = 0; sy < scale; sy++)
                {
                    for (int sx = 0; sx < scale; sx++)
                    {
                        total += source.GetPixel(x * scale + sx, y * scale + sy).A;
                    }
                }

                int value = (int)Math.Round(total / (double)(scale * scale));
                output.SetPixel(x, y, Color.FromArgb(value, 255, 255, 255));
            }
        }

        return output;
    }

    private static int NearestIa8Step(int value)
    {
        int nearest = Ia8Steps[0];
        int nearestDistance = Math.Abs(value - nearest);
        for (int i = 1; i < Ia8Steps.Length; i++)
        {
            int distance = Math.Abs(value - Ia8Steps[i]);
            if (distance < nearestDistance)
            {
                nearest = Ia8Steps[i];
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private static Bitmap CreateBlankMask(int width, int height)
        => new(width, height, PixelFormat.Format32bppArgb);
}
