using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using HylianGrimoire.Games;
using HylianGrimoire.Games.MajorasMask;
using HylianGrimoire.Glyphs;

namespace HylianGrimoire.Preview;

public static class MmBitmapCache
{
    private static readonly string AssetRoot = Path.Combine(
        AppContext.BaseDirectory,
        GameProfiles.Get(GameKind.MajorasMask).Assets.PreviewRoot);
    private static readonly string CacheRoot = Path.Combine(Path.GetTempPath(), "HylianGrimoirePreviewCache");

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
        string tokenKey = string.Join('-', tokens.Select(token => $"{(int)token.Kind:x}{token.Value:x2}"));
        string guideKey = showAlignmentGuides ? "guides-on" : "guides-off";
        string optionKey = $"icon-{options.IconId:x2}-center-{options.Centered}";
        string output = GetCachePath($"mm-preview-v27-{glyphSource.CacheKey}-{style}-{lastBox}-{guideKey}-{optionKey}-{tokenKey}");
        if (File.Exists(output))
        {
            return new Uri(output);
        }

        Directory.CreateDirectory(CacheRoot);

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

        using var scaled = new Bitmap((int)(canvas.Width * OutputScale), (int)(canvas.Height * OutputScale), PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(scaled))
        {
            graphics.Clear(Color.Transparent);
            graphics.InterpolationMode = InterpolationMode.High;
            graphics.DrawImage(canvas, 0, 0, scaled.Width, scaled.Height);
        }

        scaled.Save(output, ImageFormat.Png);
        return new Uri(output);
    }

    private static void DrawBox(Graphics graphics, MmPreviewStyle style)
    {
        if (IsClearStyle(style))
        {
            return;
        }

        using var half = new Bitmap(GetMessageBoxSource(style));
        using var box = new Bitmap(256, 64, PixelFormat.Format32bppArgb);
        using (var boxGraphics = Graphics.FromImage(box))
        {
            boxGraphics.Clear(Color.Transparent);
            boxGraphics.DrawImage(half, 0, 0, 128, 64);
            half.RotateFlip(RotateFlipType.RotateNoneFlipX);
            boxGraphics.DrawImage(half, 128, 0, 128, 64);
        }

        using Bitmap styledBox = style switch
        {
            MmPreviewStyle.Wooden => ColorizeMultiply(box, Color.FromArgb(230, 70, 50, 30)),
            MmPreviewStyle.Ocarina => ColorizeMultiply(box, Color.FromArgb(180, 255, 0, 0)),
            MmPreviewStyle.Blue or MmPreviewStyle.BlueDefault => ColorizeAlpha(box, Color.FromArgb(170, 0, 10, 50)),
            MmPreviewStyle.Notebook => ColorizeAlpha(box, Color.FromArgb(220, 255, 255, 195)),
            _ => ColorizeAlpha(box, Color.FromArgb(170, 0, 0, 0)),
        };

        graphics.DrawImage(styledBox, 0, 0, 256, 64);

        if (style == MmPreviewStyle.Ocarina)
        {
            DrawOcarinaTrebleClef(graphics);
        }
    }

    private static void DrawText(
        Graphics graphics,
        MmPreviewStyle style,
        IReadOnlyList<OotPreviewToken> tokens,
        bool lastBox,
        IGlyphSource glyphSource,
        bool centerText,
        bool hasIcon)
    {
        float baseX = hasIcon ? 48 : 32;
        float y = GetStartY(style, tokens);
        float x = GetLineStartX(tokens, 0, glyphSource, centerText, baseX, 0, y);
        Color currentColor = GetDefaultTextColor(style);
        int activeChoiceCount = 0;
        int lineBreakCount = tokens.Count(token => token.Kind == OotPreviewTokenKind.LineBreak);

        for (int tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
        {
            OotPreviewToken token = tokens[tokenIndex];
            switch (token.Kind)
            {
                case OotPreviewTokenKind.LineBreak:
                    y += 12;
                    x = GetLineStartX(tokens, tokenIndex + 1, glyphSource, centerText, baseX, activeChoiceCount, y);
                    continue;

                case OotPreviewTokenKind.CarriageReturn:
                    x = GetLineStartX(tokens, tokenIndex + 1, glyphSource, centerText, baseX, activeChoiceCount, y);
                    continue;

                case OotPreviewTokenKind.Shift:
                    x += token.Value;
                    continue;

                case OotPreviewTokenKind.Color:
                    currentColor = GetTextColor(style, token.Value);
                    continue;

                case OotPreviewTokenKind.Center:
                    x = 128 - (GetLineWidth(tokens, tokenIndex + 1, glyphSource) / 2.0f);
                    continue;

                case OotPreviewTokenKind.Choice:
                    activeChoiceCount = token.Value;
                    DrawChoiceArrows(graphics, token.Value, lineBreakCount);
                    x = GetLineStartX(tokens, tokenIndex + 1, glyphSource, centerText, baseX, activeChoiceCount, y);
                    continue;

                case OotPreviewTokenKind.Background:
                    DrawOcarinaBackgroundX(graphics);
                    x = 43;
                    continue;

                case OotPreviewTokenKind.Glyph:
                    DrawGlyph(graphics, token.Value, GetGlyphColor(token.Value, currentColor), x, y, glyphSource, TextScale);
                    x += GetGlyphAdvance(token.Value, glyphSource, TextScale);
                    continue;
            }
        }

        if (IsClearStyle(style))
        {
            return;
        }

        string marker = FromAssetRoot(lastBox
            ? @"message_static\gMessageEndSquareTex.png"
            : @"message_static\gMessageContinueTriangleTex.png");
        DrawMaskImage(graphics, marker, Color.FromArgb(255, 0, 110, 255), 124, 60, (int)(16 * TextScale), (int)(16 * TextScale), brighten: false);
    }

    private static float GetLineStartX(
        IReadOnlyList<OotPreviewToken> tokens,
        int lineStartIndex,
        IGlyphSource glyphSource,
        bool centerText,
        float baseX,
        int choiceCount,
        float y)
    {
        if (centerText)
        {
            return 128 - (GetLineWidth(tokens, lineStartIndex, glyphSource) / 2.0f);
        }

        return baseX + GetChoiceLineOffset(choiceCount, y);
    }

    private static bool IsClearStyle(MmPreviewStyle style)
        => style is MmPreviewStyle.Clear
            or MmPreviewStyle.ClearBlackText
            or MmPreviewStyle.TypeB
            or MmPreviewStyle.TitleCard
            or MmPreviewStyle.OcarinaFreePlay
            or MmPreviewStyle.StaffCredits;

    private static Color GetCanvasBackground(MmPreviewStyle style)
        => style == MmPreviewStyle.StaffCredits ? Color.Black : Color.Transparent;

    private static void DrawStaffCreditsText(
        Graphics graphics,
        IReadOnlyList<OotPreviewToken> tokens,
        IGlyphSource glyphSource)
    {
        const float scale = 0.85f;
        float x = 20;
        float y = 48;
        Color currentColor = Color.White;

        for (int tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
        {
            OotPreviewToken token = tokens[tokenIndex];
            switch (token.Kind)
            {
                case OotPreviewTokenKind.LineBreak:
                    x = 20;
                    y += 6;
                    continue;

                case OotPreviewTokenKind.CarriageReturn:
                    x = 20;
                    continue;

                case OotPreviewTokenKind.Shift:
                    x += token.Value;
                    continue;

                case OotPreviewTokenKind.Color:
                    currentColor = GetTextColor(MmPreviewStyle.StaffCredits, token.Value);
                    continue;

                case OotPreviewTokenKind.Center:
                    x = 160 - (GetLineWidth(tokens, tokenIndex + 1, glyphSource, scale) / 2.0f);
                    continue;

                case OotPreviewTokenKind.Glyph:
                    DrawGlyph(graphics, token.Value, GetGlyphColor(token.Value, currentColor), x, y, glyphSource, scale);
                    x += GetGlyphAdvance(token.Value, glyphSource, scale);
                    continue;
            }
        }
    }

    private static float GetChoiceLineOffset(int choiceCount, float y)
    {
        return choiceCount switch
        {
            2 when y >= 32 => 10,
            3 => 22,
            _ => 0,
        };
    }

    private static void DrawChoiceArrows(Graphics graphics, byte choiceCount, int lineBreakCount)
    {
        float x = 13;
        float y = choiceCount == 3 ? 13 : 25;
        if (lineBreakCount >= 3)
        {
            y += 7;
        }

        int size = (int)(16 * TextScale);
        string arrow = FromAssetRoot(@"message_static\gMessageArrowTex.png");

        for (int i = 0; i < choiceCount; i++)
        {
            DrawMaskImage(graphics, arrow, Color.FromArgb(255, 0, 110, 255), (int)x, (int)y, size, size, brighten: false);
            y += 12;
        }
    }

    private static void DrawOcarinaTrebleClef(Graphics graphics)
    {
        string clef = FromAssetRoot(@"parameter_static\gOcarinaTrebleClefTex.png");
        if (!File.Exists(clef))
        {
            return;
        }

        // The game draws the clef at screen coordinates (78,166) while the ocarina box starts at (34,142).
        DrawMaskImage(graphics, clef, Color.FromArgb(255, 100, 0), 44, 24, 16, 32, brighten: false);
    }

    private static void DrawOcarinaBackgroundX(Graphics graphics)
    {
        string left = FromAssetRoot(@"message_texture_static\gMessageXLeftTex.png");
        string right = FromAssetRoot(@"message_texture_static\gMessageXRightTex.png");
        if (!File.Exists(left) || !File.Exists(right))
        {
            return;
        }

        const int x = 11;
        const int y = 8;
        const int width = 96;
        const int height = 48;
        Color orange = Color.FromArgb(255, 60, 0);

        DrawMaskImage(graphics, left, Color.Black, x, y + 1, width, height, brighten: false);
        DrawMaskImage(graphics, right, Color.Black, x + width, y + 1, width, height, brighten: false);
        DrawMaskImage(graphics, left, orange, x, y, width, height, brighten: false);
        DrawMaskImage(graphics, right, orange, x + width, y, width, height, brighten: false);
    }

    private static float GetLineWidth(
        IReadOnlyList<OotPreviewToken> tokens,
        int lineStartIndex,
        IGlyphSource glyphSource,
        float scale = TextScale)
    {
        float width = 0;
        for (int i = lineStartIndex; i < tokens.Count; i++)
        {
            OotPreviewToken token = tokens[i];
            if (token.Kind is OotPreviewTokenKind.LineBreak or OotPreviewTokenKind.CarriageReturn or OotPreviewTokenKind.Center or OotPreviewTokenKind.BoxBreak2)
            {
                break;
            }

            if (token.Kind == OotPreviewTokenKind.Glyph)
            {
                width += GetGlyphAdvance(token.Value, glyphSource, scale);
            }
            else if (token.Kind == OotPreviewTokenKind.Shift)
            {
                width += token.Value;
            }
        }

        return width;
    }

    private static void DrawAlignmentGuides(Graphics graphics, int width, int height)
    {
        float centerX = width / 2.0f;
        float leftX = centerX - AlignmentGuideHalfSpan;
        float step = (AlignmentGuideHalfSpan * 2) / (AlignmentGuideCount - 1);

        using var redPen = new Pen(AlignmentGuideRed, 1);
        using var greenPen = new Pen(AlignmentGuideGreen, 1);

        for (int i = 0; i < AlignmentGuideCount; i++)
        {
            float x = leftX + (step * i);
            graphics.DrawLine(i % 2 == 0 ? redPen : greenPen, x, 0, x, height);
        }
    }

    private static float GetStartY(MmPreviewStyle style, IReadOnlyList<OotPreviewToken> tokens)
        => MmPreviewLayout.GetStartY(style, tokens);

    private static void DrawGlyph(
        Graphics graphics,
        byte value,
        Color color,
        float x,
        float y,
        IGlyphSource glyphSource,
        float scale)
    {
        string path = glyphSource.GetGlyphPath(value);
        if (!File.Exists(path) || value == 0x20)
        {
            return;
        }

        int size = (int)(16 * scale);
        DrawMaskImage(graphics, path, Color.Black, (int)x + 1, (int)y + 1, size, size, brighten: false);
        DrawMaskImage(graphics, path, color, (int)x, (int)y, size, size, brighten: true);
    }

    private static bool DrawMessageIcon(Graphics graphics, byte iconId)
    {
        MmMessageIconEntry entry = MmMessageIconCatalog.Get(iconId);
        if (entry.RelativePath is null)
        {
            return false;
        }

        return entry.DrawKind switch
        {
            MmMessageIconDrawKind.Heart => DrawSmallMaskIcon(graphics, entry.RelativePath, Color.FromArgb(255, 0, 0)),
            MmMessageIconDrawKind.Rupee => DrawSmallMaskIcon(graphics, entry.RelativePath, GetRupeeColor(entry.ItemId)),
            MmMessageIconDrawKind.StrayFairy => DrawStrayFairyIcon(graphics, entry.RelativePath),
            MmMessageIconDrawKind.Image => DrawImageIcon(graphics, entry.RelativePath),
            _ => false,
        };
    }

    private static bool DrawImageIcon(Graphics graphics, string relativePath)
    {
        string path = FromAssetRoot(relativePath);
        if (!File.Exists(path))
        {
            return false;
        }

        using var icon = new Bitmap(path);
        int x = icon.Width == 24 ? 16 : 12;
        int y = icon.Height == 24 ? 20 : 16;
        graphics.DrawImage(icon, x, y, icon.Width, icon.Height);
        return true;
    }

    private static bool DrawSmallMaskIcon(Graphics graphics, string relativePath, Color color)
    {
        string path = FromAssetRoot(relativePath);
        if (!File.Exists(path))
        {
            return false;
        }

        DrawMaskImage(graphics, path, color, 16, 20, 16, 16, brighten: false);
        return true;
    }

    private static bool DrawStrayFairyIcon(Graphics graphics, string relativePath)
    {
        string fairyPath = FromAssetRoot(relativePath);
        if (!File.Exists(fairyPath))
        {
            return false;
        }

        string glowPath = FromAssetRoot(@"parameter_static\gStrayFairyGlowingCircleIconTex.png");
        if (File.Exists(glowPath))
        {
            DrawMaskImage(graphics, glowPath, Color.FromArgb(255, 110, 160), 12, 16, 32, 24, brighten: false);
        }

        using var fairy = new Bitmap(fairyPath);
        graphics.DrawImage(fairy, 12, 16, 32, 24);
        return true;
    }

    private static Color GetRupeeColor(byte? itemId) => itemId switch
    {
        0x84 => Color.FromArgb(0, 255, 0),
        0x85 => Color.FromArgb(0, 0, 255),
        0x86 => Color.White,
        0x87 => Color.Red,
        0x88 => Color.FromArgb(255, 0, 255),
        0x89 => Color.White,
        0x8A => Color.FromArgb(255, 100, 0),
        _ => Color.White,
    };

    private static float GetGlyphAdvance(byte value, IGlyphSource glyphSource, float scale)
    {
        if (value == 0x20)
        {
            return 6.0f;
        }

        return (int)(glyphSource.GetAdvance(value) * scale);
    }

    private static Color GetDefaultTextColor(MmPreviewStyle style)
        => style is MmPreviewStyle.Notebook or MmPreviewStyle.ClearBlackText ? Color.Black : Color.White;

    private static Color GetTextColor(MmPreviewStyle style, byte index) => style switch
    {
        MmPreviewStyle.Wooden => GetWoodenTextColor(index),
        MmPreviewStyle.Notebook => GetNotebookTextColor(index),
        MmPreviewStyle.ClearBlackText when index == 0 => Color.Black,
        _ => GetNormalTextColor(index),
    };

    private static Color GetGlyphColor(byte value, Color currentColor)
        => IsButtonGlyph(value) ? GetButtonGlyphColor(value) : currentColor;

    private static bool IsButtonGlyph(byte value)
        => value is >= 0xB0 and <= 0xBB;

    private static Color GetButtonGlyphColor(byte value)
        => value switch
        {
            0xB0 => Color.FromArgb(80, 90, 255),
            0xB1 => Color.FromArgb(70, 255, 80),
            0xB2 or 0xB6 or 0xB7 or 0xB8 or 0xB9 => Color.FromArgb(255, 255, 50),
            0xBA => Color.FromArgb(70, 255, 80),
            _ => Color.FromArgb(180, 180, 200),
        };

    private static Color GetNormalTextColor(byte index) => index switch
    {
        1 => Color.FromArgb(255, 60, 60),
        2 => Color.FromArgb(70, 255, 80),
        3 => Color.FromArgb(80, 90, 255),
        4 => Color.FromArgb(255, 255, 50),
        5 => Color.FromArgb(80, 150, 255),
        6 => Color.FromArgb(255, 150, 180),
        7 => Color.FromArgb(170, 170, 170),
        8 => Color.FromArgb(255, 130, 30),
        _ => Color.White,
    };

    private static Color GetWoodenTextColor(byte index) => index switch
    {
        1 => Color.FromArgb(255, 120, 0),
        2 => Color.FromArgb(70, 255, 80),
        3 => Color.FromArgb(80, 110, 255),
        4 => Color.FromArgb(255, 255, 30),
        5 => Color.FromArgb(90, 180, 255),
        6 => Color.FromArgb(210, 100, 255),
        7 => Color.FromArgb(170, 170, 170),
        8 => Color.FromArgb(255, 130, 30),
        _ => Color.White,
    };

    private static Color GetNotebookTextColor(byte index) => index switch
    {
        1 => Color.FromArgb(195, 0, 0),
        2 => Color.FromArgb(70, 255, 80),
        3 => Color.FromArgb(80, 90, 255),
        4 => Color.FromArgb(255, 255, 50),
        5 => Color.FromArgb(80, 150, 255),
        6 => Color.FromArgb(255, 150, 180),
        7 => Color.FromArgb(170, 170, 170),
        8 => Color.FromArgb(255, 130, 30),
        _ => Color.Black,
    };

    private static string GetMessageBoxSource(MmPreviewStyle style)
    {
        string relativePath = style switch
        {
            MmPreviewStyle.Wooden => @"message_static\gMessageSignBackgroundTex.png",
            MmPreviewStyle.Ocarina => @"message_static\gMessageNoteStaffBackgroundTex.png",
            MmPreviewStyle.Notebook => @"message_static\gMessageNotebookBackgroundTex.png",
            MmPreviewStyle.Blue => @"message_static\gMessageFadingBackgroundTex.png",
            _ => @"message_static\gMessageDefaultBackgroundTex.png",
        };

        return FromAssetRoot(relativePath);
    }

    private static void DrawMaskImage(Graphics graphics, string source, Color color, int x, int y, int width, int height, bool brighten)
    {
        using var mask = new Bitmap(source);
        using var tinted = CreateTintedMask(mask, color, brighten: brighten);
        graphics.DrawImage(tinted, x, y, width, height);
    }

    private static Bitmap ColorizeAlpha(Bitmap source, Color color)
    {
        return TransformPixels(source, (a, r, g, b) =>
            ((byte)(r * color.A / 255), color.R, color.G, color.B));
    }

    private static Bitmap ColorizeMultiply(Bitmap source, Color color)
    {
        return TransformPixels(source, (a, r, g, b) =>
            ((byte)(a * color.A / 255), (byte)(r * color.R / 255), (byte)(g * color.G / 255), (byte)(b * color.B / 255)));
    }

    private static Bitmap CreateTintedMask(Bitmap source, Color color, bool brighten)
    {
        return TransformPixels(source, (a, r, g, b) =>
            (r, color.R, color.G, color.B));
    }

    private static Bitmap TransformPixels(Bitmap source, Func<byte, byte, byte, byte, (byte A, byte R, byte G, byte B)> transform)
    {
        using Bitmap input = source.PixelFormat == PixelFormat.Format32bppArgb
            ? (Bitmap)source.Clone()
            : CloneAsArgb(source);

        var output = new Bitmap(input.Width, input.Height, PixelFormat.Format32bppArgb);
        Rectangle bounds = new(0, 0, input.Width, input.Height);
        BitmapData inputData = input.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData outputData = output.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            int inputStride = Math.Abs(inputData.Stride);
            int outputStride = Math.Abs(outputData.Stride);
            byte[] inputBytes = new byte[inputStride * input.Height];
            byte[] outputBytes = new byte[outputStride * output.Height];

            Marshal.Copy(inputData.Scan0, inputBytes, 0, inputBytes.Length);

            for (int y = 0; y < input.Height; y++)
            {
                int inputRow = GetRowOffset(inputData.Stride, inputStride, input.Height, y);
                int outputRow = GetRowOffset(outputData.Stride, outputStride, output.Height, y);
                for (int x = 0; x < input.Width; x++)
                {
                    int inputOffset = inputRow + x * 4;
                    byte b = inputBytes[inputOffset];
                    byte g = inputBytes[inputOffset + 1];
                    byte r = inputBytes[inputOffset + 2];
                    byte a = inputBytes[inputOffset + 3];
                    var pixel = transform(a, r, g, b);

                    int outputOffset = outputRow + x * 4;
                    outputBytes[outputOffset] = pixel.B;
                    outputBytes[outputOffset + 1] = pixel.G;
                    outputBytes[outputOffset + 2] = pixel.R;
                    outputBytes[outputOffset + 3] = pixel.A;
                }
            }

            Marshal.Copy(outputBytes, 0, outputData.Scan0, outputBytes.Length);
        }
        finally
        {
            input.UnlockBits(inputData);
            output.UnlockBits(outputData);
        }

        return output;
    }

    private static int GetRowOffset(int stride, int absoluteStride, int height, int y)
        => stride < 0 ? (height - 1 - y) * absoluteStride : y * absoluteStride;

    private static Bitmap CloneAsArgb(Bitmap source)
    {
        var clone = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(clone);
        graphics.DrawImage(source, 0, 0, source.Width, source.Height);
        return clone;
    }

    private static string GetCachePath(string key)
    {
        byte[] hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key));
        string name = Convert.ToHexString(hash)[..16].ToLowerInvariant();
        return Path.Combine(CacheRoot, $"{name}.png");
    }

    private static string FromAssetRoot(string relativePath)
        => Path.Combine(AssetRoot, relativePath.Replace('\\', Path.DirectorySeparatorChar));
}
