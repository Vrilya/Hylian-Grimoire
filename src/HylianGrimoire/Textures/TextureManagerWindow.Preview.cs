using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using HylianGrimoire.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace HylianGrimoire.Textures;

public sealed partial class TextureManagerWindow
{
    private void RefreshSelectedTexture()
        => RefreshSelectedDetails();

    private TextureListItem? GetSelectedTextureItem()
        => TextureTree.SelectedNode?.Content as TextureListItem;

    private void OnPreviewViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_romData is not null && GetSelectedTextureItem() is TextureListItem item)
        {
            UpdatePreviewImage(item.Texture);
        }
    }

    private string GetPreviewPath(TextureDefinition texture, string suffix = "texture")
    {
        string root = Path.Combine(Path.GetTempPath(), "HylianGrimoireTexturePreview");
        Directory.CreateDirectory(root);
        string safeName = string.Concat(texture.Name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        return Path.Combine(root, $"{++_previewCounter:D4}_{safeName}_{suffix}.png");
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
        string path = GetPreviewPath(texture);
        string alphaPath = GetPreviewPath(texture, "alpha");

        using Bitmap scaledBitmap = ScaleNearestNeighbor(bitmap, width, height);
        using Bitmap alphaBitmap = CreateAlphaPreviewBackground(width, height);
        PngFileWriter.SaveDirect(alphaBitmap, alphaPath);
        PngFileWriter.SaveDirect(scaledBitmap, path);

        PreviewAlphaImage.Source = new BitmapImage(new Uri(alphaPath));
        PreviewImage.Source = new BitmapImage(new Uri(path));
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
