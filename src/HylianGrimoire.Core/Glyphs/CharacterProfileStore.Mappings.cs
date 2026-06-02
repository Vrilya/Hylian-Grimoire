using HylianGrimoire.Games;

namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileStore
{
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
        return CharacterProfileTextRemapper.Remap(
            _activeGameKind,
            editorText,
            GetProfile(sourceProfileName),
            GetProfile(targetProfileName));
    }

    public string RemapEditorText(string editorText, CharacterProfile? sourceProfile, string targetProfileName)
    {
        return CharacterProfileTextRemapper.Remap(
            _activeGameKind,
            editorText,
            sourceProfile,
            GetProfile(targetProfileName));
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
        char defaultChar = GameProfiles.GetOriginalEncodingProfile(_activeGameKind).GetDefaultEditorChar(value);
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
        SetWidth(value, width, GameGlyphCatalog.GetDefaultAdvance(_activeGameKind, value));
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

}
