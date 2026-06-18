using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class EndTitleTextureRenderer
{
    private const double OrnamentWhiteStrength = 2.0;
    private const double TmWhiteStrength = 3.0;

    private static Bitmap RenderOcarinaOfTime(
        EndTitleTextParts parts,
        TextTextureFont font,
        EndTitleTextureRenderSettings settings,
        int width,
        int height,
        EndTitleTextureAssets? assets)
    {
        CachedDrawingFontFace face = GetDrawingFontFace(font);
        if (!CanRenderOcarinaAssets(parts, assets))
        {
            using Bitmap fallbackFill = RenderMask(parts, face.Family, face.Style, settings, width, height, fillMask: true);
            using Bitmap fallbackStroke = RenderMask(parts, face.Family, face.Style, settings, width, height, fillMask: false);
            return Compose(fallbackFill, fallbackStroke, width, height, settings.StrokeStrength, settings.BlurRadius, settings.BlurStrength);
        }

        using Bitmap textFill = RenderMask(
            new EndTitleTextParts(string.Empty, parts.Title, string.Empty, string.Empty),
            face.Family,
            face.Style,
            settings,
            width,
            height,
            fillMask: true);
        using Bitmap textStroke = RenderMask(
            new EndTitleTextParts(string.Empty, parts.Title, string.Empty, string.Empty),
            face.Family,
            face.Style,
            settings,
            width,
            height,
            fillMask: false);
        using Bitmap assetStroke = RenderAssetStrokeMask(parts, settings, width, height, assets);
        using Bitmap assetColors = RenderAssetColors(parts, settings, width, height, assets);
        double assetStrokeStrength = settings.StrokeStrength * Math.Max(0, settings.OcarinaAssetOutlinePercent) / 100d;
        double assetBlurStrength = settings.BlurStrength * Math.Max(0, settings.OcarinaAssetShadowPercent) / 100d;
        Bitmap output = Compose(
            textFill,
            textStroke,
            width,
            height,
            settings.StrokeStrength,
            settings.BlurRadius,
            settings.BlurStrength,
            assetStroke,
            assetStrokeStrength,
            assetBlurStrength);
        PasteNonTransparent(assetColors, output, 0, 0);
        return output;
    }

    private static bool CanRenderOcarinaAssets(EndTitleTextParts parts, EndTitleTextureAssets? assets)
    {
        bool needsOrnament = !string.IsNullOrEmpty(parts.Prefix) || !string.IsNullOrEmpty(parts.Suffix);
        bool needsTm = !string.IsNullOrEmpty(parts.Tm);
        return (!needsOrnament || File.Exists(assets?.OcarinaOrnamentPath))
            && (!needsTm || File.Exists(assets?.OcarinaTmPath));
    }

    private static Bitmap RenderMask(
        EndTitleTextParts parts,
        FontFamily family,
        FontStyle style,
        EndTitleTextureRenderSettings settings,
        int width,
        int height,
        bool fillMask)
    {
        int scale = Math.Max(1, settings.RenderScale);
        Bitmap high = new(width * scale, height * scale, PixelFormat.Format32bppArgb);

        DrawComponent(
            high,
            parts.Prefix,
            family,
            style,
            settings.PrefixSize,
            settings.PrefixX + settings.XNudge,
            settings.PrefixY + settings.YOffset,
            scale,
            fillMask ? settings.PrefixWhiteSpread : settings.StrokeWidth,
            widthScale: 1.0,
            alphaStrength: fillMask ? OrnamentWhiteStrength : 1.0);

        DrawComponent(
            high,
            parts.Title,
            family,
            style,
            settings.TitleSize,
            settings.TitleX + settings.XNudge,
            settings.TitleY + settings.YOffset,
            scale,
            fillMask ? settings.WhiteSpread : settings.StrokeWidth,
            settings.TitleWidthScale,
            alphaStrength: fillMask ? settings.WhiteStrength : 1.0);

        DrawComponent(
            high,
            parts.Tm,
            family,
            style,
            settings.TmSize,
            settings.TmX + settings.XNudge,
            settings.TmY + settings.YOffset,
            scale,
            fillMask ? settings.WhiteSpread : settings.StrokeWidth,
            widthScale: 1.0,
            alphaStrength: fillMask ? TmWhiteStrength : 1.0);

        DrawComponent(
            high,
            parts.Suffix,
            family,
            style,
            settings.SuffixSize,
            settings.SuffixX + settings.XNudge,
            settings.SuffixY + settings.YOffset,
            scale,
            fillMask ? settings.SuffixWhiteSpread : settings.StrokeWidth,
            widthScale: 1.0,
            alphaStrength: fillMask ? OrnamentWhiteStrength : 1.0);

        Bitmap low = ResizeMask(high, width, height);
        high.Dispose();
        return low;
    }

    private static Bitmap RenderAssetStrokeMask(
        EndTitleTextParts parts,
        EndTitleTextureRenderSettings settings,
        int width,
        int height,
        EndTitleTextureAssets? assets)
    {
        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        DrawAssetStrokeComponent(
            output,
            parts.Prefix,
            assets?.OcarinaOrnamentPath,
            settings.PrefixX + settings.XNudge,
            settings.PrefixY + settings.YOffset,
            settings.OcarinaAssetOutlineWidth);
        DrawAssetStrokeComponent(
            output,
            parts.Tm,
            assets?.OcarinaTmPath,
            settings.TmX + settings.XNudge,
            settings.TmY + settings.YOffset,
            settings.OcarinaAssetOutlineWidth);
        DrawAssetStrokeComponent(
            output,
            parts.Suffix,
            assets?.OcarinaOrnamentPath,
            settings.SuffixX + settings.XNudge,
            settings.SuffixY + settings.YOffset,
            settings.OcarinaAssetOutlineWidth);
        return output;
    }

    private static void DrawAssetStrokeComponent(
        Bitmap output,
        string markerText,
        string? imagePath,
        double x,
        double y,
        double outlineWidth)
    {
        if (string.IsNullOrEmpty(markerText)
            || string.IsNullOrWhiteSpace(imagePath)
            || !File.Exists(imagePath))
        {
            return;
        }

        int radius = Math.Max(0, (int)Math.Round(outlineWidth));
        using Bitmap source = new(imagePath);
        using Bitmap sourceMask = CreateAssetAlphaMask(source);
        using Bitmap stamp = radius > 0
            ? CreateDilatedAssetMask(sourceMask, radius)
            : (Bitmap)sourceMask.Clone();
        PasteMaxAlpha(
            stamp,
            output,
            (int)Math.Round(x) - radius,
            (int)Math.Round(y) - radius,
            alphaStrength: 1.0);
    }

    private static Bitmap CreateDilatedAssetMask(Bitmap source, int radius)
    {
        using Bitmap padded = new(source.Width + radius * 2, source.Height + radius * 2, PixelFormat.Format32bppArgb);
        PasteMaxAlpha(source, padded, radius, radius, alphaStrength: 1.0);
        return MaxFilterAlpha(padded, radius);
    }

    private static Bitmap RenderAssetColors(
        EndTitleTextParts parts,
        EndTitleTextureRenderSettings settings,
        int width,
        int height,
        EndTitleTextureAssets? assets)
    {
        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        DrawAssetColorComponent(
            output,
            parts.Prefix,
            assets?.OcarinaOrnamentPath,
            settings.PrefixX + settings.XNudge,
            settings.PrefixY + settings.YOffset);
        DrawAssetColorComponent(
            output,
            parts.Tm,
            assets?.OcarinaTmPath,
            settings.TmX + settings.XNudge,
            settings.TmY + settings.YOffset);
        DrawAssetColorComponent(
            output,
            parts.Suffix,
            assets?.OcarinaOrnamentPath,
            settings.SuffixX + settings.XNudge,
            settings.SuffixY + settings.YOffset);
        return output;
    }

    private static void DrawAssetColorComponent(
        Bitmap output,
        string markerText,
        string? imagePath,
        double x,
        double y)
    {
        if (string.IsNullOrEmpty(markerText)
            || string.IsNullOrWhiteSpace(imagePath)
            || !File.Exists(imagePath))
        {
            return;
        }

        using Bitmap source = new(imagePath);
        using Bitmap sourceColors = CreateAssetColorBitmap(source);
        PasteNonTransparent(
            sourceColors,
            output,
            (int)Math.Round(x),
            (int)Math.Round(y));
    }

    private static Bitmap CreateAssetAlphaMask(Bitmap source)
    {
        Bitmap mask = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Color pixel = source.GetPixel(x, y);
                if (pixel.A > 0)
                {
                    mask.SetPixel(x, y, Color.FromArgb(pixel.A, 255, 255, 255));
                }
            }
        }

        return mask;
    }

    private static Bitmap CreateAssetColorBitmap(Bitmap source)
    {
        Bitmap output = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Color pixel = source.GetPixel(x, y);
                if (pixel.A > 0)
                {
                    output.SetPixel(x, y, pixel);
                }
            }
        }

        return output;
    }

    private static void DrawComponent(
        Bitmap mask,
        string text,
        FontFamily family,
        FontStyle style,
        double size,
        double x,
        double y,
        int scale,
        double strokeWidth,
        double widthScale,
        double alphaStrength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        float emSize = Math.Max(1, (float)(size * scale));
        float scaledStrokeWidth = Math.Max(0, (float)Math.Round(strokeWidth * scale));
        using GraphicsPath path = BuildPath(text, family, style, emSize);
        RectangleF bounds = path.GetBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        if (Math.Abs(widthScale - 1.0) < 0.001)
        {
            using Bitmap directMask = new(mask.Width, mask.Height, PixelFormat.Format32bppArgb);
            using Matrix transform = new(1, 0, 0, 1, (float)Math.Round(x * scale) - bounds.Left, (float)Math.Round(y * scale) - bounds.Top);
            path.Transform(transform);
            DrawPath(directMask, path, scaledStrokeWidth);
            PasteMaxAlpha(directMask, mask, 0, 0, alphaStrength);
            return;
        }

        int pad = (int)Math.Round(scaledStrokeWidth) + 4 * scale;
        int tempWidth = Math.Max(1, (int)Math.Ceiling(bounds.Width) + pad * 2);
        int tempHeight = Math.Max(1, (int)Math.Ceiling(bounds.Height) + pad * 2);
        using Bitmap temp = new(tempWidth, tempHeight, PixelFormat.Format32bppArgb);
        using Matrix tempTransform = new(1, 0, 0, 1, pad - bounds.Left, pad - bounds.Top);
        path.Transform(tempTransform);
        DrawPath(temp, path, scaledStrokeWidth);

        int scaledWidth = Math.Max(1, (int)Math.Round(temp.Width * widthScale));
        using Bitmap scaled = ResizeMask(temp, scaledWidth, temp.Height);
        int pasteX = (int)Math.Round(x * scale) - (int)Math.Round(pad * widthScale);
        int pasteY = (int)Math.Round(y * scale) - pad;
        PasteMaxAlpha(scaled, mask, pasteX, pasteY, alphaStrength);
    }
}
