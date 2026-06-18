using HylianGrimoire.Services;

namespace HylianGrimoire.Textures;

public sealed partial class TextureManagerWindow
{
    private static int ExportTexturesToFolder(
        byte[] rom,
        IReadOnlyList<TextureDefinition> textures,
        string folder,
        IProgress<int> progress)
    {
        int exported = 0;
        for (int i = 0; i < textures.Count; i++)
        {
            TextureDefinition texture = textures[i];
            string path = GetTextureFilePath(folder, texture);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var bitmap = TextureRomService.Decode(rom, texture);
            PngFileWriter.SaveDirect(bitmap, path);
            exported++;
            progress.Report(GetPercent(i + 1, textures.Count));
        }

        return exported;
    }

    private static int ReplaceTexturesFromFolder(
        byte[] rom,
        IReadOnlyList<TextureDefinition> textures,
        string folder,
        IProgress<int> progress)
    {
        int replaced = 0;
        for (int i = 0; i < textures.Count; i++)
        {
            TextureDefinition texture = textures[i];
            string path = GetTextureFilePath(folder, texture);
            if (File.Exists(path))
            {
                TextureRomService.EncodeAndWrite(rom, texture, path);
                replaced++;
            }

            progress.Report(GetPercent(i + 1, textures.Count));
        }

        return replaced;
    }
}
