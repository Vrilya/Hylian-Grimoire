using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using HylianGrimoire.Interop;
using HylianGrimoire.Rom;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace HylianGrimoire.Textures;

public sealed partial class TextureManagerWindow : Window
{
    private const int PreviewPadding = 32;
    private readonly Action<string> _onChanged;
    private RomMessageData? _romData;
    private int _previewCounter;

    public TextureManagerWindow(RomMessageData? romData, Action<string> onChanged)
    {
        InitializeComponent();
        _onChanged = onChanged;

        SystemBackdrop = new MicaBackdrop();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(980, 680));
        WindowSizeLimits.SetFixedWidth(this, 980, 680);
        WindowIcon.Apply(this);
        AppWindow.TitleBar.ResetToDefault();
        WindowTheme.Register(this);

        SetRomData(romData);
    }

    public void SetRomData(RomMessageData? romData)
    {
        _romData = romData;
        TextureTree.RootNodes.Clear();
        PreviewAlphaImage.Source = null;
        PreviewImage.Source = null;

        if (_romData is null)
        {
            ProfileText.Text = "Load a ROM.";
            DetailsText.Text = "Select a texture.";
            StatusText.Text = "Load a ROM to export or replace textures.";
            TextureTree.IsEnabled = false;
            ExportButton.IsEnabled = false;
            ReplaceButton.IsEnabled = false;
            ExportFolderButton.IsEnabled = false;
            ReplaceFolderButton.IsEnabled = false;
            return;
        }

        ProfileText.Text = _romData.Profile.Name;
        if (!TextureCatalog.TryGetTextures(_romData.Profile, out IReadOnlyList<TextureDefinition>? textures))
        {
            DetailsText.Text = "No texture catalog is available for this ROM.";
            StatusText.Text = "This ROM has no texture catalog.";
            TextureTree.IsEnabled = false;
            ExportButton.IsEnabled = false;
            ReplaceButton.IsEnabled = false;
            ExportFolderButton.IsEnabled = false;
            ReplaceFolderButton.IsEnabled = false;
            return;
        }

        foreach (IGrouping<string, TextureDefinition> group in textures.GroupBy(texture => texture.Group).OrderBy(group => group.Key))
        {
            var groupNode = new TreeViewNode
            {
                Content = $"{group.Key} ({group.Count()})",
                IsExpanded = false,
            };

            foreach (TextureDefinition texture in group.OrderBy(texture => texture.Name))
            {
                groupNode.Children.Add(new TreeViewNode
                {
                    Content = new TextureListItem(texture),
                });
            }

            TextureTree.RootNodes.Add(groupNode);
        }

        TextureTree.IsEnabled = true;
        ExportFolderButton.IsEnabled = true;
        ReplaceFolderButton.IsEnabled = true;
        ExportButton.IsEnabled = false;
        ReplaceButton.IsEnabled = false;
        DetailsText.Text = $"{textures.Count} textures available.";
        StatusText.Text = "PNG replacement must match the selected texture's exact pixel size.";
    }

    private void OnTextureSelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs e)
    {
        if (_romData is null || GetSelectedTextureItem() is not TextureListItem item)
        {
            ClearPreview();
            ExportButton.IsEnabled = false;
            ReplaceButton.IsEnabled = false;
            return;
        }

        try
        {
            UpdatePreviewImage(item.Texture);
            int byteLength = TextureCodec.GetByteLength(item.Texture.Width, item.Texture.Height, item.Texture.Format);
            DetailsText.Text =
                $"{item.Texture.Group}\n" +
                $"0x{item.Texture.RomAddress:X}  {item.Texture.Width}x{item.Texture.Height}  {item.Texture.Format}  {byteLength} bytes";
            ExportButton.IsEnabled = true;
            ReplaceButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            ClearPreview();
            DetailsText.Text = ex.Message;
            ExportButton.IsEnabled = false;
            ReplaceButton.IsEnabled = false;
        }
    }

    private async void OnExportTexture(object sender, RoutedEventArgs e)
    {
        if (_romData is null || GetSelectedTextureItem() is not TextureListItem item)
        {
            return;
        }

        string? path = await PickSavePngAsync(item.Texture.Name);
        if (path is null)
        {
            return;
        }

        using var bitmap = TextureRomService.Decode(_romData.DecompressedRom, item.Texture);
        bitmap.Save(path, ImageFormat.Png);
        SetLocalStatus($"Exported {Path.GetFileName(path)} successfully.");
        _onChanged($"Exported {item.Texture.Name}.");
    }

    private async void OnReplaceTexture(object sender, RoutedEventArgs e)
    {
        if (_romData is null || GetSelectedTextureItem() is not TextureListItem item)
        {
            return;
        }

        string? path = await PickOpenPngAsync();
        if (path is null)
        {
            return;
        }

        try
        {
            TextureRomService.EncodeAndWrite(_romData.DecompressedRom, item.Texture, path);
            RefreshSelectedTexture();
            SetLocalStatus($"Replaced {Path.GetFileName(path)} successfully.");
            _onChanged($"Replaced {item.Texture.Name}.");
        }
        catch (Exception ex)
        {
            SetLocalStatus("Texture replacement failed.");
            await ShowErrorAsync("Failed to replace texture", ex.Message);
        }
    }

    private async void OnExportFolder(object sender, RoutedEventArgs e)
    {
        if (_romData is null || !TextureCatalog.TryGetTextures(_romData.Profile, out IReadOnlyList<TextureDefinition>? textures))
        {
            return;
        }

        string? folder = await PickFolderAsync();
        if (folder is null)
        {
            return;
        }

        try
        {
            int exported = 0;
            foreach (TextureDefinition texture in textures)
            {
                string path = GetTextureFilePath(folder, texture);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var bitmap = TextureRomService.Decode(_romData.DecompressedRom, texture);
                bitmap.Save(path, ImageFormat.Png);
                exported++;
            }

            SetLocalStatus($"Exported folder successfully. {exported} textures exported.");
            _onChanged($"Exported {exported} textures.");
        }
        catch (Exception ex)
        {
            SetLocalStatus("Folder export failed.");
            await ShowErrorAsync("Failed to export textures", ex.Message);
        }
    }

    private async void OnReplaceFolder(object sender, RoutedEventArgs e)
    {
        if (_romData is null || !TextureCatalog.TryGetTextures(_romData.Profile, out IReadOnlyList<TextureDefinition>? textures))
        {
            return;
        }

        string? folder = await PickFolderAsync();
        if (folder is null)
        {
            return;
        }

        try
        {
            int replaced = 0;
            foreach (TextureDefinition texture in textures)
            {
                string path = GetTextureFilePath(folder, texture);
                if (!File.Exists(path))
                {
                    continue;
                }

                TextureRomService.EncodeAndWrite(_romData.DecompressedRom, texture, path);
                replaced++;
            }

            RefreshSelectedTexture();
            if (replaced > 0)
            {
                SetLocalStatus($"Replaced folder successfully. {replaced} textures replaced.");
                _onChanged($"Replaced {replaced} textures.");
            }
            else
            {
                SetLocalStatus("No matching texture PNGs were found.");
                _onChanged("No matching texture PNGs found.");
            }
        }
        catch (Exception ex)
        {
            SetLocalStatus("Folder replacement failed.");
            await ShowErrorAsync("Failed to replace textures", ex.Message);
        }
    }

    private void RefreshSelectedTexture()
    {
        if (GetSelectedTextureItem() is not null)
        {
            UpdateSelectedTexturePreview();
        }
    }

    private TextureListItem? GetSelectedTextureItem()
        => TextureTree.SelectedNode?.Content as TextureListItem;

    private void UpdateSelectedTexturePreview()
        => OnTextureSelectionChanged(TextureTree, null!);

    private void OnPreviewViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_romData is not null && GetSelectedTextureItem() is TextureListItem item)
        {
            UpdatePreviewImage(item.Texture);
        }
    }

    private async Task<string?> PickOpenPngAsync()
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.FileTypeFilter.Add(".png");
        Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    private async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.FileTypeFilter.Add("*");
        StorageFolder? folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private async Task<string?> PickSavePngAsync(string textureName)
    {
        var picker = new FileSavePicker
        {
            SuggestedFileName = textureName,
        };
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        picker.FileTypeChoices.Add("PNG image", [".png"]);
        Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        return file?.Path;
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot,
        };

        await dialog.ShowAsync();
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
        alphaBitmap.Save(alphaPath, ImageFormat.Png);
        scaledBitmap.Save(path, ImageFormat.Png);

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

    private static string GetTextureFilePath(string root, TextureDefinition texture)
    {
        string[] groupParts = texture.Group
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizePathPart)
            .ToArray();

        return Path.Combine([root, .. groupParts, $"{SanitizePathPart(texture.Name)}.png"]);
    }

    private static string SanitizePathPart(string value)
    {
        HashSet<char> invalid = Path.GetInvalidFileNameChars().ToHashSet();
        return string.Concat(value.Select(ch => invalid.Contains(ch) ? '_' : ch));
    }

    private void SetLocalStatus(string message) => StatusText.Text = message;

    private sealed class TextureListItem(TextureDefinition texture)
    {
        public TextureDefinition Texture { get; } = texture;

        public string Name => Texture.Name;

        public string Summary => $"{Texture.Group}  {Texture.Width}x{Texture.Height}  {Texture.Format}";

        public override string ToString() => $"{Texture.Name}  {Texture.Width}x{Texture.Height}  {Texture.Format}";
    }
}
