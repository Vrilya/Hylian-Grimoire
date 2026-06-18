using HylianGrimoire.Rom;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.Textures;

public sealed partial class TextureManagerWindow
{
    public void SetRomData(RomMessageData? romData)
    {
        _romData = romData;
        _textureCount = 0;
        TextureTree.SelectedNode = null;
        TextureTree.RootNodes.Clear();
        ClearSelectedTextureDetails();

        if (_romData is null)
        {
            ProfileText.Text = "Load a ROM.";
            DetailsText.Text = "Select a texture.";
            StatusText.Text = "Load a ROM to export or replace textures.";
            TextureTree.IsEnabled = false;
            SetSelectedTextureActionsEnabled(false);
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
            SetSelectedTextureActionsEnabled(false);
            ExportFolderButton.IsEnabled = false;
            ReplaceFolderButton.IsEnabled = false;
            return;
        }

        _textureCount = textures.Count;
        foreach (TextureFolderItem folder in BuildTextureTree(textures))
        {
            TextureTree.RootNodes.Add(CreateFolderNode(folder, isExpanded: false));
        }

        TextureTree.IsEnabled = true;
        ExportFolderButton.IsEnabled = true;
        ReplaceFolderButton.IsEnabled = true;
        RefreshSelectedDetails();
        StatusText.Text = "PNG replacement must match the selected texture's exact pixel size.";
    }

    private void OnTextureTreeExpanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        if (args.Node.Content is not TextureFolderItem folder || folder.IsPopulated)
        {
            return;
        }

        args.Node.Children.Clear();
        args.Node.HasUnrealizedChildren = false;
        folder.IsPopulated = true;

        foreach (TextureFolderItem childFolder in folder.Folders.Values.OrderBy(folder => folder.Name))
        {
            args.Node.Children.Add(CreateFolderNode(childFolder, isExpanded: false));
        }

        foreach (TextureDefinition texture in folder.Textures.OrderBy(texture => texture.Name))
        {
            args.Node.Children.Add(new TreeViewNode
            {
                Content = new TextureListItem(texture),
            });
        }
    }

    private void OnTextureSelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs e)
        => RefreshSelectedDetails();

    private void RefreshSelectedDetails()
    {
        if (_romData is null || GetSelectedTextureItem() is not { } item)
        {
            ClearSelectedTextureDetails();
            return;
        }

        RefreshSelectedTextureDetails(item);
    }

    private void RefreshSelectedTextureDetails(TextureListItem item)
    {
        try
        {
            UpdatePreviewImage(item.Texture);
            int byteLength = TextureCodec.GetByteLength(item.Texture.Width, item.Texture.Height, item.Texture.Format);
            DetailsText.Text =
                $"{item.Texture.Group}\n" +
                $"0x{item.Texture.RomAddress:X}  {item.Texture.Width}x{item.Texture.Height}  {item.Texture.Format}  {byteLength} bytes";
            SetSelectedTextureActionsEnabled(true);
        }
        catch (Exception ex)
        {
            ClearPreview();
            DetailsText.Text = UiOperationExceptionHandler.GetDisplayMessage("Failed to preview texture", ex);
            SetSelectedTextureActionsEnabled(false);
        }
    }

    private void ClearSelectedTextureDetails()
    {
        ClearPreview();
        SetSelectedTextureActionsEnabled(false);
        DetailsText.Text = GetTextureSummaryText();
    }

    private string GetTextureSummaryText()
        => _textureCount > 0 ? $"{_textureCount} textures available." : "Select a texture.";

    private void SetSelectedTextureActionsEnabled(bool enabled)
    {
        ExportButton.IsEnabled = enabled;
        ReplaceButton.IsEnabled = enabled;
    }

    private static IReadOnlyList<TextureFolderItem> BuildTextureTree(IReadOnlyList<TextureDefinition> textures)
    {
        var root = new TextureFolderItem(string.Empty);
        foreach (TextureDefinition texture in textures)
        {
            TextureFolderItem folder = root;
            foreach (string part in texture.Group.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries))
            {
                folder = folder.GetOrAddFolder(part);
            }

            folder.Textures.Add(texture);
        }

        root.UpdateTotalCount();
        return [.. root.Folders.Values.OrderBy(folder => folder.Name)];
    }

    private static TreeViewNode CreateFolderNode(TextureFolderItem folder, bool isExpanded)
        => new()
        {
            Content = folder,
            IsExpanded = isExpanded,
            HasUnrealizedChildren = folder.Folders.Count > 0 || folder.Textures.Count > 0,
        };
}
