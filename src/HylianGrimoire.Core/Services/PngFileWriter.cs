using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.Services;

public static class PngFileWriter
{
    public static void Save(Bitmap bitmap, string path)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        AtomicFileWriter.WriteAllBytes(path, stream.ToArray());
    }

    public static void SaveDirect(Bitmap bitmap, string path)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        bitmap.Save(path, ImageFormat.Png);
    }
}
