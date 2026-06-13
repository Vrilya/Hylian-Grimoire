using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace HylianGrimoire.TextTextures;

public static class PauseHeaderTextureRenderer
{
    private static readonly int[] GrayPalette = [0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255];

    public static Bitmap Render(
        string text,
        string fontPath,
        string templateRoot,
        PauseHeaderTextureTarget target,
        PauseHeaderTextureRenderSettings settings)
        => Render(text, TextTextureFont.FromPath(fontPath), templateRoot, target, settings);

    public static Bitmap Render(
        string text,
        TextTextureFont font,
        string templateRoot,
        PauseHeaderTextureTarget target,
        PauseHeaderTextureRenderSettings settings)
    {
        if (!File.Exists(font.Path))
        {
            throw new FileNotFoundException("Pause-header texture font is missing.", font.Path);
        }

        using Bitmap baseRow = LoadTemplateRow(templateRoot, target.Spec);
        using TextTextureDrawingFont drawingFont = new(font);

        PauseHeaderTextureRenderSettings effectiveSettings = settings with
        {
            CenterX = settings.Center ? settings.CenterX : settings.XNudge,
        };
        (Bitmap fillMask, Bitmap strokeMask) = CreateTextMasks(text, drawingFont.Family, drawingFont.Style, effectiveSettings);
        using (fillMask)
        using (strokeMask)
        {
            return Compose(baseRow, fillMask, strokeMask, effectiveSettings);
        }
    }

    public static IReadOnlyList<Bitmap> SplitTriplet(Bitmap row)
    {
        if (row.Width != PauseHeaderTextureCatalog.Width || row.Height != PauseHeaderTextureCatalog.Height)
        {
            throw new InvalidDataException($"Pause-header row must be {PauseHeaderTextureCatalog.Width}x{PauseHeaderTextureCatalog.Height} pixels.");
        }

        var images = new List<Bitmap>(3);
        for (int index = 0; index < 3; index++)
        {
            Rectangle source = new(index * PauseHeaderTextureCatalog.TileWidth, 0, PauseHeaderTextureCatalog.TileWidth, PauseHeaderTextureCatalog.TileHeight);
            images.Add(Crop(row, source));
        }

        return images;
    }

    public static Bitmap CombineTriplet(IReadOnlyList<Bitmap> images)
    {
        if (images.Count != 3)
        {
            throw new InvalidDataException("Pause-header triplet must contain exactly three textures.");
        }

        Bitmap row = new(PauseHeaderTextureCatalog.Width, PauseHeaderTextureCatalog.Height, PixelFormat.Format32bppArgb);
        for (int index = 0; index < images.Count; index++)
        {
            Bitmap image = images[index];
            if (image.Width != PauseHeaderTextureCatalog.TileWidth || image.Height != PauseHeaderTextureCatalog.TileHeight)
            {
                row.Dispose();
                throw new InvalidDataException($"Pause-header template {index + 1} must be {PauseHeaderTextureCatalog.TileWidth}x{PauseHeaderTextureCatalog.TileHeight} pixels.");
            }

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    row.SetPixel(index * PauseHeaderTextureCatalog.TileWidth + x, y, image.GetPixel(x, y));
                }
            }
        }

        return row;
    }

    public static Bitmap ApplyOriginalColors(Bitmap source, PauseHeaderTextureTarget target)
        => ApplyOriginalColors(source, target.Spec);

    public static Bitmap ApplyOriginalColors(Bitmap source, PauseHeaderTextureSpec spec)
    {
        if (source.Width != PauseHeaderTextureCatalog.Width || source.Height != PauseHeaderTextureCatalog.Height)
        {
            throw new InvalidDataException($"Pause-header row must be {PauseHeaderTextureCatalog.Width}x{PauseHeaderTextureCatalog.Height} pixels.");
        }

        Bitmap output = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Color pixel = source.GetPixel(x, y);
                PauseHeaderPageColor pageColor = GetInterpolatedPageColor(spec.OriginalColorRamp, x);
                int intensity = (pixel.R + pixel.G + pixel.B) / 3;
                output.SetPixel(
                    x,
                    y,
                    Color.FromArgb(
                        pixel.A,
                        Modulate(pageColor.Red, intensity),
                        Modulate(pageColor.Green, intensity),
                        Modulate(pageColor.Blue, intensity)));
            }
        }

        return output;
    }

    private static Bitmap LoadTemplateRow(string templateRoot, PauseHeaderTextureSpec spec)
    {
        var images = new List<Bitmap>(3);
        try
        {
            foreach (string fileName in spec.TemplateFileNames)
            {
                string path = Path.Combine(templateRoot, fileName);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException("Pause-header template is missing.", path);
                }

                Bitmap image = new(path);
                if (image.Width != PauseHeaderTextureCatalog.TileWidth || image.Height != PauseHeaderTextureCatalog.TileHeight)
                {
                    image.Dispose();
                    throw new InvalidDataException($"{path} must be {PauseHeaderTextureCatalog.TileWidth}x{PauseHeaderTextureCatalog.TileHeight} pixels.");
                }

                images.Add(CloneAsArgb(image));
                image.Dispose();
            }

            return CombineTriplet(images);
        }
        finally
        {
            foreach (Bitmap image in images)
            {
                image.Dispose();
            }
        }
    }

    private static (Bitmap Fill, Bitmap Stroke) CreateTextMasks(
        string text,
        FontFamily family,
        FontStyle style,
        PauseHeaderTextureRenderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (CreateBlankMask(), CreateBlankMask());
        }

        int scale = Math.Max(1, settings.RenderScale);
        float emSize = Math.Max(1, settings.FontSize * scale);
        float strokeWidth = Math.Max(0, settings.StrokeWidth * scale);
        int tracking = (int)Math.Round(settings.Tracking * scale);

        using GraphicsPath rawPath = BuildTrackingPath(text, family, style, emSize, tracking);
        RectangleF bounds = rawPath.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return (CreateBlankMask(), CreateBlankMask());
        }

        int padding = 24 * scale;
        int localWidth = Math.Max(256 * scale, (int)Math.Ceiling(bounds.Width + strokeWidth * 2 + padding * 2));
        int localHeight = 64 * scale;
        using GraphicsPath path = (GraphicsPath)rawPath.Clone();
        using Matrix localTransform = new(1, 0, 0, 1, padding - (bounds.Left - strokeWidth), padding - (bounds.Top - strokeWidth));
        path.Transform(localTransform);

        using Bitmap highFillTemp = new(localWidth, localHeight, PixelFormat.Format32bppArgb);
        using Bitmap highStrokeTemp = new(localWidth, localHeight, PixelFormat.Format32bppArgb);
        DrawFillMask(highFillTemp, path);
        DrawStrokeMask(highStrokeTemp, path, strokeWidth);

        Rectangle contentBounds = GetContentBounds(highStrokeTemp);
        if (contentBounds.IsEmpty)
        {
            return (CreateBlankMask(), CreateBlankMask());
        }

        using Bitmap croppedFill = Crop(highFillTemp, contentBounds);
        using Bitmap croppedStroke = Crop(highStrokeTemp, contentBounds);
        int scaledWidth = Math.Max(1, (int)Math.Round(croppedStroke.Width * settings.WidthScale));
        using Bitmap adjustedFill = ResizeMask(croppedFill, scaledWidth, croppedFill.Height);
        using Bitmap adjustedStroke = ResizeMask(croppedStroke, scaledWidth, croppedStroke.Height);

        using Bitmap fillHigh = CreateBlankHighMask(scale);
        using Bitmap strokeHigh = CreateBlankHighMask(scale);
        int destinationX = (int)Math.Round(settings.CenterX * scale - adjustedStroke.Width / 2d);
        int destinationY = (int)Math.Round(settings.Y * scale);
        PasteMask(adjustedFill, fillHigh, destinationX, destinationY);
        PasteMask(adjustedStroke, strokeHigh, destinationX, destinationY);

        return (ResizeMask(fillHigh, PauseHeaderTextureCatalog.Width, PauseHeaderTextureCatalog.Height),
            ResizeMask(strokeHigh, PauseHeaderTextureCatalog.Width, PauseHeaderTextureCatalog.Height));
    }

    private static GraphicsPath BuildTrackingPath(string text, FontFamily family, FontStyle style, float emSize, int tracking)
    {
        GraphicsPath path = new();
        using Bitmap probeBitmap = new(1, 1, PixelFormat.Format32bppArgb);
        using Graphics probe = Graphics.FromImage(probeBitmap);
        probe.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using Font font = new(family, emSize, style, GraphicsUnit.Pixel);
        float spaceAdvance = MeasureSpaceAdvance(probe, font);
        float x = 0;
        foreach (char value in text)
        {
            if (char.IsWhiteSpace(value))
            {
                x += spaceAdvance + tracking;
                continue;
            }

            string character = value.ToString();
            using GraphicsPath characterPath = new();
            characterPath.AddString(character, family, (int)style, emSize, new PointF(x, 0), StringFormat.GenericTypographic);
            if (characterPath.PointCount > 0)
            {
                path.AddPath(characterPath, connect: false);
            }

            x += MeasureCharacterAdvance(probe, font, character, characterPath) + tracking;
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

    private static Bitmap Compose(Bitmap baseRow, Bitmap fillMask, Bitmap strokeMask, PauseHeaderTextureRenderSettings settings)
    {
        using Bitmap highlightMask = Shifted(strokeMask, settings.HighlightDx, settings.HighlightDy);
        using Bitmap shadowMask = Shifted(strokeMask, settings.ShadowDx, settings.ShadowDy);
        Bitmap output = new(PauseHeaderTextureCatalog.Width, PauseHeaderTextureCatalog.Height, PixelFormat.Format32bppArgb);

        for (int y = 0; y < PauseHeaderTextureCatalog.Height; y++)
        {
            for (int x = 0; x < PauseHeaderTextureCatalog.Width; x++)
            {
                Color baseColor = baseRow.GetPixel(x, y);
                double gray = baseColor.R;
                gray = BlendToward(gray, highlightMask.GetPixel(x, y).A, settings.HighlightGray, settings.HighlightStrength);
                gray = BlendToward(gray, shadowMask.GetPixel(x, y).A, 0, settings.ShadowStrength);

                int strokeOnly = Math.Max(0, strokeMask.GetPixel(x, y).A - fillMask.GetPixel(x, y).A);
                gray = BlendToward(gray, strokeOnly, 0, settings.StrokeStrength);
                gray = BlendToward(gray, fillMask.GetPixel(x, y).A, 0, settings.FillStrength);

                int snapped = NearestGray((int)Math.Round(gray));
                output.SetPixel(x, y, Color.FromArgb(baseColor.A, snapped, snapped, snapped));
            }
        }

        return output;
    }

    private static PauseHeaderPageColor GetInterpolatedPageColor(PauseHeaderColorRamp ramp, int x)
    {
        int tileIndex = Math.Clamp(x / PauseHeaderTextureCatalog.TileWidth, 0, 2);
        int localX = x - tileIndex * PauseHeaderTextureCatalog.TileWidth;
        double t = localX / (double)(PauseHeaderTextureCatalog.TileWidth - 1);
        PauseHeaderPageColor left = ramp.GetColumn(tileIndex);
        PauseHeaderPageColor right = ramp.GetColumn(tileIndex + 1);
        return new(
            Interpolate(left.Red, right.Red, t),
            Interpolate(left.Green, right.Green, t),
            Interpolate(left.Blue, right.Blue, t));
    }

    private static byte Interpolate(byte left, byte right, double t)
        => (byte)Math.Clamp((int)Math.Round(left + (right - left) * t), 0, 255);

    private static int Modulate(byte color, int intensity)
        => Math.Clamp((int)Math.Round(color * Math.Clamp(intensity, 0, 255) / 255d), 0, 255);

    private static double BlendToward(double value, int maskAlpha, int color, double strength)
    {
        if (strength <= 0 || maskAlpha <= 0)
        {
            return value;
        }

        double alpha = Math.Clamp(maskAlpha / 255d * strength, 0, 1);
        return value * (1 - alpha) + color * alpha;
    }

    private static Bitmap Shifted(Bitmap mask, int dx, int dy)
    {
        Bitmap output = CreateBlankMask();
        for (int y = 0; y < mask.Height; y++)
        {
            int targetY = y + dy;
            if (targetY < 0 || targetY >= mask.Height)
            {
                continue;
            }

            for (int x = 0; x < mask.Width; x++)
            {
                int targetX = x + dx;
                if (targetX < 0 || targetX >= mask.Width)
                {
                    continue;
                }

                output.SetPixel(targetX, targetY, mask.GetPixel(x, y));
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
        for (int y = 0; y < bounds.Height; y++)
        {
            for (int x = 0; x < bounds.Width; x++)
            {
                output.SetPixel(x, y, source.GetPixel(bounds.X + x, bounds.Y + y));
            }
        }

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

    private static Bitmap CloneAsArgb(Bitmap source)
    {
        Bitmap output = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                output.SetPixel(x, y, source.GetPixel(x, y));
            }
        }

        return output;
    }

    private static int NearestGray(int value)
    {
        int clamped = Math.Clamp(value, 0, 255);
        int nearest = GrayPalette[0];
        int nearestDistance = Math.Abs(clamped - nearest);
        for (int i = 1; i < GrayPalette.Length; i++)
        {
            int distance = Math.Abs(clamped - GrayPalette[i]);
            if (distance < nearestDistance)
            {
                nearest = GrayPalette[i];
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private static Bitmap CreateBlankMask()
        => new(PauseHeaderTextureCatalog.Width, PauseHeaderTextureCatalog.Height, PixelFormat.Format32bppArgb);

    private static Bitmap CreateBlankHighMask(int scale)
        => new(PauseHeaderTextureCatalog.Width * scale, PauseHeaderTextureCatalog.Height * scale, PixelFormat.Format32bppArgb);
}
