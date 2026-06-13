using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static class PlaceTitleCardTextureRenderer
{
    private static readonly int[] Ia4Steps = [0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255];

    public static Bitmap Render(string text, string fontPath, PlaceTitleCardTextureRenderSettings settings)
        => Render(text, TextTextureFont.FromPath(fontPath), settings);

    public static Bitmap Render(string text, TextTextureFont font, PlaceTitleCardTextureRenderSettings settings)
    {
        if (!File.Exists(font.Path))
        {
            throw new FileNotFoundException("Place title-card texture font is missing.", font.Path);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new Bitmap(PlaceTitleCardTextureCatalog.Width, PlaceTitleCardTextureCatalog.Height, PixelFormat.Format32bppArgb);
        }

        using TextTextureDrawingFont drawingFont = new(font);

        (Bitmap fillMask, Bitmap strokeMask) = CreateTextMasks(text, drawingFont.Family, drawingFont.Style, settings);
        using (fillMask)
        using (strokeMask)
        {
            return Compose(fillMask, strokeMask, settings);
        }
    }

    private static (Bitmap Fill, Bitmap Stroke) CreateTextMasks(
        string text,
        FontFamily family,
        FontStyle style,
        PlaceTitleCardTextureRenderSettings settings)
    {
        int scale = Math.Max(1, settings.RenderScale);
        float emSize = Math.Max(1, settings.FontSize * scale);
        float strokeWidth = Math.Max(0, (float)(settings.StrokeWidth * scale));

        using GraphicsPath basePath = new();
        basePath.AddString(text, family, (int)style, emSize, Point.Empty, StringFormat.GenericTypographic);
        RectangleF bounds = basePath.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return (CreateBlankMask(), CreateBlankMask());
        }

        int padding = Math.Max(8, (int)Math.Ceiling(settings.StrokeWidth) + 5) * scale;
        int localWidth = Math.Max(1, (int)Math.Ceiling(bounds.Width + strokeWidth * 2 + padding * 2));
        int localHeight = Math.Max(1, (int)Math.Ceiling(bounds.Height + strokeWidth * 2 + padding * 2));
        float x = padding - (bounds.Left - strokeWidth);
        float y = padding - (bounds.Top - strokeWidth);

        using Matrix localTransform = new(1, 0, 0, 1, x, y);
        using GraphicsPath path = (GraphicsPath)basePath.Clone();
        path.Transform(localTransform);

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

        int targetWidth = Math.Max(1, (int)Math.Round(naturalStroke.Width * Math.Max(1, settings.HorizontalScale) / 100d));
        int targetHeight = Math.Max(1, (int)Math.Round(naturalStroke.Height * Math.Max(1, settings.HeightScale) / 100d));
        double widthFitScale = targetWidth > settings.MaxWidth ? settings.MaxWidth / (double)targetWidth : 1d;
        double heightFitScale = targetHeight > settings.MaxHeight ? settings.MaxHeight / (double)targetHeight : 1d;
        double fitScale = Math.Min(widthFitScale, heightFitScale);
        if (fitScale < 1d)
        {
            targetWidth = Math.Max(1, (int)Math.Round(targetWidth * fitScale));
            targetHeight = Math.Max(1, (int)Math.Round(targetHeight * fitScale));
        }

        using Bitmap targetFill = ResizeMask(naturalFill, targetWidth, targetHeight);
        using Bitmap targetStroke = ResizeMask(naturalStroke, targetWidth, targetHeight);
        Bitmap canvasFill = CreateBlankMask();
        Bitmap canvasStroke = CreateBlankMask();
        int destinationX = settings.Center
            ? (int)((PlaceTitleCardTextureCatalog.Width - targetWidth) / 2d + 0.5) + settings.XNudge
            : settings.XNudge;
        int destinationY = (int)((PlaceTitleCardTextureCatalog.Height - targetHeight) / 2d + 0.5) + settings.YOffset;
        PasteMask(targetFill, canvasFill, destinationX, destinationY);
        PasteMask(targetStroke, canvasStroke, destinationX, destinationY);
        if (settings.StrokeSoftness > 0)
        {
            Bitmap softenedStroke = BlurAlphaMask(canvasStroke, settings.StrokeSoftness);
            canvasStroke.Dispose();
            canvasStroke = softenedStroke;
        }

        return (canvasFill, canvasStroke);
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

    private static Bitmap Compose(Bitmap fillMask, Bitmap strokeMask, PlaceTitleCardTextureRenderSettings settings)
    {
        Bitmap output = new(PlaceTitleCardTextureCatalog.Width, PlaceTitleCardTextureCatalog.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < PlaceTitleCardTextureCatalog.Height; y++)
        {
            for (int x = 0; x < PlaceTitleCardTextureCatalog.Width; x++)
            {
                int fillValue = fillMask.GetPixel(x, y).A;
                int strokeValue = strokeMask.GetPixel(x, y).A;
                if (fillValue >= settings.FillThreshold)
                {
                    int boosted = Math.Min(255, (int)Math.Round(fillValue * settings.FillBoost / 100d));
                    int gray = fillValue >= settings.WhiteThreshold ? 255 : NearestIa4Step(boosted);
                    gray = Math.Max(gray, settings.FillFloor);
                    output.SetPixel(x, y, Color.FromArgb(255, gray, gray, gray));
                }
                else if (strokeValue > 0)
                {
                    strokeValue = Math.Min(255, (int)Math.Round(strokeValue * settings.StrokeAlpha / 100d));
                    int alpha = NearestIa4Step(strokeValue);
                    if (alpha > 0)
                    {
                        output.SetPixel(x, y, Color.FromArgb(alpha, 0, 0, 0));
                    }
                }
            }
        }

        return output;
    }

    private static Bitmap BlurAlphaMask(Bitmap source, double radius)
    {
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
                    int sampleX = x + offset;
                    alpha += GetAlphaOrZero(source, sampleX, y) * kernel[offset + kernelRadius];
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
                    int sampleY = y + offset;
                    alpha += GetAlphaOrZero(horizontal, x, sampleY) * kernel[offset + kernelRadius];
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
        => new(PlaceTitleCardTextureCatalog.Width, PlaceTitleCardTextureCatalog.Height, PixelFormat.Format32bppArgb);
}
