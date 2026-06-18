using System.Drawing;

namespace HylianGrimoire.Preview;

public static partial class MmMessagePreviewRenderer
{
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
}
