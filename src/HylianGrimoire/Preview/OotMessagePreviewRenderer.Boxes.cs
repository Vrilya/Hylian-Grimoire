using System.Drawing;
using System.Drawing.Imaging;
using HylianGrimoire.Services;

namespace HylianGrimoire.Preview;

public static partial class OotMessagePreviewRenderer
{
    private static Uri GetMessageBox(OotPreviewStyle style)
    {
        string source = style switch
        {
            OotPreviewStyle.Wooden => Assets.Resolve(@"message_static\gSignMessageBackgroundTex.png"),
            OotPreviewStyle.Ocarina => Assets.Resolve(@"message_static\gNoteStaffMessageBackgroundTex.png"),
            OotPreviewStyle.Black => Assets.Resolve(@"message_static\gDefaultMessageBackgroundTex.png"),
            OotPreviewStyle.Blue => Assets.Resolve(@"message_static\gFadingMessageBackgroundTex.png"),
            _ => Assets.Resolve(@"message_static\gFadingMessageBackgroundTex.png"),
        };

        string key = $"box-v6-mask-alpha-{style}-{source}";
        string output = Cache.GetPath(key);
        if (File.Exists(output))
        {
            return new Uri(output);
        }

        Cache.EnsureDirectory();

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
            OotPreviewStyle.Wooden => PreviewBitmapTransforms.ColorizeMultiply(box, Color.FromArgb(230, 70, 50, 30)),
            OotPreviewStyle.Ocarina => PreviewBitmapTransforms.ColorizeMultiply(box, Color.FromArgb(180, 255, 0, 0)),
            OotPreviewStyle.Blue => PreviewBitmapTransforms.ColorizeAlpha(box, Color.FromArgb(170, 0, 10, 50)),
            OotPreviewStyle.Black => PreviewBitmapTransforms.ColorizeAlpha(box, Color.FromArgb(170, 0, 0, 0)),
            _ => new Bitmap(box),
        };

        PngFileWriter.SaveDirect(tinted, output);
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

        using var box = new Bitmap(GetMessageBox(style).LocalPath);
        graphics.DrawImage(box, 0, 0, 256, 64);
    }

    private static bool IsNoneBoxStyle(OotPreviewStyle style)
    {
        return style is OotPreviewStyle.None or OotPreviewStyle.NoneDarkText;
    }
}
