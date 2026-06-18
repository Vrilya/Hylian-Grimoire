using System.Security.Cryptography;
using HylianGrimoire.Rom;

namespace HylianGrimoire.PromptEditor;

public static partial class PromptEditorPreviewRenderer
{
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
