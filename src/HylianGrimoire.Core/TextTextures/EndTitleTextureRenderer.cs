using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HylianGrimoire.TextTextures;

public static class EndTitleTextureRenderer
{
    private const double OrnamentWhiteStrength = 2.0;
    private const double TmWhiteStrength = 3.0;
    private const double PresentedByFontSize = 12.0;
    private const double PresentedByWidthScale = 0.88;
    private const double PresentedByX = 9.0;
    private const double PresentedByY = 4.0;
    private const double PresentedByStrokeWidth = 1.50;
    private const double PresentedByStrokeStrength = 0.85;
    private const double PresentedByBlurRadius = 1.00;
    private const double PresentedByBlurStrength = 1.60;
    private const double PresentedByWhiteStrength = 1.00;
    private const double PresentedByWhiteSpread = 0.40;
    private const int PresentedByFitHorizontalInset = 1;
    private const double TheEndFontSize = 21.0;
    private const double TheEndWidthScale = 0.869;
    private const double TheEndX = 5.0;
    private const double TheEndY = 4.0;
    private const double TheEndStrokeWidth = 2.10;
    private const double TheEndStrokeStrength = 1.65;
    private const double TheEndBlurRadius = 1.10;
    private const double TheEndBlurStrength = 2.15;
    private const double TheEndWhiteStrength = 1.00;
    private const double TheEndWhiteSpread = 0.20;
    private const double LegendWidthScale = 0.81;
    private const double LegendHeightScale = 0.842;
    private const double LegendTracking = -0.55;
    private const double LegendX = 2.0;
    private const double LegendY = 7.0;
    private const double LegendStrokeWidth = 2.20;
    private const double LegendStrokeStrength = 0.80;
    private const double LegendBlurRadius = 0.90;
    private const double LegendBlurStrength = 1.20;
    private const double LegendWhiteStrength = 1.00;
    private const double LegendWhiteSpread = 0.20;
    private const double LegendRegX = 110.0;
    private const double LegendRegY = 0.0;
    private const int LegendRegStampThreshold = 170;
    private const int LegendRegStampX = 2;
    private const int LegendRegStampY = 2;
    private const double LegendRegStrokeWidth = 1.10;
    private const double LegendRegStrokeStrength = 0.80;
    private const double LegendRegBlurRadius = 0.65;
    private const double LegendRegBlurStrength = 0.95;

    private const int QuantizationStep = 17;
    private static readonly ConcurrentDictionary<string, Lazy<CachedDrawingFontFace>> DrawingFontFaces = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, Lazy<CachedImageSharpFontFace>> ImageSharpFontFaces = new(StringComparer.OrdinalIgnoreCase);

    public static Bitmap Render(string text, string fontPath, EndTitleTextureRenderSettings settings, int width, int height)
        => Render(text, TextTextureFont.FromPath(fontPath), settings, width, height);

    public static Bitmap Render(string text, TextTextureFont font, EndTitleTextureRenderSettings settings, int width, int height)
    {
        if (!File.Exists(font.Path))
        {
            throw new FileNotFoundException("End-title texture font is missing.", font.Path);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        EndTitleTextParts parts = EndTitleTextParts.Parse(text);
        return Render(parts.Prefix, parts.Title, parts.Tm, parts.Suffix, font, EndTitleTextureCatalog.Specs[0], settings, width, height);
    }

    public static Bitmap Render(
        string prefix,
        string title,
        string tm,
        string suffix,
        string fontPath,
        EndTitleTextureSpec spec,
        EndTitleTextureRenderSettings settings,
        int width,
        int height,
        EndTitleTextureAssets? assets = null)
        => Render(prefix, title, tm, suffix, TextTextureFont.FromPath(fontPath), spec, settings, width, height, assets);

    public static Bitmap Render(
        string prefix,
        string title,
        string tm,
        string suffix,
        TextTextureFont font,
        EndTitleTextureSpec spec,
        EndTitleTextureRenderSettings settings,
        int width,
        int height,
        EndTitleTextureAssets? assets = null)
    {
        if (!File.Exists(font.Path))
        {
            throw new FileNotFoundException("End-title texture font is missing.", font.Path);
        }

        if (string.IsNullOrWhiteSpace(prefix)
            && string.IsNullOrWhiteSpace(title)
            && string.IsNullOrWhiteSpace(tm)
            && string.IsNullOrWhiteSpace(suffix))
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        if (spec.Style == EndTitleTextureStyle.PresentedBy)
        {
            return RenderPresentedBy(title, font, settings, width, height);
        }

        if (spec.Style == EndTitleTextureStyle.LegendOfZelda)
        {
            return RenderLegendOfZelda(title, font, assets?.LegendRegisteredPath, settings, width, height);
        }

        if (spec.Style == EndTitleTextureStyle.TheEnd)
        {
            return RenderTheEnd(title, font, settings, width, height);
        }

        return RenderOcarinaOfTime(
            new EndTitleTextParts(prefix, title, tm, suffix),
            font,
            settings,
            width,
            height,
            assets);
    }

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

    private static Bitmap RenderPresentedBy(
        string text,
        TextTextureFont font,
        EndTitleTextureRenderSettings settings,
        int width,
        int height)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        CachedImageSharpFontFace face = GetImageSharpFontFace(font);
        SixLabors.Fonts.FontStyle style = settings.PresentedByBold
            ? SixLabors.Fonts.FontStyle.Bold
            : face.Style;

        int horizontalPadding = Math.Max(width, 128);
        int renderWidth = width + horizontalPadding * 2;
        double x = horizontalPadding + PresentedByX + settings.XNudge;
        double y = PresentedByY + settings.YOffset;

        using Bitmap fill = RenderPresentedByMask(
            text.Trim(),
            face.Family,
            style,
            settings,
            renderWidth,
            height,
            x,
            y,
            PresentedByWhiteSpread,
            alphaStrength: PresentedByWhiteStrength);
        using Bitmap stroke = RenderPresentedByMask(
            text.Trim(),
            face.Family,
            style,
            settings,
            renderWidth,
            height,
            x,
            y,
            PresentedByStrokeWidth,
            alphaStrength: 1.0);
        Bitmap composed = Compose(fill, stroke, renderWidth, height, PresentedByStrokeStrength, PresentedByBlurRadius, PresentedByBlurStrength);
        using (composed)
        {
            return FitAlphaToCanvas(composed, width, height, settings.Center, (int)Math.Round(PresentedByX + settings.XNudge));
        }
    }

    private static Bitmap RenderTheEnd(
        string text,
        TextTextureFont font,
        EndTitleTextureRenderSettings settings,
        int width,
        int height)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        CachedImageSharpFontFace face = GetImageSharpFontFace(font);

        int horizontalPadding = settings.Center ? Math.Max(width, 128) : 0;
        int renderWidth = width + horizontalPadding * 2;
        double x = horizontalPadding + TheEndX + (settings.Center ? 0 : settings.XNudge);
        double y = TheEndY + settings.YOffset;
        using Bitmap fill = RenderSimpleLineMask(
            text.Trim(),
            face.Family,
            face.Style,
            settings,
            renderWidth,
            height,
            TheEndFontSize,
            TheEndWidthScale,
            x,
            y,
            TheEndStrokeWidth,
            TheEndWhiteSpread,
            alphaStrength: TheEndWhiteStrength);
        using Bitmap stroke = RenderSimpleLineMask(
            text.Trim(),
            face.Family,
            face.Style,
            settings,
            renderWidth,
            height,
            TheEndFontSize,
            TheEndWidthScale,
            x,
            y,
            TheEndStrokeWidth,
            TheEndStrokeWidth,
            alphaStrength: 1.0);
        Bitmap composed = Compose(fill, stroke, renderWidth, height, TheEndStrokeStrength, TheEndBlurRadius, TheEndBlurStrength);
        if (!settings.Center)
        {
            return composed;
        }

        using (composed)
        {
            return FitAlphaToCanvas(composed, width, height, center: true, destinationX: 0);
        }
    }

    private static Bitmap RenderLegendOfZelda(
        string text,
        TextTextureFont font,
        string? legendStampPath,
        EndTitleTextureRenderSettings settings,
        int width,
        int height)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        if (settings.LegendShowRegistered
            && (string.IsNullOrWhiteSpace(legendStampPath) || !File.Exists(legendStampPath)))
        {
            throw new FileNotFoundException("Legend of Zelda registered-mark stamp asset is missing.", legendStampPath);
        }

        CachedDrawingFontFace face = GetDrawingFontFace(font);

        double x = LegendX + settings.XNudge;
        double y = LegendY + settings.YOffset;
        using Bitmap fill = RenderTrackedLineMask(
            text.Trim(),
            face.Family,
            face.Style,
            settings,
            width,
            height,
            Math.Max(1.0, settings.LegendFontSize),
            LegendWidthScale,
            LegendHeightScale,
            x,
            y,
            Math.Max(LegendStrokeWidth, LegendWhiteSpread),
            LegendWhiteSpread,
            LegendWhiteStrength,
            LegendTracking);
        using Bitmap stroke = RenderTrackedLineMask(
            text.Trim(),
            face.Family,
            face.Style,
            settings,
            width,
            height,
            Math.Max(1.0, settings.LegendFontSize),
            LegendWidthScale,
            LegendHeightScale,
            x,
            y,
            Math.Max(LegendStrokeWidth, LegendWhiteSpread),
            LegendStrokeWidth,
            alphaStrength: 1.0,
            tracking: LegendTracking);
        double textBlack = Math.Max(0, settings.LegendTextBlack);
        Bitmap composed = Compose(
            fill,
            stroke,
            width,
            height,
            LegendStrokeStrength * textBlack,
            LegendBlurRadius,
            LegendBlurStrength * textBlack);
        if (!settings.LegendShowRegistered)
        {
            return composed;
        }

        try
        {
            using Bitmap stamp = RenderLegendRegisteredStamp(legendStampPath!, settings, width, height);
            AlphaComposite(stamp, composed);
            QuantizeBitmap(composed);
            return composed;
        }
        catch
        {
            composed.Dispose();
            throw;
        }
    }

    private static Bitmap RenderLegendRegisteredStamp(
        string stampPath,
        EndTitleTextureRenderSettings settings,
        int width,
        int height)
    {
        using Bitmap source = new(stampPath);
        Bitmap fill = new(width, height, PixelFormat.Format32bppArgb);
        int targetX = (int)Math.Round(LegendRegX + settings.XNudge + settings.LegendRegisteredXNudge) + LegendRegStampX;
        int targetY = (int)Math.Round(LegendRegY + settings.YOffset) + LegendRegStampY;

        for (int y = 0; y < source.Height; y++)
        {
            int destinationY = targetY + y;
            if (destinationY < 0 || destinationY >= height)
            {
                continue;
            }

            for (int x = 0; x < source.Width; x++)
            {
                int destinationX = targetX + x;
                if (destinationX < 0 || destinationX >= width)
                {
                    continue;
                }

                Color pixel = source.GetPixel(x, y);
                if (pixel.A <= 0
                    || pixel.R < LegendRegStampThreshold
                    || pixel.G < LegendRegStampThreshold
                    || pixel.B < LegendRegStampThreshold)
                {
                    continue;
                }

                fill.SetPixel(destinationX, destinationY, pixel);
            }
        }

        try
        {
            int scale = Math.Max(1, settings.RenderScale);
            using Bitmap high = ResizeNearest(fill, width * scale, height * scale);
            using Bitmap dilated = MaxFilterAlpha(high, Math.Max(1, (int)Math.Round(LegendRegStrokeWidth * scale)));
            using Bitmap strokeLow = ResizeMaskLanczos(dilated, width, height);
            double[,] blurred = BlurAlpha(dilated, LegendRegBlurRadius * scale);
            using Bitmap blurredHigh = CreateAlphaBitmap(blurred, dilated.Width, dilated.Height);
            using Bitmap blurLow = ResizeMaskLanczos(blurredHigh, width, height);

            Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
            double regBlack = Math.Max(0, settings.LegendRegisteredBlack);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double strokeAlpha = strokeLow.GetPixel(x, y).A * LegendRegStrokeStrength * regBlack;
                    double blurAlpha = blurLow.GetPixel(x, y).A * LegendRegBlurStrength * regBlack;
                    int alpha = Quantize(Math.Max(strokeAlpha, blurAlpha));
                    if (alpha > 0)
                    {
                        output.SetPixel(x, y, Color.FromArgb(alpha, 0, 0, 0));
                    }
                }
            }

            AlphaComposite(fill, output);
            return output;
        }
        finally
        {
            fill.Dispose();
        }
    }

    private static Bitmap Compose(
        Bitmap fill,
        Bitmap stroke,
        int width,
        int height,
        double strokeStrength,
        double blurRadius,
        double blurStrength,
        Bitmap? assetStroke = null,
        double assetStrokeStrength = 0,
        double assetBlurStrength = 0)
    {
        double[,] blur = BlurAlpha(stroke, blurRadius);
        double[,]? assetBlur = assetStroke is null ? null : BlurAlpha(assetStroke, blurRadius);
        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double strokeAlpha = stroke.GetPixel(x, y).A * strokeStrength;
                double blurAlpha = blur[x, y] * blurStrength;
                double assetStrokeAlpha = assetStroke is null ? 0 : assetStroke.GetPixel(x, y).A * assetStrokeStrength;
                double assetBlurAlpha = assetBlur is null ? 0 : assetBlur[x, y] * assetBlurStrength;
                double blackAlpha = Math.Clamp(Math.Max(Math.Max(strokeAlpha, blurAlpha), Math.Max(assetStrokeAlpha, assetBlurAlpha)), 0, 255);
                double fillAlpha = fill.GetPixel(x, y).A;
                if (blackAlpha <= 0 && fillAlpha <= 0)
                {
                    continue;
                }

                double alpha = fillAlpha + blackAlpha * (1 - fillAlpha / 255d);
                int gray = alpha <= 0 ? 0 : Quantize(255 * fillAlpha / alpha);
                int red = gray;
                int green = gray;
                int blue = gray;
                output.SetPixel(x, y, Color.FromArgb(Quantize(alpha), red, green, blue));
            }
        }

        return output;
    }

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

    private static Bitmap RenderPresentedByMask(
        string text,
        SixLabors.Fonts.FontFamily family,
        SixLabors.Fonts.FontStyle style,
        EndTitleTextureRenderSettings settings,
        int width,
        int height,
        double x,
        double y,
        double strokeWidth,
        double alphaStrength)
        => RenderSimpleLineMask(
            text,
            family,
            style,
            settings,
            width,
            height,
            PresentedByFontSize,
            PresentedByWidthScale,
            x,
            y,
            Math.Max(PresentedByStrokeWidth, PresentedByWhiteSpread),
            strokeWidth,
            alphaStrength);

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

    private static Bitmap ResizeNearest(Bitmap source, int width, int height)
    {
        if (source.Width == width && source.Height == height)
        {
            return (Bitmap)source.Clone();
        }

        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(output);
        graphics.Clear(Color.Transparent);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.DrawImage(
            source,
            new Rectangle(0, 0, width, height),
            new Rectangle(0, 0, source.Width, source.Height),
            GraphicsUnit.Pixel);
        return output;
    }

    private static Bitmap ResizeMaskLanczos(Bitmap source, int width, int height)
    {
        if (source.Width == width && source.Height == height)
        {
            return (Bitmap)source.Clone();
        }

        using SixLabors.ImageSharp.Image<Rgba32> image = ToImageSharp(source);
        image.Mutate(context => context.Resize(width, height, KnownResamplers.Lanczos3));
        return ToBitmap(image);
    }

    private static SixLabors.ImageSharp.Image<Rgba32> ToImageSharp(Bitmap source)
    {
        SixLabors.ImageSharp.Image<Rgba32> image = new(source.Width, source.Height);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Color color = source.GetPixel(x, y);
                image[x, y] = new Rgba32(color.R, color.G, color.B, color.A);
            }
        }

        return image;
    }

    private static Bitmap ToBitmap(SixLabors.ImageSharp.Image<Rgba32> image)
    {
        Bitmap bitmap = new(image.Width, image.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Rgba32 pixel = image[x, y];
                bitmap.SetPixel(x, y, Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B));
            }
        }

        return bitmap;
    }

    private static Bitmap MaxFilterAlpha(Bitmap source, int radius)
    {
        int width = source.Width;
        int height = source.Height;
        double[,] alpha = new double[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                alpha[x, y] = source.GetPixel(x, y).A;
            }
        }

        double[,] temp = new double[width, height];
        double[,] output = new double[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int sampleX = Math.Clamp(x + k, 0, width - 1);
                    value = Math.Max(value, alpha[sampleX, y]);
                }

                temp[x, y] = value;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int sampleY = Math.Clamp(y + k, 0, height - 1);
                    value = Math.Max(value, temp[x, sampleY]);
                }

                output[x, y] = value;
            }
        }

        return CreateAlphaBitmap(output, width, height);
    }

    private static Bitmap CreateAlphaBitmap(double[,] alpha, int width, int height)
    {
        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int value = Math.Clamp((int)Math.Round(alpha[x, y]), 0, 255);
                if (value > 0)
                {
                    output.SetPixel(x, y, Color.FromArgb(value, 255, 255, 255));
                }
            }
        }

        return output;
    }

    private static Bitmap FitAlphaToCanvas(Bitmap source, int width, int height, bool center, int destinationX)
    {
        Rectangle bounds = GetAlphaBounds(source);
        if (bounds.IsEmpty)
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        int maxWidth = Math.Max(1, width - PresentedByFitHorizontalInset * 2);
        int maxHeight = Math.Max(1, height);
        double widthFitScale = bounds.Width > maxWidth ? maxWidth / (double)bounds.Width : 1d;
        double heightFitScale = bounds.Height > maxHeight ? maxHeight / (double)bounds.Height : 1d;
        double fitScale = Math.Min(widthFitScale, heightFitScale);
        int targetWidth = Math.Max(1, (int)Math.Round(bounds.Width * fitScale));
        int targetHeight = Math.Max(1, (int)Math.Round(bounds.Height * fitScale));

        using Bitmap cropped = Crop(source, bounds);
        using Bitmap fitted = ResizeMaskLanczos(cropped, targetWidth, targetHeight);
        Bitmap output = new(width, height, PixelFormat.Format32bppArgb);
        int targetX = center
            ? (int)((width - targetWidth) / 2d + 0.5)
            : destinationX;
        int targetY = (int)((height - targetHeight) / 2d + 0.5);
        PasteBitmap(fitted, output, targetX, targetY);
        return output;
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

    private static void PasteBitmap(Bitmap source, Bitmap destination, int destinationX, int destinationY)
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

    private static void PasteNonTransparent(Bitmap source, Bitmap destination, int destinationX, int destinationY)
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

                Color sourceColor = source.GetPixel(x, y);
                if (sourceColor.A > 0)
                {
                    destination.SetPixel(targetX, targetY, sourceColor);
                }
            }
        }
    }

    private static void AlphaComposite(Bitmap source, Bitmap destination)
    {
        int width = Math.Min(source.Width, destination.Width);
        int height = Math.Min(source.Height, destination.Height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color top = source.GetPixel(x, y);
                if (top.A == 0)
                {
                    continue;
                }

                Color bottom = destination.GetPixel(x, y);
                double topAlpha = top.A / 255d;
                double bottomAlpha = bottom.A / 255d;
                double outputAlpha = topAlpha + bottomAlpha * (1 - topAlpha);
                if (outputAlpha <= 0)
                {
                    destination.SetPixel(x, y, Color.Transparent);
                    continue;
                }

                int red = (int)Math.Round((top.R * topAlpha + bottom.R * bottomAlpha * (1 - topAlpha)) / outputAlpha);
                int green = (int)Math.Round((top.G * topAlpha + bottom.G * bottomAlpha * (1 - topAlpha)) / outputAlpha);
                int blue = (int)Math.Round((top.B * topAlpha + bottom.B * bottomAlpha * (1 - topAlpha)) / outputAlpha);
                destination.SetPixel(
                    x,
                    y,
                    Color.FromArgb(
                        Math.Clamp((int)Math.Round(outputAlpha * 255), 0, 255),
                        Math.Clamp(red, 0, 255),
                        Math.Clamp(green, 0, 255),
                        Math.Clamp(blue, 0, 255)));
            }
        }
    }

    private static void QuantizeBitmap(Bitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color color = bitmap.GetPixel(x, y);
                if (color.A == 0)
                {
                    continue;
                }

                bitmap.SetPixel(
                    x,
                    y,
                    Color.FromArgb(
                        Quantize(color.A),
                        Quantize(color.R),
                        Quantize(color.G),
                        Quantize(color.B)));
            }
        }
    }

    private static Rectangle GetAlphaBounds(Bitmap source)
    {
        int left = source.Width;
        int top = source.Height;
        int right = -1;
        int bottom = -1;
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                if (source.GetPixel(x, y).A == 0)
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return right < left || bottom < top
            ? Rectangle.Empty
            : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    private static void PasteMaxAlpha(Bitmap source, Bitmap destination, int destinationX, int destinationY, double alphaStrength)
    {
        Rectangle sourceBounds = new(0, 0, source.Width, source.Height);
        Rectangle destinationBounds = new(0, 0, destination.Width, destination.Height);
        BitmapData sourceData = source.LockBits(sourceBounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData destinationData = destination.LockBits(destinationBounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        bool useFallback = sourceData.Stride < 0 || destinationData.Stride < 0;
        try
        {
            if (!useFallback)
            {
                int sourceStride = Math.Abs(sourceData.Stride);
                int destinationStride = Math.Abs(destinationData.Stride);
                byte[] sourcePixels = new byte[sourceStride * source.Height];
                byte[] destinationPixels = new byte[destinationStride * destination.Height];
                Marshal.Copy(sourceData.Scan0, sourcePixels, 0, sourcePixels.Length);
                Marshal.Copy(destinationData.Scan0, destinationPixels, 0, destinationPixels.Length);

                for (int y = 0; y < source.Height; y++)
                {
                    int targetY = destinationY + y;
                    if (targetY < 0 || targetY >= destination.Height)
                    {
                        continue;
                    }

                    int sourceRow = GetBitmapRowOffset(sourceData, sourceStride, source.Height, y);
                    int destinationRow = GetBitmapRowOffset(destinationData, destinationStride, destination.Height, targetY);
                    for (int x = 0; x < source.Width; x++)
                    {
                        int targetX = destinationX + x;
                        if (targetX < 0 || targetX >= destination.Width)
                        {
                            continue;
                        }

                        int sourceOffset = sourceRow + x * 4;
                        int alpha = Math.Clamp((int)Math.Round(sourcePixels[sourceOffset + 3] * alphaStrength), 0, 255);
                        if (alpha <= 0)
                        {
                            continue;
                        }

                        int destinationOffset = destinationRow + targetX * 4;
                        if (alpha > destinationPixels[destinationOffset + 3])
                        {
                            destinationPixels[destinationOffset + 0] = sourcePixels[sourceOffset + 0];
                            destinationPixels[destinationOffset + 1] = sourcePixels[sourceOffset + 1];
                            destinationPixels[destinationOffset + 2] = sourcePixels[sourceOffset + 2];
                            destinationPixels[destinationOffset + 3] = (byte)alpha;
                        }
                    }
                }

                Marshal.Copy(destinationPixels, 0, destinationData.Scan0, destinationPixels.Length);
            }
        }
        finally
        {
            source.UnlockBits(sourceData);
            destination.UnlockBits(destinationData);
        }

        if (useFallback)
        {
            PasteMaxAlphaSlow(source, destination, destinationX, destinationY, alphaStrength);
        }
    }

    private static int GetBitmapRowOffset(BitmapData data, int stride, int height, int y)
        => data.Stride >= 0 ? y * stride : (height - 1 - y) * stride;

    private static void PasteMaxAlphaSlow(Bitmap source, Bitmap destination, int destinationX, int destinationY, double alphaStrength)
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

                Color sourceColor = source.GetPixel(x, y);
                int alpha = Math.Clamp((int)Math.Round(sourceColor.A * alphaStrength), 0, 255);
                Color destinationColor = destination.GetPixel(targetX, targetY);
                if (alpha > destinationColor.A)
                {
                    destination.SetPixel(targetX, targetY, Color.FromArgb(alpha, sourceColor.R, sourceColor.G, sourceColor.B));
                }
            }
        }
    }

    private static double[,] BlurAlpha(Bitmap source, double radius)
    {
        int width = source.Width;
        int height = source.Height;
        double[,] alpha = new double[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                alpha[x, y] = source.GetPixel(x, y).A;
            }
        }

        if (radius <= 0)
        {
            return alpha;
        }

        double[] kernel = CreateGaussianKernel(radius);
        int kernelRadius = kernel.Length / 2;
        double[,] temp = new double[width, height];
        double[,] output = new double[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = 0;
                for (int k = -kernelRadius; k <= kernelRadius; k++)
                {
                    int sampleX = Math.Clamp(x + k, 0, width - 1);
                    value += alpha[sampleX, y] * kernel[k + kernelRadius];
                }

                temp[x, y] = value;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = 0;
                for (int k = -kernelRadius; k <= kernelRadius; k++)
                {
                    int sampleY = Math.Clamp(y + k, 0, height - 1);
                    value += temp[x, sampleY] * kernel[k + kernelRadius];
                }

                output[x, y] = value;
            }
        }

        return output;
    }

    private static double[] CreateGaussianKernel(double radius)
    {
        int kernelRadius = Math.Max(1, (int)Math.Ceiling(radius * 3));
        double sigma = Math.Max(0.001, radius);
        double[] kernel = new double[kernelRadius * 2 + 1];
        double total = 0;
        for (int i = -kernelRadius; i <= kernelRadius; i++)
        {
            double value = Math.Exp(-(i * i) / (2 * sigma * sigma));
            kernel[i + kernelRadius] = value;
            total += value;
        }

        for (int i = 0; i < kernel.Length; i++)
        {
            kernel[i] /= total;
        }

        return kernel;
    }

    private static int Quantize(double value)
    {
        int snapped = (int)Math.Round(Math.Clamp(value, 0, 255) / QuantizationStep) * QuantizationStep;
        return Math.Clamp(snapped, 0, 255);
    }

    private static CachedDrawingFontFace GetDrawingFontFace(TextTextureFont font)
        => DrawingFontFaces.GetOrAdd(
            GetFontCacheKey(font),
            _ => new Lazy<CachedDrawingFontFace>(() => new CachedDrawingFontFace(font)))
            .Value;

    private static CachedImageSharpFontFace GetImageSharpFontFace(TextTextureFont font)
        => ImageSharpFontFaces.GetOrAdd(
            GetFontCacheKey(font),
            _ => new Lazy<CachedImageSharpFontFace>(() => new CachedImageSharpFontFace(font)))
            .Value;

    private static string GetFontCacheKey(TextTextureFont font)
        => string.Join("|", Path.GetFullPath(font.Path), font.FamilyName ?? string.Empty, font.Style);

    private sealed class CachedDrawingFontFace
    {
        private readonly PrivateFontCollection _collection = new();

        public CachedDrawingFontFace(TextTextureFont font)
        {
            _collection.AddFontFile(font.Path);
            Family = TextTextureFontResolver.ResolveDrawingFamily(_collection, font);
            Style = TextTextureFontResolver.ResolveDrawingStyle(Family, font);
        }

        public FontFamily Family { get; }

        public FontStyle Style { get; }
    }

    private sealed class CachedImageSharpFontFace
    {
        private readonly SixLabors.Fonts.FontCollection _collection = new();

        public CachedImageSharpFontFace(TextTextureFont font)
        {
            IReadOnlyCollection<SixLabors.Fonts.FontFamily> families = Path.GetExtension(font.Path).Equals(".ttc", StringComparison.OrdinalIgnoreCase)
                ? _collection.AddCollection(font.Path).ToArray()
                : [_collection.Add(font.Path)];

            Family = ResolveFamily(families, font);
            Style = TextTextureFontResolver.ToImageSharpStyle(font.Style);
        }

        public SixLabors.Fonts.FontFamily Family { get; }

        public SixLabors.Fonts.FontStyle Style { get; }

        private static SixLabors.Fonts.FontFamily ResolveFamily(
            IReadOnlyCollection<SixLabors.Fonts.FontFamily> families,
            TextTextureFont font)
        {
            if (string.IsNullOrWhiteSpace(font.FamilyName))
            {
                return families.First();
            }

            foreach (SixLabors.Fonts.FontFamily family in families)
            {
                if (string.Equals(family.Name, font.FamilyName, StringComparison.OrdinalIgnoreCase))
                {
                    return family;
                }
            }

            string availableFamilies = string.Join(", ", families.Select(family => family.Name));
            throw new InvalidOperationException(
                $"Font family '{font.FamilyName}' was not found in {Path.GetFileName(font.Path)}. Available families: {availableFamilies}.");
        }
    }
}
