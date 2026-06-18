using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.Preview;

public static partial class MmMessagePreviewRenderer
{
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
            MmPreviewStyle.Wooden => PreviewBitmapTransforms.ColorizeMultiply(box, Color.FromArgb(230, 70, 50, 30)),
            MmPreviewStyle.Ocarina => PreviewBitmapTransforms.ColorizeMultiply(box, Color.FromArgb(180, 255, 0, 0)),
            MmPreviewStyle.Blue or MmPreviewStyle.BlueDefault => PreviewBitmapTransforms.ColorizeAlpha(box, Color.FromArgb(170, 0, 10, 50)),
            MmPreviewStyle.Notebook => PreviewBitmapTransforms.ColorizeAlpha(box, Color.FromArgb(220, 255, 255, 195)),
            _ => PreviewBitmapTransforms.ColorizeAlpha(box, Color.FromArgb(170, 0, 0, 0)),
        };

        graphics.DrawImage(styledBox, 0, 0, 256, 64);

        if (style == MmPreviewStyle.Ocarina)
        {
            DrawOcarinaTrebleClef(graphics);
        }
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

    private static void DrawOcarinaTrebleClef(Graphics graphics)
    {
        string clef = Assets.Resolve(@"parameter_static\gOcarinaTrebleClefTex.png");
        if (!File.Exists(clef))
        {
            return;
        }

        // The game draws the clef at screen coordinates (78,166) while the ocarina box starts at (34,142).
        DrawMaskImage(graphics, clef, Color.FromArgb(255, 100, 0), 44, 24, 16, 32, brighten: false);
    }

    private static void DrawOcarinaBackgroundX(Graphics graphics)
    {
        string left = Assets.Resolve(@"message_texture_static\gMessageXLeftTex.png");
        string right = Assets.Resolve(@"message_texture_static\gMessageXRightTex.png");
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

        return Assets.Resolve(relativePath);
    }
}
