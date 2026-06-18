using System.Drawing;
using HylianGrimoire.Glyphs;

namespace HylianGrimoire.Preview;

public static partial class MmMessagePreviewRenderer
{
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

        string marker = Assets.Resolve(lastBox
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

    private static float GetStartY(MmPreviewStyle style, IReadOnlyList<OotPreviewToken> tokens)
        => MmPreviewLayout.GetStartY(style, tokens);

    private static float GetGlyphAdvance(byte value, IGlyphSource glyphSource, float scale)
    {
        if (value == 0x20)
        {
            return 6.0f;
        }

        return (int)(glyphSource.GetAdvance(value) * scale);
    }
}
