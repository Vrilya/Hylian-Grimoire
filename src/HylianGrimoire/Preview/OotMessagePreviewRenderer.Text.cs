using System.Drawing;
using HylianGrimoire.Glyphs;

namespace HylianGrimoire.Preview;

public static partial class OotMessagePreviewRenderer
{
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

    private static int GetChoiceCount(IReadOnlyList<OotPreviewToken> tokens)
    {
        return tokens.FirstOrDefault(token => token.Kind == OotPreviewTokenKind.Choice).Value;
    }

    private static bool ShouldIndentChoiceLine(int choiceCount, float y)
    {
        return choiceCount == 2 && y >= 32
            || choiceCount == 3 && y >= 20;
    }

    private static float GetStartY(IReadOnlyList<OotPreviewToken> tokens)
    {
        int lineBreaks = tokens.Count(token => token.Kind == OotPreviewTokenKind.LineBreak);
        return Math.Max(8, (52 - (12 * lineBreaks)) / 2.0f);
    }

    private static float GetGlyphAdvance(byte value, float scale, IGlyphSource glyphSource)
    {
        if (value == 0x20)
        {
            return 6.0f;
        }

        return (int)(glyphSource.GetAdvance(value) * scale);
    }
}
