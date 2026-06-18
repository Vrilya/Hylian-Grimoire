using System.Drawing;

namespace HylianGrimoire.PromptEditor;

public static partial class PromptEditorPreviewRenderer
{
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

    private static int GetEncodedLength(PromptEditorAsset asset) =>
        asset.Format switch
        {
            PromptEditorAssetFormat.Ia8 => asset.Width * asset.Height,
            PromptEditorAssetFormat.Ia4 => (asset.Width * asset.Height + 1) / 2,
            _ => throw new InvalidDataException($"Unsupported prompt asset format {asset.Format}."),
        };
}
