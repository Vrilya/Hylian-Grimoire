using System.Drawing;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Rom;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class RomGlyphEditorSessionTests
{
    [Fact]
    public void ApplyCharacterProfileResetsGlyphsAndWidthsNotPresentInProfile()
    {
        const byte profiledGlyph = 0x9e;
        const byte staleGlyph = 0x9d;
        const double profiledWidth = 17.0;
        const double staleWidth = 19.0;
        const RomFontBaseline baseline = RomFontBaseline.MajorasMask;

        (byte[] rom, RomFontResources resources) = CreateMmFontRom(baseline);
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        store.SetGameKind(GameKind.MajorasMask);
        string profileName = $"ROM Apply {Guid.NewGuid():N}";
        string profiledImagePath = CreateTempGlyphImage(Color.White);
        string staleImagePath = CreateTempGlyphImage(Color.Black);

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetWidth(profiledGlyph, profiledWidth, RomFontBaselineMetrics.GetDefaultAdvance(baseline, profiledGlyph));
            store.SetImage(profiledGlyph, profiledImagePath);
            CharacterProfileSnapshot snapshot = store.CreateSnapshot();

            var session = new RomGlyphEditorSession(rom, resources, baseline, GameKind.MajorasMask);
            session.SetWidth(staleGlyph, staleWidth);
            session.SetImage(staleGlyph, staleImagePath);

            Assert.NotEqual(
                GameGlyphCatalog.GetOriginalGlyphBytes(GameKind.MajorasMask, staleGlyph, baseline),
                RomFontService.ReadGlyph(rom, resources, staleGlyph));
            Assert.Equal((float)staleWidth, RomFontService.ReadWidth(rom, resources, staleGlyph));

            session.ApplyCharacterProfile(snapshot);

            Assert.Equal((float)profiledWidth, RomFontService.ReadWidth(rom, resources, profiledGlyph));
            Assert.Equal(
                RomGlyphCodec.EncodeI4Glyph(profiledImagePath),
                RomFontService.ReadGlyph(rom, resources, profiledGlyph));
            Assert.Equal(
                (float)RomFontBaselineMetrics.GetDefaultAdvance(baseline, staleGlyph),
                RomFontService.ReadWidth(rom, resources, staleGlyph));
            Assert.Equal(
                GameGlyphCatalog.GetOriginalGlyphBytes(GameKind.MajorasMask, staleGlyph, baseline),
                RomFontService.ReadGlyph(rom, resources, staleGlyph));
        }
        finally
        {
            store.DeleteSelectedProfile();
            DeleteIfExists(profiledImagePath);
            DeleteIfExists(staleImagePath);
        }
    }

    private static (byte[] Rom, RomFontResources Resources) CreateMmFontRom(RomFontBaseline baseline)
    {
        int glyphCount = 0x4e00 / RomFontResources.GlyphByteSize;
        int glyphBytes = glyphCount * RomFontResources.GlyphByteSize;
        int widthOffset = glyphBytes;
        byte[] rom = new byte[widthOffset + (MmGlyphMetrics.DefaultWidths.Length * sizeof(float))];
        var resources = new RomFontResources(
            GlyphDataOffset: 0,
            GlyphCount: glyphCount,
            WidthTableOffset: widthOffset,
            WidthCount: MmGlyphMetrics.DefaultWidths.Length);

        foreach (byte value in GameGlyphCatalog.GetGlyphValues(GameKind.MajorasMask))
        {
            RomFontService.WriteGlyph(
                rom,
                resources,
                value,
                GameGlyphCatalog.GetOriginalGlyphBytes(GameKind.MajorasMask, value, baseline));
            RomFontService.WriteWidth(
                rom,
                resources,
                value,
                (float)RomFontBaselineMetrics.GetDefaultAdvance(baseline, value));
        }

        return (rom, resources);
    }

    private static string CreateTempGlyphImage(Color color)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        using var bitmap = new Bitmap(RomGlyphCodec.GlyphWidth, RomGlyphCodec.GlyphHeight);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        bitmap.Save(path);
        return path;
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
