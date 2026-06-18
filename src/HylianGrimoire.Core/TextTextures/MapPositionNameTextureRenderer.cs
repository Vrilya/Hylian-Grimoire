using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static class MapPositionNameTextureRenderer
{
    public static Bitmap Render(string text, TextTextureFont font, MapPositionNameTextureRenderSettings settings)
    {
        string[] lines = text
            .ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Render(
            lines.ElementAtOrDefault(0) ?? string.Empty,
            lines.ElementAtOrDefault(1) ?? string.Empty,
            font,
            settings);
    }

    public static Bitmap Render(
        string firstLine,
        string secondLine,
        TextTextureFont font,
        MapPositionNameTextureRenderSettings settings)
    {
        if (!File.Exists(font.Path))
        {
            throw new FileNotFoundException("Map-position-name texture font is missing.", font.Path);
        }

        if (string.IsNullOrWhiteSpace(firstLine) && string.IsNullOrWhiteSpace(secondLine))
        {
            return new Bitmap(MapPositionNameTextureCatalog.Width, MapPositionNameTextureCatalog.Height, PixelFormat.Format32bppArgb);
        }

        using TextTextureDrawingFont drawingFont = new(font);
        return Render(
            [new(firstLine, settings.FirstLineWidthScale), new(secondLine, settings.SecondLineWidthScale)],
            drawingFont.Family,
            drawingFont.Style,
            settings);
    }

    private static Bitmap Render(
        IReadOnlyList<MapPositionNameLine> inputLines,
        FontFamily family,
        FontStyle style,
        MapPositionNameTextureRenderSettings settings)
    {
        MapPositionNameLine[] lines = inputLines
            .Where(line => !string.IsNullOrWhiteSpace(line.Text))
            .Select(line => line with { Text = line.Text.Trim() })
            .ToArray();
        if (lines.Length == 0)
        {
            return new Bitmap(MapPositionNameTextureCatalog.Width, MapPositionNameTextureCatalog.Height, PixelFormat.Format32bppArgb);
        }

        var fillLines = new List<Bitmap>(lines.Length);
        var strokeLines = new List<Bitmap>(lines.Length);
        try
        {
            foreach (MapPositionNameLine line in lines)
            {
                (Bitmap fill, Bitmap stroke) = CreateLineMasks(line.Text, line.WidthScale, family, style, settings);
                fillLines.Add(fill);
                strokeLines.Add(stroke);
            }

            using Bitmap fillCanvas = TextTextureBitmapOps.CreateBlankMask(MapPositionNameTextureCatalog.Width, MapPositionNameTextureCatalog.Height);
            using Bitmap strokeCanvas = TextTextureBitmapOps.CreateBlankMask(MapPositionNameTextureCatalog.Width, MapPositionNameTextureCatalog.Height);

            int totalHeight = strokeLines.Sum(mask => mask.Height) + Math.Max(0, lines.Length - 1) * settings.LineSpacing;
            int y = (int)Math.Round((MapPositionNameTextureCatalog.Height - totalHeight) / 2d);
            for (int i = 0; i < lines.Length; i++)
            {
                int x = (int)Math.Round((MapPositionNameTextureCatalog.Width - strokeLines[i].Width) / 2d);
                TextTextureBitmapOps.PasteMask(fillLines[i], fillCanvas, x, y);
                TextTextureBitmapOps.PasteMask(strokeLines[i], strokeCanvas, x, y);
                y += strokeLines[i].Height + settings.LineSpacing;
            }

            using Bitmap blurredStrokeCanvas = TextTextureBitmapOps.BlurAlphaMask(strokeCanvas, settings.StrokeBlurRadius);
            return Compose(fillCanvas, strokeCanvas, blurredStrokeCanvas, settings);
        }
        finally
        {
            foreach (Bitmap fill in fillLines)
            {
                fill.Dispose();
            }

            foreach (Bitmap stroke in strokeLines)
            {
                stroke.Dispose();
            }
        }
    }

    private static (Bitmap Fill, Bitmap Stroke) CreateLineMasks(
        string text,
        double widthScale,
        FontFamily family,
        FontStyle style,
        MapPositionNameTextureRenderSettings settings)
    {
        int scale = Math.Max(1, settings.RenderScale);
        float emSize = Math.Max(1, (float)(settings.FontSize * scale));
        float strokeWidth = Math.Max(0, (float)(settings.StrokeWidth * scale));
        using GraphicsPath rawPath = new();
        rawPath.AddString(text, family, (int)style, emSize, Point.Empty, StringFormat.GenericTypographic);
        RectangleF bounds = rawPath.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return (new Bitmap(1, 1, PixelFormat.Format32bppArgb), new Bitmap(1, 1, PixelFormat.Format32bppArgb));
        }

        int padding = Math.Max(6, (int)Math.Ceiling(settings.StrokeWidth) + 4) * scale;
        int localWidth = Math.Max(1, (int)Math.Ceiling(bounds.Width + strokeWidth * 2 + padding * 2));
        int localHeight = Math.Max(1, (int)Math.Ceiling(bounds.Height + strokeWidth * 2 + padding * 2));
        using GraphicsPath path = (GraphicsPath)rawPath.Clone();
        using Matrix transform = new(1, 0, 0, 1, padding - (bounds.Left - strokeWidth), padding - (bounds.Top - strokeWidth));
        path.Transform(transform);

        using Bitmap highFillMask = new(localWidth, localHeight, PixelFormat.Format32bppArgb);
        using Bitmap highStrokeMask = new(localWidth, localHeight, PixelFormat.Format32bppArgb);
        DrawFillMask(highFillMask, path);
        DrawStrokeMask(highStrokeMask, path, strokeWidth);

        Rectangle contentBounds = TextTextureBitmapOps.GetContentBounds(highStrokeMask);
        if (contentBounds.IsEmpty)
        {
            return (new Bitmap(1, 1, PixelFormat.Format32bppArgb), new Bitmap(1, 1, PixelFormat.Format32bppArgb));
        }

        using Bitmap croppedFill = TextTextureBitmapOps.Crop(highFillMask, contentBounds);
        using Bitmap croppedStroke = TextTextureBitmapOps.Crop(highStrokeMask, contentBounds);
        int naturalWidth = Math.Max(1, (int)Math.Round(croppedStroke.Width / (double)scale));
        int naturalHeight = Math.Max(1, (int)Math.Round(croppedStroke.Height / (double)scale));
        using Bitmap naturalFill = TextTextureBitmapOps.ResizeMask(croppedFill, naturalWidth, naturalHeight);
        using Bitmap naturalStroke = TextTextureBitmapOps.ResizeMask(croppedStroke, naturalWidth, naturalHeight);

        int targetWidth = Math.Max(1, (int)Math.Round(naturalStroke.Width * Math.Max(1.0, widthScale) / 100d));
        targetWidth = Math.Min(targetWidth, MapPositionNameTextureCatalog.Width);
        if (targetWidth == naturalStroke.Width)
        {
            using Bitmap adjustedStroke = AdjustStrokeMask(naturalStroke, settings);
            return ((Bitmap)naturalFill.Clone(), (Bitmap)adjustedStroke.Clone());
        }

        using Bitmap targetFill = TextTextureBitmapOps.ResizeMask(naturalFill, targetWidth, naturalStroke.Height);
        using Bitmap targetStroke = TextTextureBitmapOps.ResizeMask(naturalStroke, targetWidth, naturalStroke.Height);
        using Bitmap adjustedTargetStroke = AdjustStrokeMask(targetStroke, settings);
        return ((Bitmap)targetFill.Clone(), (Bitmap)adjustedTargetStroke.Clone());
    }

    private static Bitmap Compose(
        Bitmap fillMask,
        Bitmap strokeMask,
        Bitmap blurredStrokeMask,
        MapPositionNameTextureRenderSettings settings)
    {
        Bitmap output = new(MapPositionNameTextureCatalog.Width, MapPositionNameTextureCatalog.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < MapPositionNameTextureCatalog.Height; y++)
        {
            for (int x = 0; x < MapPositionNameTextureCatalog.Width; x++)
            {
                int fillValue = fillMask.GetPixel(x, y).A;
                int strokeValue = strokeMask.GetPixel(x, y).A;
                int blurredValue = Math.Min(255, (int)Math.Round(blurredStrokeMask.GetPixel(x, y).A * settings.StrokeBlurStrength / 100d));
                if (fillValue >= settings.FillMin)
                {
                    int gray = fillValue >= settings.WhiteThreshold
                        ? 255
                        : TextTextureBitmapOps.NearestIaStep(Math.Min(255, (int)Math.Round(fillValue * settings.FillBoost / 100d)));
                    output.SetPixel(x, y, Color.FromArgb(255, gray, gray, gray));
                }
                else if (strokeValue > 0 || blurredValue > 0)
                {
                    int alpha = TextTextureBitmapOps.NearestIaStep(Math.Max(strokeValue, blurredValue));
                    if (alpha > 0)
                    {
                        output.SetPixel(x, y, Color.FromArgb(alpha, 0, 0, 0));
                    }
                }
            }
        }

        return output;
    }

    private static Bitmap AdjustStrokeMask(Bitmap mask, MapPositionNameTextureRenderSettings settings)
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
                int adjusted = (int)Math.Round(255 * Math.Pow(scaled / 255d, settings.StrokeGamma));
                output.SetPixel(x, y, Color.FromArgb(adjusted, 255, 255, 255));
            }
        }

        return output;
    }

    private static void DrawFillMask(Bitmap mask, GraphicsPath path)
    {
        using Graphics graphics = Graphics.FromImage(mask);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
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

    private sealed record MapPositionNameLine(string Text, double WidthScale);
}
