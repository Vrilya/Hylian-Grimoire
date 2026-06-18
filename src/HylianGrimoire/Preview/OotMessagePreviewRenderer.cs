using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Services;

namespace HylianGrimoire.Preview;

public static partial class OotMessagePreviewRenderer
{
    private static readonly PreviewAssetResolver Assets = new(GameKind.OcarinaOfTime);
    private static readonly PreviewBitmapCache Cache = new("oot");

    static OotMessagePreviewRenderer()
    {
        Cache.ClearTemporaryFiles();
    }

    private const float TextScale = 0.75f;
    private const float OutputScale = 1.75f;
    private const int AlignmentGuideCount = 9;
    private const float AlignmentGuideHalfSpan = 98f;
    private const float AlignmentGuideCenterOffset = 1f;
    private static readonly Color AlignmentGuideRed = Color.FromArgb(230, 255, 30, 30);
    private static readonly Color AlignmentGuideGreen = Color.FromArgb(230, 0, 255, 0);

    public static Uri RenderPreview(
        OotPreviewStyle style,
        IReadOnlyList<OotPreviewToken> tokens,
        bool darkText,
        bool lastBox,
        bool showAlignmentGuides,
        IGlyphSource? glyphSource = null)
    {
        glyphSource ??= OotGlyphSources.OriginalAssets;
        return new Uri(EnsureRenderPreviewFile(style, tokens, darkText, lastBox, showAlignmentGuides, glyphSource));
    }

    private static string EnsureRenderPreviewFile(
        OotPreviewStyle style,
        IReadOnlyList<OotPreviewToken> tokens,
        bool darkText,
        bool lastBox,
        bool showAlignmentGuides,
        IGlyphSource glyphSource)
    {
        string output = GetRenderPreviewCachePath(style, tokens, darkText, lastBox, showAlignmentGuides, glyphSource);
        if (File.Exists(output))
        {
            return output;
        }

        Cache.EnsureDirectory();
        using Bitmap scaled = RenderPreviewUncached(style, tokens, darkText, lastBox, showAlignmentGuides, glyphSource);
        PngFileWriter.SaveDirect(scaled, output);
        return output;
    }

    private static string GetRenderPreviewCachePath(
        OotPreviewStyle style,
        IReadOnlyList<OotPreviewToken> tokens,
        bool darkText,
        bool lastBox,
        bool showAlignmentGuides,
        IGlyphSource glyphSource)
    {
        string tokenKey = string.Join('-', tokens.Select(token => $"{(int)token.Kind:x}{token.Value:x2}"));
        string guideKey = showAlignmentGuides
            ? $"guides-{AlignmentGuideCount}-{AlignmentGuideHalfSpan:0.###}-{AlignmentGuideCenterOffset:0.###}"
            : "guides-off";
        return Cache.GetPath($"preview-v16-glyph-{glyphSource.CacheKey}-{style}-{darkText}-{lastBox}-{guideKey}-{tokenKey}");
    }

    private static Bitmap RenderPreviewUncached(
        OotPreviewStyle style,
        IReadOnlyList<OotPreviewToken> tokens,
        bool darkText,
        bool lastBox,
        bool showAlignmentGuides,
        IGlyphSource glyphSource)
    {
        int width = style == OotPreviewStyle.Credits ? 320 : 256;
        int height = style == OotPreviewStyle.Credits ? 240 : 72;
        using var canvas = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(canvas))
        {
            graphics.Clear(style == OotPreviewStyle.None ? Color.Black : Color.Transparent);
            graphics.InterpolationMode = InterpolationMode.High;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            DrawBox(graphics, style);
            DrawText(graphics, style, tokens, darkText, lastBox, glyphSource);
            if (showAlignmentGuides)
            {
                DrawAlignmentGuides(graphics, canvas.Width, canvas.Height);
            }
        }

        return PreviewBitmapTransforms.Scale(canvas, OutputScale);
    }
}
