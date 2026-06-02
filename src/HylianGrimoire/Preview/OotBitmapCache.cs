using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using HylianGrimoire.Glyphs;

namespace HylianGrimoire.Preview;

public static class OotBitmapCache
{
    private static readonly string AssetRoot = Path.Combine(
        AppContext.BaseDirectory,
        HylianGrimoire.Games.GameProfiles.Get(HylianGrimoire.Games.GameKind.OcarinaOfTime).Assets.PreviewRoot);
    private static readonly string CacheRoot = Path.Combine(Path.GetTempPath(), "HylianGrimoirePreviewCache");

    static OotBitmapCache()
    {
        ClearTemporaryCache();
    }

    private const float TextScale = 0.75f;
    private const float OutputScale = 1.75f;
    private const int AlignmentGuideCount = 9;
    private const float AlignmentGuideHalfSpan = 98f;
    private const float AlignmentGuideCenterOffset = 1f;
    private static readonly Color AlignmentGuideRed = Color.FromArgb(230, 255, 30, 30);
    private static readonly Color AlignmentGuideGreen = Color.FromArgb(230, 0, 255, 0);
    private static readonly IReadOnlyDictionary<byte, string> IconRelativePaths = new Dictionary<byte, string>
    {
        [0x00] = @"icon_item_static\gItemIconDekuStickTex.png",
        [0x01] = @"icon_item_static\gItemIconDekuNutTex.png",
        [0x02] = @"icon_item_static\gItemIconBombTex.png",
        [0x03] = @"icon_item_static\gItemIconBowTex.png",
        [0x04] = @"icon_item_static\gItemIconArrowFireTex.png",
        [0x05] = @"icon_item_static\gItemIconDinsFireTex.png",
        [0x06] = @"icon_item_static\gItemIconSlingshotTex.png",
        [0x07] = @"icon_item_static\gItemIconOcarinaFairyTex.png",
        [0x08] = @"icon_item_static\gItemIconOcarinaOfTimeTex.png",
        [0x09] = @"icon_item_static\gItemIconBombchuTex.png",
        [0x0A] = @"icon_item_static\gItemIconHookshotTex.png",
        [0x0B] = @"icon_item_static\gItemIconLongshotTex.png",
        [0x0C] = @"icon_item_static\gItemIconArrowIceTex.png",
        [0x0D] = @"icon_item_static\gItemIconFaroresWindTex.png",
        [0x0E] = @"icon_item_static\gItemIconBoomerangTex.png",
        [0x0F] = @"icon_item_static\gItemIconLensOfTruthTex.png",
        [0x10] = @"icon_item_static\gItemIconMagicBeanTex.png",
        [0x11] = @"icon_item_static\gItemIconHammerTex.png",
        [0x12] = @"icon_item_static\gItemIconArrowLightTex.png",
        [0x13] = @"icon_item_static\gItemIconNayrusLoveTex.png",
        [0x14] = @"icon_item_static\gItemIconBottleEmptyTex.png",
        [0x15] = @"icon_item_static\gItemIconBottlePotionRedTex.png",
        [0x16] = @"icon_item_static\gItemIconBottlePotionGreenTex.png",
        [0x17] = @"icon_item_static\gItemIconBottlePotionBlueTex.png",
        [0x18] = @"icon_item_static\gItemIconBottleFairyTex.png",
        [0x19] = @"icon_item_static\gItemIconBottleFishTex.png",
        [0x1A] = @"icon_item_static\gItemIconBottleMilkFullTex.png",
        [0x1B] = @"icon_item_static\gItemIconBottleRutosLetterTex.png",
        [0x1C] = @"icon_item_static\gItemIconBottleBlueFireTex.png",
        [0x1D] = @"icon_item_static\gItemIconBottleBugTex.png",
        [0x1E] = @"icon_item_static\gItemIconBottlePoeTex.png",
        [0x1F] = @"icon_item_static\gItemIconBottleMilkHalfTex.png",
        [0x20] = @"icon_item_static\gItemIconBottleBigPoeTex.png",
        [0x21] = @"icon_item_static\gItemIconPocketEggTex.png",
        [0x22] = @"icon_item_static\gItemIconChickenTex.png",
        [0x23] = @"icon_item_static\gItemIconZeldasLetterTex.png",
        [0x24] = @"icon_item_static\gItemIconMaskKeatonTex.png",
        [0x25] = @"icon_item_static\gItemIconMaskSkullTex.png",
        [0x26] = @"icon_item_static\gItemIconMaskSpookyTex.png",
        [0x27] = @"icon_item_static\gItemIconMaskBunnyHoodTex.png",
        [0x28] = @"icon_item_static\gItemIconMaskGoronTex.png",
        [0x29] = @"icon_item_static\gItemIconMaskZoraTex.png",
        [0x2A] = @"icon_item_static\gItemIconMaskGerudoTex.png",
        [0x2B] = @"icon_item_static\gItemIconMaskTruthTex.png",
        [0x2C] = @"icon_item_static\gItemIconSoldOutTex.png",
        [0x2D] = @"icon_item_static\gItemIconPocketEggTex.png",
        [0x2E] = @"icon_item_static\gItemIconChickenTex.png",
        [0x2F] = @"icon_item_static\gItemIconCojiroTex.png",
        [0x30] = @"icon_item_static\gItemIconOddMushroomTex.png",
        [0x31] = @"icon_item_static\gItemIconOddPotionTex.png",
        [0x32] = @"icon_item_static\gItemIconPoachersSawTex.png",
        [0x33] = @"icon_item_static\gItemIconBrokenGiantsKnifeTex.png",
        [0x34] = @"icon_item_static\gItemIconPrescriptionTex.png",
        [0x35] = @"icon_item_static\gItemIconEyeballFrogTex.png",
        [0x36] = @"icon_item_static\gItemIconEyeDropsTex.png",
        [0x37] = @"icon_item_static\gItemIconClaimCheckTex.png",
        [0x38] = @"icon_item_static\gItemIconBowFireTex.png",
        [0x39] = @"icon_item_static\gItemIconBowIceTex.png",
        [0x3A] = @"icon_item_static\gItemIconBowLightTex.png",
        [0x3B] = @"icon_item_static\gItemIconSwordKokiriTex.png",
        [0x3C] = @"icon_item_static\gItemIconSwordMasterTex.png",
        [0x3D] = @"icon_item_static\gItemIconSwordBiggoronTex.png",
        [0x3E] = @"icon_item_static\gItemIconShieldDekuTex.png",
        [0x3F] = @"icon_item_static\gItemIconShieldHylianTex.png",
        [0x40] = @"icon_item_static\gItemIconShieldMirrorTex.png",
        [0x41] = @"icon_item_static\gItemIconTunicKokiriTex.png",
        [0x42] = @"icon_item_static\gItemIconTunicGoronTex.png",
        [0x43] = @"icon_item_static\gItemIconTunicZoraTex.png",
        [0x44] = @"icon_item_static\gItemIconBootsKokiriTex.png",
        [0x45] = @"icon_item_static\gItemIconBootsIronTex.png",
        [0x46] = @"icon_item_static\gItemIconBootsHoverTex.png",
        [0x47] = @"icon_item_static\gItemIconBulletBag30Tex.png",
        [0x48] = @"icon_item_static\gItemIconBulletBag40Tex.png",
        [0x49] = @"icon_item_static\gItemIconBulletBag50Tex.png",
        [0x4A] = @"icon_item_static\gItemIconQuiver30Tex.png",
        [0x4B] = @"icon_item_static\gItemIconQuiver40Tex.png",
        [0x4C] = @"icon_item_static\gItemIconQuiver50Tex.png",
        [0x4D] = @"icon_item_static\gItemIconBombBag20Tex.png",
        [0x4E] = @"icon_item_static\gItemIconBombBag30Tex.png",
        [0x4F] = @"icon_item_static\gItemIconBombBag40Tex.png",
        [0x50] = @"icon_item_static\gItemIconGoronsBraceletTex.png",
        [0x51] = @"icon_item_static\gItemIconSilverGauntletsTex.png",
        [0x52] = @"icon_item_static\gItemIconGoldenGauntletsTex.png",
        [0x53] = @"icon_item_static\gItemIconScaleSilverTex.png",
        [0x54] = @"icon_item_static\gItemIconScaleGoldenTex.png",
        [0x55] = @"icon_item_static\gItemIconBrokenGoronsSwordTex.png",
        [0x56] = @"icon_item_static\gItemIconAdultsWalletTex.png",
        [0x57] = @"icon_item_static\gItemIconGiantsWalletTex.png",
        [0x58] = @"icon_item_static\gItemIconDekuSeedsTex.png",
        [0x59] = @"icon_item_static\gItemIconFishingPoleTex.png",
        [0x66] = @"icon_item_24_static\gQuestIconMedallionForestTex.png",
        [0x67] = @"icon_item_24_static\gQuestIconMedallionFireTex.png",
        [0x68] = @"icon_item_24_static\gQuestIconMedallionWaterTex.png",
        [0x69] = @"icon_item_24_static\gQuestIconMedallionSpiritTex.png",
        [0x6A] = @"icon_item_24_static\gQuestIconMedallionShadowTex.png",
        [0x6B] = @"icon_item_24_static\gQuestIconMedallionLightTex.png",
        [0x6C] = @"icon_item_24_static\gQuestIconKokiriEmeraldTex.png",
        [0x6D] = @"icon_item_24_static\gQuestIconGoronRubyTex.png",
        [0x6E] = @"icon_item_24_static\gQuestIconZoraSapphireTex.png",
        [0x6F] = @"icon_item_24_static\gQuestIconStoneOfAgonyTex.png",
        [0x70] = @"icon_item_24_static\gQuestIconGerudosCardTex.png",
        [0x71] = @"icon_item_24_static\gQuestIconGoldSkulltulaTex.png",
        [0x72] = @"icon_item_24_static\gQuestIconHeartContainerTex.png",
        [0x73] = @"icon_item_24_static\gQuestIconHeartPieceTex.png",
        [0x74] = @"icon_item_24_static\gQuestIconDungeonBossKeyTex.png",
        [0x75] = @"icon_item_24_static\gQuestIconDungeonCompassTex.png",
        [0x76] = @"icon_item_24_static\gQuestIconDungeonMapTex.png",
        [0x77] = @"icon_item_24_static\gQuestIconSmallKeyTex.png",
        [0x78] = @"icon_item_24_static\gQuestIconMagicJarSmallTex.png",
        [0x79] = @"icon_item_24_static\gQuestIconMagicJarBigTex.png",
    };

    public static Uri GetMessageBox(OotPreviewStyle style)
    {
        string source = style switch
        {
            OotPreviewStyle.Wooden => FromAssetRoot(@"message_static\gSignMessageBackgroundTex.png"),
            OotPreviewStyle.Ocarina => FromAssetRoot(@"message_static\gNoteStaffMessageBackgroundTex.png"),
            OotPreviewStyle.Black => FromAssetRoot(@"message_static\gDefaultMessageBackgroundTex.png"),
            OotPreviewStyle.Blue => FromAssetRoot(@"message_static\gFadingMessageBackgroundTex.png"),
            _ => FromAssetRoot(@"message_static\gFadingMessageBackgroundTex.png"),
        };

        string key = $"box-v6-mask-alpha-{style}-{source}";
        string output = GetCachePath(key);
        if (File.Exists(output))
        {
            return new Uri(output);
        }

        Directory.CreateDirectory(CacheRoot);

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
            OotPreviewStyle.Wooden => ColorizeMultiply(box, Color.FromArgb(230, 70, 50, 30)),
            OotPreviewStyle.Ocarina => ColorizeMultiply(box, Color.FromArgb(180, 255, 0, 0)),
            OotPreviewStyle.Blue => ColorizeAlpha(box, Color.FromArgb(170, 0, 10, 50)),
            OotPreviewStyle.Black => ColorizeAlpha(box, Color.FromArgb(170, 0, 0, 0)),
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
        string source = FromAssetRoot(lastBox
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
        string tokenKey = string.Join('-', tokens.Select(token => $"{(int)token.Kind:x}{token.Value:x2}"));
        string guideKey = showAlignmentGuides
            ? $"guides-{AlignmentGuideCount}-{AlignmentGuideHalfSpan:0.###}-{AlignmentGuideCenterOffset:0.###}"
            : "guides-off";
        string output = GetCachePath($"preview-v16-glyph-{glyphSource.CacheKey}-{style}-{darkText}-{lastBox}-{guideKey}-{tokenKey}");
        if (File.Exists(output))
        {
            return new Uri(output);
        }

        Directory.CreateDirectory(CacheRoot);

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

        using var box = new Bitmap(OotBitmapCache.GetMessageBox(style).LocalPath);
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

                case OotPreviewTokenKind.Center:
                    x = 128 - (GetLineWidth(tokens, tokenIndex, scale, glyphSource) / 2.0f);
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

        string marker = FromAssetRoot(lastBox
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
        string arrow = FromAssetRoot(@"message_static\gMessageArrowTex.png");

        for (int i = 0; i < choiceCount; i++)
        {
            DrawMaskImage(graphics, arrow, Color.FromArgb(255, 0, 110, 255), (int)x, (int)y, size, size, brighten: false);
            y += 12;
        }
    }

    private static float GetLineWidth(IReadOnlyList<OotPreviewToken> tokens, int centerIndex, float scale, IGlyphSource glyphSource)
    {
        float width = 0;
        for (int i = centerIndex + 1; i < tokens.Count; i++)
        {
            OotPreviewToken token = tokens[i];
            if (token.Kind is OotPreviewTokenKind.LineBreak or OotPreviewTokenKind.Center)
            {
                break;
            }

            if (token.Kind == OotPreviewTokenKind.Glyph)
            {
                width += GetGlyphAdvance(token.Value, scale, glyphSource);
            }
        }

        return width;
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

        DrawMaskImage(graphics, path, color, (int)x, (int)y, size, size, brighten: true);
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
        using var tinted = CreateTintedMask(mask, color, brighten: false);
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
        string output = GetCachePath(key);
        if (File.Exists(output))
        {
            return new Uri(output);
        }

        Directory.CreateDirectory(CacheRoot);

        using var mask = new Bitmap(source);
        using var tinted = CreateTintedMask(mask, Color.FromArgb(color.A, color.R, color.G, color.B), brighten);

        tinted.Save(output, ImageFormat.Png);
        return new Uri(output);
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
            brighten
                ? (r, (byte)255, (byte)255, (byte)255)
                : (r, color.R, color.G, color.B));
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
    {
        return stride < 0 ? (height - 1 - y) * absoluteStride : y * absoluteStride;
    }

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

    private static string ResolveIconAsset(byte value)
    {
        return IconRelativePaths.TryGetValue(value, out string? relativePath)
            ? FromAssetRoot(relativePath)
            : Path.Combine(AssetRoot, "__missing__", $"icon_{value}.png");
    }

    private static string FromAssetRoot(string relativePath)
    {
        return Path.Combine(AssetRoot, relativePath.Replace('\\', Path.DirectorySeparatorChar));
    }

    private static void ClearTemporaryCache()
    {
        try
        {
            Directory.Delete(CacheRoot, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
