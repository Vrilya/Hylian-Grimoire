using System.Drawing;
using HylianGrimoire.Games.MajorasMask;

namespace HylianGrimoire.Preview;

public static partial class MmMessagePreviewRenderer
{
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
        string path = Assets.Resolve(relativePath);
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
        string path = Assets.Resolve(relativePath);
        if (!File.Exists(path))
        {
            return false;
        }

        DrawMaskImage(graphics, path, color, 16, 20, 16, 16, brighten: false);
        return true;
    }

    private static bool DrawStrayFairyIcon(Graphics graphics, string relativePath)
    {
        string fairyPath = Assets.Resolve(relativePath);
        if (!File.Exists(fairyPath))
        {
            return false;
        }

        string glowPath = Assets.Resolve(@"parameter_static\gStrayFairyGlowingCircleIconTex.png");
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
}
