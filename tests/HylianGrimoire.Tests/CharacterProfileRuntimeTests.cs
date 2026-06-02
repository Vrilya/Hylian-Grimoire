using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Services;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class CharacterProfileRuntimeTests
{
    [Fact]
    public void CreateEncodingProfileUsesStableSnapshot()
    {
        CharacterProfileStore store = CharacterProfileStore.Current;
        CharacterProfileRuntime runtime = new(store);
        store.SetGameKind(GameKind.OcarinaOfTime);
        string profileName = $"Runtime encoding {Guid.NewGuid():N}";
        const byte value = 0x92;

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetDisplayChar(value, 'Q');
            MessageEncodingProfile encodingProfile = runtime.CreateEncodingProfile(GameProfiles.Get(GameKind.OcarinaOfTime));

            store.SetDisplayChar(value, 'R');

            Assert.True(encodingProfile.TryGetEditorChar(value, out char snapshotChar));
            Assert.Equal('Q', snapshotChar);

            MessageEncodingProfile updatedEncodingProfile = runtime.CreateEncodingProfile(GameProfiles.Get(GameKind.OcarinaOfTime));
            Assert.True(updatedEncodingProfile.TryGetEditorChar(value, out char updatedChar));
            Assert.Equal('R', updatedChar);
        }
        finally
        {
            store.ResetDisplayChar(value);
            store.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void CreateGlyphSourceUsesStableSnapshot()
    {
        CharacterProfileStore store = CharacterProfileStore.Current;
        CharacterProfileRuntime runtime = new(store);
        store.SetGameKind(GameKind.OcarinaOfTime);
        string profileName = $"Runtime glyph source {Guid.NewGuid():N}";
        const byte value = 0x81;
        double defaultWidth = GameGlyphCatalog.GetDefaultAdvance(GameKind.OcarinaOfTime, value);

        Assert.True(store.CreateProfile(profileName));
        try
        {
            store.SetWidth(value, defaultWidth + 2.0, defaultWidth);
            IGlyphSource glyphSource = runtime.CreateGlyphSource(GameProfiles.Get(GameKind.OcarinaOfTime), romData: null);

            store.SetWidth(value, defaultWidth + 4.0, defaultWidth);

            Assert.Equal(defaultWidth + 2.0, glyphSource.GetAdvance(value));

            IGlyphSource updatedGlyphSource = runtime.CreateGlyphSource(GameProfiles.Get(GameKind.OcarinaOfTime), romData: null);
            Assert.Equal(defaultWidth + 4.0, updatedGlyphSource.GetAdvance(value));
        }
        finally
        {
            store.ResetWidth(value);
            store.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void RemapEditorTextUsesSelectionChangeProfiles()
    {
        CharacterProfileStore store = CharacterProfileStore.Current;
        CharacterProfileRuntime runtime = new(store);
        store.SetGameKind(GameKind.OcarinaOfTime);
        string targetProfileName = $"Runtime remap {Guid.NewGuid():N}";
        var previousProfile = new CharacterProfile
        {
            GameKind = GameKind.OcarinaOfTime,
            Characters = new Dictionary<string, string>
            {
                ["0x92"] = "Q",
            },
        };

        Assert.True(store.CreateProfile(targetProfileName));
        try
        {
            store.SetDisplayChar(0x92, 'R');
            var args = new CharacterProfileSelectionChangedEventArgs(
                previousProfileName: "Previous",
                targetProfileName,
                previousProfile);

            string remapped = runtime.RemapEditorText("Q [color:red]Q", args);

            Assert.Equal("R [color:red]R", remapped);
        }
        finally
        {
            store.ResetDisplayChar(0x92);
            store.DeleteSelectedProfile();
        }
    }

    [Fact]
    public void ProfileEditingApiUpdatesSelectedProfileSnapshot()
    {
        CharacterProfileStore store = CharacterProfileStore.Current;
        CharacterProfileRuntime runtime = new(store);
        runtime.SetActiveGame(GameKind.OcarinaOfTime);
        string profileName = $"Runtime edit {Guid.NewGuid():N}";
        const byte value = 0x81;
        double defaultWidth = GameGlyphCatalog.GetDefaultAdvance(GameKind.OcarinaOfTime, value);

        Assert.True(runtime.CreateProfile(profileName));
        try
        {
            runtime.SetDisplayChar(value, 'Q');
            runtime.SetWidth(value, defaultWidth + 3.0, defaultWidth);

            CharacterProfileSnapshot snapshot = runtime.CreateSnapshot(GameKind.OcarinaOfTime);

            Assert.Equal(profileName, runtime.SelectedProfileName);
            Assert.Contains(profileName, runtime.ProfileNames);
            Assert.True(runtime.CanEditSelectedProfile);
            Assert.True(runtime.CanDeleteSelectedProfile);
            Assert.True(snapshot.TryGetDisplayChar(value, out char displayChar));
            Assert.Equal('Q', displayChar);
            Assert.True(snapshot.TryGetWidth(value, out double width));
            Assert.Equal(defaultWidth + 3.0, width);
        }
        finally
        {
            runtime.ResetDisplayChar(value);
            runtime.ResetWidth(value);
            runtime.DeleteSelectedProfile();
        }
    }
}
