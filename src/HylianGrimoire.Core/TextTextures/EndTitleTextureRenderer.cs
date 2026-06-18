using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class EndTitleTextureRenderer
{
    public static Bitmap Render(string text, string fontPath, EndTitleTextureRenderSettings settings, int width, int height)
        => Render(text, TextTextureFont.FromPath(fontPath), settings, width, height);

    public static Bitmap Render(string text, TextTextureFont font, EndTitleTextureRenderSettings settings, int width, int height)
    {
        if (!File.Exists(font.Path))
        {
            throw new FileNotFoundException("End-title texture font is missing.", font.Path);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        EndTitleTextParts parts = EndTitleTextParts.Parse(text);
        return Render(parts.Prefix, parts.Title, parts.Tm, parts.Suffix, font, EndTitleTextureCatalog.Specs[0], settings, width, height);
    }

    public static Bitmap Render(
        string prefix,
        string title,
        string tm,
        string suffix,
        string fontPath,
        EndTitleTextureSpec spec,
        EndTitleTextureRenderSettings settings,
        int width,
        int height,
        EndTitleTextureAssets? assets = null)
        => Render(prefix, title, tm, suffix, TextTextureFont.FromPath(fontPath), spec, settings, width, height, assets);

    public static Bitmap Render(
        string prefix,
        string title,
        string tm,
        string suffix,
        TextTextureFont font,
        EndTitleTextureSpec spec,
        EndTitleTextureRenderSettings settings,
        int width,
        int height,
        EndTitleTextureAssets? assets = null)
    {
        if (!File.Exists(font.Path))
        {
            throw new FileNotFoundException("End-title texture font is missing.", font.Path);
        }

        if (string.IsNullOrWhiteSpace(prefix)
            && string.IsNullOrWhiteSpace(title)
            && string.IsNullOrWhiteSpace(tm)
            && string.IsNullOrWhiteSpace(suffix))
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        if (spec.Style == EndTitleTextureStyle.PresentedBy)
        {
            return RenderPresentedBy(title, font, settings, width, height);
        }

        if (spec.Style == EndTitleTextureStyle.LegendOfZelda)
        {
            return RenderLegendOfZelda(title, font, assets?.LegendRegisteredPath, settings, width, height);
        }

        if (spec.Style == EndTitleTextureStyle.TheEnd)
        {
            return RenderTheEnd(title, font, settings, width, height);
        }

        return RenderOcarinaOfTime(
            new EndTitleTextParts(prefix, title, tm, suffix),
            font,
            settings,
            width,
            height,
            assets);
    }
}
