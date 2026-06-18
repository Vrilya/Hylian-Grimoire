using System.Drawing;
using System.Drawing.Imaging;
using HylianGrimoire.Services;

namespace HylianGrimoire.PromptEditor;

public static partial class PromptEditorPreviewRenderer
{
    private const string CacheVersion = "prompt-editor-v12";
    private const int LogicalWidth = 320;
    private const int GuideCenterX = LogicalWidth / 2;
    private const int GuideSpacing = 24;
    private const int GuideLineCount = 7;
    private const int PreviewScaleWidth = 880;
    private const int PreviewPaddingX = 24;
    private const int PreviewPaddingY = 24;
    private const int PanelTopY = 54;
    private const int PanelGapY = 100;
    private const int PanelN64X = -72;
    private static readonly string CacheRoot = Path.Combine(Path.GetTempPath(), "HylianGrimoirePromptEditorPreview");

    public static Uri Render(
        ReadOnlySpan<byte> rom,
        PromptEditorProfile profile,
        string languageKey,
        IReadOnlyList<PromptEditorLine> lines,
        PromptEditorKind selectedKind,
        bool showGuides,
        bool showFrames)
    {
        Directory.CreateDirectory(CacheRoot);
        string key = CreateCacheKey(rom, profile, languageKey, lines, selectedKind, showGuides, showFrames);
        string path = Path.Combine(CacheRoot, $"{key}.png");
        if (File.Exists(path))
        {
            return new Uri(path);
        }

        float scale = GetScale();
        PreviewLayout layout = BuildLayout(profile, languageKey, lines);
        var segmentCache = new PromptEditorSegmentCache();
        using Bitmap panelLeft = ReadAsset(rom, profile.IconSegment, profile.IconAssets["PanelLeft"], segmentCache);
        using Bitmap panelRight = ReadAsset(rom, profile.IconSegment, profile.IconAssets["PanelRight"], segmentCache);
        using Bitmap scaledPanelLeft = ScaleBitmap(panelLeft, scale);
        using Bitmap scaledPanelRight = ScaleBitmap(panelRight, scale);

        var rows = new List<PreviewRow>();
        try
        {
            for (int i = 0; i < lines.Count; i++)
            {
                PromptEditorLine line = lines[i];
                PreviewLayoutRow layoutRow = layout.Rows[i];

                using Bitmap icon = ReadAsset(rom, profile.IconSegment, profile.IconAssets[line.IconKey], segmentCache);
                using Bitmap text = ReadAsset(rom, GetTextSegment(profile, languageKey), profile.TextAssets[languageKey][line.TextKey], segmentCache);

                Bitmap scaledIcon = ScaleBitmap(icon, scale);
                Bitmap scaledText = ScaleBitmap(text, scale);
                var row = new PreviewRow(line.Kind, scaledIcon, scaledText, layoutRow.PanelRect, layoutRow.IconRect, layoutRow.TextRect);
                rows.Add(row);
            }

            int outputWidth = Math.Max(1, layout.ContentBounds.Width + PreviewPaddingX * 2);
            int outputHeight = Math.Max(1, layout.ContentBounds.Height + PreviewPaddingY * 2);

            using var bitmap = new Bitmap(outputWidth, outputHeight, PixelFormat.Format32bppArgb);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.FromArgb(5, 5, 5));
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            foreach (PreviewRow row in rows)
            {
                Rectangle panelRect = Offset(row.PanelRect, layout.OriginX, layout.OriginY);
                Rectangle iconRect = Offset(row.IconRect, layout.OriginX, layout.OriginY);
                Rectangle textRect = Offset(row.TextRect, layout.OriginX, layout.OriginY);

                graphics.DrawImageUnscaled(scaledPanelLeft, panelRect.Left, panelRect.Top);
                graphics.DrawImageUnscaled(scaledPanelRight, panelRect.Left + scaledPanelLeft.Width, panelRect.Top);
                graphics.DrawImageUnscaled(row.Icon, iconRect.Left, iconRect.Top);
                graphics.DrawImageUnscaled(row.Text, textRect.Left, textRect.Top);

                if (showFrames)
                {
                    using var borderPen = new Pen(row.Kind == selectedKind ? Color.White : Color.FromArgb(55, 55, 55));
                    using var textPen = new Pen(Color.FromArgb(100, 255, 255));
                    using var iconPen = new Pen(Color.FromArgb(255, 255, 100));
                    graphics.DrawRectangle(borderPen, panelRect);
                    graphics.DrawRectangle(textPen, textRect);
                    graphics.DrawRectangle(iconPen, iconRect);
                }
            }

            if (showGuides)
            {
                DrawGuides(graphics, scale, layout.OriginX);
            }

            PngFileWriter.SaveDirect(bitmap, path);
        }
        finally
        {
            foreach (PreviewRow row in rows)
            {
                row.Dispose();
            }
        }

        return new Uri(path);
    }
}
