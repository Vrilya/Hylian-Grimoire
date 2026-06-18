using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class PlaceTitleCardTextureRenderer
{
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
}
