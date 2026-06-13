using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace HylianGrimoire.TextTextures;

public static class ContinuePlayingTextureRenderer
{
    private static readonly int[] Ia8Steps = [0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255];

    public static Bitmap Render(string text, string fontPath, ContinuePlayingTextureRenderSettings settings)
        => Render(text, TextTextureFont.FromPath(fontPath), settings);

    public static Bitmap Render(string text, TextTextureFont font, ContinuePlayingTextureRenderSettings settings)
    {
        if (!File.Exists(font.Path))
        {
            throw new FileNotFoundException("Continue Playing texture font is missing.", font.Path);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return CreateBlankCanvas();
        }

        using TextTextureDrawingFont drawingFont = new(font);
        (Bitmap fillMask, Bitmap strokeMask) = CreateLineMasks(text, drawingFont.Family, drawingFont.Style, settings);
        using (fillMask)
        using (strokeMask)
        using (Bitmap adjustedStroke = AdjustStrokeMask(strokeMask, settings))
        using (Bitmap blurredStroke = BlurAlphaMask(adjustedStroke, settings.BlurRadius))
        {
            return Compose(fillMask, adjustedStroke, blurredStroke, settings);
        }
    }

    private static (Bitmap Fill, Bitmap Stroke) CreateLineMasks(
        string text,
        FontFamily family,
        FontStyle style,
        ContinuePlayingTextureRenderSettings settings)
    {
        int scale = Math.Max(1, settings.RenderScale);
        float emSize = Math.Max(1, (float)(settings.FontSize * scale));
        float strokeWidth = Math.Max(0, (float)(settings.StrokeWidth * scale));
        float tracking = (float)(settings.Tracking * scale);
        double widthScale = Math.Max(1, settings.WidthScale) / 100d;
        float glyphGap = (float)(settings.GlyphGap / widthScale * scale);
        using GraphicsPath rawPath = BuildLinePath(text, family, style, emSize, tracking, glyphGap);
        RectangleF bounds = rawPath.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return (CreateBlankMask(), CreateBlankMask());
        }

        int padding = Math.Max(8, (int)Math.Ceiling(settings.StrokeWidth) + 6) * scale;
        int localWidth = Math.Max(1, (int)Math.Ceiling(bounds.Width + strokeWidth * 2 + padding * 2));
        int localHeight = Math.Max(1, (int)Math.Ceiling(bounds.Height + strokeWidth * 2 + padding * 2));
        using GraphicsPath path = (GraphicsPath)rawPath.Clone();
        using Matrix transform = new(1, 0, 0, 1, padding - (bounds.Left - strokeWidth), padding - (bounds.Top - strokeWidth));
        path.Transform(transform);

        using Bitmap highFillMask = new(localWidth, localHeight, PixelFormat.Format32bppArgb);
        using Bitmap highStrokeMask = new(localWidth, localHeight, PixelFormat.Format32bppArgb);
        DrawFillMask(highFillMask, path);
        DrawStrokeMask(highStrokeMask, path, strokeWidth);

        Rectangle contentBounds = GetContentBounds(highStrokeMask);
        if (contentBounds.IsEmpty)
        {
            return (CreateBlankMask(), CreateBlankMask());
        }

        using Bitmap croppedFill = Crop(highFillMask, contentBounds);
        using Bitmap croppedStroke = Crop(highStrokeMask, contentBounds);
        int naturalWidth = Math.Max(1, (int)Math.Round(croppedStroke.Width / (double)scale));
        int naturalHeight = Math.Max(1, (int)Math.Round(croppedStroke.Height / (double)scale));
        using Bitmap naturalFill = ResizeMask(croppedFill, naturalWidth, naturalHeight);
        using Bitmap naturalStroke = ResizeMask(croppedStroke, naturalWidth, naturalHeight);

        int targetWidth = Math.Max(1, (int)Math.Round(naturalStroke.Width * widthScale));
        int targetHeight = Math.Max(1, (int)Math.Round(naturalStroke.Height * Math.Max(1, settings.HeightScale) / 100d));
        using Bitmap targetFill = ResizeMask(naturalFill, targetWidth, targetHeight);
        using Bitmap targetStroke = ResizeMask(naturalStroke, targetWidth, targetHeight);

        Bitmap canvasFill = CreateBlankMask();
        Bitmap canvasStroke = CreateBlankMask();
        double xNudge = settings.Center ? ContinuePlayingTextureRenderSettings.CenteredXNudge : settings.XNudge;
        int destinationX = (int)Math.Round((GameOverTextureCatalog.ContinuePlayingWidth - targetStroke.Width) / 2d + xNudge);
        int destinationY = (int)Math.Round((GameOverTextureCatalog.ContinuePlayingHeight - targetStroke.Height) / 2d + settings.YNudge);
        PasteMask(targetFill, canvasFill, destinationX, destinationY);
        PasteMask(targetStroke, canvasStroke, destinationX, destinationY);
        return (canvasFill, canvasStroke);
    }

    private static GraphicsPath BuildLinePath(
        string text,
        FontFamily family,
        FontStyle style,
        float emSize,
        float tracking,
        float glyphGap)
    {
        GraphicsPath path = new();
        using Bitmap probeBitmap = new(1, 1, PixelFormat.Format32bppArgb);
        using Graphics probe = Graphics.FromImage(probeBitmap);
        probe.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using Font font = new(family, emSize, style, GraphicsUnit.Pixel);
        float spaceAdvance = MeasureSpaceAdvance(probe, font);
        float x = 0;
        for (int index = 0; index < text.Length; index++)
        {
            char ch = text[index];
            if (char.IsWhiteSpace(ch))
            {
                x += spaceAdvance + tracking;
                continue;
            }

            string character = ch.ToString();
            using GraphicsPath characterPath = new();
            characterPath.AddString(character, family, (int)style, emSize, new PointF(x, 0), StringFormat.GenericTypographic);
            if (characterPath.PointCount > 0)
            {
                path.AddPath(characterPath, connect: false);
            }

            float gap = HasVisiblePair(text, index) ? glyphGap : 0;
            x += MeasureCharacterAdvance(probe, font, character, characterPath) + tracking + gap;
        }

        return path;
    }

    private static bool HasVisiblePair(string text, int index)
        => index < text.Length - 1 && !char.IsWhiteSpace(text[index]) && !char.IsWhiteSpace(text[index + 1]);

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

    private static Bitmap AdjustStrokeMask(Bitmap mask, ContinuePlayingTextureRenderSettings settings)
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

    private static Bitmap Compose(
        Bitmap fillMask,
        Bitmap strokeMask,
        Bitmap blurredStrokeMask,
        ContinuePlayingTextureRenderSettings settings)
    {
        Bitmap output = CreateBlankCanvas();
        for (int y = 0; y < GameOverTextureCatalog.ContinuePlayingHeight; y++)
        {
            for (int x = 0; x < GameOverTextureCatalog.ContinuePlayingWidth; x++)
            {
                int fillValue = fillMask.GetPixel(x, y).A;
                if (fillValue >= settings.FillThreshold)
                {
                    int boosted = Math.Min(255, (int)Math.Round(fillValue * settings.FillBoost / 100d));
                    int gray = fillValue >= settings.WhiteThreshold ? 255 : NearestIa8Step(boosted);
                    output.SetPixel(x, y, Color.FromArgb(255, gray, gray, gray));
                    continue;
                }

                int strokeValue = strokeMask.GetPixel(x, y).A;
                int blurredValue = (int)Math.Round(blurredStrokeMask.GetPixel(x, y).A * settings.BlurStrength / 100d);
                int alpha = NearestIa8Step(Math.Min(255, Math.Max(strokeValue, blurredValue)));
                if (alpha > 0)
                {
                    output.SetPixel(x, y, Color.FromArgb(alpha, 0, 0, 0));
                }
            }
        }

        return output;
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

    private static Bitmap CreateBlankCanvas()
        => new(GameOverTextureCatalog.ContinuePlayingWidth, GameOverTextureCatalog.ContinuePlayingHeight, PixelFormat.Format32bppArgb);

    private static Bitmap CreateBlankMask()
        => CreateBlankCanvas();
}
