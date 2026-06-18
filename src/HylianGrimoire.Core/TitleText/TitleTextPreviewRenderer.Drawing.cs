using System.Drawing;
using HylianGrimoire.Rom;

namespace HylianGrimoire.TitleText;

public static partial class TitleTextPreviewRenderer
{
    private static void DrawLine(
        Graphics graphics,
        ReadOnlySpan<byte> rom,
        RomFontResources fontResources,
        TitleTextLineProfile profile,
        TitleTextLine line)
    {
        DrawLine(
            graphics,
            rom,
            fontResources,
            line.Text,
            line.X,
            profile.PreviewY,
            profile.PreviewAdvance,
            profile.PreviewGapWidth,
            Color.FromArgb(profile.PreviewColorArgb));
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

    private static void DrawLocalizedLine(
        Graphics graphics,
        ReadOnlySpan<byte> rom,
        RomFontResources fontResources,
        IReadOnlyList<TitleTextPreviewGlyph> glyphs,
        int x,
        int y,
        Color color)
    {
        DrawLocalizedGlyphs(graphics, rom, fontResources, glyphs, x + 1, y + 1, Color.Black);
        DrawLocalizedGlyphs(graphics, rom, fontResources, glyphs, x, y, color);
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

    private static void DrawLocalizedGlyphs(
        Graphics graphics,
        ReadOnlySpan<byte> rom,
        RomFontResources fontResources,
        IReadOnlyList<TitleTextPreviewGlyph> glyphs,
        int x,
        int y,
        Color color)
    {
        int currentX = x;
        foreach (TitleTextPreviewGlyph glyph in glyphs)
        {
            byte[] glyphBytes = RomFontService.ReadGlyph(rom, fontResources, glyph.GlyphValue);
            using Bitmap tinted = CreateTintedGlyph(glyphBytes, color);
            graphics.DrawImage(
                tinted,
                ScaleRect(currentX, y, GlyphDrawSize, GlyphDrawSize, graphics),
                new Rectangle(0, 0, GlyphSourceSize, GlyphSourceSize),
                GraphicsUnit.Pixel);
            currentX += glyph.Advance;
        }
    }
}
