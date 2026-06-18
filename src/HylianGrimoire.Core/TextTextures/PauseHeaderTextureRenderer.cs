using System.Drawing;
using System.Drawing.Imaging;

namespace HylianGrimoire.TextTextures;

public static partial class PauseHeaderTextureRenderer
{
    private static readonly int[] GrayPalette = [0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255];

    public static Bitmap Render(
        string text,
        string fontPath,
        string templateRoot,
        PauseHeaderTextureTarget target,
        PauseHeaderTextureRenderSettings settings)
        => Render(text, TextTextureFont.FromPath(fontPath), templateRoot, target, settings);

    public static Bitmap Render(
        string text,
        TextTextureFont font,
        string templateRoot,
        PauseHeaderTextureTarget target,
        PauseHeaderTextureRenderSettings settings)
    {
        if (!File.Exists(font.Path))
        {
            throw new FileNotFoundException("Pause-header texture font is missing.", font.Path);
        }

        using Bitmap baseRow = LoadTemplateRow(templateRoot, target.Spec);
        using TextTextureDrawingFont drawingFont = new(font);

        PauseHeaderTextureRenderSettings effectiveSettings = settings with
        {
            CenterX = settings.Center ? settings.CenterX : settings.XNudge,
        };
        (Bitmap fillMask, Bitmap strokeMask) = CreateTextMasks(text, drawingFont.Family, drawingFont.Style, effectiveSettings);
        using (fillMask)
        using (strokeMask)
        {
            return Compose(baseRow, fillMask, strokeMask, effectiveSettings);
        }
    }

    public static IReadOnlyList<Bitmap> SplitTriplet(Bitmap row)
    {
        if (row.Width != PauseHeaderTextureCatalog.Width || row.Height != PauseHeaderTextureCatalog.Height)
        {
            throw new InvalidDataException($"Pause-header row must be {PauseHeaderTextureCatalog.Width}x{PauseHeaderTextureCatalog.Height} pixels.");
        }

        var images = new List<Bitmap>(3);
        for (int index = 0; index < 3; index++)
        {
            Rectangle source = new(index * PauseHeaderTextureCatalog.TileWidth, 0, PauseHeaderTextureCatalog.TileWidth, PauseHeaderTextureCatalog.TileHeight);
            images.Add(Crop(row, source));
        }

        return images;
    }

    public static Bitmap CombineTriplet(IReadOnlyList<Bitmap> images)
    {
        if (images.Count != 3)
        {
            throw new InvalidDataException("Pause-header triplet must contain exactly three textures.");
        }

        Bitmap row = new(PauseHeaderTextureCatalog.Width, PauseHeaderTextureCatalog.Height, PixelFormat.Format32bppArgb);
        for (int index = 0; index < images.Count; index++)
        {
            Bitmap image = images[index];
            if (image.Width != PauseHeaderTextureCatalog.TileWidth || image.Height != PauseHeaderTextureCatalog.TileHeight)
            {
                row.Dispose();
                throw new InvalidDataException($"Pause-header template {index + 1} must be {PauseHeaderTextureCatalog.TileWidth}x{PauseHeaderTextureCatalog.TileHeight} pixels.");
            }

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    row.SetPixel(index * PauseHeaderTextureCatalog.TileWidth + x, y, image.GetPixel(x, y));
                }
            }
        }

        return row;
    }

    public static Bitmap ApplyOriginalColors(Bitmap source, PauseHeaderTextureTarget target)
        => ApplyOriginalColors(source, target.Spec);

    public static Bitmap ApplyOriginalColors(Bitmap source, PauseHeaderTextureSpec spec)
    {
        if (source.Width != PauseHeaderTextureCatalog.Width || source.Height != PauseHeaderTextureCatalog.Height)
        {
            throw new InvalidDataException($"Pause-header row must be {PauseHeaderTextureCatalog.Width}x{PauseHeaderTextureCatalog.Height} pixels.");
        }

        Bitmap output = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Color pixel = source.GetPixel(x, y);
                PauseHeaderPageColor pageColor = GetInterpolatedPageColor(spec.OriginalColorRamp, x);
                int intensity = (pixel.R + pixel.G + pixel.B) / 3;
                output.SetPixel(
                    x,
                    y,
                    Color.FromArgb(
                        pixel.A,
                        Modulate(pageColor.Red, intensity),
                        Modulate(pageColor.Green, intensity),
                        Modulate(pageColor.Blue, intensity)));
            }
        }

        return output;
    }
}
