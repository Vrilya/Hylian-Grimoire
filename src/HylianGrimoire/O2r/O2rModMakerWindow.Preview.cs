using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using HylianGrimoire.Textures;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace HylianGrimoire.O2r;

public sealed partial class O2rModMakerWindow
{
    private const int PreviewPadding = 32;

    private void OnPreviewViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_romData is not null && GetSelectedTextureItem() is TextureListItem item)
        {
            UpdatePreviewImage(item.Texture);
        }
    }

    private void UpdatePreviewImage(TextureDefinition texture)
    {
        if (_romData is null)
        {
            ClearPreview();
            return;
        }

        using Bitmap bitmap = TextureRomService.Decode(_romData.DecompressedRom, texture);
        (int width, int height) = GetScaledPreviewSize(texture.Width, texture.Height);
        string path = GetPreviewPath(texture.Name);
        string alphaPath = GetPreviewPath(texture.Name, "alpha");

        using Bitmap scaledBitmap = ScaleNearestNeighbor(bitmap, width, height);
        using Bitmap alphaBitmap = CreateAlphaPreviewBackground(width, height);
        alphaBitmap.Save(alphaPath, ImageFormat.Png);
        scaledBitmap.Save(path, ImageFormat.Png);

        PreviewAlphaImage.Source = new BitmapImage(new Uri(alphaPath));
        PreviewImage.Source = new BitmapImage(new Uri(path));
    }

    private void UpdateArchiveTexturePreview(O2rArchiveTextureResource resource)
    {
        try
        {
            if (!_existingEntries.TryGetValue(resource.ResourcePath, out byte[]? data))
            {
                ClearPreview();
                DetailsText.Text = $"{resource.ResourcePath}\nMissing from loaded .o2r.";
                return;
            }

            O2rTextureResource textureResource = O2rTextureResource.Read(data);
            TextureDefinition? matchingTexture = FindTexture(resource.ResourcePath);
            using Bitmap bitmap = DecodeArchiveTexture(textureResource, matchingTexture);
            (int width, int height) = GetScaledPreviewSize(textureResource.Width, textureResource.Height);
            string path = GetPreviewPath(resource.Name);
            string alphaPath = GetPreviewPath(resource.Name, "alpha");

            using Bitmap scaledBitmap = ScaleNearestNeighbor(bitmap, width, height);
            using Bitmap alphaBitmap = CreateAlphaPreviewBackground(width, height);
            alphaBitmap.Save(alphaPath, ImageFormat.Png);
            scaledBitmap.Save(path, ImageFormat.Png);

            PreviewAlphaImage.Source = new BitmapImage(new Uri(alphaPath));
            PreviewImage.Source = new BitmapImage(new Uri(path));
            DetailsText.Text =
                $"{resource.ResourcePath}\n" +
                $"{textureResource.Width}x{textureResource.Height}  {textureResource.Format}  {textureResource.RawPixels.Length} bytes\n" +
                $"{resource.StatusText}\n" +
                (_selectedResources.Contains(resource.ResourcePath) ? "Included in mod." : "Not included.");
        }
        catch (Exception ex)
        {
            ClearPreview();
            DetailsText.Text = $"{resource.ResourcePath}\n{resource.StatusText}\n{ex.Message}";
        }
    }

    private Bitmap DecodeArchiveTexture(
        O2rTextureResource textureResource,
        TextureDefinition? matchingTexture)
    {
        if (textureResource.Format is not (TextureFormat.CI4 or TextureFormat.CI8))
        {
            return TextureCodec.Decode(textureResource.RawPixels, textureResource.Width, textureResource.Height, textureResource.Format);
        }

        if (_romData is null || matchingTexture is null)
        {
            throw new InvalidDataException("CI texture preview needs a loaded matching ROM palette.");
        }

        if (!TextureDefinitionMatches(textureResource, matchingTexture))
        {
            throw new InvalidDataException("CI texture preview cannot use the loaded ROM palette because the resource metadata differs from the ROM catalog.");
        }

        byte[] tlut = TextureRomService.ReadTlutRaw(_romData.DecompressedRom, matchingTexture);
        return TextureCodec.Decode(
            textureResource.RawPixels,
            textureResource.Width,
            textureResource.Height,
            textureResource.Format,
            tlut,
            matchingTexture.EffectiveTlutColorCount);
    }

    private (int Width, int Height) GetScaledPreviewSize(int sourceWidth, int sourceHeight)
    {
        double availableWidth = Math.Max(1, PreviewViewport.ActualWidth - PreviewPadding);
        double availableHeight = Math.Max(1, PreviewViewport.ActualHeight - PreviewPadding);
        double scale = Math.Min(availableWidth / sourceWidth, availableHeight / sourceHeight);
        scale = Math.Max(1, scale);

        return (
            Math.Max(1, (int)Math.Floor(sourceWidth * scale)),
            Math.Max(1, (int)Math.Floor(sourceHeight * scale)));
    }

    private string GetPreviewPath(string name, string suffix = "texture")
    {
        string root = Path.Combine(Path.GetTempPath(), "HylianGrimoireO2rModPreview");
        Directory.CreateDirectory(root);
        string safeName = string.Concat(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        return Path.Combine(root, $"{++_previewCounter:D4}_{safeName}_{suffix}.png");
    }

    private static Bitmap ScaleNearestNeighbor(Bitmap source, int width, int height)
    {
        Bitmap scaled = new(width, height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(scaled);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.DrawImage(source, new Rectangle(0, 0, width, height), new Rectangle(0, 0, source.Width, source.Height), GraphicsUnit.Pixel);
        return scaled;
    }

    private void ClearPreview()
    {
        PreviewAlphaImage.Source = null;
        PreviewImage.Source = null;
    }

    private static Bitmap CreateAlphaPreviewBackground(int width, int height)
    {
        const int cellSize = 12;
        Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        for (int y = 0; y < height; y += cellSize)
        {
            for (int x = 0; x < width; x += cellSize)
            {
                bool dark = ((x / cellSize) + (y / cellSize)) % 2 == 0;
                using SolidBrush brush = new(dark ? System.Drawing.Color.FromArgb(76, 76, 76) : System.Drawing.Color.FromArgb(132, 132, 132));
                graphics.FillRectangle(brush, x, y, cellSize, cellSize);
            }
        }

        return bitmap;
    }
}
