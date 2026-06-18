using System.Security.Cryptography;
using HylianGrimoire.Rom;

namespace HylianGrimoire.TitleText;

public static partial class TitleTextPreviewRenderer
{
    private static string CreateCacheKey(
        ReadOnlySpan<byte> rom,
        TitleTextPatchProfile profile,
        RomFontResources fontResources,
        TitleTextLine? noController,
        TitleTextLine pressStart,
        bool showGuides,
        int languageIndex)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(System.Text.Encoding.UTF8.GetBytes(CacheVersion));
        hash.AppendData(rom.Slice(
            fontResources.GlyphDataOffset,
            Math.Min(rom.Length - fontResources.GlyphDataOffset, fontResources.GlyphCount * RomFontResources.GlyphByteSize)));
        hash.AppendData(System.Text.Encoding.UTF8.GetBytes(profile.DisplayName));
        hash.AppendData(System.Text.Encoding.UTF8.GetBytes(profile.BackgroundPath));
        if (noController is not null)
        {
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(noController.ToString()));
        }

        hash.AppendData(System.Text.Encoding.UTF8.GetBytes(pressStart.ToString()));
        hash.AppendData([(byte)(showGuides ? 1 : 0)]);
        hash.AppendData(BitConverter.GetBytes(languageIndex));
        if (File.Exists(profile.BackgroundPath))
        {
            hash.AppendData(File.ReadAllBytes(profile.BackgroundPath));
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
