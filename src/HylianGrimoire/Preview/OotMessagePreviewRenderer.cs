using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;

namespace HylianGrimoire.Preview;

public static class OotMessagePreviewRenderer
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

    public static Uri GetMessageBox(OotPreviewStyle style)
    {
        string source = style switch
        {
            OotPreviewStyle.Wooden => Assets.Resolve(@"message_static\gSignMessageBackgroundTex.png"),
            OotPreviewStyle.Ocarina => Assets.Resolve(@"message_static\gNoteStaffMessageBackgroundTex.png"),
            OotPreviewStyle.Black => Assets.Resolve(@"message_static\gDefaultMessageBackgroundTex.png"),
            OotPreviewStyle.Blue => Assets.Resolve(@"message_static\gFadingMessageBackgroundTex.png"),
            _ => Assets.Resolve(@"message_static\gFadingMessageBackgroundTex.png"),
        };

        string key = $"box-v6-mask-alpha-{style}-{source}";
        string output = Cache.GetPath(key);
        if (File.Exists(output))
        {
            return new Uri(output);
        }

        Cache.EnsureDirectory();

        using var half = new Bitmap(source);
        using var box = new Bitmap(256, 64, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(box))
        {
            graphics.Clear(Color.Transparent);
            graphics.DrawImage(half, 0, 0, 128, 64);
            half.RotateFlip(RotateFlipType.RotateNoneFlipX);
            graphics.DrawImage(half, 128, 0, 128, 64);
        }

        using Bitmap tinted = style switch
        {
            OotPreviewStyle.Wooden => PreviewBitmapTransforms.ColorizeMultiply(box, Color.FromArgb(230, 70, 50, 30)),
            OotPreviewStyle.Ocarina => PreviewBitmapTransforms.ColorizeMultiply(box, Color.FromArgb(180, 255, 0, 0)),
            OotPreviewStyle.Blue => PreviewBitmapTransforms.ColorizeAlpha(box, Color.FromArgb(170, 0, 10, 50)),
            OotPreviewStyle.Black => PreviewBitmapTransforms.ColorizeAlpha(box, Color.FromArgb(170, 0, 0, 0)),
            _ => new Bitmap(box),
        };

        tinted.Save(output, ImageFormat.Png);
        return new Uri(output);
    }

    public static Uri GetGlyph(byte value, Windows.UI.Color color, bool shadow = false)
    {
        return GetGlyph(value, color, OotGlyphSources.OriginalAssets, shadow);
    }

    public static Uri GetGlyph(byte value, Windows.UI.Color color, IGlyphSource glyphSource, bool shadow = false)
    {
        string source = glyphSource.GetGlyphPath(value);
        return GetMaskImage(source, $"glyph-{glyphSource.CacheKey}-{value:x2}", color, brighten: !shadow);
    }

    public static Uri GetMarker(bool lastBox)
    {
        string source = Assets.Resolve(lastBox
            ? @"message_static\gMessageEndSquareTex.png"
            : @"message_static\gMessageContinueTriangleTex.png");
        return GetMaskImage(source, Path.GetFileName(source), Windows.UI.Color.FromArgb(255, 50, 170, 255), brighten: false);
    }

    public static Uri RenderPreview(
        OotPreviewStyle style,
        IReadOnlyList<OotPreviewToken> tokens,
        bool darkText,
        bool lastBox,
        bool showAlignmentGuides,
        IGlyphSource? glyphSource = null)
    {
        glyphSource ??= OotGlyphSources.OriginalAssets;
        string output = GetRenderPreviewCachePath(style, tokens, darkText, lastBox, showAlignmentGuides, glyphSource);
        if (!File.Exists(output))
        {
            RenderPreviewBitmap(style, tokens, darkText, lastBox, showAlignmentGuides, glyphSource).Dispose();
        }

        return new Uri(output);
    }

    public static Bitmap RenderPreviewBitmap(
        OotPreviewStyle style,
        IReadOnlyList<OotPreviewToken> tokens,
        bool darkText,
        bool lastBox,
        bool showAlignmentGuides,
        IGlyphSource? glyphSource = null)
    {
        glyphSource ??= OotGlyphSources.OriginalAssets;
        string output = GetRenderPreviewCachePath(style, tokens, darkText, lastBox, showAlignmentGuides, glyphSource);
        if (File.Exists(output))
        {
            return new Bitmap(output);
        }

        Cache.EnsureDirectory();

        Bitmap scaled = RenderPreviewUncached(style, tokens, darkText, lastBox, showAlignmentGuides, glyphSource);
        scaled.Save(output, ImageFormat.Png);
        return scaled;
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

    private static void DrawBox(Graphics graphics, OotPreviewStyle style)
    {
        if (style == OotPreviewStyle.Credits)
        {
            graphics.Clear(Color.Black);
            return;
        }

        if (IsNoneBoxStyle(style))
        {
            return;
        }

        using var box = new Bitmap(GetMessageBox(style).LocalPath);
        graphics.DrawImage(box, 0, 0, 256, 64);
    }

    private static void DrawText(
        Graphics graphics,
        OotPreviewStyle style,
        IReadOnlyList<OotPreviewToken> tokens,
        bool darkText,
        bool lastBox,
        IGlyphSource glyphSource)
    {
        float scale = style == OotPreviewStyle.Credits ? 0.85f : TextScale;
        float x = style == OotPreviewStyle.Credits ? 20 : 32;
        float y = style == OotPreviewStyle.Credits ? 48 : GetStartY(tokens);
        Color currentColor = darkText ? Color.Black : Color.White;
        bool drawShadow = !darkText;
        bool hasIcon = tokens.Any(token => token.Kind == OotPreviewTokenKind.Icon);
        int choiceCount = GetChoiceCount(tokens);

        for (int tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
        {
            var token = tokens[tokenIndex];
            switch (token.Kind)
            {
                case OotPreviewTokenKind.LineBreak:
                    x = style == OotPreviewStyle.Credits ? 20 : hasIcon ? 64 : 32;
                    y += style == OotPreviewStyle.Credits ? 6 : 12;
                    if (ShouldIndentChoiceLine(choiceCount, y))
                    {
                        x = 64;
                    }
                    continue;

                case OotPreviewTokenKind.Shift:
                    x += token.Value;
                    continue;

                case OotPreviewTokenKind.Color:
                    currentColor = GetTextColor(token.Value, darkText ? Color.Black : Color.White, style);
                    continue;

                case OotPreviewTokenKind.Choice:
                    DrawChoiceArrows(graphics, token.Value, scale);
                    continue;

                case OotPreviewTokenKind.Icon:
                    DrawIcon(graphics, token.Value, x - 10, style == OotPreviewStyle.None ? 36 : 16);
                    x += 32;
                    continue;

                case OotPreviewTokenKind.Glyph:
                    DrawGlyph(graphics, token.Value, currentColor, x, y, drawShadow, scale, glyphSource);
                    x += GetGlyphAdvance(token.Value, scale, glyphSource);
                    continue;
            }
        }

        if (style == OotPreviewStyle.Credits || IsNoneBoxStyle(style))
        {
            return;
        }

        string marker = Assets.Resolve(lastBox
            ? @"message_static\gMessageEndSquareTex.png"
            : @"message_static\gMessageContinueTriangleTex.png");
        DrawMaskImage(graphics, marker, Color.FromArgb(255, 0, 110, 255), 124, 60, (int)(16 * TextScale), (int)(16 * TextScale), brighten: false);
    }

    private static bool IsNoneBoxStyle(OotPreviewStyle style)
    {
        return style is OotPreviewStyle.None or OotPreviewStyle.NoneDarkText;
    }

    private static int GetChoiceCount(IReadOnlyList<OotPreviewToken> tokens)
    {
        return tokens.FirstOrDefault(token => token.Kind == OotPreviewTokenKind.Choice).Value;
    }

    private static bool ShouldIndentChoiceLine(int choiceCount, float y)
    {
        return choiceCount == 2 && y >= 32
            || choiceCount == 3 && y >= 20;
    }

    private static void DrawChoiceArrows(Graphics graphics, byte choiceCount, float scale)
    {
        float x = 16;
        float y = choiceCount == 3 ? 20 : 32;
        int size = (int)(16 * scale);
        string arrow = Assets.Resolve(@"message_static\gMessageArrowTex.png");

        for (int i = 0; i < choiceCount; i++)
        {
            DrawMaskImage(graphics, arrow, Color.FromArgb(255, 0, 110, 255), (int)x, (int)y, size, size, brighten: false);
            y += 12;
        }
    }

    private static void DrawAlignmentGuides(Graphics graphics, int width, int height)
    {
        float centerX = (width / 2.0f) + AlignmentGuideCenterOffset;
        float leftX = centerX - AlignmentGuideHalfSpan;
        float step = (AlignmentGuideHalfSpan * 2) / (AlignmentGuideCount - 1);

        using var redPen = new Pen(AlignmentGuideRed, 1);
        using var greenPen = new Pen(AlignmentGuideGreen, 1);

        for (int i = 0; i < AlignmentGuideCount; i++)
        {
            float x = leftX + (step * i);
            Pen pen = i % 2 == 0 ? redPen : greenPen;
            graphics.DrawLine(pen, x, 0, x, height);
        }
    }

    private static float GetStartY(IReadOnlyList<OotPreviewToken> tokens)
    {
        int lineBreaks = tokens.Count(token => token.Kind == OotPreviewTokenKind.LineBreak);
        return Math.Max(8, (52 - (12 * lineBreaks)) / 2.0f);
    }

    private static void DrawIcon(Graphics graphics, byte value, float x, float y)
    {
        string path = ResolveIconAsset(value);
        if (!File.Exists(path))
        {
            return;
        }

        using var icon = new Bitmap(path);
        int size = value < 102 ? 32 : 24;
        graphics.DrawImage(icon, (int)x, (int)y, size, size);
    }

    private static void DrawGlyph(Graphics graphics, byte value, Color color, float x, float y, bool shadow, float scale, IGlyphSource glyphSource)
    {
        string path = glyphSource.GetGlyphPath(value);
        if (!File.Exists(path) || value == 0x20)
        {
            return;
        }

        int size = (int)(16 * scale);
        if (shadow)
        {
            DrawMaskImage(graphics, path, Color.Black, (int)x + 1, (int)y + 1, size, size, brighten: false);
        }

        DrawMaskImage(graphics, path, color, (int)x, (int)y, size, size, brighten: false);
    }

    private static float GetGlyphAdvance(byte value, float scale, IGlyphSource glyphSource)
    {
        if (value == 0x20)
        {
            return 6.0f;
        }

        return (int)(glyphSource.GetAdvance(value) * scale);
    }

    private static void DrawMaskImage(Graphics graphics, string source, Color color, int x, int y, int width, int height, bool brighten)
    {
        using var mask = new Bitmap(source);
        using var tinted = PreviewBitmapTransforms.CreateTintedMask(mask, color, brighten);
        graphics.DrawImage(tinted, x, y, width, height);
    }

    private static Color GetTextColor(byte index, Color fallback, OotPreviewStyle style)
    {
        bool wooden = style == OotPreviewStyle.Wooden;
        return index switch
        {
            1 => wooden ? Color.FromArgb(255, 120, 0) : Color.FromArgb(255, 60, 60),
            2 => Color.FromArgb(70, 255, 80),
            3 => wooden ? Color.FromArgb(80, 90, 255) : Color.FromArgb(80, 110, 255),
            4 => Color.FromArgb(90, 180, 255),
            5 => wooden ? Color.FromArgb(255, 150, 180) : Color.FromArgb(210, 100, 255),
            6 => Color.FromArgb(255, 255, 30),
            7 => Color.Black,
            _ => fallback,
        };
    }

    private static Uri GetMaskImage(string source, string name, Windows.UI.Color color, bool brighten)
    {
        string key = $"{name}-{color.A:x2}{color.R:x2}{color.G:x2}{color.B:x2}-{brighten}";
        string output = Cache.GetPath(key);
        if (File.Exists(output))
        {
            return new Uri(output);
        }

        Cache.EnsureDirectory();

        using var mask = new Bitmap(source);
        using var tinted = PreviewBitmapTransforms.CreateTintedMask(mask, Color.FromArgb(color.A, color.R, color.G, color.B), brighten);

        tinted.Save(output, ImageFormat.Png);
        return new Uri(output);
    }

    private static string ResolveIconAsset(byte value)
    {
        return OotPreviewIconCatalog.TryGetRelativePath(value, out string? relativePath)
            ? Assets.Resolve(relativePath)
            : Assets.ResolveMissing($"icon_{value}.png");
    }
}
