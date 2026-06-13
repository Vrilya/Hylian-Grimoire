using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HylianGrimoire.TextTextures;

public static partial class EndTitleTextureRenderer
{
    private static Bitmap RenderSimpleLineMask(
        string text,
        SixLabors.Fonts.FontFamily family,
        SixLabors.Fonts.FontStyle style,
        EndTitleTextureRenderSettings settings,
        int width,
        int height,
        double fontSize,
        double widthScale,
        double x,
        double y,
        double maxStrokeWidth,
        double strokeWidth,
        double alphaStrength)
    {
        int scale = Math.Max(1, settings.RenderScale);
        SixLabors.Fonts.Font font = family.CreateFont((float)(fontSize * scale), style);
        SixLabors.Fonts.TextOptions measureOptions = new(font);
        SixLabors.Fonts.FontRectangle bounds = SixLabors.Fonts.TextMeasurer.MeasureBounds(text, measureOptions);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        double maxSpread = Math.Max(maxStrokeWidth, strokeWidth);
        int pad = (int)Math.Round(maxSpread * scale + 5 * scale);
        int tempWidth = Math.Max(1, (int)Math.Ceiling(bounds.Width) + pad * 2);
        int tempHeight = Math.Max(1, (int)Math.Ceiling(bounds.Height) + pad * 2);
        using SixLabors.ImageSharp.Image<Rgba32> temp = new(tempWidth, tempHeight);
        SixLabors.ImageSharp.PointF origin = new((float)(pad - bounds.Left), (float)(pad - bounds.Top));
        RichTextOptions textOptions = new(font)
        {
            Origin = origin,
        };
        SixLabors.ImageSharp.Drawing.Processing.Brush brush =
            SixLabors.ImageSharp.Drawing.Processing.Brushes.Solid(SixLabors.ImageSharp.Color.White);
        SixLabors.ImageSharp.Drawing.Processing.Pen pen =
            SixLabors.ImageSharp.Drawing.Processing.Pens.Solid(SixLabors.ImageSharp.Color.White, Math.Max(0, (float)Math.Round(strokeWidth * scale)));
        temp.Mutate(context => context.DrawText(textOptions, text, brush, pen));

        int scaledWidth = Math.Max(1, (int)Math.Round(temp.Width * widthScale));
        temp.Mutate(context => context.Resize(scaledWidth, temp.Height, KnownResamplers.Lanczos3));
        int pasteX = (int)Math.Round(x * scale) - (int)Math.Round(pad * widthScale);
        int pasteY = (int)Math.Round(y * scale) - pad;
        using Bitmap high = new(width * scale, height * scale, PixelFormat.Format32bppArgb);
        using Bitmap scaled = ToBitmap(temp);
        PasteMaxAlpha(scaled, high, pasteX, pasteY, alphaStrength);
        return ResizeMaskLanczos(high, width, height);
    }

    private static Bitmap RenderTrackedLineMask(
        string text,
        FontFamily family,
        FontStyle style,
        EndTitleTextureRenderSettings settings,
        int width,
        int height,
        double fontSize,
        double widthScale,
        double heightScale,
        double x,
        double y,
        double maxStrokeWidth,
        double strokeWidth,
        double alphaStrength,
        double tracking)
    {
        int scale = Math.Max(1, settings.RenderScale);
        float emSize = Math.Max(1, (float)(fontSize * scale));
        float scaledStrokeWidth = Math.Max(0, (float)Math.Round(strokeWidth * scale));
        using GraphicsPath path = BuildTrackedPath(text, family, style, emSize, (float)(tracking * scale));
        RectangleF bounds = path.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        int pad = (int)Math.Round(maxStrokeWidth * scale + 5 * scale);
        int tempWidth = Math.Max(1, (int)Math.Ceiling(bounds.Width) + pad * 2);
        int tempHeight = Math.Max(1, (int)Math.Ceiling(bounds.Height) + pad * 2);
        using Bitmap temp = new(tempWidth, tempHeight, PixelFormat.Format32bppArgb);
        using Matrix tempTransform = new(1, 0, 0, 1, pad - bounds.Left, pad - bounds.Top);
        path.Transform(tempTransform);
        DrawPath(temp, path, scaledStrokeWidth);

        int scaledWidth = Math.Max(1, (int)Math.Round(temp.Width * widthScale));
        int scaledHeight = Math.Max(1, (int)Math.Round(temp.Height * heightScale));
        using Bitmap scaled = ResizeMask(temp, scaledWidth, scaledHeight);
        int pasteX = (int)Math.Round(x * scale) - (int)Math.Round(pad * widthScale);
        int pasteY = (int)Math.Round(y * scale) - (int)Math.Round(pad * heightScale);
        using Bitmap high = new(width * scale, height * scale, PixelFormat.Format32bppArgb);
        PasteMaxAlpha(scaled, high, pasteX, pasteY, alphaStrength);
        return ResizeMask(high, width, height);
    }

    private static GraphicsPath BuildPath(string text, FontFamily family, FontStyle style, float emSize)
    {
        GraphicsPath path = new();
        path.AddString(text, family, (int)style, emSize, Point.Empty, StringFormat.GenericTypographic);
        return path;
    }

    private static GraphicsPath BuildTrackedPath(string text, FontFamily family, FontStyle style, float emSize, float tracking)
    {
        GraphicsPath path = new();
        if (Math.Abs(tracking) < 0.001f)
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
        foreach (char ch in text)
        {
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

            x += MeasureCharacterAdvance(probe, font, character, characterPath) + tracking;
        }

        return path;
    }

    private static float MeasureCharacterAdvance(Graphics probe, Font font, string character, GraphicsPath characterPath)
    {
        float advance = probe.MeasureString(character, font, PointF.Empty, StringFormat.GenericTypographic).Width;
        return advance > 0 ? advance : characterPath.GetBounds().Width;
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

    private static void DrawPath(Bitmap mask, GraphicsPath path, float strokeWidth)
    {
        using Graphics graphics = Graphics.FromImage(mask);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        if (strokeWidth > 0)
        {
            using System.Drawing.Pen pen = new(Color.White, strokeWidth)
            {
                LineJoin = LineJoin.Round,
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            graphics.DrawPath(pen, path);
        }

        using System.Drawing.SolidBrush brush = new(Color.White);
        graphics.FillPath(brush, path);
    }
}
