using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace HylianGrimoire.TextTextures;

public static partial class PauseHeaderTextureRenderer
{
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
}
