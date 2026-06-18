using HylianGrimoire.Textures;

namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow
{
    private bool IsTextResourcePath(string resourcePath)
        => _portProfile.IsTextResourcePath(resourcePath);

    private bool IsTextureResourcePath(string resourcePath)
        => !IsTextResourcePath(resourcePath);

    private IReadOnlyList<O2rArchiveTextureResource> BuildArchiveTextureResources(
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

    private O2rArchiveTextureResource CreateArchiveTextureResource(
        string resourcePath,
        byte[] data,
        IReadOnlyList<TextureDefinition> textures,
        byte[]? rom)
    {
        TextureDefinition? matchingTexture = textures.FirstOrDefault(texture => _portProfile.GetTextureResourcePath(texture) == resourcePath);
        if (matchingTexture is null)
        {
            return new O2rArchiveTextureResource(resourcePath, O2rArchiveTextureStatus.External);
        }

        if (rom is null)
        {
            return new O2rArchiveTextureResource(resourcePath, O2rArchiveTextureStatus.InArchive);
        }

        try
        {
            O2rTextureResource textureResource = O2rTextureResource.Read(data);
            if (!TextureDefinitionMatches(textureResource, matchingTexture))
            {
                return new O2rArchiveTextureResource(resourcePath, O2rArchiveTextureStatus.DiffersFromRom);
            }

            byte[] romRaw = TextureRomService.ReadRaw(rom, matchingTexture);
            return textureResource.RawPixels.SequenceEqual(romRaw)
                ? new O2rArchiveTextureResource(resourcePath, O2rArchiveTextureStatus.MatchesRom)
                : new O2rArchiveTextureResource(resourcePath, O2rArchiveTextureStatus.DiffersFromRom);
        }
        catch (InvalidDataException)
        {
            return new O2rArchiveTextureResource(resourcePath, O2rArchiveTextureStatus.External);
        }
    }

    private TextureDefinition? FindTexture(string resourcePath)
        => _textures.FirstOrDefault(texture => _portProfile.GetTextureResourcePath(texture) == resourcePath);

    private static bool TextureDefinitionMatches(O2rTextureResource textureResource, TextureDefinition texture)
        => textureResource.Width == texture.Width
            && textureResource.Height == texture.Height
            && textureResource.Format == texture.Format;
}
