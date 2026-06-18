using System.Drawing;

namespace HylianGrimoire.PromptEditor;

public static partial class PromptEditorPreviewRenderer
{
    private static PreviewLayout BuildLayout(
        PromptEditorProfile profile,
        string languageKey,
        IReadOnlyList<PromptEditorLine> lines)
    {
        float scale = GetScale();
        int panelX = N64ToPixel(PanelN64X);
        int panelWidth = Scale(profile.IconAssets["PanelLeft"].Width + profile.IconAssets["PanelRight"].Width, scale);
        int panelHeight = Scale(profile.IconAssets["PanelLeft"].Height, scale);
        var rows = new List<PreviewLayoutRow>(lines.Count);
        Rectangle contentBounds = Rectangle.Empty;

        for (int i = 0; i < lines.Count; i++)
        {
            PromptEditorLine line = lines[i];
            PromptEditorAsset iconAsset = profile.IconAssets[line.IconKey];
            PromptEditorAsset textAsset = profile.TextAssets[languageKey][line.TextKey];
            int panelY = PanelTopY + i * PanelGapY;
            int y = panelY + Scale(4, scale);
            Rectangle panelRect = new(panelX, panelY, panelWidth, panelHeight);
            Rectangle iconRect = new(N64ToPixel(line.IconX), y, Scale(iconAsset.Width, scale), Scale(iconAsset.Height, scale));
            Rectangle textRect = new(N64ToPixel(line.TextX), y, Scale(textAsset.DrawWidth, scale), Scale(textAsset.Height, scale));
            rows.Add(new PreviewLayoutRow(line.Kind, panelRect, iconRect, textRect));

            contentBounds = contentBounds.IsEmpty ? panelRect : Rectangle.Union(contentBounds, panelRect);
        }

        int originX = PreviewPaddingX - contentBounds.Left;
        int originY = PreviewPaddingY - contentBounds.Top;
        return new PreviewLayout(rows, contentBounds, originX, originY);
    }

    private static float GetScale() => PreviewScaleWidth / (float)LogicalWidth;

    private static int N64ToPixel(int x) =>
        (int)Math.Round((x + 160) * GetScale());

    private static int Scale(int value, float scale) =>
        Math.Max(1, (int)Math.Round(value * scale));

    private static Rectangle Offset(Rectangle rectangle, int x, int y) =>
        new(rectangle.Left + x, rectangle.Top + y, rectangle.Width, rectangle.Height);

    private static void DrawGuides(Graphics graphics, float scale, int originX)
    {
        int sideLines = GuideLineCount / 2;
        float width = Math.Max(2f, scale);

        for (int i = -sideLines; i <= sideLines; i++)
        {
            int x = GuideCenterX + i * GuideSpacing;
            Color color = i == 0
                ? Color.FromArgb(230, 255, 230, 40)
                : Color.FromArgb(220, 255, 60, 60);
            using var pen = new Pen(color, width);
            float canvasX = N64ToPixel(x - 160) + originX;
            graphics.DrawLine(pen, canvasX, 0, canvasX, graphics.VisibleClipBounds.Height);
        }
    }

    private sealed record PreviewRow(
        PromptEditorKind Kind,
        Bitmap Icon,
        Bitmap Text,
        Rectangle PanelRect,
        Rectangle IconRect,
        Rectangle TextRect) : IDisposable
    {
        public void Dispose()
        {
            Icon.Dispose();
            Text.Dispose();
        }
    }

    private sealed record PreviewLayout(
        IReadOnlyList<PreviewLayoutRow> Rows,
        Rectangle ContentBounds,
        int OriginX,
        int OriginY);

    private sealed record PreviewLayoutRow(
        PromptEditorKind Kind,
        Rectangle PanelRect,
        Rectangle IconRect,
        Rectangle TextRect);
}
