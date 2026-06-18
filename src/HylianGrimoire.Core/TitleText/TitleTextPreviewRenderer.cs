using System.Drawing;
using HylianGrimoire.Rom;
using HylianGrimoire.Services;

namespace HylianGrimoire.TitleText;

public static partial class TitleTextPreviewRenderer
{
    private const string CacheVersion = "title-text-preview-v6";
    private const int LogicalWidth = 320;
    private const int LogicalHeight = 240;
    private const int GlyphSourceSize = 16;
    private const int GlyphDrawSize = 10;
    private const int GuideCenterX = LogicalWidth / 2;
    private const int GuideSpacing = 24;
    private const int GuideLineCount = 7;
    private static readonly string CacheRoot = Path.Combine(Path.GetTempPath(), "HylianGrimoireTitleTextPreview");

    public static Uri Render(
        ReadOnlySpan<byte> rom,
        TitleTextPatchProfile profile,
        RomFontResources fontResources,
        TitleTextLine? noController,
        TitleTextLine pressStart,
        bool showGuides,
        int languageIndex = 0)
    {
        Directory.CreateDirectory(CacheRoot);
        string key = CreateCacheKey(rom, profile, fontResources, noController, pressStart, showGuides, languageIndex);
        string path = Path.Combine(CacheRoot, $"{key}.png");
        if (File.Exists(path))
        {
            return new Uri(path);
        }

        using Bitmap bitmap = LoadBackground(profile.BackgroundPath);
        float scale = bitmap.Width / (float)LogicalWidth;
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        if (showGuides)
        {
            DrawGuides(graphics, scale);
        }

        if (profile.NoController is not null && noController is not null)
        {
            DrawLine(graphics, rom, fontResources, profile.NoController, noController);
        }

        IReadOnlyList<TitleTextPreviewGlyph>? localizedPressStart =
            TitleTextService.GetLocalizedPreviewGlyphs(profile, pressStart, languageIndex);
        if (localizedPressStart is null)
        {
            DrawLine(graphics, rom, fontResources, profile.PressStart, pressStart);
        }
        else
        {
            TitleTextLocalizedLineProfile localizedProfile = profile.LocalizedPressStarts[
                Math.Clamp(languageIndex, 0, profile.LocalizedPressStarts.Count - 1)];
            DrawLocalizedLine(
                graphics,
                rom,
                fontResources,
                localizedPressStart,
                pressStart.X,
                localizedProfile.PreviewY,
                Color.FromArgb(localizedProfile.PreviewColorArgb));
        }

        PngFileWriter.SaveDirect(bitmap, path);
        return new Uri(path);
    }
}
