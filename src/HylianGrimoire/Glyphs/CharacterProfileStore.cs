using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileStore
{
    public const string AutomaticProfileName = "Auto";
    public const string DefaultProfileName = "Default";
    public const string CustomGlyphsProfileName = "Custom glyphs";

    public static CharacterProfileStore Current { get; } = Load();

    private readonly List<CharacterProfile> _profiles;
    private CharacterProfile? _temporaryProfile;

    private CharacterProfileStore(List<CharacterProfile> profiles, string automaticProfileName, string? loadWarning = null)
    {
        _profiles = profiles;
        AutomaticProfileNameSetting = IsAutomaticProfileValid(automaticProfileName) ? automaticProfileName : AutomaticProfileName;
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

    public string AutomaticProfileNameSetting { get; private set; }

    public IReadOnlyList<string> NamedProfileNames =>
        _profiles.Select(profile => profile.Name).Order(StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<string> ProfileNames
    {
        get
        {
            var names = new List<string> { DefaultProfileName };
            names.AddRange(_profiles.Select(profile => profile.Name).Order(StringComparer.OrdinalIgnoreCase));
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

    public void UseDefaultProfile()
    {
        SelectProfile(DefaultProfileName);
    }

    public void ApplyAutomaticProfile(bool hasCustomRomGlyphs)
    {
        SetCustomGlyphsAvailable(hasCustomRomGlyphs);

        if (AutomaticProfileNameSetting == AutomaticProfileName)
        {
            SelectProfile(hasCustomRomGlyphs ? CustomGlyphsProfileName : DefaultProfileName);
        }
        else
        {
            SelectProfile(AutomaticProfileNameSetting);
        }
    }

    public void SetCustomGlyphsAvailable(bool available)
    {
        if (available)
        {
            if (_temporaryProfile is null)
            {
                _temporaryProfile = new CharacterProfile { Name = CustomGlyphsProfileName };
                Version++;
                ProfilesChanged?.Invoke(this, EventArgs.Empty);
            }

            return;
        }

        if (_temporaryProfile is null)
        {
            return;
        }

        _temporaryProfile = null;
        Version++;
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
        if (SelectedProfileName == CustomGlyphsProfileName)
        {
            string previousProfileName = SelectedProfileName;
            CharacterProfile? previousProfile = CloneProfile(GetProfile(previousProfileName));
            SelectedProfileName = DefaultProfileName;
            Version++;
            RaiseSelectionChanged(previousProfileName, previousProfile);
        }
    }

    public void UseCustomGlyphsProfile()
    {
        if (_temporaryProfile is null)
        {
            _temporaryProfile = new CharacterProfile { Name = CustomGlyphsProfileName };
            Version++;
            ProfilesChanged?.Invoke(this, EventArgs.Empty);
        }

        SelectProfile(CustomGlyphsProfileName);
    }

    public void SetAutomaticProfile(string name)
    {
        if (!IsAutomaticProfileValid(name))
        {
            name = AutomaticProfileName;
        }

        if (AutomaticProfileNameSetting == name)
        {
            return;
        }

        AutomaticProfileNameSetting = name;
        SaveConfig();
        AutomaticProfileChanged?.Invoke(this, EventArgs.Empty);
    }

    public void HideCustomGlyphsProfile()
    {
        if (_temporaryProfile is null)
        {
            return;
        }

        _temporaryProfile = null;
        Version++;
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
        if (SelectedProfileName == CustomGlyphsProfileName)
        {
            string previousProfileName = SelectedProfileName;
            CharacterProfile? previousProfile = CloneProfile(GetProfile(previousProfileName));
            SelectedProfileName = DefaultProfileName;
            Version++;
            RaiseSelectionChanged(previousProfileName, previousProfile);
        }
    }

    public void SelectProfile(string name)
    {
        if (!ProfileExists(name))
        {
            name = DefaultProfileName;
        }

        if (SelectedProfileName == name)
        {
            return;
        }

        string previousProfileName = SelectedProfileName;
        CharacterProfile? previousProfile = CloneProfile(GetProfile(previousProfileName));
        SelectedProfileName = name;
        Version++;
        RaiseSelectionChanged(previousProfileName, previousProfile);
    }

    public bool CreateProfile(string name)
    {
        name = NormalizeProfileName(name);
        if (name.Length == 0
            || name.Equals(DefaultProfileName, StringComparison.OrdinalIgnoreCase)
            || name.Equals(CustomGlyphsProfileName, StringComparison.OrdinalIgnoreCase)
            || ProfileExists(name))
        {
            return false;
        }

        var profile = new CharacterProfile
        {
            Name = name,
            Characters = CopyCurrentProfileCharacters(),
            Widths = CopyCurrentProfileWidths(),
            Images = CopyCurrentProfileImages(name),
        };
        profile.ImageData = CopyCurrentProfileImageData(profile.Images);

        _profiles.Add(profile);
        string previousProfileName = SelectedProfileName;
        CharacterProfile? previousProfile = CloneProfile(GetProfile(previousProfileName));
        SelectedProfileName = name;
        SaveConfig();
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
        RaiseSelectionChanged(previousProfileName, previousProfile);
        return true;
    }

    public bool DeleteSelectedProfile()
    {
        if (!CanDeleteSelectedProfile)
        {
            return false;
        }

        string deletedProfileName = SelectedProfileName;
        CharacterProfile? deletedProfile = CloneProfile(GetProfile(deletedProfileName));
        int removed = _profiles.RemoveAll(profile => profile.Name.Equals(deletedProfileName, StringComparison.OrdinalIgnoreCase));
        if (removed == 0)
        {
            return false;
        }

        CharacterProfileAssets.DeleteProfileAssets(deletedProfileName);
        SelectedProfileName = DefaultProfileName;
        bool automaticProfileChanged = false;
        if (!IsAutomaticProfileValid(AutomaticProfileNameSetting))
        {
            AutomaticProfileNameSetting = AutomaticProfileName;
            automaticProfileChanged = true;
        }

        SaveConfig();
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
        RaiseSelectionChanged(deletedProfileName, deletedProfile);
        if (automaticProfileChanged)
        {
            AutomaticProfileChanged?.Invoke(this, EventArgs.Empty);
        }

        return true;
    }

    public bool TryGetDisplayChar(byte value, out char displayChar)
    {
        displayChar = default;
        CharacterProfile? profile = GetSelectedEditableProfile();
        if (profile is null || !profile.Characters.TryGetValue(ToKey(value), out string? text) || text.Length == 0)
        {
            return false;
        }

        displayChar = text[0];
        return true;
    }

    public bool TryGetByte(char displayChar, out byte value)
    {
        CharacterProfile? profile = GetSelectedEditableProfile();
        if (profile is not null)
        {
            foreach ((string key, string text) in profile.Characters)
            {
                if (text == displayChar.ToString() && TryParseKey(key, out value))
                {
                    return true;
                }
            }
        }

        value = 0;
        return false;
    }

    public string RemapEditorText(string editorText, string sourceProfileName, string targetProfileName)
    {
        return RemapEditorText(editorText, GetProfile(sourceProfileName), GetProfile(targetProfileName));
    }

    public string RemapEditorText(string editorText, CharacterProfile? sourceProfile, string targetProfileName)
    {
        return RemapEditorText(editorText, sourceProfile, GetProfile(targetProfileName));
    }

    private static string RemapEditorText(string editorText, CharacterProfile? sourceProfile, CharacterProfile? targetProfile)
    {
        List<MessageToken> tokens = MessageTextSyntax.FromEditorText(editorText);
        var remapped = new List<MessageToken>(tokens.Count);

        foreach (MessageToken token in tokens)
        {
            remapped.Add(token is TextToken text
                ? new TextToken(RemapPlainText(text.Text, sourceProfile, targetProfile))
                : token);
        }

        return MessageTextSyntax.ToEditorText(remapped);
    }

    public bool TryGetWidth(byte value, out double width)
    {
        width = 0;
        CharacterProfile? profile = GetSelectedEditableProfile();
        return profile is not null
            && profile.Widths.TryGetValue(ToKey(value), out width);
    }

    public void SetDisplayChar(byte value, char displayChar)
    {
        if (!CanEditSelectedProfile)
        {
            return;
        }

        CharacterProfile profile = GetOrCreateSelectedEditableProfile();
        string key = ToKey(value);
        char defaultChar = MessageEncodingProfile.Original.GetDefaultEditorChar(value);
        if (displayChar == defaultChar)
        {
            profile.Characters.Remove(key);
            SaveConfig();
            MappingsChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        string text = displayChar.ToString();
        foreach (string existingKey in profile.Characters
                     .Where(pair => pair.Value == text && !pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            profile.Characters.Remove(existingKey);
        }

        profile.Characters[key] = text;
        SaveConfig();
        MappingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetWidth(byte value, double width)
    {
        SetWidth(value, width, OotGlyphMetrics.GetDefaultAdvance(value));
    }

    public void SetWidth(byte value, double width, double defaultWidth)
    {
        if (!CanEditSelectedProfile)
        {
            return;
        }

        CharacterProfile profile = GetOrCreateSelectedEditableProfile();
        string key = ToKey(value);
        if (Math.Abs(width - defaultWidth) < 0.001)
        {
            profile.Widths.Remove(key);
            SaveConfig();
            MappingsChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        profile.Widths[key] = width;
        SaveConfig();
        MappingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ResetWidth(byte value)
    {
        CharacterProfile? profile = GetSelectedEditableProfile();
        if (profile is null)
        {
            return;
        }

        if (profile.Widths.Remove(ToKey(value)))
        {
            SaveConfig();
            MappingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ResetDisplayChar(byte value)
    {
        CharacterProfile? profile = GetSelectedEditableProfile();
        if (profile is null)
        {
            return;
        }

        if (profile.Characters.Remove(ToKey(value)))
        {
            SaveConfig();
            MappingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private Dictionary<string, string> CopyCurrentProfileCharacters()
    {
        CharacterProfile? source = GetSelectedEditableProfile();
        return source is null
            ? []
            : source.Characters.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, double> CopyCurrentProfileWidths()
    {
        CharacterProfile? source = GetSelectedEditableProfile();
        return source is null
            ? []
            : source.Widths.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<byte, double> GetSelectedProfileWidths()
    {
        CharacterProfile? profile = GetSelectedEditableProfile();
        return profile is null
            ? new Dictionary<byte, double>()
            : profile.Widths
                .Where(pair => TryParseKey(pair.Key, out _))
                .ToDictionary(pair => ParseKey(pair.Key), pair => pair.Value);
    }

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
    {
        if (profileName == CustomGlyphsProfileName)
        {
            return _temporaryProfile;
        }

        return _profiles.FirstOrDefault(profile => profile.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
    }

    private static CharacterProfile? CloneProfile(CharacterProfile? profile)
    {
        return profile is null
            ? null
            : new CharacterProfile
            {
                Name = profile.Name,
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
            return _temporaryProfile ??= new CharacterProfile { Name = CustomGlyphsProfileName };
        }

        CharacterProfile? profile = _profiles.FirstOrDefault(profile => profile.Name.Equals(SelectedProfileName, StringComparison.OrdinalIgnoreCase));
        if (profile is not null)
        {
            return profile;
        }

        _temporaryProfile ??= new CharacterProfile { Name = CustomGlyphsProfileName };
        SelectedProfileName = CustomGlyphsProfileName;
        return _temporaryProfile;
    }

    private bool ProfileExists(string name)
    {
        return name == DefaultProfileName
            || name == CustomGlyphsProfileName && _temporaryProfile is not null
            || _profiles.Any(profile => profile.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsAutomaticProfileValid(string name)
    {
        return name == AutomaticProfileName
            || name == DefaultProfileName
            || _profiles.Any(profile => profile.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
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

    private static string RemapPlainText(string text, CharacterProfile? sourceProfile, CharacterProfile? targetProfile)
    {
        var result = new char[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            result[i] = TryGetByteFromProfile(sourceProfile, text[i], out byte value)
                ? GetEditorCharFromProfile(targetProfile, value)
                : text[i];
        }

        return new string(result);
    }

    private static bool TryGetByteFromProfile(CharacterProfile? profile, char displayChar, out byte value)
    {
        if (profile is not null)
        {
            string text = displayChar.ToString();
            foreach ((string key, string mappedText) in profile.Characters)
            {
                if (mappedText == text && TryParseKey(key, out value))
                {
                    return true;
                }
            }
        }

        if (MessageEncodingProfile.Original.TryGetByte(displayChar, out value))
        {
            return true;
        }

        if (displayChar is >= (char)0x20 and <= (char)0x7e)
        {
            value = (byte)displayChar;
            return true;
        }

        value = 0;
        return false;
    }

    private static char GetEditorCharFromProfile(CharacterProfile? profile, byte value)
    {
        if (profile is not null
            && profile.Characters.TryGetValue(ToKey(value), out string? text)
            && text.Length > 0)
        {
            return text[0];
        }

        return MessageEncodingProfile.Original.GetDefaultEditorChar(value);
    }

}
