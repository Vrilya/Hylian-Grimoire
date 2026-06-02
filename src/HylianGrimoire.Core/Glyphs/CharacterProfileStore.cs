using HylianGrimoire.Games;

namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileStore
{
    public const string AutomaticProfileName = "Auto";
    public const string DefaultProfileName = "Default";
    public const string CustomGlyphsProfileName = "Custom glyphs";

    public static CharacterProfileStore Current { get; } = Load();

    private readonly List<CharacterProfile> _profiles;
    private readonly Dictionary<GameKind, string> _automaticProfileByGame;
    private CharacterProfile? _temporaryProfile;
    private GameKind _activeGameKind = GameKind.OcarinaOfTime;

    private CharacterProfileStore(
        List<CharacterProfile> profiles,
        Dictionary<GameKind, string> automaticProfileByGame,
        string? loadWarning = null)
    {
        _profiles = profiles;
        _automaticProfileByGame = automaticProfileByGame;
        SelectedProfileName = AutomaticProfileNameSetting == AutomaticProfileName
            ? DefaultProfileName
            : AutomaticProfileNameSetting;
        LoadWarning = loadWarning;
    }

    public event EventHandler? AutomaticProfileChanged;

    public event EventHandler<CharacterProfileSelectionChangedEventArgs>? SelectionChanged;

    public event EventHandler? ProfilesChanged;

    public event EventHandler? MappingsChanged;

    public string? LoadWarning { get; }

    public int Version { get; private set; }

    public string SelectedProfileName { get; private set; }

    public GameKind ActiveGameKind => _activeGameKind;

    public string AutomaticProfileNameSetting => GetAutomaticProfileName(_activeGameKind);

    public IReadOnlyList<string> NamedProfileNames =>
        ActiveProfiles.Select(profile => profile.Name).Order(StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<string> ProfileNames
    {
        get
        {
            var names = new List<string> { DefaultProfileName };
            names.AddRange(ActiveProfiles.Select(profile => profile.Name).Order(StringComparer.OrdinalIgnoreCase));
            if (_temporaryProfile is not null)
            {
                names.Add(CustomGlyphsProfileName);
            }

            return names;
        }
    }

    public bool CanEditSelectedProfile => SelectedProfileName != DefaultProfileName;

    public bool CanDeleteSelectedProfile =>
        SelectedProfileName != DefaultProfileName
        && SelectedProfileName != CustomGlyphsProfileName
        && ProfileExists(SelectedProfileName);

    private IEnumerable<CharacterProfile> ActiveProfiles =>
        _profiles.Where(profile => profile.GameKind == _activeGameKind);

    private void RaiseSelectionChanged(string previousProfileName, CharacterProfile? previousProfile)
    {
        SelectionChanged?.Invoke(
            this,
            new CharacterProfileSelectionChangedEventArgs(
                previousProfileName,
                SelectedProfileName,
                previousProfile));
    }

    private CharacterProfile? GetSelectedEditableProfile()
    {
        return GetProfile(SelectedProfileName);
    }

    private CharacterProfile? GetProfile(string profileName)
        => GetProfile(_activeGameKind, profileName);

    private CharacterProfile? GetProfile(GameKind gameKind, string profileName)
    {
        if (profileName == CustomGlyphsProfileName)
        {
            return gameKind == _activeGameKind ? _temporaryProfile : null;
        }

        return _profiles.FirstOrDefault(profile => profile.GameKind == gameKind
            && profile.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
    }

    private static CharacterProfile? CloneProfile(CharacterProfile? profile)
    {
        return profile is null
            ? null
            : new CharacterProfile
            {
                Name = profile.Name,
                GameKind = profile.GameKind,
                Characters = profile.Characters.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
                Widths = profile.Widths.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
                Images = profile.Images.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
                ImageData = profile.ImageData.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
            };
    }

    private CharacterProfile GetOrCreateSelectedEditableProfile()
    {
        if (SelectedProfileName == CustomGlyphsProfileName)
        {
            return _temporaryProfile ??= new CharacterProfile { Name = CustomGlyphsProfileName, GameKind = _activeGameKind };
        }

        CharacterProfile? profile = ActiveProfiles.FirstOrDefault(profile => profile.Name.Equals(SelectedProfileName, StringComparison.OrdinalIgnoreCase));
        if (profile is not null)
        {
            return profile;
        }

        _temporaryProfile ??= new CharacterProfile { Name = CustomGlyphsProfileName, GameKind = _activeGameKind };
        SelectedProfileName = CustomGlyphsProfileName;
        return _temporaryProfile;
    }

    private bool ProfileExists(string name)
    {
        return name == DefaultProfileName
            || name == CustomGlyphsProfileName && _temporaryProfile is not null
            || ActiveProfiles.Any(profile => profile.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsAutomaticProfileValid(string name)
    {
        return name == AutomaticProfileName
            || name == DefaultProfileName
            || ActiveProfiles.Any(profile => profile.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private string GetAutomaticProfileName(GameKind gameKind)
    {
        if (!_automaticProfileByGame.TryGetValue(gameKind, out string? name))
        {
            return AutomaticProfileName;
        }

        return IsAutomaticProfileValidForGame(name, gameKind) ? name : AutomaticProfileName;
    }

    private bool IsAutomaticProfileValidForGame(string name, GameKind gameKind)
    {
        return name == AutomaticProfileName
            || name == DefaultProfileName
            || _profiles.Any(profile => profile.GameKind == gameKind
                && profile.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeProfileName(string name)
    {
        return name.Trim();
    }

    private static string ToKey(byte value) => $"0x{value:X2}";

    private static byte ParseKey(string key)
    {
        _ = TryParseKey(key, out byte value);
        return value;
    }

    private static bool TryParseKey(string key, out byte value)
    {
        string text = key.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? key[2..] : key;
        return byte.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out value);
    }

}
