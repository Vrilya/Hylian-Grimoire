using HylianGrimoire.Textures;

namespace HylianGrimoire.Soh;

public sealed partial class SohModMakerWindow
{
    private static bool IsTextResourcePath(string resourcePath)
        => resourcePath.StartsWith("text/", StringComparison.Ordinal);

    private static bool IsTextureResourcePath(string resourcePath)
        => !IsTextResourcePath(resourcePath);

    private IReadOnlyList<SohArchiveTextureResource> BuildArchiveTextureResources(
        IReadOnlyDictionary<string, byte[]> entries,
        IReadOnlyList<TextureDefinition> textures,
        byte[]? rom)
    {
        return
        [
            .. entries.Keys
                .Where(IsTextureResourcePath)
                .Order(StringComparer.Ordinal)
                .Select(path => CreateArchiveTextureResource(path, entries[path], textures, rom)),
        ];
    }

    private SohArchiveTextureResource CreateArchiveTextureResource(
        string resourcePath,
        byte[] data,
        IReadOnlyList<TextureDefinition> textures,
        byte[]? rom)
    {
        TextureDefinition? matchingTexture = textures.FirstOrDefault(texture => SohResourcePacker.GetTextureResourcePath(texture) == resourcePath);
        if (matchingTexture is null)
        {
            return new SohArchiveTextureResource(resourcePath, SohArchiveTextureStatus.External);
        }

        if (rom is null)
        {
            return new SohArchiveTextureResource(resourcePath, SohArchiveTextureStatus.InArchive);
        }

        try
        {
            SohTextureResource textureResource = SohTextureResource.Read(data);
            if (!TextureDefinitionMatches(textureResource, matchingTexture))
            {
                return new SohArchiveTextureResource(resourcePath, SohArchiveTextureStatus.DiffersFromRom);
            }

            byte[] romRaw = TextureRomService.ReadRaw(rom, matchingTexture);
            return textureResource.RawPixels.SequenceEqual(romRaw)
                ? new SohArchiveTextureResource(resourcePath, SohArchiveTextureStatus.MatchesRom)
                : new SohArchiveTextureResource(resourcePath, SohArchiveTextureStatus.DiffersFromRom);
        }
        catch (InvalidDataException)
        {
            return new SohArchiveTextureResource(resourcePath, SohArchiveTextureStatus.External);
        }
    }

    private TextureDefinition? FindTexture(string resourcePath)
        => _textures.FirstOrDefault(texture => SohResourcePacker.GetTextureResourcePath(texture) == resourcePath);

    private static bool TextureDefinitionMatches(SohTextureResource textureResource, TextureDefinition texture)
        => textureResource.Width == texture.Width
            && textureResource.Height == texture.Height
            && textureResource.Format == texture.Format;
}
