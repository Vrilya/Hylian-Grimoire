using HylianGrimoire.Games;

namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileStore
{
    public CharacterProfileSnapshot CreateSnapshot() => CreateSnapshot(_activeGameKind);

    public CharacterProfileSnapshot CreateSnapshot(GameKind gameKind)
    {
        string profileName = GetSnapshotProfileName(gameKind);
        CharacterProfile? profile = GetProfile(gameKind, profileName);
        return new CharacterProfileSnapshot(
            gameKind,
            profileName,
            Version,
            CopyDisplayChars(profile),
            CopyWidths(profile),
            CopyImagePaths(profile));
    }

    private string GetSnapshotProfileName(GameKind gameKind)
    {
        if (gameKind == _activeGameKind)
        {
            return SelectedProfileName;
        }

        string automaticProfileName = GetAutomaticProfileName(gameKind);
        return automaticProfileName == AutomaticProfileName
            ? DefaultProfileName
            : automaticProfileName;
    }

    private static Dictionary<byte, char> CopyDisplayChars(CharacterProfile? profile)
    {
        if (profile is null)
        {
            return [];
        }

        var result = new Dictionary<byte, char>();
        foreach ((string key, string text) in profile.Characters)
        {
            if (TryParseKey(key, out byte value) && text.Length > 0)
            {
                result[value] = text[0];
            }
        }

        return result;
    }

    private static Dictionary<byte, double> CopyWidths(CharacterProfile? profile)
    {
        if (profile is null)
        {
            return [];
        }

        return profile.Widths
            .Where(pair => TryParseKey(pair.Key, out _))
            .ToDictionary(pair => ParseKey(pair.Key), pair => pair.Value);
    }

    private Dictionary<byte, string> CopyImagePaths(CharacterProfile? profile)
    {
        if (profile is null)
        {
            return [];
        }

        return profile.Images
            .Where(pair => TryParseKey(pair.Key, out _) && !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => new
            {
                Value = ParseKey(pair.Key),
                Path = GetProfileImagePath(profile, pair.Key, pair.Value),
            })
            .Where(pair => File.Exists(pair.Path))
            .ToDictionary(pair => pair.Value, pair => pair.Path);
    }
}
