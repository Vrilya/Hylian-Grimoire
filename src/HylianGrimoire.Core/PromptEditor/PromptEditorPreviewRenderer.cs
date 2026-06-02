using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using HylianGrimoire.Rom;

namespace HylianGrimoire.PromptEditor;

public static class PromptEditorPreviewRenderer
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

            bitmap.Save(path, ImageFormat.Png);
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

    private static PromptEditorSegment GetTextSegment(PromptEditorProfile profile, string languageKey) =>
        profile.TextSegments.TryGetValue(languageKey, out PromptEditorSegment? segment)
            ? segment
            : profile.TextSegments[profile.LanguageKeys[0]];

    private static Bitmap ReadAsset(
        ReadOnlySpan<byte> rom,
        PromptEditorSegment segment,
        PromptEditorAsset asset,
        PromptEditorSegmentCache segmentCache)
    {
        int length = GetEncodedLength(asset);
        Color color = asset.ColorDisplayListOffset is int dlistOffset
            ? ReadDisplayListPrimColor(rom, segment, dlistOffset, segmentCache)
            : asset.FallbackColor;
        ReadOnlySpan<byte> payload = segmentCache.Slice(rom, segment, asset.LocalOffset, length);
        return asset.Format switch
        {
            PromptEditorAssetFormat.Ia8 => Ia8ToBitmap(payload, asset.Width, asset.Height, asset.DrawWidth, color),
            PromptEditorAssetFormat.Ia4 => Ia4ToBitmap(payload, asset.Width, asset.Height, asset.DrawWidth, color),
            _ => throw new InvalidDataException($"Unsupported prompt asset format {asset.Format}."),
        };
    }

    private static Color ReadDisplayListPrimColor(
        ReadOnlySpan<byte> rom,
        PromptEditorSegment segment,
        int localOffset,
        PromptEditorSegmentCache segmentCache)
    {
        ReadOnlySpan<byte> source = segmentCache.SegmentSpan(rom, segment);
        int end = Math.Min(localOffset + 0x80, source.Length);
        for (int cursor = localOffset; cursor + 7 < end; cursor += 8)
        {
            if (source[cursor] == 0xfa)
            {
                return Color.FromArgb(source[cursor + 7], source[cursor + 4], source[cursor + 5], source[cursor + 6]);
            }
        }

        return Color.White;
    }

    private static Bitmap Ia8ToBitmap(ReadOnlySpan<byte> data, int width, int height, int drawWidth, Color color)
    {
        drawWidth = Math.Clamp(drawWidth, 1, width);
        var bitmap = new Bitmap(drawWidth, height, PixelFormat.Format32bppArgb);
        Rectangle bounds = new(0, 0, drawWidth, height);
        BitmapData bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            int stride = Math.Abs(bitmapData.Stride);
            byte[] pixels = new byte[stride * height];
            for (int y = 0; y < height; y++)
            {
                int row = bitmapData.Stride < 0 ? (height - 1 - y) * stride : y * stride;
                for (int x = 0; x < drawWidth; x++)
                {
                    byte value = data[y * width + x];
                    int intensity = (value >> 4) * 17;
                    int alpha = (value & 0x0f) * 17 * color.A / 255;
                    int offset = row + x * 4;
                    pixels[offset] = (byte)(color.B * intensity / 255);
                    pixels[offset + 1] = (byte)(color.G * intensity / 255);
                    pixels[offset + 2] = (byte)(color.R * intensity / 255);
                    pixels[offset + 3] = (byte)alpha;
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

    private static Bitmap ScaleBitmap(Bitmap source, float scale)
    {
        var bitmap = new Bitmap(Scale(source.Width, scale), Scale(source.Height, scale), PixelFormat.Format32bppArgb);
        Rectangle sourceBounds = new(0, 0, source.Width, source.Height);
        Rectangle targetBounds = new(0, 0, bitmap.Width, bitmap.Height);
        BitmapData sourceData = source.LockBits(sourceBounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        BitmapData targetData = bitmap.LockBits(targetBounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            int sourceStride = Math.Abs(sourceData.Stride);
            int targetStride = Math.Abs(targetData.Stride);
            byte[] sourcePixels = new byte[sourceStride * source.Height];
            byte[] targetPixels = new byte[targetStride * bitmap.Height];
            System.Runtime.InteropServices.Marshal.Copy(sourceData.Scan0, sourcePixels, 0, sourcePixels.Length);

            for (int y = 0; y < bitmap.Height; y++)
            {
                int sourceY = Math.Min(source.Height - 1, (int)(y / scale));
                int sourceRow = sourceData.Stride < 0 ? (source.Height - 1 - sourceY) * sourceStride : sourceY * sourceStride;
                int targetRow = targetData.Stride < 0 ? (bitmap.Height - 1 - y) * targetStride : y * targetStride;
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int sourceX = Math.Min(source.Width - 1, (int)(x / scale));
                    int sourceOffset = sourceRow + sourceX * 4;
                    int targetOffset = targetRow + x * 4;
                    targetPixels[targetOffset] = sourcePixels[sourceOffset];
                    targetPixels[targetOffset + 1] = sourcePixels[sourceOffset + 1];
                    targetPixels[targetOffset + 2] = sourcePixels[sourceOffset + 2];
                    targetPixels[targetOffset + 3] = sourcePixels[sourceOffset + 3];
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(targetPixels, 0, targetData.Scan0, targetPixels.Length);
        }
        finally
        {
            source.UnlockBits(sourceData);
            bitmap.UnlockBits(targetData);
        }

        return bitmap;
    }

    private static Bitmap Ia4ToBitmap(ReadOnlySpan<byte> data, int width, int height, int drawWidth, Color color)
    {
        drawWidth = Math.Clamp(drawWidth, 1, width);
        var bitmap = new Bitmap(drawWidth, height, PixelFormat.Format32bppArgb);
        Rectangle bounds = new(0, 0, drawWidth, height);
        BitmapData bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            int stride = Math.Abs(bitmapData.Stride);
            byte[] pixels = new byte[stride * height];
            for (int y = 0; y < height; y++)
            {
                int row = bitmapData.Stride < 0 ? (height - 1 - y) * stride : y * stride;
                for (int x = 0; x < drawWidth; x++)
                {
                    int pixelIndex = y * width + x;
                    byte packed = data[pixelIndex / 2];
                    int value = (pixelIndex & 1) == 0 ? packed >> 4 : packed & 0x0f;
                    int intensity = ((value >> 1) & 0x07) * 255 / 7;
                    int alpha = (value & 0x01) == 0 ? 0 : color.A;
                    int offset = row + x * 4;
                    pixels[offset] = (byte)(color.B * intensity / 255);
                    pixels[offset + 1] = (byte)(color.G * intensity / 255);
                    pixels[offset + 2] = (byte)(color.R * intensity / 255);
                    pixels[offset + 3] = (byte)alpha;
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

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

    private static string CreateCacheKey(
        ReadOnlySpan<byte> rom,
        PromptEditorProfile profile,
        string languageKey,
        IReadOnlyList<PromptEditorLine> lines,
        PromptEditorKind selectedKind,
        bool showGuides,
        bool showFrames)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(System.Text.Encoding.UTF8.GetBytes(CacheVersion));
        hash.AppendData(BitConverter.GetBytes(PreviewScaleWidth));
        hash.AppendData(BitConverter.GetBytes(PreviewPaddingX));
        hash.AppendData(BitConverter.GetBytes(PreviewPaddingY));
        hash.AppendData(BitConverter.GetBytes(PanelTopY));
        hash.AppendData(BitConverter.GetBytes(PanelGapY));
        hash.AppendData(BitConverter.GetBytes(PanelN64X));
        hash.AppendData(System.Text.Encoding.UTF8.GetBytes(profile.DisplayName));
        hash.AppendData(System.Text.Encoding.UTF8.GetBytes(languageKey));
        foreach (PromptEditorLine line in lines)
        {
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(line.ToString()));
        }

        hash.AppendData([(byte)selectedKind, (byte)(showGuides ? 1 : 0), (byte)(showFrames ? 1 : 0)]);

        var segmentCache = new PromptEditorSegmentCache();
        AppendAssetBytes(hash, rom, profile.IconSegment, profile.IconAssets.Values, segmentCache);
        foreach (string key in profile.LanguageKeys)
        {
            if (profile.TextAssets.TryGetValue(key, out IReadOnlyDictionary<string, PromptEditorAsset>? assets))
            {
                AppendAssetBytes(hash, rom, GetTextSegment(profile, key), assets.Values, segmentCache);
            }
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void AppendAssetBytes(
        IncrementalHash hash,
        ReadOnlySpan<byte> rom,
        PromptEditorSegment segment,
        IEnumerable<PromptEditorAsset> assets,
        PromptEditorSegmentCache segmentCache)
    {
        foreach (PromptEditorAsset asset in assets)
        {
            int length = GetEncodedLength(asset);
            hash.AppendData(segmentCache.Slice(rom, segment, asset.LocalOffset, length));

            if (asset.ColorDisplayListOffset is int colorOffset)
            {
                ReadOnlySpan<byte> source = segmentCache.SegmentSpan(rom, segment);
                int displayListLength = Math.Min(0x80, source.Length - colorOffset);
                if (colorOffset >= 0 && displayListLength > 0)
                {
                    hash.AppendData(source.Slice(colorOffset, displayListLength));
                }
            }
        }
    }

    private static int GetEncodedLength(PromptEditorAsset asset) =>
        asset.Format switch
        {
            PromptEditorAssetFormat.Ia8 => asset.Width * asset.Height,
            PromptEditorAssetFormat.Ia4 => (asset.Width * asset.Height + 1) / 2,
            _ => throw new InvalidDataException($"Unsupported prompt asset format {asset.Format}."),
        };

    private sealed class PromptEditorSegmentCache
    {
        private readonly Dictionary<PromptEditorSegment, byte[]> _decodedArchives = new();

        public ReadOnlySpan<byte> Slice(ReadOnlySpan<byte> rom, PromptEditorSegment segment, int localOffset, int length)
        {
            ReadOnlySpan<byte> source = SegmentSpan(rom, segment);
            if (localOffset < 0 || length < 0 || localOffset + length > source.Length)
            {
                throw new InvalidDataException(
                    $"Pause-prompt asset extends past {segment.Format} segment at 0x{segment.RomBase:x8}+0x{localOffset:x}.");
            }

            return source.Slice(localOffset, length);
        }

        public ReadOnlySpan<byte> SegmentSpan(ReadOnlySpan<byte> rom, PromptEditorSegment segment) =>
            segment.Format switch
            {
                PromptEditorSegmentFormat.Raw => RawSegmentSpan(rom, segment),
                PromptEditorSegmentFormat.CmpDmaArchive => GetDecodedArchive(rom, segment).AsSpan(),
                _ => throw new InvalidDataException($"Unsupported prompt segment format {segment.Format}."),
            };

        private static ReadOnlySpan<byte> RawSegmentSpan(ReadOnlySpan<byte> rom, PromptEditorSegment segment)
        {
            if (segment.RomBase < 0 || segment.RomBase > rom.Length)
            {
                throw new InvalidDataException($"Prompt segment starts outside the ROM at 0x{segment.RomBase:x8}.");
            }

            return rom.Slice(segment.RomBase);
        }

        private byte[] GetDecodedArchive(ReadOnlySpan<byte> rom, PromptEditorSegment segment)
        {
            if (_decodedArchives.TryGetValue(segment, out byte[]? decoded))
            {
                return decoded;
            }

            decoded = CmpDmaArchive.DecodeAll(rom, segment.RomBase);
            _decodedArchives.Add(segment, decoded);
            return decoded;
        }
    }
}
