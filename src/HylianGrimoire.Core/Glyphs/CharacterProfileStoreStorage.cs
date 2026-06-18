namespace HylianGrimoire.Glyphs;

public sealed class CharacterProfileStoreStorage
{
    public const string ConfigDirectoryEnvironmentVariable = "OOT_EDITOR_CHARACTER_PROFILE_CONFIG_DIR";

    public CharacterProfileStoreStorage(string configDirectory, string assetRoot)
    {
        ConfigDirectory = RequirePath(configDirectory, nameof(configDirectory));
        AssetRoot = RequirePath(assetRoot, nameof(assetRoot));
    }

    public string ConfigDirectory { get; }

    public string AssetRoot { get; }

    public string ConfigPath => Path.Combine(ConfigDirectory, "character_profiles.json");

    public static CharacterProfileStoreStorage CreateDefault()
    {
        string appDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HylianGrimoire");
        string configDirectory = Environment.GetEnvironmentVariable(ConfigDirectoryEnvironmentVariable)
            ?? appDataRoot;

        return new CharacterProfileStoreStorage(
            configDirectory,
            Path.Combine(appDataRoot, "CharacterProfiles"));
    }

    public static CharacterProfileStoreStorage CreateIsolated(string rootDirectory)
    {
        string root = RequirePath(rootDirectory, nameof(rootDirectory));
        return new CharacterProfileStoreStorage(
            Path.Combine(root, "config"),
            Path.Combine(root, "assets"));
    }

    private static string RequirePath(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must not be empty.", parameterName);
        }

        return path;
    }
}
