using System.Text.Json;

namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string ConfigPath => Path.Combine(
        Environment.GetEnvironmentVariable("OOT_EDITOR_CHARACTER_PROFILE_CONFIG_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HylianGrimoire"),
        "character_profiles.json");

    private void SaveConfig()
    {
        WriteConfig(new CharacterProfileConfig
        {
            AutomaticProfile = IsAutomaticProfileValid(AutomaticProfileNameSetting)
                ? AutomaticProfileNameSetting
                : AutomaticProfileName,
            Profiles = _profiles.OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase).ToList(),
        });

        Version++;
    }

    private static CharacterProfileStore Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                CharacterProfileConfig? config = JsonSerializer.Deserialize<CharacterProfileConfig>(File.ReadAllText(ConfigPath));
                List<CharacterProfile> profiles = SanitizeProfiles(config?.Profiles ?? []);
                return new CharacterProfileStore(profiles, config?.AutomaticProfile ?? config?.SelectedProfile ?? AutomaticProfileName);
            }
        }
        catch (Exception ex)
        {
            return new CharacterProfileStore([], AutomaticProfileName, $"Character profiles could not be loaded: {ex.Message}");
        }

        return new CharacterProfileStore([], AutomaticProfileName);
    }

    private static List<CharacterProfile> SanitizeProfiles(List<CharacterProfile> profiles)
    {
        var result = new List<CharacterProfile>();
        foreach (CharacterProfile profile in profiles)
        {
            profile.Name = NormalizeProfileName(profile.Name);
            if (profile.Name.Length == 0
                || profile.Name.Equals(DefaultProfileName, StringComparison.OrdinalIgnoreCase)
                || profile.Name.Equals(CustomGlyphsProfileName, StringComparison.OrdinalIgnoreCase)
                || result.Any(existing => existing.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            profile.Characters = profile.Characters
                .Where(pair => TryParseKey(pair.Key, out _) && pair.Value.Length > 0)
                .ToDictionary(pair => ToKey(ParseKey(pair.Key)), pair => pair.Value[..1], StringComparer.OrdinalIgnoreCase);
            profile.Widths = profile.Widths
                .Where(pair => TryParseKey(pair.Key, out _) && !double.IsNaN(pair.Value))
                .ToDictionary(pair => ToKey(ParseKey(pair.Key)), pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            profile.Images = profile.Images
                .Where(pair => TryParseKey(pair.Key, out _) && !string.IsNullOrWhiteSpace(pair.Value))
                .ToDictionary(pair => ToKey(ParseKey(pair.Key)), pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            profile.ImageData = profile.ImageData
                .Where(pair => TryParseKey(pair.Key, out byte value)
                    && profile.Images.ContainsKey(ToKey(value))
                    && !string.IsNullOrWhiteSpace(pair.Value)
                    && IsValidBase64(pair.Value))
                .ToDictionary(pair => ToKey(ParseKey(pair.Key)), pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            result.Add(profile);
        }

        return result;
    }

    private static void WriteConfig(CharacterProfileConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, JsonOptions));
    }

    private static bool IsValidBase64(string value)
    {
        byte[] buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out _);
    }

    private sealed class CharacterProfileConfig
    {
        public string AutomaticProfile { get; set; } = AutomaticProfileName;

        public string SelectedProfile { get; set; } = DefaultProfileName;

        public List<CharacterProfile> Profiles { get; set; } = [];
    }
}
