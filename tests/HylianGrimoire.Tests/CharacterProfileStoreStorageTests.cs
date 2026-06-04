using System.Drawing;
using HylianGrimoire.Glyphs;
using Xunit;

namespace HylianGrimoire.Tests;

public sealed class CharacterProfileStoreStorageTests
{
    [Fact]
    public void LoadUsesConfiguredStorageForConfigAndAssets()
    {
        using CharacterProfileStoreTestScope scope = CharacterProfileStoreTestScope.Create();
        CharacterProfileStore store = scope.Store;
        string sourcePath = CreateTempGlyphImage();

        Assert.True(store.CreateProfile("Stored profile"));
        try
        {
            store.SetDisplayChar(0x92, 'Q');
            store.SetImage(0x82, sourcePath);

            CharacterProfileStore reloaded = CharacterProfileStore.Load(
                CharacterProfileStoreStorage.CreateIsolated(scope.Root));

            Assert.Contains("Stored profile", reloaded.ProfileNames);
            reloaded.SelectProfile("Stored profile");
            Assert.True(reloaded.TryGetDisplayChar(0x92, out char displayChar));
            Assert.Equal('Q', displayChar);
            Assert.True(reloaded.TryGetImagePath(0x82, out string? imagePath));
            Assert.StartsWith(reloaded.ProfileAssetRoot, imagePath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteIfExists(sourcePath);
        }
    }

    [Fact]
    public void DefaultStorageKeepsExistingProductPaths()
    {
        CharacterProfileStoreStorage storage = CharacterProfileStoreStorage.CreateDefault();
        string appDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HylianGrimoire");

        Assert.Equal(Path.Combine(appDataRoot, "CharacterProfiles"), storage.AssetRoot);
    }

    [Fact]
    public void DefaultStorageKeepsLegacyConfigDirectoryOverride()
    {
        string? previous = Environment.GetEnvironmentVariable(
            CharacterProfileStoreStorage.ConfigDirectoryEnvironmentVariable);
        string configDirectory = Path.Combine(
            Path.GetTempPath(),
            "HylianGrimoireTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            Environment.SetEnvironmentVariable(
                CharacterProfileStoreStorage.ConfigDirectoryEnvironmentVariable,
                configDirectory);

            CharacterProfileStoreStorage storage = CharacterProfileStoreStorage.CreateDefault();

            Assert.Equal(configDirectory, storage.ConfigDirectory);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                CharacterProfileStoreStorage.ConfigDirectoryEnvironmentVariable,
                previous);
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
