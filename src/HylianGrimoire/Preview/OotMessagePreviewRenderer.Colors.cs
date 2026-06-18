using System.Drawing;

namespace HylianGrimoire.Preview;

public static partial class OotMessagePreviewRenderer
{
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
}
