namespace HylianGrimoire.Textures;

public sealed partial class TextureManagerWindow
{
    private static string GetTextureFilePath(string root, TextureDefinition texture)
    {
        string[] groupParts = texture.Group
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizePathPart)
            .ToArray();

        return Path.Combine([root, .. groupParts, $"{SanitizePathPart(texture.ExportName)}.png"]);
    }

    private static string SanitizePathPart(string value)
    {
        HashSet<char> invalid = Path.GetInvalidFileNameChars().ToHashSet();
        return string.Concat(value.Select(ch => invalid.Contains(ch) ? '_' : ch));
    }
}
