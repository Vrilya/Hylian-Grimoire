using System.Drawing;

namespace HylianGrimoire.TextTextures;

public static partial class PauseHeaderTextureRenderer
{
    private static Bitmap LoadTemplateRow(string templateRoot, PauseHeaderTextureSpec spec)
    {
        var images = new List<Bitmap>(3);
        try
        {
            foreach (string fileName in spec.TemplateFileNames)
            {
                string path = Path.Combine(templateRoot, fileName);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException("Pause-header template is missing.", path);
                }

                Bitmap image = new(path);
                if (image.Width != PauseHeaderTextureCatalog.TileWidth || image.Height != PauseHeaderTextureCatalog.TileHeight)
                {
                    image.Dispose();
                    throw new InvalidDataException($"{path} must be {PauseHeaderTextureCatalog.TileWidth}x{PauseHeaderTextureCatalog.TileHeight} pixels.");
                }

                images.Add(CloneAsArgb(image));
                image.Dispose();
            }

            return CombineTriplet(images);
        }
        finally
        {
            foreach (Bitmap image in images)
            {
                image.Dispose();
            }
        }
    }
}
