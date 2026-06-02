using HylianGrimoire.Codecs;
using HylianGrimoire.Games;
using HylianGrimoire.Glyphs;
using HylianGrimoire.Rom;

namespace HylianGrimoire.Services;

public sealed class CharacterProfileRuntime
{
    private readonly CharacterProfileStore _profiles;

    public CharacterProfileRuntime(CharacterProfileStore profiles)
    {
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
    }

    public event EventHandler? AutomaticProfileChanged
    {
        add => _profiles.AutomaticProfileChanged += value;
        remove => _profiles.AutomaticProfileChanged -= value;
    }

    public event EventHandler<CharacterProfileSelectionChangedEventArgs>? SelectionChanged
    {
        add => _profiles.SelectionChanged += value;
        remove => _profiles.SelectionChanged -= value;
    }

    public event EventHandler? ProfilesChanged
    {
        add => _profiles.ProfilesChanged += value;
        remove => _profiles.ProfilesChanged -= value;
    }

    public event EventHandler? MappingsChanged
    {
        add => _profiles.MappingsChanged += value;
        remove => _profiles.MappingsChanged -= value;
    }

    public string SelectedProfileName => _profiles.SelectedProfileName;

    public GameKind ActiveGameKind => _profiles.ActiveGameKind;

    public string AutomaticProfileNameSetting => _profiles.AutomaticProfileNameSetting;

    public IReadOnlyList<string> NamedProfileNames => _profiles.NamedProfileNames;

    public IReadOnlyList<string> ProfileNames => _profiles.ProfileNames;

    public bool CanEditSelectedProfile => _profiles.CanEditSelectedProfile;

    public bool CanDeleteSelectedProfile => _profiles.CanDeleteSelectedProfile;

    public void SetActiveGame(GameKind gameKind) => _profiles.SetGameKind(gameKind);

    public void SelectProfile(string profileName) => _profiles.SelectProfile(profileName);

    public bool CreateProfile(string profileName) => _profiles.CreateProfile(profileName);

    public bool DeleteSelectedProfile() => _profiles.DeleteSelectedProfile();

    public void SetAutomaticProfile(string profileName) => _profiles.SetAutomaticProfile(profileName);

    public void SetCustomGlyphsAvailable(bool available) => _profiles.SetCustomGlyphsAvailable(available);

    public void ClearCustomGlyphs() => SetCustomGlyphsAvailable(false);

    public void ApplyAutomaticProfileForLoadedRom(RomMessageData romData)
    {
        if (!romData.Profile.GameProfile.Capabilities.SupportsRomGlyphEditor)
        {
            ClearCustomGlyphs();
            return;
        }

        var glyphSession = new RomGlyphEditorSession(
            romData.DecompressedRom,
            romData.FontResources,
            romData.Profile.FontBaseline,
            romData.Profile.Game);
        _profiles.ApplyAutomaticProfile(glyphSession.HasLoadedCustomGlyphOrWidth());
    }

    public string RemapEditorText(string editorText, CharacterProfileSelectionChangedEventArgs args)
        => _profiles.RemapEditorText(editorText, args.PreviousProfile, args.SelectedProfileName);

    public void SetDisplayChar(byte value, char displayChar) => _profiles.SetDisplayChar(value, displayChar);

    public void ResetDisplayChar(byte value) => _profiles.ResetDisplayChar(value);

    public void SetWidth(byte value, double width) => _profiles.SetWidth(value, width);

    public void SetWidth(byte value, double width, double defaultWidth) => _profiles.SetWidth(value, width, defaultWidth);

    public void ResetWidth(byte value) => _profiles.ResetWidth(value);

    public void SetImage(byte value, string sourcePath) => _profiles.SetImage(value, sourcePath);

    public void ResetImage(byte value) => _profiles.ResetImage(value);

    public CharacterProfileSnapshot CreateSnapshot(GameKind gameKind)
        => _profiles.CreateSnapshot(gameKind);

    public CharacterProfileSnapshot CreateSnapshot(GameProfile profile)
        => CreateSnapshot(profile.Kind);

    public MessageEncodingProfile CreateEncodingProfile(GameKind gameKind)
        => CreateEncodingProfile(GameProfiles.Get(gameKind));

    public MessageEncodingProfile CreateEncodingProfile(GameProfile profile)
        => CreateEncodingProfile(profile, CreateSnapshot(profile));

    public MessageEncodingProfile CreateEncodingProfile(GameProfile profile, CharacterProfileSnapshot snapshot)
        => profile.EncodingProfile.WithCharacterProfileSnapshot(snapshot);

    public GameProfile CreateEncodingGameProfile(GameProfile profile)
        => CreateEncodingGameProfile(profile, CreateSnapshot(profile));

    public GameProfile CreateEncodingGameProfile(GameProfile profile, CharacterProfileSnapshot snapshot)
        => profile with
        {
            EncodingProfile = CreateEncodingProfile(profile, snapshot),
        };

    public IGlyphSource CreateGlyphSource(
        GameProfile profile,
        RomMessageData? romData,
        CharacterProfileSnapshot? snapshot = null)
    {
        snapshot ??= CreateSnapshot(profile);

        return profile.Kind switch
        {
            GameKind.OcarinaOfTime => romData is null
                ? OotGlyphSources.FromSnapshot(snapshot)
                : new RomGlyphSource(romData.DecompressedRom, romData.FontResources),
            GameKind.MajorasMask => MmGlyphSources.FromSnapshot(snapshot),
            _ => throw new InvalidDataException($"No glyph source is registered for {profile.DisplayName}.")
        };
    }
}
