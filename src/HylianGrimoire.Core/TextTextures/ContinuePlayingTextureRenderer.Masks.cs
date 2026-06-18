using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace HylianGrimoire.TextTextures;

public static partial class ContinuePlayingTextureRenderer
{
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
}
