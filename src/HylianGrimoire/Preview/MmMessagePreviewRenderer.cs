using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Services;

namespace HylianGrimoire.Preview;

public static partial class MmMessagePreviewRenderer
{
    private static readonly PreviewAssetResolver Assets = new(GameKind.MajorasMask);
    private static readonly PreviewBitmapCache Cache = new("mm");

    private const float TextScale = 0.75f;
    private const float OutputScale = 1.75f;
    private const int AlignmentGuideCount = 9;
    private const float AlignmentGuideHalfSpan = 98f;
    private static readonly Color AlignmentGuideRed = Color.FromArgb(230, 255, 30, 30);
    private static readonly Color AlignmentGuideGreen = Color.FromArgb(230, 0, 255, 0);

    public static Uri RenderPreview(
        MmPreviewStyle style,
        IReadOnlyList<OotPreviewToken> tokens,
        bool lastBox,
        MmPreviewRenderOptions options,
        bool showAlignmentGuides,
        IGlyphSource? glyphSource = null)
    {
        glyphSource ??= MmGlyphSources.Assets;
        return new Uri(EnsureRenderPreviewFile(style, tokens, lastBox, options, showAlignmentGuides, glyphSource));
    }

    private static string EnsureRenderPreviewFile(
        MmPreviewStyle style,
        IReadOnlyList<OotPreviewToken> tokens,
        bool lastBox,
        MmPreviewRenderOptions options,
        bool showAlignmentGuides,
        IGlyphSource glyphSource)
    {
        string output = GetRenderPreviewCachePath(style, tokens, lastBox, options, showAlignmentGuides, glyphSource);
        if (File.Exists(output))
        {
            return output;
        }

        Cache.EnsureDirectory();
        using Bitmap scaled = RenderPreviewUncached(style, tokens, lastBox, options, showAlignmentGuides, glyphSource);
        PngFileWriter.SaveDirect(scaled, output);
        return output;
    }

    private static string GetRenderPreviewCachePath(
        MmPreviewStyle style,
        IReadOnlyList<OotPreviewToken> tokens,
        bool lastBox,
        MmPreviewRenderOptions options,
        bool showAlignmentGuides,
        IGlyphSource glyphSource)
    {
        string tokenKey = string.Join('-', tokens.Select(token => $"{(int)token.Kind:x}{token.Value:x2}"));
        string guideKey = showAlignmentGuides ? "guides-on" : "guides-off";
        string optionKey = $"icon-{options.IconId:x2}-center-{options.Centered}";
        return Cache.GetPath($"mm-preview-v27-{glyphSource.CacheKey}-{style}-{lastBox}-{guideKey}-{optionKey}-{tokenKey}");
    }

    private static Bitmap RenderPreviewUncached(
        MmPreviewStyle style,
        IReadOnlyList<OotPreviewToken> tokens,
        bool lastBox,
        MmPreviewRenderOptions options,
        bool showAlignmentGuides,
        IGlyphSource glyphSource)
    {
        int canvasWidth = style == MmPreviewStyle.StaffCredits ? 320 : 256;
        int canvasHeight = style == MmPreviewStyle.StaffCredits ? 240 : 72;
        using var canvas = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(canvas))
        {
            graphics.Clear(GetCanvasBackground(style));
            graphics.InterpolationMode = InterpolationMode.High;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            DrawBox(graphics, style);
            bool hasIcon = style != MmPreviewStyle.StaffCredits
                && style != MmPreviewStyle.OcarinaFreePlay
                && DrawMessageIcon(graphics, options.IconId);
            if (style == MmPreviewStyle.StaffCredits)
            {
                DrawStaffCreditsText(graphics, tokens, glyphSource);
            }
            else
            {
                DrawText(graphics, style, tokens, lastBox, glyphSource, options.Centered, hasIcon);
            }

            if (showAlignmentGuides)
            {
                DrawAlignmentGuides(graphics, canvas.Width, canvas.Height);
            }
        }

        return PreviewBitmapTransforms.Scale(canvas, OutputScale);
    }
}
