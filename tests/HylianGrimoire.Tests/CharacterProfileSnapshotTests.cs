using System.Drawing;
using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Headers;
using HylianGrimoire.Headers.MajorasMask;
using HylianGrimoire.Models;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class CharacterProfileSnapshotTests
{
    [Fact]
    public void SnapshotMatchesSelectedProfileMappings()
    {
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        store.SetGameKind(GameKind.OcarinaOfTime);
        string profileName = $"Snapshot {Guid.NewGuid():N}";
        string sourcePath = CreateTempGlyphImage();

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetDisplayChar(0x92, 'å');
            store.SetWidth(0x81, 12.0, defaultWidth: 6.0);
            store.SetImage(0x82, sourcePath);

            CharacterProfileSnapshot snapshot = store.CreateSnapshot();

            Assert.Equal(GameKind.OcarinaOfTime, snapshot.GameKind);
            Assert.Equal(profileName, snapshot.ProfileName);
            Assert.Equal(store.Version, snapshot.Version);

            Assert.True(store.TryGetDisplayChar(0x92, out char storeChar));
            Assert.True(snapshot.TryGetDisplayChar(0x92, out char snapshotChar));
            Assert.Equal(storeChar, snapshotChar);

            Assert.True(store.TryGetByte('å', out byte storeByte));
            Assert.True(snapshot.TryGetByte('å', out byte snapshotByte));
            Assert.Equal(storeByte, snapshotByte);

            Assert.True(store.TryGetWidth(0x81, out double storeWidth));
            Assert.True(snapshot.TryGetWidth(0x81, out double snapshotWidth));
            Assert.Equal(storeWidth, snapshotWidth);

            Assert.True(store.TryGetImagePath(0x82, out string? storeImagePath));
            Assert.True(snapshot.TryGetImagePath(0x82, out string? snapshotImagePath));
            Assert.Equal(storeImagePath, snapshotImagePath);
        }
        finally
        {
            store.ResetDisplayChar(0x92);
            store.ResetWidth(0x81);
            store.ResetImage(0x82);
            store.DeleteSelectedProfile();
            DeleteIfExists(sourcePath);
        }
    }

    [Fact]
    public void SnapshotRemainsStableAfterSelectedProfileChanges()
    {
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        store.SetGameKind(GameKind.OcarinaOfTime);
        string profileName = $"Stable snapshot {Guid.NewGuid():N}";

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetDisplayChar(0x92, 'å');
            CharacterProfileSnapshot snapshot = store.CreateSnapshot();

            store.SetDisplayChar(0x92, 'ø');

            Assert.True(snapshot.TryGetDisplayChar(0x92, out char snapshotChar));
            Assert.Equal('å', snapshotChar);
            Assert.True(snapshot.TryGetByte('å', out byte snapshotByte));
            Assert.Equal(0x92, snapshotByte);
            Assert.False(snapshot.TryGetByte('ø', out _));

            Assert.True(store.TryGetDisplayChar(0x92, out char storeChar));
            Assert.Equal('ø', storeChar);
        }
        finally
        {
            store.ResetDisplayChar(0x92);
            store.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void OotSnapshotGlyphSourceRemainsStableAfterProfileChanges()
    {
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        store.SetGameKind(GameKind.OcarinaOfTime);
        string profileName = $"OoT source {Guid.NewGuid():N}";

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetWidth(0x81, 12.0, defaultWidth: 6.0);
            IGlyphSource glyphSource = OotGlyphSources.FromSnapshot(store.CreateSnapshot());
            string cacheKey = glyphSource.CacheKey;

            store.SetWidth(0x81, 14.0, defaultWidth: 6.0);

            Assert.Equal(cacheKey, glyphSource.CacheKey);
            Assert.Equal(12.0, glyphSource.GetAdvance(0x81));
            Assert.Equal(14.0, OotGlyphSources.FromSnapshot(store.CreateSnapshot()).GetAdvance(0x81));
        }
        finally
        {
            store.ResetWidth(0x81);
            store.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void MmSnapshotGlyphSourceRemainsStableAfterProfileChanges()
    {
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        store.SetGameKind(GameKind.MajorasMask);
        string profileName = $"MM source {Guid.NewGuid():N}";

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetWidth(0x9e, 11.0, defaultWidth: 7.0);
            IGlyphSource glyphSource = MmGlyphSources.FromSnapshot(store.CreateSnapshot());
            string cacheKey = glyphSource.CacheKey;

            store.SetWidth(0x9e, 13.0, defaultWidth: 7.0);

            Assert.Equal(cacheKey, glyphSource.CacheKey);
            Assert.Equal(11.0, glyphSource.GetAdvance(0x9e));
            Assert.Equal(13.0, MmGlyphSources.FromSnapshot(store.CreateSnapshot()).GetAdvance(0x9e));
        }
        finally
        {
            store.ResetWidth(0x9e);
            store.DeleteSelectedProfile();
            store.SetGameKind(GameKind.OcarinaOfTime);
        }
    }

    [Fact]
    public void GameGlyphCatalogUsesExplicitSnapshot()
    {
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        store.SetGameKind(GameKind.OcarinaOfTime);
        string profileName = $"Glyph catalog {Guid.NewGuid():N}";
        const byte value = 0x92;
        double defaultWidth = GameGlyphCatalog.GetDefaultAdvance(GameKind.OcarinaOfTime, value);

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetDisplayChar(value, 'Q');
            store.SetWidth(value, defaultWidth + 2.0, defaultWidth);
            CharacterProfileSnapshot snapshot = store.CreateSnapshot();

            store.SetDisplayChar(value, 'R');
            store.SetWidth(value, defaultWidth + 4.0, defaultWidth);

            GlyphInfo info = GameGlyphCatalog.GetGlyphInfo(GameKind.OcarinaOfTime, value, snapshot);

            Assert.Equal('Q', info.CurrentChar);
            Assert.Equal(defaultWidth + 2.0, info.CurrentWidth);
            Assert.True(info.HasDisplayOverride);
            Assert.True(info.HasWidthOverride);
        }
        finally
        {
            store.ResetDisplayChar(value);
            store.ResetWidth(value);
            store.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void GameGlyphCatalogRejectsSnapshotFromDifferentGame()
    {
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        store.SetGameKind(GameKind.MajorasMask);
        try
        {
            CharacterProfileSnapshot snapshot = store.CreateSnapshot();

            Assert.Throws<InvalidOperationException>(
                () => GameGlyphCatalog.GetGlyphInfo(GameKind.OcarinaOfTime, 0x92, snapshot));
        }
        finally
        {
            store.SetGameKind(GameKind.OcarinaOfTime);
        }
    }

    [Fact]
    public void EncodingProfileSnapshotRemainsStableAfterProfileChanges()
    {
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        store.SetGameKind(GameKind.OcarinaOfTime);
        string profileName = $"Encoding snapshot {Guid.NewGuid():N}";

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetDisplayChar(0x92, 'å');
            MessageEncodingProfile encodingProfile = MessageEncodingProfile.Default.WithCharacterProfileSnapshot(store.CreateSnapshot());

            store.SetDisplayChar(0x92, 'ø');

            Assert.True(encodingProfile.TryGetEditorChar(0x92, out char snapshotChar));
            Assert.Equal('å', snapshotChar);
            Assert.True(encodingProfile.TryGetByte('å', out byte snapshotByte));
            Assert.Equal(0x92, snapshotByte);
            Assert.False(encodingProfile.TryGetByte('ø', out _));

            MessageEncodingProfile updatedEncodingProfile = MessageEncodingProfile.Default.WithCharacterProfileSnapshot(store.CreateSnapshot());
            Assert.True(updatedEncodingProfile.TryGetEditorChar(0x92, out char updatedSnapshotChar));
            Assert.Equal('ø', updatedSnapshotChar);
        }
        finally
        {
            store.ResetDisplayChar(0x92);
            store.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void OriginalEncodingProfileIgnoresCharacterProfileSnapshot()
    {
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        store.SetGameKind(GameKind.OcarinaOfTime);
        string profileName = $"Original encoding {Guid.NewGuid():N}";

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetDisplayChar(0x92, 'å');
            MessageEncodingProfile encodingProfile = MessageEncodingProfile.Original.WithCharacterProfileSnapshot(store.CreateSnapshot());

            Assert.Same(MessageEncodingProfile.Original, encodingProfile);
            Assert.True(encodingProfile.TryGetEditorChar(0x92, out char originalChar));
            Assert.Equal('â', originalChar);
            Assert.False(encodingProfile.TryGetByte('å', out _));
        }
        finally
        {
            store.ResetDisplayChar(0x92);
            store.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void EncodingProfileRejectsSnapshotFromDifferentGame()
    {
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        store.SetGameKind(GameKind.MajorasMask);
        try
        {
            CharacterProfileSnapshot snapshot = store.CreateSnapshot();

            Assert.Throws<InvalidOperationException>(
                () => MessageEncodingProfile.Default.WithCharacterProfileSnapshot(snapshot));
        }
        finally
        {
            store.SetGameKind(GameKind.OcarinaOfTime);
        }
    }

    [Fact]
    public void SnapshotForInactiveGameUsesThatGamesAutomaticProfile()
    {
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        store.SetGameKind(GameKind.MajorasMask);
        string mmProfileName = $"MM inactive {Guid.NewGuid():N}";

        Assert.True(store.CreateProfile(mmProfileName));
        try
        {
            store.SetDisplayChar(0x9e, 'å');
            store.SetAutomaticProfile(mmProfileName);

            store.SetGameKind(GameKind.OcarinaOfTime);
            string ootProfileName = $"OoT active {Guid.NewGuid():N}";
            Assert.True(store.CreateProfile(ootProfileName));
            try
            {
                store.SetDisplayChar(0x92, 'ø');

                CharacterProfileSnapshot snapshot = store.CreateSnapshot(GameKind.MajorasMask);

                Assert.Equal(GameKind.MajorasMask, snapshot.GameKind);
                Assert.Equal(mmProfileName, snapshot.ProfileName);
                Assert.True(snapshot.TryGetDisplayChar(0x9e, out char mmChar));
                Assert.Equal('å', mmChar);
                Assert.False(snapshot.TryGetByte('ø', out _));
            }
            finally
            {
                store.ResetDisplayChar(0x92);
                store.DeleteSelectedProfile();
            }
        }
        finally
        {
            store.SetGameKind(GameKind.MajorasMask);
            store.ResetDisplayChar(0x9e);
            store.DeleteSelectedProfile();
            store.SetGameKind(GameKind.OcarinaOfTime);
        }
    }

    [Fact]
    public void OotHeaderImporterUsesExplicitEncodingProfileSnapshot()
    {
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        store.SetGameKind(GameKind.OcarinaOfTime);
        string profileName = $"OoT header import {Guid.NewGuid():N}";

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetDisplayChar(0x92, 'å');
            MessageEncodingProfile encodingProfile = MessageEncodingProfile.Default.WithCharacterProfileSnapshot(store.CreateSnapshot());

            store.SetDisplayChar(0x92, 'ø');

            List<MessageEntry> entries = CHeaderImporter.Import(
                """
                DEFINE_MESSAGE(0x1234, TEXTBOX_TYPE_BLACK, TEXTBOX_POS_VARIABLE,
                "\x92"
                )
                """,
                encodingProfile: encodingProfile);

            Assert.Equal("å", Assert.Single(entries).Text);
        }
        finally
        {
            store.ResetDisplayChar(0x92);
            store.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void MmHeaderImporterUsesExplicitEncodingProfileSnapshot()
    {
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        store.SetGameKind(GameKind.MajorasMask);
        string profileName = $"MM header import {Guid.NewGuid():N}";

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetDisplayChar(0x9e, 'å');
            MessageEncodingProfile encodingProfile = MessageEncodingProfile.MajorasMask.WithCharacterProfileSnapshot(store.CreateSnapshot());

            store.SetDisplayChar(0x9e, 'ø');

            List<MessageEntry> entries = MmCHeaderImporter.Import(
                """
                DEFINE_MESSAGE(0x1234, 0x00, 0x00,
                MSG(
                HEADER(0x0000, 0xFE, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF)
                "\x9E"
                )
                )
                """,
                encodingProfile);

            Assert.Equal("å", Assert.Single(entries).Text);
        }
        finally
        {
            store.ResetDisplayChar(0x9e);
            store.DeleteSelectedProfile();
            store.SetGameKind(GameKind.OcarinaOfTime);
        }
    }

    private static string CreateTempGlyphImage()
    {
        string sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        using var bitmap = new Bitmap(16, 16);
        bitmap.SetPixel(0, 0, Color.White);
        bitmap.Save(sourcePath);
        return sourcePath;
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
