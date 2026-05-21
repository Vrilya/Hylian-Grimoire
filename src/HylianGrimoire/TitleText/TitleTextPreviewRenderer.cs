using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using HylianGrimoire.Rom;

namespace HylianGrimoire.TitleText;

public static class TitleTextPreviewRenderer
{
    private const string CacheVersion = "title-text-preview-v4";
    private const int LogicalWidth = 320;
    private const int LogicalHeight = 240;
    private const int GlyphSourceSize = 16;
    private const int GlyphDrawSize = 10;
    private const int PressStartY = 174;
    private const int NoControllerY = 174;
    private const int PressStartAdvance = 7;
    private const int NoControllerAdvance = 9;
    private const int PressStartDefaultGap = 5;
    private const int NoControllerDefaultGap = 10;
    private const int GuideCenterX = LogicalWidth / 2;
    private const int GuideSpacing = 24;
    private const int GuideLineCount = 7;
    private static readonly string BackgroundPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Title", "Titel.png");
    private static readonly string CacheRoot = Path.Combine(Path.GetTempPath(), "HylianGrimoireTitleTextPreview");

    public static Uri Render(
        ReadOnlySpan<byte> rom,
        RomFontResources fontResources,
        TitleTextLine noController,
        TitleTextLine pressStart,
        bool showGuides)
    {
        Directory.CreateDirectory(CacheRoot);
        string key = CreateCacheKey(rom, fontResources, noController, pressStart, showGuides);
        string path = Path.Combine(CacheRoot, $"{key}.png");
        if (File.Exists(path))
        {
            return new Uri(path);
        }

        using Bitmap bitmap = LoadBackground();
        float scale = bitmap.Width / (float)LogicalWidth;
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        if (showGuides)
        {
            DrawGuides(graphics, noController, pressStart, scale);
        }

        DrawLine(
            graphics,
            rom,
            fontResources,
            noController.Text,
            noController.X,
            NoControllerY,
            NoControllerAdvance,
            NoControllerDefaultGap,
            Color.FromArgb(100, 255, 255));

        DrawLine(
            graphics,
            rom,
            fontResources,
            pressStart.Text,
            pressStart.X,
            PressStartY + 18,
            PressStartAdvance,
            PressStartDefaultGap,
            Color.FromArgb(255, 30, 30));

        bitmap.Save(path, ImageFormat.Png);
        return new Uri(path);
    }

    private static void DrawLine(
        Graphics graphics,
        ReadOnlySpan<byte> rom,
        RomFontResources fontResources,
        string text,
        int x,
        int y,
        int advance,
        int gapWidth,
        Color color)
    {
        (string cleanText, int gapAfterIndex) = PrepareTextForDrawing(text);

        DrawGlyphs(graphics, rom, fontResources, cleanText, x + 1, y + 1, advance, gapWidth, gapAfterIndex, Color.Black);
        DrawGlyphs(graphics, rom, fontResources, cleanText, x, y, advance, gapWidth, gapAfterIndex, color);
    }

    private static void DrawGlyphs(
        Graphics graphics,
        ReadOnlySpan<byte> rom,
        RomFontResources fontResources,
        string text,
        int x,
        int y,
        int advance,
        int gapWidth,
        int gapAfterIndex,
        Color color)
    {
        int currentX = x;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] is >= 'A' and <= 'Z')
            {
                byte glyphValue = (byte)text[i];
                byte[] glyphBytes = RomFontService.ReadGlyph(rom, fontResources, glyphValue);
                using Bitmap tinted = CreateTintedGlyph(glyphBytes, color);
                graphics.DrawImage(
                    tinted,
                    ScaleRect(currentX, y, GlyphDrawSize, GlyphDrawSize, graphics),
                    new Rectangle(0, 0, GlyphSourceSize, GlyphSourceSize),
                    GraphicsUnit.Pixel);
            }

            currentX += advance;
            if (i == gapAfterIndex)
            {
                currentX += gapWidth;
            }
        }
    }

    private static Bitmap LoadBackground()
    {
        if (File.Exists(BackgroundPath))
        {
            using var source = new Bitmap(BackgroundPath);
            return source.Clone(new Rectangle(0, 0, source.Width, source.Height), PixelFormat.Format32bppArgb);
        }

        var fallback = new Bitmap(700, 525, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(fallback);
        graphics.Clear(Color.Black);
        return fallback;
    }

    private static Bitmap CreateTintedGlyph(ReadOnlySpan<byte> glyphBytes, Color color)
    {
        if (glyphBytes.Length != RomFontResources.GlyphByteSize)
        {
            throw new InvalidDataException($"ROM glyph payload must be exactly {RomFontResources.GlyphByteSize} bytes.");
        }

        var output = new Bitmap(GlyphSourceSize, GlyphSourceSize, PixelFormat.Format32bppArgb);
        Rectangle bounds = new(0, 0, output.Width, output.Height);
        BitmapData data = output.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        try
        {
            int stride = Math.Abs(data.Stride);
            byte[] pixels = new byte[stride * output.Height];

            for (int y = 0; y < output.Height; y++)
            {
                int row = data.Stride < 0 ? (output.Height - 1 - y) * stride : y * stride;
                for (int x = 0; x < output.Width; x++)
                {
                    int packedIndex = (y * output.Width + x) / 2;
                    int nibble = x % 2 == 0
                        ? glyphBytes[packedIndex] >> 4
                        : glyphBytes[packedIndex] & 0x0f;
                    byte alpha = (byte)((nibble << 4) | nibble);
                    int offset = row + x * 4;
                    pixels[offset] = color.B;
                    pixels[offset + 1] = color.G;
                    pixels[offset + 2] = color.R;
                    pixels[offset + 3] = alpha;
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        }
        finally
        {
            output.UnlockBits(data);
        }

        return output;
    }

    private static void DrawGuides(Graphics graphics, TitleTextLine noController, TitleTextLine pressStart, float scale)
    {
        DrawVerticalGuides(graphics, scale);
    }

    private static void DrawVerticalGuides(Graphics graphics, float scale)
    {
        int sideLines = GuideLineCount / 2;
        float width = Math.Max(2f, scale);

        for (int i = -sideLines; i <= sideLines; i++)
        {
            int x = GuideCenterX + i * GuideSpacing;
            Color color = i == 0
                ? Color.FromArgb(230, 255, 230, 40)
                : Color.FromArgb(220, 255, 60, 60);
            using var pen = new Pen(color, width);
            graphics.DrawLine(pen, ScaleX(x, scale), 0, ScaleX(x, scale), ScaleY(LogicalHeight, scale));
        }
    }

    private static (string CleanText, int GapAfterIndex) PrepareTextForDrawing(string text)
    {
        text = text.Trim().ToUpperInvariant();
        int gapIndex = text.IndexOf(' ', StringComparison.Ordinal);
        string cleanText = text.Replace(" ", string.Empty, StringComparison.Ordinal);
        int gapAfterIndex = gapIndex > 0 ? gapIndex - 1 : cleanText.Length - 1;
        return (cleanText, gapAfterIndex);
    }

    private static Rectangle ScaleRect(int x, int y, int width, int height, Graphics graphics)
    {
        float scale = graphics.VisibleClipBounds.Width / LogicalWidth;
        return ScaleRect(x, y, width, height, scale);
    }

    private static Rectangle ScaleRect(int x, int y, int width, int height, float scale)
    {
        return new Rectangle(
            ScaleX(x, scale),
            ScaleY(y, scale),
            Math.Max(1, (int)Math.Round(width * scale)),
            Math.Max(1, (int)Math.Round(height * scale)));
    }

    private static int ScaleX(int x, float scale) => (int)Math.Round(x * scale);

    private static int ScaleY(int y, float scale) => (int)Math.Round(y * scale);

    private static string CreateCacheKey(
        ReadOnlySpan<byte> rom,
        RomFontResources fontResources,
        TitleTextLine noController,
        TitleTextLine pressStart,
        bool showGuides)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(System.Text.Encoding.UTF8.GetBytes(CacheVersion));
        hash.AppendData(rom.Slice(
            fontResources.GlyphDataOffset,
            Math.Min(rom.Length - fontResources.GlyphDataOffset, fontResources.GlyphCount * RomFontResources.GlyphByteSize)));
        hash.AppendData(System.Text.Encoding.UTF8.GetBytes(noController.ToString()));
        hash.AppendData(System.Text.Encoding.UTF8.GetBytes(pressStart.ToString()));
        hash.AppendData([(byte)(showGuides ? 1 : 0)]);
        if (File.Exists(BackgroundPath))
        {
            hash.AppendData(File.ReadAllBytes(BackgroundPath));
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
