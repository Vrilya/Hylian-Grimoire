using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace HylianGrimoire.TextTextures;

public static class BossTitleCardTextureRenderer
{
    private static readonly int[] Ia4Steps = [0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255];

    public static Bitmap Render(
        string topText,
        string bottomText,
        string topFontPath,
        string bottomFontPath,
        BossTitleCardTextureRenderSettings settings)
        => Render(topText, bottomText, TextTextureFont.FromPath(topFontPath), TextTextureFont.FromPath(bottomFontPath), settings);

    public static Bitmap Render(
        string topText,
        string bottomText,
        TextTextureFont topFont,
        TextTextureFont bottomFont,
        BossTitleCardTextureRenderSettings settings)
    {
        if (!File.Exists(topFont.Path))
        {
            throw new FileNotFoundException("Boss title-card top font is missing.", topFont.Path);
        }

        if (!File.Exists(bottomFont.Path))
        {
            throw new FileNotFoundException("Boss title-card boss font is missing.", bottomFont.Path);
        }

        if (string.IsNullOrWhiteSpace(topText) && string.IsNullOrWhiteSpace(bottomText))
        {
            return new Bitmap(BossTitleCardTextureCatalog.Width, BossTitleCardTextureCatalog.Height, PixelFormat.Format32bppArgb);
        }

        using TextTextureDrawingFont topDrawingFont = new(topFont);
        using TextTextureDrawingFont bottomDrawingFont = new(bottomFont);

        BossTitleCardComponents components = RenderComponents(
            topText,
            bottomText,
            topDrawingFont.Family,
            topDrawingFont.Style,
            bottomDrawingFont.Family,
            bottomDrawingFont.Style,
            settings);
        using (components.TopFill)
        using (components.TopStroke)
        using (components.BottomFill)
        using (components.BottomStroke)
        {
            return Compose(components, settings);
        }
    }

    private static BossTitleCardComponents RenderComponents(
        string topText,
        string bottomText,
        FontFamily topFamily,
        FontStyle topStyle,
        FontFamily bottomFamily,
        FontStyle bottomStyle,
        BossTitleCardTextureRenderSettings settings)
    {
        Bitmap topFillCanvas = CreateBlankMask();
        Bitmap topStrokeCanvas = CreateBlankMask();
        Bitmap bottomFillCanvas = CreateBlankMask();
        Bitmap bottomStrokeCanvas = CreateBlankMask();

        (Bitmap topFill, Bitmap topStroke) = CreateLineMasks(
            topText,
            topFamily,
            topStyle,
            settings.TopFontSize,
            settings.TopStrokeWidth,
            100.0,
            settings.TopMaxWidth,
            tracking: 0,
            pairKerning: new Dictionary<string, int>(StringComparer.Ordinal),
            fitHeight: false,
            settings.RenderScale);
        using (topFill)
        using (topStroke)
        using (Bitmap adjustedTopStroke = AdjustStrokeMask(topStroke, settings.TopStrokeAlpha, settings.TopStrokeGamma))
        {
            Dictionary<string, int> bottomKerning = new(StringComparer.Ordinal)
            {
                ["LV"] = settings.BottomLvKerning,
            };
            (Bitmap bottomFill, Bitmap bottomStroke) = CreateLineMasks(
                bottomText,
                bottomFamily,
                bottomStyle,
                settings.BottomFontSize,
                settings.BottomStrokeWidth,
                settings.BottomWidthScale,
                settings.BottomMaxWidth,
                settings.BottomTracking,
                bottomKerning,
                fitHeight: false,
                settings.RenderScale);
            using (bottomFill)
            using (bottomStroke)
            using (Bitmap adjustedBottomStroke = AdjustStrokeMask(bottomStroke, settings.BottomStrokeAlpha, settings.BottomStrokeGamma))
            {
                if (!string.IsNullOrWhiteSpace(topText))
                {
                    PasteLine(topFill, topFillCanvas, settings.TopY, settings.TopCenter, settings.TopXNudge);
                    PasteLine(adjustedTopStroke, topStrokeCanvas, settings.TopY, settings.TopCenter, settings.TopXNudge);
                    PasteLine(bottomFill, bottomFillCanvas, settings.BottomY, settings.BottomCenter, settings.BottomXNudge);
                    PasteLine(adjustedBottomStroke, bottomStrokeCanvas, settings.BottomY, settings.BottomCenter, settings.BottomXNudge);
                }
                else
                {
                    int bottomY = (int)((BossTitleCardTextureCatalog.Height - adjustedBottomStroke.Height) / 2d + 0.5);
                    PasteLine(bottomFill, bottomFillCanvas, bottomY, settings.BottomCenter, settings.BottomXNudge);
                    PasteLine(adjustedBottomStroke, bottomStrokeCanvas, bottomY, settings.BottomCenter, settings.BottomXNudge);
                }
            }
        }

        return new BossTitleCardComponents(topFillCanvas, topStrokeCanvas, bottomFillCanvas, bottomStrokeCanvas);
    }

    private static (Bitmap Fill, Bitmap Stroke) CreateLineMasks(
        string text,
        FontFamily family,
        FontStyle style,
        double fontSize,
        double strokeWidth,
        double widthScale,
        int maxWidth,
        int tracking,
        IReadOnlyDictionary<string, int> pairKerning,
        bool fitHeight,
        int renderScale)
    {
        if (string.IsNullOrEmpty(text))
        {
            return (new Bitmap(1, 1, PixelFormat.Format32bppArgb), new Bitmap(1, 1, PixelFormat.Format32bppArgb));
        }

        int scale = Math.Max(1, renderScale);
        float emSize = Math.Max(1, (float)(fontSize * scale));
        float scaledStrokeWidth = Math.Max(0, (float)(strokeWidth * scale));
        using GraphicsPath rawPath = BuildLinePath(text, family, style, emSize, tracking * scale, pairKerning, scale);
        RectangleF bounds = rawPath.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return (new Bitmap(1, 1, PixelFormat.Format32bppArgb), new Bitmap(1, 1, PixelFormat.Format32bppArgb));
        }

        int padding = Math.Max(6, (int)Math.Ceiling(strokeWidth) + 4) * scale;
        int localWidth = Math.Max(1, (int)Math.Ceiling(bounds.Width + scaledStrokeWidth * 2 + padding * 2));
        int localHeight = Math.Max(1, (int)Math.Ceiling(bounds.Height + scaledStrokeWidth * 2 + padding * 2));
        using GraphicsPath path = (GraphicsPath)rawPath.Clone();
        using Matrix transform = new(1, 0, 0, 1, padding - (bounds.Left - scaledStrokeWidth), padding - (bounds.Top - scaledStrokeWidth));
        path.Transform(transform);

        using Bitmap highFillMask = new(localWidth, localHeight, PixelFormat.Format32bppArgb);
        using Bitmap highStrokeMask = new(localWidth, localHeight, PixelFormat.Format32bppArgb);
        DrawFillMask(highFillMask, path);
        DrawStrokeMask(highStrokeMask, path, scaledStrokeWidth);

        Rectangle contentBounds = GetContentBounds(highStrokeMask);
        if (contentBounds.IsEmpty)
        {
            return (new Bitmap(1, 1, PixelFormat.Format32bppArgb), new Bitmap(1, 1, PixelFormat.Format32bppArgb));
        }

        using Bitmap croppedFill = Crop(highFillMask, contentBounds);
        using Bitmap croppedStroke = Crop(highStrokeMask, contentBounds);
        int naturalWidth = Math.Max(1, (int)Math.Round(croppedStroke.Width / (double)scale));
        int naturalHeight = Math.Max(1, (int)Math.Round(croppedStroke.Height / (double)scale));
        using Bitmap naturalFill = ResizeMask(croppedFill, naturalWidth, naturalHeight);
        using Bitmap naturalStroke = ResizeMask(croppedStroke, naturalWidth, naturalHeight);

        int targetWidth = Math.Max(1, (int)Math.Round(naturalStroke.Width * Math.Max(1.0, widthScale) / 100d));
        int targetHeight = naturalStroke.Height;
        if (targetWidth > maxWidth)
        {
            targetHeight = fitHeight
                ? Math.Max(1, (int)Math.Round(targetHeight * maxWidth / (double)targetWidth))
                : targetHeight;
            targetWidth = maxWidth;
        }

        if (targetWidth == naturalStroke.Width && targetHeight == naturalStroke.Height)
        {
            return ((Bitmap)naturalFill.Clone(), (Bitmap)naturalStroke.Clone());
        }

        return (ResizeMask(naturalFill, targetWidth, targetHeight), ResizeMask(naturalStroke, targetWidth, targetHeight));
    }

    private static GraphicsPath BuildLinePath(
        string text,
        FontFamily family,
        FontStyle style,
        float emSize,
        int tracking,
        IReadOnlyDictionary<string, int> pairKerning,
        int scale)
    {
        GraphicsPath path = new();
        if (tracking == 0 && pairKerning.Count == 0)
        {
            path.AddString(text, family, (int)style, emSize, Point.Empty, StringFormat.GenericTypographic);
            return path;
        }

        using Bitmap probeBitmap = new(1, 1, PixelFormat.Format32bppArgb);
        using Graphics probe = Graphics.FromImage(probeBitmap);
        probe.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using Font font = new(family, emSize, style, GraphicsUnit.Pixel);
        float spaceAdvance = MeasureSpaceAdvance(probe, font);
        float x = 0;
        for (int i = 0; i < text.Length; i++)
        {
            string character = text[i].ToString();
            if (char.IsWhiteSpace(text[i]))
            {
                x += spaceAdvance + tracking;
                continue;
            }

            using GraphicsPath characterPath = new();
            characterPath.AddString(character, family, (int)style, emSize, new PointF(x, 0), StringFormat.GenericTypographic);
            if (characterPath.PointCount > 0)
            {
                path.AddPath(characterPath, connect: false);
            }

            x += MeasureCharacterAdvance(probe, font, character, characterPath) + tracking;
            if (i < text.Length - 1 && pairKerning.TryGetValue(text.Substring(i, 2), out int adjustment))
            {
                x += adjustment * scale;
            }
        }

        return path;
    }

    private static float MeasureCharacterAdvance(Graphics probe, Font font, string character, GraphicsPath characterPath)
    {
        float advance = probe.MeasureString(character, font, PointF.Empty, StringFormat.GenericTypographic).Width;
        if (advance > 0)
        {
            return advance;
        }

        return characterPath.GetBounds().Width;
    }

    private static float MeasureSpaceAdvance(Graphics probe, Font font)
    {
        float measured = probe.MeasureString(" ", font, PointF.Empty, StringFormat.GenericTypographic).Width;
        if (measured > 0)
        {
            return measured;
        }

        float spaced = probe.MeasureString("M M", font, PointF.Empty, StringFormat.GenericTypographic).Width;
        float compact = probe.MeasureString("MM", font, PointF.Empty, StringFormat.GenericTypographic).Width;
        float contextual = spaced - compact;
        return contextual > 0 ? contextual : font.Size * 0.25f;
    }

    private static void PasteLine(Bitmap source, Bitmap destination, int y, bool center, int xNudge)
    {
        if (GetContentBounds(source).IsEmpty)
        {
            return;
        }

        int x = center
            ? (int)((BossTitleCardTextureCatalog.Width - source.Width) / 2d + 0.5) + xNudge
            : xNudge;
        PasteMask(source, destination, x, y);
    }

    private static Bitmap AdjustStrokeMask(Bitmap mask, int alpha, double gamma)
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

                int scaled = Math.Min(255, (int)Math.Round(value * alpha / 100d));
                int adjusted = (int)Math.Round(255 * Math.Pow(scaled / 255d, gamma));
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

    private static Bitmap Compose(BossTitleCardComponents components, BossTitleCardTextureRenderSettings settings)
    {
        Bitmap output = new(BossTitleCardTextureCatalog.Width, BossTitleCardTextureCatalog.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < BossTitleCardTextureCatalog.Height; y++)
        {
            for (int x = 0; x < BossTitleCardTextureCatalog.Width; x++)
            {
                int bottomFillValue = components.BottomFill.GetPixel(x, y).A;
                int bottomStrokeValue = components.BottomStroke.GetPixel(x, y).A;
                int topFillValue = components.TopFill.GetPixel(x, y).A;
                int topStrokeValue = components.TopStroke.GetPixel(x, y).A;

                if (bottomFillValue > 0)
                {
                    int gray = bottomFillValue >= settings.BottomWhiteThreshold
                        ? 255
                        : NearestIa4Step(Math.Min(255, (int)Math.Round(bottomFillValue * settings.BottomFillBoost / 100d)));
                    output.SetPixel(x, y, Color.FromArgb(255, gray, gray, gray));
                }
                else if (bottomStrokeValue > 0)
                {
                    int alpha = NearestIa4Step(bottomStrokeValue);
                    if (alpha > 0)
                    {
                        output.SetPixel(x, y, Color.FromArgb(alpha, 0, 0, 0));
                    }
                }
                else if (topFillValue >= settings.TopFillMin)
                {
                    int gray = topFillValue >= settings.TopWhiteThreshold
                        ? 255
                        : NearestIa4Step(Math.Min(255, (int)Math.Round(topFillValue * settings.TopFillBoost / 100d)));
                    output.SetPixel(x, y, Color.FromArgb(255, gray, gray, gray));
                }
                else if (topStrokeValue > 0)
                {
                    int alpha = NearestIa4Step(topStrokeValue);
                    if (alpha > 0)
                    {
                        output.SetPixel(x, y, Color.FromArgb(alpha, 0, 0, 0));
                    }
                }
            }
        }

        return output;
    }

    private static Rectangle GetContentBounds(Bitmap mask)
    {
        int left = mask.Width;
        int top = mask.Height;
        int right = -1;
        int bottom = -1;
        for (int y = 0; y < mask.Height; y++)
        {
            for (int x = 0; x < mask.Width; x++)
            {
                if (mask.GetPixel(x, y).A == 0)
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return right < left || bottom < top ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static Bitmap Crop(Bitmap source, Rectangle bounds)
    {
        Bitmap output = new(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(output);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.DrawImage(
            source,
            new Rectangle(0, 0, bounds.Width, bounds.Height),
            bounds,
            GraphicsUnit.Pixel);
        return output;
    }

    private static Bitmap ResizeMask(Bitmap source, int width, int height)
    {
        if (source.Width == width && source.Height == height)
        {
            return (Bitmap)source.Clone();
        }

        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(output);
        graphics.Clear(Color.Transparent);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.DrawImage(
            source,
            new Rectangle(0, 0, width, height),
            new Rectangle(0, 0, source.Width, source.Height),
            GraphicsUnit.Pixel);
        return output;
    }

    private static void PasteMask(Bitmap source, Bitmap destination, int destinationX, int destinationY)
    {
        for (int y = 0; y < source.Height; y++)
        {
            int targetY = destinationY + y;
            if (targetY < 0 || targetY >= destination.Height)
            {
                continue;
            }

            for (int x = 0; x < source.Width; x++)
            {
                int targetX = destinationX + x;
                if (targetX < 0 || targetX >= destination.Width)
                {
                    continue;
                }

                destination.SetPixel(targetX, targetY, source.GetPixel(x, y));
            }
        }
    }

    private static int NearestIa4Step(int value)
    {
        int nearest = Ia4Steps[0];
        int nearestDistance = Math.Abs(value - nearest);
        for (int i = 1; i < Ia4Steps.Length; i++)
        {
            int distance = Math.Abs(value - Ia4Steps[i]);
            if (distance < nearestDistance)
            {
                nearest = Ia4Steps[i];
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private static Bitmap CreateBlankMask()
        => new(BossTitleCardTextureCatalog.Width, BossTitleCardTextureCatalog.Height, PixelFormat.Format32bppArgb);

    private sealed record BossTitleCardComponents(Bitmap TopFill, Bitmap TopStroke, Bitmap BottomFill, Bitmap BottomStroke);
}
