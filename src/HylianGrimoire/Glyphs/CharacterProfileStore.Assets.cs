namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileStore
{
    public string ProfileAssetRoot => CharacterProfileAssets.ProfileAssetRoot;

    public bool TryGetImagePath(byte value, out string? path)
    {
        path = null;
        CharacterProfile? profile = GetSelectedEditableProfile();
        if (profile is null
            || !profile.Images.TryGetValue(ToKey(value), out string? relativePath)
            || string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        string key = ToKey(value);
        string candidate = CharacterProfileAssets.GetProfileAssetPath(profile.Name, relativePath);
        if (!File.Exists(candidate)
            && profile.ImageData.TryGetValue(key, out string? encodedImage)
            && CharacterProfileAssets.TryRestoreProfileImage(profile.Name, relativePath, encodedImage))
        {
            candidate = CharacterProfileAssets.GetProfileAssetPath(profile.Name, relativePath);
        }

        if (!File.Exists(candidate))
        {
            return false;
        }

        path = candidate;
        return true;
    }

    public void SetImage(byte value, string sourcePath)
    {
        if (!CanEditSelectedProfile)
        {
            return;
        }

        CharacterProfile profile = GetOrCreateSelectedEditableProfile();
        string key = ToKey(value);
        string relativePath = OotGlyphCatalog.GetGlyphRelativePath(value);
        CharacterProfileAssets.CopyProfileImage(profile.Name, relativePath, sourcePath);

        profile.Images[key] = relativePath;
        profile.ImageData[key] = Convert.ToBase64String(File.ReadAllBytes(sourcePath));
        SaveConfig();
        MappingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ResetImage(byte value)
    {
        CharacterProfile? profile = GetSelectedEditableProfile();
        if (profile is null)
        {
            return;
        }

        string key = ToKey(value);
        bool removed = profile.Images.Remove(key);
        removed |= profile.ImageData.Remove(key);
        if (removed)
        {
            SaveConfig();
            MappingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public IReadOnlyDictionary<byte, string> GetSelectedProfileImagePaths()
    {
        CharacterProfile? profile = GetSelectedEditableProfile();
        if (profile is null)
        {
            return new Dictionary<byte, string>();
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

    private Dictionary<string, string> CopyCurrentProfileImages(string destinationProfileName)
    {
        CharacterProfile? source = GetSelectedEditableProfile();
        if (source is null)
        {
            return [];
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach ((string key, string relativePath) in source.Images)
        {
            string sourcePath = CharacterProfileAssets.GetProfileAssetPath(source.Name, relativePath);
            if (!File.Exists(sourcePath)
                && source.ImageData.TryGetValue(key, out string? encodedImage))
            {
                CharacterProfileAssets.TryRestoreProfileImage(source.Name, relativePath, encodedImage);
            }

            if (!File.Exists(sourcePath))
            {
                continue;
            }

            CharacterProfileAssets.CopyProfileImage(destinationProfileName, relativePath, sourcePath);
            result[key] = relativePath;
        }

        return result;
    }

    private Dictionary<string, string> CopyCurrentProfileImageData(IReadOnlyDictionary<string, string> copiedImages)
    {
        CharacterProfile? source = GetSelectedEditableProfile();
        if (source is null)
        {
            return [];
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach ((string key, string relativePath) in copiedImages)
        {
            if (source.ImageData.TryGetValue(key, out string? encodedImage) && !string.IsNullOrWhiteSpace(encodedImage))
            {
                result[key] = encodedImage;
                continue;
            }

            string sourcePath = CharacterProfileAssets.GetProfileAssetPath(source.Name, relativePath);
            if (File.Exists(sourcePath))
            {
                result[key] = Convert.ToBase64String(File.ReadAllBytes(sourcePath));
            }
        }

        return result;
    }

    private static string GetProfileImagePath(CharacterProfile profile, string key, string relativePath)
    {
        string path = CharacterProfileAssets.GetProfileAssetPath(profile.Name, relativePath);
        if (!File.Exists(path)
            && profile.ImageData.TryGetValue(key, out string? encodedImage))
        {
            CharacterProfileAssets.TryRestoreProfileImage(profile.Name, relativePath, encodedImage);
        }

        return path;
    }
}

internal static class CharacterProfileAssets
{
    public static string ProfileAssetRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HylianGrimoire",
        "CharacterProfiles");

    public static string GetProfileAssetPath(string profileName, string relativePath)
    {
        return Path.Combine(ProfileAssetRoot, GetProfileAssetFolder(profileName), relativePath);
    }

    public static void CopyProfileImage(string profileName, string relativePath, string sourcePath)
    {
        string destination = GetProfileAssetPath(profileName, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(sourcePath, destination, overwrite: true);
    }

    public static bool TryRestoreProfileImage(string profileName, string relativePath, string encodedImage)
    {
        try
        {
            byte[] bytes = Convert.FromBase64String(encodedImage);
            string destination = GetProfileAssetPath(profileName, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllBytes(destination, bytes);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static void DeleteProfileAssets(string profileName)
    {
        string folder = Path.Combine(ProfileAssetRoot, GetProfileAssetFolder(profileName));
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (IOException)
        {
            // A failed asset cleanup should not make profile deletion fail.
        }
        catch (UnauthorizedAccessException)
        {
            // A failed asset cleanup should not make profile deletion fail.
        }
    }

    private static string GetProfileAssetFolder(string profileName)
    {
        string folder = profileName.Trim();
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            folder = folder.Replace(invalid, '_');
        }

        return folder.Length == 0 ? "Profile" : folder;
    }
}
