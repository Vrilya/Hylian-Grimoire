using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static class ItemNameTextureRenderer
{
    private static readonly int[] GrayPalette = [0, 36, 73, 109, 146, 182, 219, 255];

    public static Bitmap Render(string text, string fontPath, ItemNameTextureRenderSettings settings)
        => Render(text, TextTextureFont.FromPath(fontPath), settings, ItemNameTextureCatalog.Width, ItemNameTextureCatalog.Height);

    public static Bitmap Render(string text, TextTextureFont font, ItemNameTextureRenderSettings settings)
        => Render(text, font, settings, ItemNameTextureCatalog.Width, ItemNameTextureCatalog.Height);

    public static Bitmap Render(string text, string fontPath, ItemNameTextureRenderSettings settings, int width, int height)
        => Render(text, TextTextureFont.FromPath(fontPath), settings, width, height);

    public static Bitmap Render(string text, TextTextureFont font, ItemNameTextureRenderSettings settings, int width, int height)
    {
        if (!File.Exists(font.Path))
        {
            throw new FileNotFoundException("Item-name texture font is missing.", font.Path);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        using TextTextureDrawingFont drawingFont = new(font);

        (Bitmap fillMask, Bitmap strokeMask) = CreateTextMasks(text, drawingFont.Family, drawingFont.Style, settings, width, height);
        using (fillMask)
        using (strokeMask)
        {
            return Compose(fillMask, strokeMask, settings, width, height);
        }
    }

    private static (Bitmap Fill, Bitmap Stroke) CreateTextMasks(
        string text,
        FontFamily family,
        FontStyle style,
        ItemNameTextureRenderSettings settings,
        int width,
        int height)
    {
        int scale = Math.Max(1, settings.RenderScale);
        float emSize = Math.Max(1, (float)(settings.FontSize * scale));
        float strokeWidth = Math.Max(0, (float)(settings.StrokeWidth * scale));

        using GraphicsPath basePath = new();
        basePath.AddString(text, family, (int)style, emSize, Point.Empty, StringFormat.GenericTypographic);
        RectangleF bounds = basePath.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return (CreateBlankMask(width, height), CreateBlankMask(width, height));
        }

        int highWidth = width * scale;
        int highHeight = height * scale;
        double scaleX = Math.Max(1, settings.HorizontalScale) / 100d;
        double scaleY = Math.Max(1, settings.VerticalScale) / 100d;
        double strokeBoundsWidth = bounds.Width + strokeWidth * 2;
        double targetWidth = strokeBoundsWidth * scaleX / scale;
        if (settings.FitToWidth && targetWidth > settings.MaxWidth)
        {
            double fitScale = settings.MaxWidth / targetWidth;
            scaleX *= fitScale;
        }

        float x = settings.Center
            ? (float)((highWidth - strokeBoundsWidth * scaleX) / 2d + settings.XNudge * scale - (bounds.Left - strokeWidth) * scaleX)
            : (float)(settings.XNudge * scale - (bounds.Left - strokeWidth) * scaleX);

        float ascentPx = emSize * family.GetCellAscent(style) / family.GetEmHeight(style);
        float baseline = (settings.BaselineY + settings.YOffset) * scale;
        float y = baseline - (float)(ascentPx * scaleY);

        using Bitmap highFillMask = new(highWidth, highHeight, PixelFormat.Format32bppArgb);
        using Bitmap highStrokeMask = new(highWidth, highHeight, PixelFormat.Format32bppArgb);
        using Matrix transform = new((float)scaleX, 0, 0, (float)scaleY, x, y);
        using GraphicsPath path = (GraphicsPath)basePath.Clone();
        path.Transform(transform);

        DrawFillMask(highFillMask, path);
        DrawStrokeMask(highStrokeMask, path, strokeWidth);

        using Bitmap lowFillMask = scale > 1 ? DownsampleAverage(highFillMask, scale) : (Bitmap)highFillMask.Clone();
        using Bitmap lowStrokeMask = scale > 1 ? DownsampleAverage(highStrokeMask, scale) : (Bitmap)highStrokeMask.Clone();
        return ((Bitmap)lowFillMask.Clone(), (Bitmap)lowStrokeMask.Clone());
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

    private static Bitmap Compose(Bitmap fillMask, Bitmap strokeMask, ItemNameTextureRenderSettings settings, int width, int height)
    {
        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int fillValue = fillMask.GetPixel(x, y).A;
                int strokeValue = strokeMask.GetPixel(x, y).A;
                bool hasFill = fillValue >= settings.FillThreshold;
                bool hasStroke = strokeValue >= settings.StrokeThreshold;
                if (!hasFill && !hasStroke)
                {
                    continue;
                }

                int gray;
                if (hasFill)
                {
                    int boosted = Math.Min(255, (int)Math.Round(fillValue * settings.FillBoost / 100d));
                    gray = fillValue >= settings.WhiteThreshold ? 255 : NearestGray(boosted);
                    gray = Math.Max(gray, settings.FillFloor);
                }
                else
                {
                    gray = 0;
                }

                output.SetPixel(x, y, Color.FromArgb(255, gray, gray, gray));
            }
        }

        return output;
    }

    private static Bitmap DownsampleAverage(Bitmap source, int scale)
        => TextTextureBitmapOps.DownsampleAverage(source, scale);

    private static int NearestGray(int value)
        => TextTextureBitmapOps.NearestStep(value, GrayPalette);

    private static Bitmap CreateBlankMask(int width, int height)
        => TextTextureBitmapOps.CreateBlankMask(width, height);
}
